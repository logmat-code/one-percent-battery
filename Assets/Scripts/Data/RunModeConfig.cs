using OnePercentBattery.Core;

namespace OnePercentBattery.Data
{
    /// <summary>
    /// Rules for one run mode. Plain data (not a ScriptableObject) so the four modes are guaranteed
    /// to exist with no Editor asset-creation step required.
    /// </summary>
    public sealed class RunModeConfig
    {
        public readonly RunModeType Mode;
        public readonly string DisplayName;
        public readonly string Tagline;
        public readonly string RulesLine;
        public readonly float StartingChargePercent;
        public readonly float HitDamagePercent;
        public readonly bool OneHitKnockout;
        public readonly bool PowerUpsEnabled;
        public readonly bool AllowSecondChance;
        public readonly float BaseSpeed;
        public readonly float SpeedPerMeter;
        public readonly float MaxSpeed;
        public readonly float PassiveDrainPerSecond;

        private RunModeConfig(RunModeType mode, string displayName, string tagline, string rulesLine,
            float startingChargePercent, float hitDamagePercent, bool oneHitKnockout, bool powerUpsEnabled,
            bool allowSecondChance, float baseSpeed, float speedPerMeter, float maxSpeed, float passiveDrainPerSecond)
        {
            Mode = mode;
            DisplayName = displayName;
            Tagline = tagline;
            RulesLine = rulesLine;
            StartingChargePercent = startingChargePercent;
            HitDamagePercent = hitDamagePercent;
            OneHitKnockout = oneHitKnockout;
            PowerUpsEnabled = powerUpsEnabled;
            AllowSecondChance = allowSecondChance;
            BaseSpeed = baseSpeed;
            SpeedPerMeter = speedPerMeter;
            MaxSpeed = maxSpeed;
            PassiveDrainPerSecond = passiveDrainPerSecond;
        }

        // Calibrated for a gentler opening pace (real-device feedback: opening was too fast to orient):
        // BaseSpeed reduced across all modes with compensated SpeedPerMeter so early game is forgiving,
        // mid-game accelerates smoothly, and late-game speed ceiling is preserved without raising difficulty.
        public static readonly RunModeConfig Easy = new RunModeConfig(
            RunModeType.Easy, "EASY", "Find your rhythm. Recharge, collect and keep moving.",
            "-20% per hit · power-ups enabled",
            startingChargePercent: 100f, hitDamagePercent: 20f, oneHitKnockout: false, powerUpsEnabled: true,
            allowSecondChance: true, baseSpeed: 5.8f, speedPerMeter: 0.0038f, maxSpeed: 14f, passiveDrainPerSecond: 1f);

        public static readonly RunModeConfig Frenzy = new RunModeConfig(
            RunModeType.Frenzy, "FRENZY", "Start low. Run fast. Every recharge is a lifeline.",
            "-20% per hit · power-ups enabled",
            startingChargePercent: 20f, hitDamagePercent: 20f, oneHitKnockout: false, powerUpsEnabled: true,
            allowSecondChance: true, baseSpeed: 7.8f, speedPerMeter: 0.0050f, maxSpeed: 17f, passiveDrainPerSecond: 1.3f);

        public static readonly RunModeConfig Pro = new RunModeConfig(
            RunModeType.Pro, "PRO", "One charge. One chance. Make every jump count.",
            "One hit ends the run · no power-ups",
            startingChargePercent: 1f, hitDamagePercent: 100f, oneHitKnockout: true, powerUpsEnabled: false,
            allowSecondChance: false, baseSpeed: 6.6f, speedPerMeter: 0.0042f, maxSpeed: 15f, passiveDrainPerSecond: 0f);

        /// <summary>Same pacing as Easy; the Daily Run's identity comes from its seeded course, not different rules.</summary>
        public static readonly RunModeConfig Daily = new RunModeConfig(
            RunModeType.Daily, "DAILY", "A fresh course every day.",
            "-20% per hit · power-ups enabled",
            startingChargePercent: 100f, hitDamagePercent: 20f, oneHitKnockout: false, powerUpsEnabled: true,
            allowSecondChance: true, baseSpeed: 5.8f, speedPerMeter: 0.0038f, maxSpeed: 14f, passiveDrainPerSecond: 1f);

        public static RunModeConfig For(RunModeType mode)
        {
            switch (mode)
            {
                case RunModeType.Easy: return Easy;
                case RunModeType.Frenzy: return Frenzy;
                case RunModeType.Pro: return Pro;
                case RunModeType.Daily: return Daily;
                default: return Easy;
            }
        }
    }
}
