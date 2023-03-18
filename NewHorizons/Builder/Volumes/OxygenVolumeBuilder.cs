using NewHorizons.External.Modules;
using NewHorizons.External.Volumes;
using UnityEngine;

namespace NewHorizons.Builder.Volumes
{
    public static class OxygenVolumeBuilder
    {
        public static OxygenVolume Make(GameObject planetGO, Sector sector, OxygenVolumeInfo info)
        {
            var volume = VolumeBuilder.Make<OxygenVolume>(planetGO, sector, info);

            volume._treeVolume = info.treeVolume;
            volume._playRefillAudio = info.playRefillAudio;

            return volume;
        }
    }
}
