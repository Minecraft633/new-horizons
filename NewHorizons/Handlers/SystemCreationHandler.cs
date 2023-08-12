using NewHorizons.Builder.StarSystem;
using NewHorizons.Components;
using NewHorizons.External;
using NewHorizons.Utility;
using NewHorizons.Utility.Files;
using NewHorizons.Utility.OWML;
using NewHorizons.Utility.OuterWilds;
using UnityEngine;
using Object = UnityEngine.Object;
namespace NewHorizons.Handlers
{
    public static class SystemCreationHandler
    {
        public static void LoadSystem(NewHorizonsSystem system)
        {
            if (system.Config.Skybox?.destroyStarField ?? false)
            {
                Object.Destroy(SearchUtilities.Find("Skybox/Starfield"));
            }

            if (system.Config.Skybox?.rightPath != null ||
                system.Config.Skybox?.leftPath != null ||
                system.Config.Skybox?.topPath != null ||
                system.Config.Skybox?.bottomPath != null ||
                system.Config.Skybox?.frontPath != null ||
                system.Config.Skybox?.bottomPath != null)
            {
                SkyboxBuilder.Make(system.Config.Skybox, system.Mod);
            }

            // No time loop or travel audio at the eye
            if (Main.Instance.CurrentStarSystem == "EyeOfTheUniverse") return;

            if (system.Config.enableTimeLoop)
            {
                var timeLoopController = new GameObject("TimeLoopController");
                timeLoopController.AddComponent<TimeLoopController>();
            }

            if (system.Config.loopDuration != 22f)
            {
                TimeLoopUtilities.SetLoopDuration(system.Config.loopDuration);
            }

            if (!string.IsNullOrEmpty(system.Config.travelAudio))
            {
                Delay.FireOnNextUpdate(() => AudioUtilities.SetAudioClip(Locator.GetGlobalMusicController()._travelSource, system.Config.travelAudio, system.Mod));
            }
        }
    }
}
