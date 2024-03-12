using HarmonyLib;
using NewHorizons.Builder.Props;
using UnityEngine;

namespace NewHorizons.Patches.VolumePatches
{
    [HarmonyPatch]
    public static class FogWarpVolumePatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SphericalFogWarpVolume), nameof(SphericalFogWarpVolume.IsProbeOnly))]
        public static bool SphericalFogWarpVolume_IsProbeOnly(SphericalFogWarpVolume __instance, ref bool __result)
        {
            // Do not affect base game volumes
            if (!BrambleNodeBuilder.IsNHFogWarpVolume(__instance))
            {
                return true;
            }

            __result = Mathf.Approximately(__instance._exitRadius / __instance._warpRadius, 2f); // Check the ratio between these to determine if seed, instead of just < 10
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FogWarpVolume), nameof(FogWarpVolume.GetFogThickness))]
        public static bool FogWarpVolume_GetFogThickness(FogWarpVolume __instance, ref float __result)
        {
            // Do not affect base game volumes
            if (!BrambleNodeBuilder.IsNHFogWarpVolume(__instance))
            {
                return true;
            }

            if (__instance is InnerFogWarpVolume sph)
            {
                __result = sph._exitRadius;
                return false;
            }
            else
            {
                // Base game is hardcoded to this
                // Except for capsule volumes so either those are unused or whoever said this is a liar!
                __result = 50f;
                return true;
            }
        }
    }
}
