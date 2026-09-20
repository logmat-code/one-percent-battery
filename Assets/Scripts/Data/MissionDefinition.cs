namespace OnePercentBattery.Data
{
    public enum MissionPeriod
    {
        Daily,
        Monthly
    }

    public enum MissionMetric
    {
        JumpCount,
        CollectCoins,
        SurviveAtCriticalSeconds,
        TravelDistanceMeters,
        ReachZone
    }

    public sealed class MissionDefinition
    {
        public readonly string Id;
        public readonly string Description;
        public readonly MissionPeriod Period;
        public readonly MissionMetric Metric;
        public readonly float Target;
        public readonly int RewardCoins;
        /// <summary>Only used when Metric == ReachZone: the zone name (see ZoneCatalog) that satisfies it.</summary>
        public readonly string TargetZoneName;

        public MissionDefinition(string id, string description, MissionPeriod period, MissionMetric metric, float target, int rewardCoins, string targetZoneName = null)
        {
            Id = id;
            Description = description;
            Period = period;
            Metric = metric;
            Target = target;
            RewardCoins = rewardCoins;
            TargetZoneName = targetZoneName;
        }
    }

    public static class MissionCatalog
    {
        public static readonly MissionDefinition[] Daily =
        {
            new MissionDefinition("daily_jump_100", "Jump 100 times", MissionPeriod.Daily, MissionMetric.JumpCount, 100, 80),
            new MissionDefinition("daily_survive_1pct_15s", "Survive at 1% for 15s", MissionPeriod.Daily, MissionMetric.SurviveAtCriticalSeconds, 15, 120),
            new MissionDefinition("daily_collect_500_coins", "Collect 500 coins", MissionPeriod.Daily, MissionMetric.CollectCoins, 500, 100),
        };

        public static readonly MissionDefinition[] Monthly =
        {
            new MissionDefinition("monthly_travel_50000", "Travel 50,000 m this month", MissionPeriod.Monthly, MissionMetric.TravelDistanceMeters, 50000, 300),
            new MissionDefinition("monthly_jump_2000", "Jump 2,000 times", MissionPeriod.Monthly, MissionMetric.JumpCount, 2000, 300),
            new MissionDefinition("monthly_reach_galactic", "Reach the Galactic zone", MissionPeriod.Monthly, MissionMetric.ReachZone, 1, 400, targetZoneName: "GALACTIC"),
        };

        /// <summary>Coins-only reward is the norm; completing every monthly challenge additionally unlocks this seasonal skin id (see ShopCatalog/Exclusive-style cosmetics).</summary>
        public const string SeasonalRewardSkinId = "exclusive_sunset";
    }
}
