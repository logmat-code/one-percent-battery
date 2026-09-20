namespace UGSSpace
{
    using System.Collections;
    using System.Collections.Generic;
    using UGameStudioGDK;
    using UnityEngine;

    [System.Serializable]
    public class GameCenterLeaderboard
    {
        public enum LeaderboardTypeEnum { Score, TotalLevelsCleared }
        public enum NumberType { IntegerNumer, OneDecimal, TwoDecimals, ThreeDecimals, Time }

        //---------------------------------------------------------------------
        public string name = "NAME";
        public string leaderboardID = "";
        public LeaderboardTypeEnum leaderboardType = LeaderboardTypeEnum.Score;

        public int scoreType;
        public NumberType numberType = NumberType.IntegerNumer;

        public enum WhatScore { TheScoreObtainedInAnyLevel, TheScoreObtainedInSpecificLevel, TheTotalBestScoreObtainedInAWorld, TheAccumulatedScore }
        public WhatScore whatScore = WhatScore.TheScoreObtainedInSpecificLevel;

        public UGS_GameCenterManager.HowToReport howToReportScore = UGS_GameCenterManager.HowToReport.WhenThePlayerWinsOrLosesTheLevel;

        public int levelNumber;
        public int worldNumber;
    }

    [System.Serializable]
    public class GameCenterAchievement
    {
        public string name = "NAME";
        public string achievementID = "";

        public enum HowToUnlockAchievement { WithScore, WithLevelCompleted, WithMultipleLevelsCompleted }
        // ,WhenUnlockingCharacters, WhenUnlockingWeapons }
        public HowToUnlockAchievement howToUnlockAchievement;

        // --------------------------- WITH SCORE -------------------------------------------------
        public int scoreType = 0;
        public float requiredScore;
        public bool showProgressOnGameCenterUI;//Only if the score is accumulative

        public enum WhatScore { TheScoreObtainedInAnyLevel, TheScoreObtainedInSpecificLevel, TheAccumulatedScore, TheTotalBestScoreObtainedInAWorld }
        public WhatScore whatScore = WhatScore.TheScoreObtainedInAnyLevel;

        public UGS_GameCenterManager.HowToReport howToReportAchievementWithScore = UGS_GameCenterManager.HowToReport.WhenThePlayerWinsOrLosesTheLevel;

        public int worldNumber;
        public int levelNumber;

        // --------------------------- WITH MULTIPLE LEVELS COMPLETED -----------------------------

        public List<Vector2Int> levelNumbers = new List<Vector2Int>(1) { new Vector2Int(1, 1) };

        //-----------------------------------------------------------------------------------------

        /// <summary>
        ///  We recommend awarding 5 points for easy achievement,
        ///  20 points for medium difficulty achievement
        ///  and 50 points for difficult achievement.
        /// >> You can distribute a maximum of 1,000 points for all achievements combined in a game. <<
        /// </summary>
        public int pointsValueOfAchievement = 5; //The value of the points must be between 0 and 100.

    }

    public class UGS_GameCenterManager : MonoBehaviour
    {
        public enum HowToReport { WhenThePlayerWinsOrLosesTheLevel, WhenThePlayerWinsTheLevel, WhenThePlayerLosesTheLevel }

        public List<GameCenterLeaderboard> leaderboards = new List<GameCenterLeaderboard>(1) { new GameCenterLeaderboard() };
        public List<GameCenterAchievement> achievements = new List<GameCenterAchievement>(1) { new GameCenterAchievement() };
        public enum HowToLogin { LoginAutomatically, LoginWithButton }
        public HowToLogin howToLogin = HowToLogin.LoginAutomatically;
    }
}