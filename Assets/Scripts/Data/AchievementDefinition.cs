namespace OnePercentBattery.Data
{
    public enum AchievementMetric
    {
        RunsCompleted,
        SurviveCriticalSecondsInOneRun,
        SurviveSecondsInOneRun,
        DistanceInOneRunMeters,
        ReachZone,
        LifetimeDistanceMeters,
        CompletedProRunAtOrAboveMeters
    }

    public sealed class AchievementDefinition
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly AchievementMetric Metric;
        public readonly float Target;
        public readonly int RewardCoins;
        public readonly string TargetZoneName;

        public AchievementDefinition(string id, string displayName, AchievementMetric metric, float target, int rewardCoins, string targetZoneName = null)
        {
            Id = id;
            DisplayName = displayName;
            Metric = metric;
            Target = target;
            RewardCoins = rewardCoins;
            TargetZoneName = targetZoneName;
        }
    }

    /// <summary>The 8 achievements shown on the Profile screen ("0/8 unlocked" in the source game).</summary>
    public static class AchievementCatalog
    {
        public static readonly AchievementDefinition[] All =
        {
            new AchievementDefinition("first_steps", "First Steps", AchievementMetric.RunsCompleted, 1, 50),
            new AchievementDefinition("low_battery", "Low Battery", AchievementMetric.SurviveCriticalSecondsInOneRun, 3, 50),
            new AchievementDefinition("one_percent_club", "One Percent Club", AchievementMetric.CompletedProRunAtOrAboveMeters, 100, 100),
            new AchievementDefinition("survivor", "Survivor", AchievementMetric.SurviveSecondsInOneRun, 60, 100),
            new AchievementDefinition("unstoppable", "Unstoppable", AchievementMetric.DistanceInOneRunMeters, 1000, 200),
            new AchievementDefinition("astronaut", "Astronaut", AchievementMetric.ReachZone, 1, 200, targetZoneName: "ORBIT"),
            new AchievementDefinition("galactic", "Galactic", AchievementMetric.ReachZone, 1, 300, targetZoneName: "GALACTIC"),
            new AchievementDefinition("immortal_battery", "Immortal Battery", AchievementMetric.LifetimeDistanceMeters, 10000, 500),
        };
    }
}
