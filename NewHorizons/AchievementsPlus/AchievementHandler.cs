using NewHorizons.Utility;
using OWML.ModHelper;
using System;

namespace NewHorizons.AchievementsPlus
{
    public static class AchievementHandler
    {
        private static bool _enabled;
        private static IAchievements API;
       
        public static void Init()
        {
            API = Main.Instance.ModHelper.Interaction.TryGetModApi<IAchievements>("xen.AchievementTracker");

            if (API == null)
            {
                Logger.Log("Achievements+ isn't installed");
                _enabled = false;
                return;
            }

            _enabled = true;

            // Register base NH achievements
            NH.WarpDriveAchievement.Init();
            NH.MultipleSystemAchievement.Init();
            NH.EatenOutsideBrambleAchievement.Init();
            NH.NewFrequencyAchievement.Init();
            NH.ProbeLostAchievement.Init();

            API.RegisterTranslationsFromFiles(Main.Instance, "Assets/translations");
        }

        public static void Earn(string unique_id)
        {
            if (!_enabled) return;

            API.EarnAchievement(unique_id);
        }

        public static void Register(string unique_id, bool secret, ModBehaviour mod)
        {
            if (!_enabled) return;

            API.RegisterAchievement(unique_id, secret, mod);
        }
    }
}