using System;
using System.Collections.Generic;
using System.Globalization;
using OnePercentBattery.Core;
using OnePercentBattery.Data;
using OnePercentBattery.Save;
using OnePercentBattery.Spawning;
using OnePercentBattery.Utils;
using UnityEngine;

namespace OnePercentBattery.Meta
{
    /// <summary>
    /// Rotating Daily (24h) and Monthly (30d) missions: exactly 5 active at a time per period, chosen
    /// randomly from MissionCatalog.DailyPool/MonthlyPool and persisted (selection + progress + claimed
    /// state) until the next rotation. Rotation is elapsed-UTC-time based (see rotationTimestampUtc),
    /// not a calendar-day/month boundary, so it survives being checked at any time of day and any
    /// timezone/DST change. "Survive at 1% for Ns" is tracked as cumulative seconds spent at/below the
    /// critical threshold across runs in the period, not one unbroken streak - the source game doesn't
    /// specify which, and the cumulative reading is the one a mobile session (short runs, frequent
    /// deaths) can realistically hit.
    /// </summary>
    public sealed class MissionManager : Singleton<MissionManager>
    {
        public event Action ProgressChanged;

        private int _lastRunCoinTotal;
        private float _lastReportedDistance;
        private float _lastReportedSurvivalSeconds;
        private bool _runCompletionCountedThisRun;

        protected override void Awake()
        {
            base.Awake();
            EnsureCurrentPeriods();
        }

        private void OnEnable()
        {
            GameEvents.RunStarted += HandleRunStarted;
            GameEvents.RunEnded += HandleRunEnded;
            GameEvents.Jumped += HandleJumped;
            GameEvents.CoinCollected += HandleCoinCollected;
            GameEvents.ZoneChanged += HandleZoneChanged;
            GameEvents.PickupCollected += HandlePickupCollected;
            GameEvents.PitCleared += HandlePitCleared;
            GameEvents.LowBatteryThresholdCrossed += HandleLowBatteryThresholdCrossed;
        }

        private void OnDisable()
        {
            GameEvents.RunStarted -= HandleRunStarted;
            GameEvents.RunEnded -= HandleRunEnded;
            GameEvents.Jumped -= HandleJumped;
            GameEvents.CoinCollected -= HandleCoinCollected;
            GameEvents.ZoneChanged -= HandleZoneChanged;
            GameEvents.PickupCollected -= HandlePickupCollected;
            GameEvents.PitCleared -= HandlePitCleared;
            GameEvents.LowBatteryThresholdCrossed -= HandleLowBatteryThresholdCrossed;
        }

        /// <summary>Called on Awake, on every RunStarted, and by MissionsController.OnEnable (panel open) - cheap no-op unless 24h/30d have actually elapsed, per the class doc above.</summary>
        public void EnsureCurrentPeriods()
        {
            var data = SaveSystem.Data;
            bool changed = false;

            if (HasElapsed(data.dailyMissions.rotationTimestampUtc, TimeSpan.FromHours(24)))
            {
                GenerateRotation(data.dailyMissions, MissionCatalog.DailyPool);
                changed = true;
            }
            if (HasElapsed(data.monthlyMissions.rotationTimestampUtc, TimeSpan.FromDays(30)))
            {
                GenerateRotation(data.monthlyMissions, MissionCatalog.MonthlyPool);
                changed = true;
            }

            if (changed) SaveSystem.Save();
        }

        /// <summary>Empty/unparseable (fresh save, or one from before rotation existed) counts as elapsed so a rotation generates immediately. A clock that moved backwards makes (UtcNow - stored) negative, which already fails ">= threshold" on its own - no special-case needed.</summary>
        private static bool HasElapsed(string timestampUtcIso, TimeSpan threshold)
        {
            if (string.IsNullOrEmpty(timestampUtcIso)) return true;
            if (!DateTime.TryParse(timestampUtcIso, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime storedUtc))
            {
                return true;
            }
            return (DateTime.UtcNow - storedUtc) >= threshold;
        }

        private static void GenerateRotation(MissionPeriodState state, MissionDefinition[] pool)
        {
            state.rotationTimestampUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            state.activeMissionIds = PickRotation(pool);
            state.entries = new List<MissionProgressEntry>(state.activeMissionIds.Count);
            foreach (var id in state.activeMissionIds)
            {
                state.entries.Add(new MissionProgressEntry { id = id, progress = 0f, claimed = false });
            }
        }

        /// <summary>5 unique random picks, softly capped at 2-per-category so a rotation doesn't land on (say) 5 different "run X meters" missions - falls back to ignoring the cap only if the pool's category spread can't fill 5 slots under it.</summary>
        private static List<string> PickRotation(MissionDefinition[] pool)
        {
            var rng = new System.Random();
            var shuffled = new List<MissionDefinition>(pool);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
            }

            const int softCapPerCategory = 2;
            var picked = new List<MissionDefinition>(5);
            var categoryCounts = new Dictionary<MissionCategory, int>();
            foreach (var def in shuffled)
            {
                if (picked.Count >= 5) break;
                categoryCounts.TryGetValue(def.Category, out int count);
                if (count >= softCapPerCategory) continue;
                picked.Add(def);
                categoryCounts[def.Category] = count + 1;
            }
            if (picked.Count < 5)
            {
                foreach (var def in shuffled)
                {
                    if (picked.Count >= 5) break;
                    if (picked.Contains(def)) continue;
                    picked.Add(def);
                }
            }

            var ids = new List<string>(picked.Count);
            foreach (var d in picked) ids.Add(d.Id);
            return ids;
        }

        public List<MissionDefinition> GetActiveDailyMissions()
        {
            EnsureCurrentPeriods();
            return ResolveActive(SaveSystem.Data.dailyMissions, MissionCatalog.DailyPool);
        }

        public List<MissionDefinition> GetActiveMonthlyMissions()
        {
            EnsureCurrentPeriods();
            return ResolveActive(SaveSystem.Data.monthlyMissions, MissionCatalog.MonthlyPool);
        }

        private static List<MissionDefinition> ResolveActive(MissionPeriodState state, MissionDefinition[] pool)
        {
            var result = new List<MissionDefinition>(state.activeMissionIds.Count);
            foreach (var id in state.activeMissionIds)
            {
                var def = FindDefinition(pool, id);
                if (def != null) result.Add(def);
            }
            return result;
        }

        private static MissionDefinition FindDefinition(MissionDefinition[] pool, string id)
        {
            foreach (var d in pool) if (d.Id == id) return d;
            return null;
        }

        public MissionProgressEntry GetProgress(string missionId)
        {
            EnsureCurrentPeriods();
            var data = SaveSystem.Data;
            return Find(data.dailyMissions.entries, missionId) ?? Find(data.monthlyMissions.entries, missionId);
        }

        private static MissionProgressEntry Find(List<MissionProgressEntry> entries, string id)
        {
            foreach (var e in entries) if (e.id == id) return e;
            return null;
        }

        public bool TryClaim(MissionDefinition definition)
        {
            var entry = GetProgress(definition.Id);
            if (entry == null || entry.claimed || entry.progress < definition.Target) return false;

            entry.claimed = true;
            CurrencyManager.Instance.AddCoins(definition.RewardCoins);
            SaveSystem.Save();
            ProgressChanged?.Invoke();

            if (definition.Period == MissionPeriod.Daily)
            {
                AddProgress(MissionMetric.CompleteDailyMissions, 1f);
            }
            else
            {
                CheckSeasonalCompletion();
            }
            return true;
        }

        private void CheckSeasonalCompletion()
        {
            var data = SaveSystem.Data;
            foreach (var e in data.monthlyMissions.entries)
            {
                if (!e.claimed) return;
            }
            ShopManager.Instance.Unlock(ShopCatalog.FindSkin(MissionCatalog.SeasonalRewardSkinId));
        }

        private void HandleRunStarted(RunModeType mode)
        {
            EnsureCurrentPeriods();
            _lastRunCoinTotal = 0;
            _lastReportedDistance = 0f;
            _lastReportedSurvivalSeconds = 0f;
            _runCompletionCountedThisRun = false;
        }

        private void HandleRunEnded(RunResult result)
        {
            float deltaDistance = Mathf.Max(0f, result.DistanceMeters - _lastReportedDistance);
            _lastReportedDistance = Mathf.Max(_lastReportedDistance, result.DistanceMeters);

            float deltaSurvival = Mathf.Max(0f, result.SurvivalModeSeconds - _lastReportedSurvivalSeconds);
            _lastReportedSurvivalSeconds = Mathf.Max(_lastReportedSurvivalSeconds, result.SurvivalModeSeconds);

            if (deltaDistance > 0f)
            {
                AddProgress(MissionMetric.TravelDistanceMeters, deltaDistance);
                AddProgress(MissionMetric.TravelDistanceInMode, deltaDistance, runMode: result.Mode);
            }
            if (deltaSurvival > 0f)
            {
                AddProgress(MissionMetric.SurviveAtCriticalSeconds, deltaSurvival);
            }

            AddProgress(MissionMetric.SingleRunDistanceMeters, result.DistanceMeters);
            AddProgress(MissionMetric.SingleRunCoins, result.CoinsCollected);

            if (!_runCompletionCountedThisRun)
            {
                _runCompletionCountedThisRun = true;
                AddProgress(MissionMetric.CompleteRuns, 1f);
            }
        }

        private void HandleJumped()
        {
            AddProgress(MissionMetric.JumpCount, 1f);
        }

        private void HandleCoinCollected(int runTotal)
        {
            int delta = runTotal - _lastRunCoinTotal;
            _lastRunCoinTotal = runTotal;
            if (delta > 0) AddProgress(MissionMetric.CollectCoins, delta);
        }

        private void HandleZoneChanged(ZoneDefinition zone)
        {
            AddProgress(MissionMetric.ReachZone, 1f, zoneName: zone.Name);
        }

        private void HandlePickupCollected(PickupType type, Vector3 worldPosition)
        {
            AddProgress(MissionMetric.PickupCount, 1f, pickupType: type);
        }

        private void HandlePitCleared()
        {
            AddProgress(MissionMetric.ClearPits, 1f);
        }

        private void HandleLowBatteryThresholdCrossed(BatteryWarningTier tier)
        {
            AddProgress(MissionMetric.ReachBatteryTier, 1f, batteryTier: tier);
        }

        private void AddProgress(MissionMetric metric, float amount, string zoneName = null,
            PickupType? pickupType = null, BatteryWarningTier? batteryTier = null, RunModeType? runMode = null)
        {
            EnsureCurrentPeriods();
            bool changed = false;
            changed |= ApplyToPeriod(SaveSystem.Data.dailyMissions, MissionCatalog.DailyPool, metric, amount, zoneName, pickupType, batteryTier, runMode);
            changed |= ApplyToPeriod(SaveSystem.Data.monthlyMissions, MissionCatalog.MonthlyPool, metric, amount, zoneName, pickupType, batteryTier, runMode);

            if (changed)
            {
                SaveSystem.Save();
                ProgressChanged?.Invoke();
            }
        }

        private static bool ApplyToPeriod(MissionPeriodState state, MissionDefinition[] pool, MissionMetric metric, float amount,
            string zoneName, PickupType? pickupType, BatteryWarningTier? batteryTier, RunModeType? runMode)
        {
            bool changed = false;
            foreach (var id in state.activeMissionIds)
            {
                var def = FindDefinition(pool, id);
                if (def == null || def.Metric != metric) continue;
                if (metric == MissionMetric.ReachZone && def.TargetZoneName != zoneName) continue;
                if (metric == MissionMetric.PickupCount && def.TargetPickupType != pickupType) continue;
                if (metric == MissionMetric.ReachBatteryTier && def.TargetBatteryTier != batteryTier) continue;
                if (metric == MissionMetric.TravelDistanceInMode && def.TargetRunMode != runMode) continue;

                var entry = Find(state.entries, def.Id);
                if (entry == null || entry.claimed || entry.progress >= def.Target) continue;

                entry.progress = IsMaxMetric(metric)
                    ? Mathf.Min(def.Target, Mathf.Max(entry.progress, amount))
                    : Mathf.Min(def.Target, entry.progress + amount);
                changed = true;
            }
            return changed;
        }

        /// <summary>SingleRunDistanceMeters/SingleRunCoins track the best single run (a ceiling), everything else accumulates across the whole period.</summary>
        private static bool IsMaxMetric(MissionMetric metric)
        {
            return metric == MissionMetric.SingleRunDistanceMeters || metric == MissionMetric.SingleRunCoins;
        }
    }
}
