namespace OnePercentBattery.Core
{
    public enum RunModeType
    {
        Easy,
        Frenzy,
        Pro,
        Daily
    }

    public enum RunEndCause
    {
        HitObstacle,
        RanOutOfCharge
    }

    /// <summary>Snapshot handed to the game-over screen and to meta systems (missions/achievements/save).</summary>
    public readonly struct RunResult
    {
        public readonly RunModeType Mode;
        public readonly float DistanceMeters;
        public readonly int CoinsCollected;
        public readonly float SurvivalModeSeconds;
        public readonly float ElapsedSeconds;
        public readonly RunEndCause Cause;
        public readonly bool IsNewRecord;
        public readonly bool IsNewSurvivalRecord;

        public RunResult(RunModeType mode, float distanceMeters, int coinsCollected, float survivalModeSeconds, float elapsedSeconds, RunEndCause cause, bool isNewRecord, bool isNewSurvivalRecord)
        {
            Mode = mode;
            DistanceMeters = distanceMeters;
            CoinsCollected = coinsCollected;
            SurvivalModeSeconds = survivalModeSeconds;
            ElapsedSeconds = elapsedSeconds;
            Cause = cause;
            IsNewRecord = isNewRecord;
            IsNewSurvivalRecord = isNewSurvivalRecord;
        }
    }
}
