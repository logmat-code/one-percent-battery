namespace UGSSpace
{
    using System.Collections.Generic;
    using UGameStudioGDK;
    using UnityEngine;

    [System.Serializable]
    public class GPGLeaderboard
    {
        public enum LeaderboardTypeEnum { Score, TotalLevelsCleared }
        public enum NumberType { IntegerNumer, OneDecimal, TwoDecimals, ThreeDecimals, FourDecimals, Time }

        //---------------------------------------------------------------------
        public string name = "NAME";
        public string leaderboardID = "";
        public LeaderboardTypeEnum leaderboardType = LeaderboardTypeEnum.Score;

        public int scoreType;
        public NumberType numberType = NumberType.IntegerNumer;

        public enum WhatScore { TheScoreObtainedInAnyLevel, TheScoreObtainedInSpecificLevel, TheTotalBestScoreObtainedInAWorld, TheAccumulatedScore }
        public WhatScore whatScore = WhatScore.TheScoreObtainedInSpecificLevel;

        //If select 'The Score Obtained In Specific Level'
        public int levelNumber;

        //If select 'The Total Score Obtained In A World' 
        public int worldNumber;
        public UGS_GPGManager.HowToReport howToReportScore = UGS_GPGManager.HowToReport.WhenThePlayerWinsOrLosesTheLevel;
    }

    [System.Serializable]
    public class GPGAchievement
    {
        public string name = "NAME";
        public string achievementID = "";

        public enum HowToUnlockAchievement { WithScore, WithLevelCompleted, WithMultipleLevelsCompleted}
        // , WhenUnlockingCharacters, WhenUnlockingWeapons }
        public HowToUnlockAchievement howToUnlockAchievement;

        // --------------------------- WITH SCORE -------------------------------------------------
        public int scoreType = 0;
        public float requiredScore;

        public bool isIncrementalAchievement;//Only if the score is accumulative or it is best score obtained in a world

        public UGS_GPGManager.HowToReport howToReportAchievementWithScore = UGS_GPGManager.HowToReport.WhenThePlayerWinsOrLosesTheLevel;

        public enum WhatScore { TheScoreObtainedInAnyLevel, TheScoreObtainedInSpecificLevel, TheAccumulatedScore, TheTotalBestScoreObtainedInAWorld }
        public WhatScore whatScore = WhatScore.TheScoreObtainedInAnyLevel;

        //If Select The Total Score Obtained In A World
        public int worldNumber;
        public int levelNumber;

        public List<Vector2Int> levelNumbers = new List<Vector2Int>(1) { new Vector2Int(1, 1) };

        //-----------------------------------------------------------------------------------------

        /// <summary>
        ///  We recommend awarding 5 points for easy achievement,
        ///  20 points for medium difficulty achievement
        ///  and 50 points for difficult achievement.
        /// >> You can distribute a maximum of 1,000 points for all achievements combined in a game. <<
        /// </summary>
        public int pointsValueOfAchievement = 5; //The value of the points must be between 5 and 200 and must be a multiple of 5.

    }

    public class UGS_GPGManager : MonoBehaviour
    {
        public enum HowToReport { WhenThePlayerWinsOrLosesTheLevel, WhenThePlayerWinsTheLevel, WhenThePlayerLosesTheLevel }

        public List<GPGLeaderboard> leaderboards = new List<GPGLeaderboard>(1) { new GPGLeaderboard() };
        public List<GPGAchievement> achievements = new List<GPGAchievement>(1) { new GPGAchievement() };

        public enum HowToLogin { LoginAutomatically, LoginWithButton }
        public HowToLogin howToLogin = HowToLogin.LoginAutomatically;

    }
}