namespace OnePercentBattery.Data
{
    /// <summary>Obstacle silhouette height tier - lets CourseSpawner keep consecutive placements fair (e.g. never Tall right after Tall).</summary>
    public enum ObstacleHeightClass
    {
        Low,
        Medium,
        Tall
    }
}
