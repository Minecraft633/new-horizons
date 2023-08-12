using NewHorizons.Components.Sectored;
using NewHorizons.External.Modules;
using NewHorizons.Handlers;
using NewHorizons.Utility;
using NewHorizons.Utility.Files;
using NewHorizons.Utility.OWML;
using OWML.Common;
using UnityEngine;


namespace NewHorizons.Builder.Body
{
    public static class CloakBuilder
    {
        private static GameObject _prefab;
        
        internal static void InitPrefab()
        {
            if (_prefab == null)
            {
                _prefab = SearchUtilities.Find("RingWorld_Body/CloakingField_IP")?.InstantiateInactive()?.Rename("CloakingField")?.DontDestroyOnLoad();
                if (_prefab == null)
                {
                    NHLogger.LogWarning($"Tried to make a cloak but couldn't. Do you have the DLC installed?");
                    return;
                }
                else
                    _prefab.AddComponent<DestroyOnDLC>()._destroyOnDLCNotOwned = true;
            }
        }

        public static void Make(GameObject planetGO, Sector sector, OWRigidbody OWRB, CloakModule module, bool keepReferenceFrame, IModBehaviour mod)
        {
            InitPrefab();

            if (_prefab == null) return;

            var radius = module.radius;

            var newCloak = _prefab.InstantiateInactive();
            newCloak.transform.parent = sector?.transform ?? planetGO.transform;
            newCloak.transform.position = planetGO.transform.position;
            newCloak.transform.name = "CloakingField";
            newCloak.transform.localScale = Vector3.one * radius;

            Object.Destroy(newCloak.GetComponent<PlayerCloakEntryRedirector>());

            var cloakFieldController = newCloak.GetComponent<CloakFieldController>();
            cloakFieldController._cloakScaleDist = module.cloakScaleDist ?? (radius * 2000 / 3000f);
            cloakFieldController._farCloakRadius = module.farCloakRadius ?? (radius * 500 / 3000f);
            cloakFieldController._innerCloakRadius = module.innerCloakRadius ?? (radius * 900 / 3000f);
            cloakFieldController._nearCloakRadius = module.nearCloakRadius ?? (radius * 800 / 3000f);

            cloakFieldController._referenceFrameVolume = OWRB._attachedRFVolume;
            cloakFieldController._exclusionSector = null;

            var cloakVolumeObj = new GameObject("CloakVolume");
            cloakVolumeObj.transform.parent = planetGO.transform;
            cloakVolumeObj.transform.localPosition = Vector3.zero;
            var cloakVolume = cloakVolumeObj.AddComponent<SphereShape>();
            cloakVolume.radius = module.farCloakRadius ?? (radius * 500 / 3000f);

            cloakFieldController._cloakSphereVolume = cloakVolumeObj.AddComponent<OWTriggerVolume>();
            cloakFieldController._ringworldFadeRenderers = new OWRenderer[0];

            var cloakSectorController = newCloak.AddComponent<CloakSectorController>();
            cloakSectorController.Init(cloakFieldController, planetGO);

            var cloakAudioSource = newCloak.GetComponentInChildren<OWAudioSource>();
            cloakAudioSource._audioSource = cloakAudioSource.GetComponent<AudioSource>();
            bool hasCustomAudio = !string.IsNullOrEmpty(module.audio);
            if (hasCustomAudio) AudioUtilities.SetAudioClip(cloakAudioSource, module.audio, mod);
            
            newCloak.SetActive(true);
            cloakFieldController.enabled = true;

            cloakSectorController.EnableCloak();

            CloakHandler.RegisterCloak(cloakFieldController);

            // To cloak from the start
            Delay.FireOnNextUpdate(cloakSectorController.OnPlayerExit);
            Delay.FireOnNextUpdate(hasCustomAudio ? cloakSectorController.TurnOnMusic : cloakSectorController.TurnOffMusic);
            Delay.FireOnNextUpdate(keepReferenceFrame ? cloakSectorController.EnableReferenceFrameVolume : cloakSectorController.DisableReferenceFrameVolume);
        }
    }
}
