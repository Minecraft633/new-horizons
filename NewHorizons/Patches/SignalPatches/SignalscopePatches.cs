using HarmonyLib;
using NewHorizons.Builder.Props.Audio;

namespace NewHorizons.Patches.SignalPatches
{
    [HarmonyPatch(typeof(Signalscope))]
    public static class SignalScopePatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(nameof(Signalscope.Awake))]
        public static void Signalscope_Awake(Signalscope __instance)
        {
            __instance._strongestSignals = new AudioSignal[8];
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(Signalscope.SwitchFrequencyFilter))]
        public static bool Signalscope_SwitchFrequencyFilter(Signalscope __instance, int increment)
        {
            var count = SignalBuilder.NumberOfFrequencies;
            __instance._frequencyFilterIndex += increment;
            __instance._frequencyFilterIndex = __instance._frequencyFilterIndex >= count ? 0 : __instance._frequencyFilterIndex;
            __instance._frequencyFilterIndex = __instance._frequencyFilterIndex < 0 ? count - 1 : __instance._frequencyFilterIndex;
            var signalFrequency = AudioSignal.IndexToFrequency(__instance._frequencyFilterIndex);

            // Skip over this frequency
            var isUnknown = !PlayerData.KnowsFrequency(signalFrequency) && !(__instance._isUnknownFreqNearby && __instance._unknownFrequency == signalFrequency);
            if (isUnknown || !SignalBuilder.IsFrequencyInUse(signalFrequency))
            {
                __instance.SwitchFrequencyFilter(increment);
            }

            return false;
        }
    }
}
