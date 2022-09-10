using NewHorizons.External.Modules;
using NewHorizons.External.Modules.Reflection;
using NewHorizons.External.Modules.VariableSize;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Logger = NewHorizons.Utility.Logger;

namespace NewHorizons.External.Configs
{
    /// <summary>
    /// Describes a body to generate
    /// </summary>
    [JsonObject(Title = "Celestial Body")]
    public class PlanetConfig
    {
        /// <summary>
        /// Unique name of your planet
        /// </summary>
        [Required]
        public string name;

        /// <summary>
        /// Unique star system containing your planet. If you set this to be a custom solar system remember to add a Spawn module to one of the bodies, or else you can't get to the system.
        /// </summary>
        [DefaultValue("SolarSystem")] public string starSystem = "SolarSystem";

        /// <summary>
        /// Generate asteroids around this body
        /// </summary>
        public AsteroidBeltModule AsteroidBelt;

        /// <summary>
        /// Describes this Body's atmosphere
        /// </summary>
        public AtmosphereModule Atmosphere;

        /// <summary>
        /// Base Properties of this Body
        /// </summary>
        public BaseModule Base;

        /// <summary>
        /// Add bramble nodes to this planet and/or make this planet a bramble dimension
        /// </summary>
        public BrambleModule Bramble;

        /// <summary>
        /// Set to a higher number if you wish for this body to be built sooner
        /// </summary>
        [DefaultValue(-1)] public int buildPriority = -1;

        /// <summary>
        /// Should this planet ever be shown on the title screen?
        /// </summary>
        [DefaultValue(true)] public bool canShowOnTitle = true;

        #region Obsolete

        [Obsolete("ChildrenToDestroy is deprecated, please use RemoveChildren instead")]
        public string[] childrenToDestroy;

        [Obsolete("Singularity is deprecated, please use Props->singularities")]
        public SingularityModule Singularity;

        [Obsolete("Signal is deprecated, please use Props->signals")]
        public SignalModule Signal;

        [Obsolete("Ring is deprecated, please use Rings")]
        public RingModule Ring;

        #endregion Obsolete

        /// <summary>
        /// Add a cloaking field to this planet
        /// </summary>
        public CloakModule Cloak;

        /// <summary>
        /// `true` if you want to delete this planet
        /// </summary>
        public bool destroy;

        /// <summary>
        /// Make this body into a focal point (barycenter)
        /// </summary>
        public FocalPointModule FocalPoint;

        /// <summary>
        /// Add funnel from this planet to another
        /// </summary>
        public FunnelModule Funnel;

        /// <summary>
        /// Generate the surface of this planet using a heightmap
        /// </summary>
        public HeightMapModule HeightMap;

        /// <summary>
        /// Does this config describe a quantum state of a custom planet defined in another file?
        /// </summary>
        public bool isQuantumState;

        /// <summary>
        /// Does this config describe a stellar remnant of a custom star defined in another file?
        /// </summary>
        public bool isStellarRemnant;

        /// <summary>
        /// Add lava to this planet
        /// </summary>
        public LavaModule Lava;

        /// <summary>
        /// Describes this Body's orbit (or lack there of)
        /// </summary>
        public OrbitModule Orbit;

        /// <summary>
        /// Procedural Generation
        /// </summary>
        public ProcGenModule ProcGen;

        /// <summary>
        /// Spawn various objects on this body
        /// </summary>
        public PropModule Props;

        /// <summary>
        /// Reference frame properties of this body
        /// </summary>
        public ReferenceFrameModule ReferenceFrame;

        /// <summary>
        /// A list of paths to child GameObjects to destroy on this planet
        /// </summary>
        public string[] removeChildren;

        /// <summary>
        /// Create rings around the planet
        /// </summary>
        public RingModule[] Rings;

        /// <summary>
        /// Add sand to this planet
        /// </summary>
        public SandModule Sand;

        /// <summary>
        /// Add ship log entries to this planet and describe how it looks in map mode
        /// </summary>
        public ShipLogModule ShipLog;

        /// <summary>
        /// Settings for shock effect on planet when the nearest star goes supernova
        /// </summary>
        public ShockEffectModule ShockEffect;

        /// <summary>
        /// Spawn the player at this planet
        /// </summary>
        public SpawnModule Spawn;

        /// <summary>
        /// Make this body a star
        /// </summary>
        public StarModule Star;

        /// <summary>
        /// Add water to this planet
        /// </summary>
        public WaterModule Water;

        /// <summary>
        /// Add various volumes on this body
        /// </summary>
        public VolumesModule Volumes;

        /// <summary>
        /// Extra data that may be used by extension mods
        /// </summary>
        public object extras;

        /// <summary>
        /// Allows setting field values and calling methods when the planet is created using reflection
        /// </summary>
        public ReflectionModule Reflection;

        public PlanetConfig()
        {
            // Always have to have a base module
            if (Base == null) Base = new BaseModule();
            if (Orbit == null) Orbit = new OrbitModule();
            if (ShipLog == null) ShipLog = new ShipLogModule();
            if (ReferenceFrame == null) ReferenceFrame = new ReferenceFrameModule();
        }

        public void Validate()
        {
            // If we can correct a part of the config, do it
            // If it cannot be solved, throw an exception
            if (Base.centerOfSolarSystem) Orbit.isStatic = true;
            if (Atmosphere?.clouds?.lightningGradient != null) Atmosphere.clouds.hasLightning = true;
            if (Bramble?.dimension != null && Orbit?.staticPosition == null) throw new Exception($"Dimension {name} must have Orbit.staticPosition defined.");
            if (Bramble?.dimension != null) canShowOnTitle = false; 
            if (Orbit?.staticPosition != null) Orbit.isStatic = true;

            // For each quantum group, verify the following:
            //      this group's id should be unique
            //      if type == sockets, group.sockets should not be null or empty
            //      if type == sockets, count every prop that references this group. the number should be < group.sockets.Count
            //      if type == sockets, for each socket, if rotation == null, rotation = Vector3.zero
            //      if type == sockets, for each socket, position must not be null
            // For each detail prop,
            //      if detail.quantumGroupID != null, there exists a quantum group with that id
            if (Props?.quantumGroups != null && Props?.details != null)
            {
                Dictionary<string, PropModule.QuantumGroupInfo> existingGroups = new Dictionary<string, PropModule.QuantumGroupInfo>();
                foreach (var quantumGroup in Props.quantumGroups)
                {
                    if (existingGroups.ContainsKey(quantumGroup.id)) { Logger.LogWarning($"Duplicate quantumGroup id found: {quantumGroup.id}"); quantumGroup.type = PropModule.QuantumGroupType.FailedValidation; }

                    existingGroups[quantumGroup.id] = quantumGroup;
                    if (quantumGroup.type == PropModule.QuantumGroupType.Sockets)
                    {
                        if (quantumGroup.sockets?.Length == 0) { Logger.LogError($"quantumGroup {quantumGroup.id} is of type \"sockets\" but has no defined sockets."); quantumGroup.type = PropModule.QuantumGroupType.FailedValidation; }
                        else
                        {
                            foreach (var socket in quantumGroup.sockets)
                            {
                                if (socket.rotation == null) socket.rotation = UnityEngine.Vector3.zero;
                                if (socket.position == null) { Logger.LogError($"quantumGroup {quantumGroup.id} has a socket without a position."); quantumGroup.type = PropModule.QuantumGroupType.FailedValidation; }
                            }
                        }
                    }
                }

                var existingGroupsPropCounts = new Dictionary<string, int>();
                foreach (var prop in Props?.details)
                {
                    if (prop.quantumGroupID == null) continue;
                    if (!existingGroups.ContainsKey(prop.quantumGroupID)) Logger.LogWarning($"A prop wants to be a part of quantum group {prop.quantumGroupID}, but this group does not exist.");
                    else existingGroupsPropCounts[prop.quantumGroupID] = existingGroupsPropCounts.GetValueOrDefault(prop.quantumGroupID) + 1;
                }

                foreach (var quantumGroup in Props.quantumGroups)
                {
                    if (quantumGroup.type == PropModule.QuantumGroupType.Sockets && existingGroupsPropCounts.GetValueOrDefault(quantumGroup.id) >= quantumGroup.sockets?.Length)
                    {
                        Logger.LogError($"quantumGroup {quantumGroup.id} is of type \"sockets\" and has more props than sockets.");
                        quantumGroup.type = PropModule.QuantumGroupType.FailedValidation;
                    }
                }
            }

            // Stars and focal points shouldnt be destroyed by stars
            if (Star != null || FocalPoint != null) Base.invulnerableToSun = true;
        }

        public void Migrate()
        {
            // Backwards compatability
            // Should be the only place that obsolete things are referenced
#pragma warning disable 612, 618
            if (Base.waterSize != 0)
                Water = new WaterModule
                {
                    size = Base.waterSize,
                    tint = Base.waterTint
                };

            if (Base.lavaSize != 0)
                Lava = new LavaModule
                {
                    size = Base.lavaSize
                };

            if (Base.blackHoleSize != 0)
                Singularity = new SingularityModule
                {
                    type = SingularityModule.SingularityType.BlackHole,
                    size = Base.blackHoleSize
                };

            if (Base.isSatellite) Base.showMinimap = false;

            if (!Base.hasReferenceFrame) ReferenceFrame.enabled = false;

            if (childrenToDestroy != null) removeChildren = childrenToDestroy;

            if (Base.cloakRadius != 0)
                Cloak = new CloakModule
                {
                    radius = Base.cloakRadius
                };

            if (Base.hasAmbientLight) Base.ambientLight = 0.5f;

            if (Atmosphere != null)
            {
                if (!string.IsNullOrEmpty(Atmosphere.cloud))
                    Atmosphere.clouds = new AtmosphereModule.CloudInfo
                    {
                        outerCloudRadius = Atmosphere.size,
                        innerCloudRadius = Atmosphere.size * 0.9f,
                        tint = Atmosphere.cloudTint,
                        texturePath = Atmosphere.cloud,
                        capPath = Atmosphere.cloudCap,
                        rampPath = Atmosphere.cloudRamp,
                        fluidType = Atmosphere.fluidType,
                        useBasicCloudShader = Atmosphere.useBasicCloudShader,
                        unlit = !Atmosphere.shadowsOnClouds
                    };

                // Validate
                if (Atmosphere.clouds?.lightningGradient != null) Atmosphere.clouds.hasLightning = true;

                // Former is obsolete, latter is to validate
                if (Atmosphere.hasAtmosphere || Atmosphere.atmosphereTint != null)
                    Atmosphere.useAtmosphereShader = true;

                // useBasicCloudShader is obsolete
                if (Atmosphere.clouds != null && Atmosphere.clouds.useBasicCloudShader)
                    Atmosphere.clouds.cloudsPrefab = CloudPrefabType.Basic;
            }

            if (Props?.tornados != null)
                foreach (var tornado in Props.tornados)
                    if (tornado.downwards)
                        tornado.type = PropModule.TornadoInfo.TornadoType.Downwards;

            if (Props?.audioVolumes != null)
            {
                if (Volumes == null) Volumes = new VolumesModule();
                if (Volumes.audioVolumes == null) Volumes.audioVolumes = new VolumesModule.AudioVolumeInfo[0];
                Volumes.audioVolumes = Volumes.audioVolumes.Concat(Props.audioVolumes).ToArray();
            }

            if (Props?.reveal != null)
            {
                if (Volumes == null) Volumes = new VolumesModule();
                if (Volumes.revealVolumes == null) Volumes.revealVolumes = new VolumesModule.RevealVolumeInfo[0];
                Volumes.revealVolumes = Volumes.revealVolumes.Concat(Props.reveal).ToArray();
            }

            if (Base.sphereOfInfluence != 0f) Base.soiOverride = Base.sphereOfInfluence;

            // Moved a bunch of stuff off of shiplog module to star system module because it didnt exist when we made this
            if (ShipLog != null)
            {
                Main.SystemDict.TryGetValue(starSystem, out var system);

                if (ShipLog.entryPositions != null)
                {
                    if (system.Config.entryPositions == null) system.Config.entryPositions = ShipLog.entryPositions;
                    else system.Config.entryPositions = system.Config.entryPositions.Concat(ShipLog.entryPositions).ToArray();
                }

                if (ShipLog.curiosities != null)
                {
                    if (system.Config.curiosities == null) system.Config.curiosities = ShipLog.curiosities;
                    else system.Config.curiosities = system.Config.curiosities.Concat(ShipLog.curiosities).ToArray();
                }

                if (ShipLog.initialReveal != null)
                {
                    if (system.Config.initialReveal == null) system.Config.initialReveal = ShipLog.initialReveal;
                    else system.Config.initialReveal = system.Config.initialReveal.Concat(ShipLog.initialReveal).ToArray();
                }
            }

            // Singularity is now a list in props so you can have many per planet
            if (Singularity != null)
            {
                if (Props == null) Props = new PropModule();
                if (Props.singularities == null) Props.singularities = new SingularityModule[0];
                Props.singularities = Props.singularities.Append(Singularity).ToArray();
            }

            // Signals are now in props
            if (Signal?.signals != null)
            {
                if (Props == null) Props = new PropModule();
                if (Props.signals == null) Props.signals = new SignalModule.SignalInfo[0];
                Props.signals = Props.signals.Concat(Signal.signals).ToArray();
            }

            // Star
            if (Star != null)
            {
                if (!Star.goSupernova) Star.stellarDeathType = StellarDeathType.None;
            }

            // Signals no longer use two different variables for audio
            if (Props?.signals != null)
            {
                foreach (var signal in Props.signals)
                {
                    if (!string.IsNullOrEmpty(signal.audioClip)) signal.audio = signal.audioClip;
                    if (!string.IsNullOrEmpty(signal.audioFilePath)) signal.audio = signal.audioFilePath;
                }
            }

            // Cloak
            if (Cloak != null)
            {
                if (!string.IsNullOrEmpty(Cloak.audioClip)) Cloak.audio = Cloak.audioClip;
                if (!string.IsNullOrEmpty(Cloak.audioFilePath)) Cloak.audio = Cloak.audioFilePath;
            }

            // Ring is now a list so you can have many per planet
            if (Ring != null)
            {
                if (Rings == null) Rings = new RingModule[0];
                Rings = Rings.Append(Ring).ToArray();
            }

            // Rings are no longer variable size module
            if (Rings != null)
            {
                foreach (var ring in Rings)
                {
                    if (ring.curve != null) ring.scaleCurve = ring.curve;
                }
            }
        }
    }
}