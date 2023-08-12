using HarmonyLib;
using UnityEngine;

namespace NewHorizons.Patches.VolumePatches
{
    [HarmonyPatch]
    public static class FogWarpVolumePatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(SphericalFogWarpVolume), nameof(SphericalFogWarpVolume.IsProbeOnly))]
        public static bool SphericalFogWarpVolume_IsProbeOnly(SphericalFogWarpVolume __instance, out bool __result)
        {
            __result = Mathf.Approximately(__instance._exitRadius / __instance._warpRadius, 2f); // Check the ratio between these to determine if seed, instead of just < 10
            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(FogWarpVolume), nameof(FogWarpVolume.GetFogThickness))]
        public static bool FogWarpVolume_GetFogThickness(FogWarpVolume __instance, out float __result)
        {
            if (__instance is InnerFogWarpVolume sph) __result = sph._exitRadius;
            else __result = 50; // 50f is hardcoded as the return value in the base game

            return false;
        }
    }
}
