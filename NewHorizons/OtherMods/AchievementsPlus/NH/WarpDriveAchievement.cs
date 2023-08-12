namespace NewHorizons.OtherMods.AchievementsPlus.NH
{
    public static class WarpDriveAchievement
    {
        public static readonly string UNIQUE_ID = "NH_WARP_DRIVE";

        public static void Init()
        {
            AchievementHandler.Register(UNIQUE_ID, false, Main.Instance);
            Main.Instance.OnChangeStarSystem.AddListener(OnChangeStarSystem);
        }

        private static void OnChangeStarSystem(string system)
        {
            if (Main.Instance.IsWarpingFromShip) AchievementHandler.Earn(UNIQUE_ID);
        }
    }
}
