using HarmonyLib;
using NewHorizons.Handlers;
using UnityEngine;

namespace NewHorizons.Patches.WarpPatches
{
    [HarmonyPatch(typeof(EyeCoordinatePromptTrigger))]
    public static class EyeCoordinatePromptTriggerPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(EyeCoordinatePromptTrigger.Update))]
        public static bool EyeCoordinatePromptTrigger_Update(EyeCoordinatePromptTrigger __instance)
        {
            var showPrompts = __instance._warpController.HasPower();

            // In other systems checking if the proper fact is revealed doesn't work, so we just overwrite this function
            __instance._promptController.SetEyeCoordinatesVisibility(showPrompts && VesselCoordinatePromptHandler.KnowsEyeCoordinates());

            VesselCoordinatePromptHandler.SetPromptVisibility(showPrompts);

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(EyeCoordinatePromptTrigger.OnExit))]
        public static void EyeCoordinatePromptTrigger_OnExit(GameObject hitObj)
        {
            if (hitObj.CompareTag("PlayerDetector"))
            {
                VesselCoordinatePromptHandler.SetPromptVisibility(false);
            }
        }
    }
}
