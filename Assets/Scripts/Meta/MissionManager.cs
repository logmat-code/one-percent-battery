using System;
using System.Collections.Generic;
using OnePercentBattery.Core;
using OnePercentBattery.Data;
using OnePercentBattery.Save;
using OnePercentBattery.Utils;
using UnityEngine;

namespace OnePercentBattery.Meta
{
    /// <summary>
    /// Tracks the 3 daily + 3 monthly missions shown in the source game's Missions screen. Progress
    /// persists locally and rolls over automatically when the calendar day/month changes.
    /// "Survive at 1% for Ns" is tracked as cumulative seconds spent at/below the critical threshold
    /// across runs that day, not one unbroken streak - the source game doesn't specify which, and the
    /// cumulative reading is the one a mobile session (short runs, frequent deaths) can realistically hit.
    /// </summary>
    public sealed class MissionManager : Singleton<MissionManager>
    {
        public event Action ProgressChanged;

        private int _lastRunCoinTotal;

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
        }

        private void OnDisable()
        {
            GameEvents.RunStarted -= HandleRunStarted;
            GameEvents.RunEnded -= HandleRunEnded;
            GameEvents.Jumped -= HandleJumped;
            GameEvents.CoinCollected -= HandleCoinCollected;
            GameEvents.ZoneChanged -= HandleZoneChanged;
        }

        public void EnsureCurrentPeriods()
        {
            var data = SaveSystem.Data;
            string todayKey = DateTime.Now.ToString("yyyy-MM-dd");
            string monthKey = DateTime.Now.ToString("yyyy-MM");

            if (data.dailyMissions.periodKey != todayKey)
            {
                data.dailyMissions.periodKey = todayKey;
                data.dailyMissions.entries = BuildEntries(MissionCatalog.Daily);
                SaveSystem.Save();
            }
            if (data.monthlyMissions.periodKey != monthKey)
            {
                data.monthlyMissions.periodKey = monthKey;
                data.monthlyMissions.entries = BuildEntries(MissionCatalog.Monthly);
                SaveSystem.Save();
            }
        }

        private static List<MissionProgressEntry> BuildEntries(MissionDefinition[] definitions)
        {
            var list = new List<MissionProgressEntry>(definitions.Length);
            foreach (var def in definitions)
            {
                list.Add(new MissionProgressEntry { id = def.Id, progress = 0f, claimed = false });
            }
            return list;
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

            if (definition.Period == MissionPeriod.Monthly)
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
        }

        private void HandleRunEnded(RunResult result)
        {
            AddProgress(MissionMetric.TravelDistanceMeters, result.DistanceMeters);
            AddProgress(MissionMetric.SurviveAtCriticalSeconds, result.SurvivalModeSeconds);
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
            AddProgress(MissionMetric.ReachZone, 1f, zone.Name);
        }

        private void AddProgress(MissionMetric metric, float amount, string zoneName = null)
        {
            EnsureCurrentPeriods();
            bool changed = false;
            changed |= ApplyToPeriod(SaveSystem.Data.dailyMissions, MissionCatalog.Daily, metric, amount, zoneName);
            changed |= ApplyToPeriod(SaveSystem.Data.monthlyMissions, MissionCatalog.Monthly, metric, amount, zoneName);

            if (changed)
            {
                SaveSystem.Save();
                ProgressChanged?.Invoke();
            }
        }

        private static bool ApplyToPeriod(MissionPeriodState state, MissionDefinition[] definitions, MissionMetric metric, float amount, string zoneName)
        {
            bool changed = false;
            foreach (var def in definitions)
            {
                if (def.Metric != metric) continue;
                if (metric == MissionMetric.ReachZone && def.TargetZoneName != zoneName) continue;

                var entry = Find(state.entries, def.Id);
                if (entry == null || entry.claimed || entry.progress >= def.Target) continue;

                entry.progress = Mathf.Min(def.Target, entry.progress + amount);
                changed = true;
            }
            return changed;
        }
    }
}
