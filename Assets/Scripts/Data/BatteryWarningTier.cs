namespace OnePercentBattery.Data
{
    /// <summary>One-shot low-battery thresholds, crossed in this order as charge drains.</summary>
    public enum BatteryWarningTier
    {
        Warning25,
        Critical10,
        Severe5,
        Final1
    }
}
