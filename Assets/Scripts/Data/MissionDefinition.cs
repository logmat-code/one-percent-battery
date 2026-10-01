using OnePercentBattery.Core;
using OnePercentBattery.Spawning;

namespace OnePercentBattery.Data
{
    public enum MissionPeriod
    {
        Daily,
        Monthly
    }

    /// <summary>Purely for rotation-selection diversity (see MissionManager.PickRotation) - never read for gameplay/UI logic.</summary>
    public enum MissionCategory
    {
        Distance,
        Coins,
        Jumps,
        Runs,
        Pits,
        Pickups,
        Battery,
        ModeSpecific,
        Meta
    }

    public enum MissionMetric
    {
        JumpCount,
        CollectCoins,
        SurviveAtCriticalSeconds,
        TravelDistanceMeters,
        ReachZone,
        /// <summary>Best single run's distance - progress is the MAX seen, not a running sum (see MissionManager.IsBestOfMetric).</summary>
        SingleRunDistanceMeters,
        /// <summary>Best single run's coin haul - MAX, not a running sum.</summary>
        SingleRunCoins,
        /// <summary>+1 every time a run ends, regardless of outcome.</summary>
        CompleteRuns,
        /// <summary>+1 per GameEvents.PitCleared.</summary>
        ClearPits,
        /// <summary>+1 per GameEvents.PickupCollected matching TargetPickupType.</summary>
        PickupCount,
        /// <summary>+1 per GameEvents.LowBatteryThresholdCrossed matching TargetBatteryTier (fires once per downward crossing, including on a big instant drop that passes several tiers in one tick).</summary>
        ReachBatteryTier,
        /// <summary>Cumulative distance, filtered to runs where RunResult.Mode == TargetRunMode.</summary>
        TravelDistanceInMode,
        /// <summary>Monthly-only: +1 every time ANY Daily mission is claimed (see MissionManager.TryClaim).</summary>
        CompleteDailyMissions
    }

    public sealed class MissionDefinition
    {
        public readonly string Id;
        public readonly string Description;
        public readonly MissionPeriod Period;
        public readonly MissionCategory Category;
        public readonly MissionMetric Metric;
        public readonly float Target;
        public readonly int RewardCoins;
        /// <summary>Only used when Metric == ReachZone: the zone name (see ZoneCatalog) that satisfies it.</summary>
        public readonly string TargetZoneName;
        /// <summary>Only used when Metric == PickupCount.</summary>
        public readonly PickupType? TargetPickupType;
        /// <summary>Only used when Metric == ReachBatteryTier.</summary>
        public readonly BatteryWarningTier? TargetBatteryTier;
        /// <summary>Only used when Metric == TravelDistanceInMode.</summary>
        public readonly RunModeType? TargetRunMode;

        public MissionDefinition(string id, string description, MissionPeriod period, MissionCategory category,
            MissionMetric metric, float target, int rewardCoins, string targetZoneName = null,
            PickupType? targetPickupType = null, BatteryWarningTier? targetBatteryTier = null, RunModeType? targetRunMode = null)
        {
            Id = id;
            Description = description;
            Period = period;
            Category = category;
            Metric = metric;
            Target = target;
            RewardCoins = rewardCoins;
            TargetZoneName = targetZoneName;
            TargetPickupType = targetPickupType;
            TargetBatteryTier = targetBatteryTier;
            TargetRunMode = targetRunMode;
        }
    }

    /// <summary>
    /// The full Daily/Monthly mission POOLS - MissionManager randomly selects 5 from each into the
    /// player's actual active rotation (see MissionManager.PickRotation); these arrays are never shown
    /// or tracked in full. Ids of the 6 missions that predate the rotation system (daily_jump_100,
    /// daily_survive_1pct_15s, daily_collect_500_coins, monthly_travel_50000, monthly_jump_2000,
    /// monthly_reach_galactic) are preserved unchanged so their description/reward stay stable - they
    /// just became pool members instead of permanently-active.
    /// </summary>
    public static class MissionCatalog
    {
        public static readonly MissionDefinition[] DailyPool =
        {
            new MissionDefinition("daily_run_1000_total", "Run 1,000m total", MissionPeriod.Daily, MissionCategory.Distance, MissionMetric.TravelDistanceMeters, 1000, 50),
            new MissionDefinition("daily_run_2500_total", "Run 2,500m total", MissionPeriod.Daily, MissionCategory.Distance, MissionMetric.TravelDistanceMeters, 2500, 90),
            new MissionDefinition("daily_run_750_single", "Reach 750m in one run", MissionPeriod.Daily, MissionCategory.Distance, MissionMetric.SingleRunDistanceMeters, 750, 70),
            new MissionDefinition("daily_run_1500_single", "Reach 1,500m in one run", MissionPeriod.Daily, MissionCategory.Distance, MissionMetric.SingleRunDistanceMeters, 1500, 110),
            new MissionDefinition("daily_collect_50_coins", "Collect 50 coins", MissionPeriod.Daily, MissionCategory.Coins, MissionMetric.CollectCoins, 50, 40),
            new MissionDefinition("daily_collect_100_coins", "Collect 100 coins", MissionPeriod.Daily, MissionCategory.Coins, MissionMetric.CollectCoins, 100, 70),
            new MissionDefinition("daily_collect_500_coins", "Collect 500 coins", MissionPeriod.Daily, MissionCategory.Coins, MissionMetric.CollectCoins, 500, 100),
            new MissionDefinition("daily_collect_50_coins_single", "Collect 50 coins in one run", MissionPeriod.Daily, MissionCategory.Coins, MissionMetric.SingleRunCoins, 50, 90),
            new MissionDefinition("daily_jump_30", "Jump 30 times", MissionPeriod.Daily, MissionCategory.Jumps, MissionMetric.JumpCount, 30, 40),
            new MissionDefinition("daily_jump_75", "Jump 75 times", MissionPeriod.Daily, MissionCategory.Jumps, MissionMetric.JumpCount, 75, 65),
            new MissionDefinition("daily_jump_100", "Jump 100 times", MissionPeriod.Daily, MissionCategory.Jumps, MissionMetric.JumpCount, 100, 80),
            new MissionDefinition("daily_complete_3_runs", "Complete 3 runs", MissionPeriod.Daily, MissionCategory.Runs, MissionMetric.CompleteRuns, 3, 50),
            new MissionDefinition("daily_complete_5_runs", "Complete 5 runs", MissionPeriod.Daily, MissionCategory.Runs, MissionMetric.CompleteRuns, 5, 80),
            new MissionDefinition("daily_clear_3_pits", "Clear 3 pits", MissionPeriod.Daily, MissionCategory.Pits, MissionMetric.ClearPits, 3, 50),
            new MissionDefinition("daily_clear_10_pits", "Clear 10 pits", MissionPeriod.Daily, MissionCategory.Pits, MissionMetric.ClearPits, 10, 100),
            new MissionDefinition("daily_pickup_2_shields", "Pick up 2 Shields", MissionPeriod.Daily, MissionCategory.Pickups, MissionMetric.PickupCount, 2, 60, targetPickupType: PickupType.Shield),
            new MissionDefinition("daily_pickup_2_magnets", "Pick up 2 Magnets", MissionPeriod.Daily, MissionCategory.Pickups, MissionMetric.PickupCount, 2, 60, targetPickupType: PickupType.Magnet),
            new MissionDefinition("daily_pickup_3_chargers", "Pick up 3 Chargers", MissionPeriod.Daily, MissionCategory.Pickups, MissionMetric.PickupCount, 3, 60, targetPickupType: PickupType.Energy),
            new MissionDefinition("daily_reach_25pct", "Reach 25% battery", MissionPeriod.Daily, MissionCategory.Battery, MissionMetric.ReachBatteryTier, 1, 50, targetBatteryTier: BatteryWarningTier.Warning25),
            new MissionDefinition("daily_reach_10pct", "Reach 10% battery", MissionPeriod.Daily, MissionCategory.Battery, MissionMetric.ReachBatteryTier, 1, 70, targetBatteryTier: BatteryWarningTier.Critical10),
            new MissionDefinition("daily_reach_5pct", "Reach 5% battery", MissionPeriod.Daily, MissionCategory.Battery, MissionMetric.ReachBatteryTier, 1, 90, targetBatteryTier: BatteryWarningTier.Severe5),
            new MissionDefinition("daily_reach_1pct", "Reach 1% battery", MissionPeriod.Daily, MissionCategory.Battery, MissionMetric.ReachBatteryTier, 1, 110, targetBatteryTier: BatteryWarningTier.Final1),
            new MissionDefinition("daily_survive_1pct_15s", "Survive at 1% for 15s", MissionPeriod.Daily, MissionCategory.Battery, MissionMetric.SurviveAtCriticalSeconds, 15, 120),
            new MissionDefinition("daily_frenzy_1000", "Run 1,000m in Frenzy", MissionPeriod.Daily, MissionCategory.ModeSpecific, MissionMetric.TravelDistanceInMode, 1000, 90, targetRunMode: RunModeType.Frenzy),
            new MissionDefinition("daily_pro_500", "Run 500m in Pro", MissionPeriod.Daily, MissionCategory.ModeSpecific, MissionMetric.TravelDistanceInMode, 500, 100, targetRunMode: RunModeType.Pro),
        };

        public static readonly MissionDefinition[] MonthlyPool =
        {
            new MissionDefinition("monthly_run_25000", "Run 25,000m total", MissionPeriod.Monthly, MissionCategory.Distance, MissionMetric.TravelDistanceMeters, 25000, 280),
            new MissionDefinition("monthly_travel_50000", "Travel 50,000 m this month", MissionPeriod.Monthly, MissionCategory.Distance, MissionMetric.TravelDistanceMeters, 50000, 300),
            new MissionDefinition("monthly_run_100000", "Run 100,000m total", MissionPeriod.Monthly, MissionCategory.Distance, MissionMetric.TravelDistanceMeters, 100000, 700),
            new MissionDefinition("monthly_coins_2500", "Collect 2,500 coins", MissionPeriod.Monthly, MissionCategory.Coins, MissionMetric.CollectCoins, 2500, 300),
            new MissionDefinition("monthly_coins_5000", "Collect 5,000 coins", MissionPeriod.Monthly, MissionCategory.Coins, MissionMetric.CollectCoins, 5000, 550),
            new MissionDefinition("monthly_jump_1000", "Jump 1,000 times", MissionPeriod.Monthly, MissionCategory.Jumps, MissionMetric.JumpCount, 1000, 200),
            new MissionDefinition("monthly_jump_2000", "Jump 2,000 times", MissionPeriod.Monthly, MissionCategory.Jumps, MissionMetric.JumpCount, 2000, 300),
            new MissionDefinition("monthly_complete_50_runs", "Complete 50 runs", MissionPeriod.Monthly, MissionCategory.Runs, MissionMetric.CompleteRuns, 50, 300),
            new MissionDefinition("monthly_complete_100_runs", "Complete 100 runs", MissionPeriod.Monthly, MissionCategory.Runs, MissionMetric.CompleteRuns, 100, 550),
            new MissionDefinition("monthly_clear_100_pits", "Clear 100 pits", MissionPeriod.Monthly, MissionCategory.Pits, MissionMetric.ClearPits, 100, 320),
            new MissionDefinition("monthly_clear_250_pits", "Clear 250 pits", MissionPeriod.Monthly, MissionCategory.Pits, MissionMetric.ClearPits, 250, 550),
            new MissionDefinition("monthly_pickup_50_shields", "Collect 50 Shields", MissionPeriod.Monthly, MissionCategory.Pickups, MissionMetric.PickupCount, 50, 350, targetPickupType: PickupType.Shield),
            new MissionDefinition("monthly_pickup_50_magnets", "Collect 50 Magnets", MissionPeriod.Monthly, MissionCategory.Pickups, MissionMetric.PickupCount, 50, 350, targetPickupType: PickupType.Magnet),
            new MissionDefinition("monthly_pickup_75_chargers", "Collect 75 Chargers", MissionPeriod.Monthly, MissionCategory.Pickups, MissionMetric.PickupCount, 75, 350, targetPickupType: PickupType.Energy),
            new MissionDefinition("monthly_reach_1pct_25", "Reach 1% battery 25 times", MissionPeriod.Monthly, MissionCategory.Battery, MissionMetric.ReachBatteryTier, 25, 450, targetBatteryTier: BatteryWarningTier.Final1),
            new MissionDefinition("monthly_reach_galactic", "Reach the Galactic zone", MissionPeriod.Monthly, MissionCategory.Distance, MissionMetric.ReachZone, 1, 400, targetZoneName: "GALACTIC"),
            new MissionDefinition("monthly_frenzy_15000", "Travel 15,000m in Frenzy", MissionPeriod.Monthly, MissionCategory.ModeSpecific, MissionMetric.TravelDistanceInMode, 15000, 400, targetRunMode: RunModeType.Frenzy),
            new MissionDefinition("monthly_pro_10000", "Travel 10,000m in Pro", MissionPeriod.Monthly, MissionCategory.ModeSpecific, MissionMetric.TravelDistanceInMode, 10000, 500, targetRunMode: RunModeType.Pro),
            new MissionDefinition("monthly_complete_25_daily", "Complete 25 Daily missions", MissionPeriod.Monthly, MissionCategory.Meta, MissionMetric.CompleteDailyMissions, 25, 400),
        };

        /// <summary>Coins-only reward is the norm; completing every monthly challenge additionally unlocks this seasonal skin id (see ShopCatalog/Exclusive-style cosmetics).</summary>
        public const string SeasonalRewardSkinId = "exclusive_sunset";
    }
}
