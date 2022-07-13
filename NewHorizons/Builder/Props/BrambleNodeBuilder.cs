using NewHorizons.Builder.Body;
using NewHorizons.Handlers;
using NewHorizons.Utility;
using OWML.Common;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static NewHorizons.External.Modules.BrambleModule;
using static NewHorizons.External.Modules.SignalModule;
using Logger = NewHorizons.Utility.Logger;

namespace NewHorizons.Builder.Props
{

    // TODO
    //3) support for existing dimensions?
    //5) test whether nodes can lead to vanilla dimensions

    public static class BrambleNodeBuilder
    {
        // keys are all dimension names that have been referenced by at least one node but do not (yet) exist
        // values are all nodes' warp controllers that link to a given dimension
        // unpairedNodes[name of dimension that doesn't exist yet] => List{warp controller for node that links to that dimension, ...}
        private static Dictionary<string, List<InnerFogWarpVolume>> _unpairedNodes = new();
        private static Dictionary<string, List<SignalInfo>> _propogatedSignals = null;

        public static Dictionary<string, InnerFogWarpVolume> NamedNodes { get; private set; }
        public static Dictionary<BrambleNodeInfo, GameObject> BuiltBrambleNodes { get; private set; }

        private static string _brambleSeedPrefabPath = "DB_PioneerDimension_Body/Sector_PioneerDimension/Interactables_PioneerDimension/SeedWarp_ToPioneer (1)";
        private static string _brambleNodePrefabPath = "DB_HubDimension_Body/Sector_HubDimension/Interactables_HubDimension/InnerWarp_ToCluster";

        public static void Init()
        {
            _unpairedNodes = new();
            _propogatedSignals = null;
            NamedNodes = new();
            BuiltBrambleNodes = new();
        }


        // how warping works
        // every frame, each FogWarpDetector loops over all FogWarpVolume instances it has in _warpVolumes. Each instance gets CheckWarpProximity called
        // (pretty much every FogWarpVolume in the game is a SphericalFogWarpVolume. that's where CheckWarpProximity is called)
        // if CheckWarpProximity would return 0, it calls its own WarpDetector() function
        // 


        public static void FinishPairingNodesForDimension(string dimensionName, AstroObject dimensionAO = null)
        {
            Logger.LogWarning($"Pairing missed for {dimensionName}");
            if (!_unpairedNodes.ContainsKey(dimensionName)) return;

            Logger.LogWarning("proceeding");
            foreach (var nodeWarpController in _unpairedNodes[dimensionName])
            {
                Logger.LogWarning($"Pairing node {nodeWarpController.gameObject.name} links to {dimensionName}");
                PairEntrance(nodeWarpController, dimensionName, dimensionAO);
            }

            _unpairedNodes.Remove(dimensionName);
        }

        private static void RecordUnpairedNode(InnerFogWarpVolume warpVolume, string linksTo)
        {
            if (!_unpairedNodes.ContainsKey(linksTo)) _unpairedNodes[linksTo] = new();

            Logger.LogWarning($"Recording node {warpVolume.gameObject.name} links to {linksTo}");

            _unpairedNodes[linksTo].Add(warpVolume);
        }

        private static OuterFogWarpVolume GetOuterFogWarpVolumeFromAstroObject(GameObject go)
        {
            var outerWarpGO = go.FindChild("Sector/OuterWarp");
            if (outerWarpGO == null) return null;

            var outerFogWarpVolume = outerWarpGO.GetComponent<OuterFogWarpVolume>();
            return outerFogWarpVolume;
        }

        private static void PropogateSignals()
        {
            // The purpose of this function is to determine which signals any given node should play, based on which dimension it links to
            // you know how the main dark bramble node, the one that forms the core of the planet, plays Feldspar's harmonica signal, even though Feldspar isn't in the dimension that the node links directly to?
            // that's what this function is for. it would determine that the main node should play Feldspar's signal

            // New Strategy (thanks Damian):
            // 1) Run Floyd-Warshall on the dimensions (where each dimension is a vertex and each node is an edge)
            // 2) For each dimension A, if it's possible to reach dimension B, add dimension B's signals to the list propogatedSignals[A]

            var allDimensions = PlanetCreationHandler.allBodies.Where(body => body?.Config?.Bramble?.dimension != null).Select(body => body.Config).ToList();

            //
            // Floyd Warshall
            //

            // access will be our final answer - if access[i, j], then nodes linking to dimension i should display all of dimension j's signals
            var access = new bool[allDimensions.Count(), allDimensions.Count()];

            var dimensionNameToIndex = new Dictionary<string, int>();
            for (int dimensionIndex = 0; dimensionIndex < allDimensions.Count(); dimensionIndex++) dimensionNameToIndex[allDimensions[dimensionIndex].name] = dimensionIndex;

            // set up the direct links (ie, if dimension 0 contains a node that links to dimension 3, set access[0, 3] = true)
            for (int dimensionIndex = 0; dimensionIndex < allDimensions.Count(); dimensionIndex++)
            {
                var dimension = allDimensions[dimensionIndex];
                if (dimension.Bramble.nodes == null) continue;
                foreach (var node in dimension.Bramble.nodes)
                {
                    var destinationDimensionIndex = dimensionNameToIndex[node.linksTo];
                    access[dimensionIndex, destinationDimensionIndex] = true;
                }
            }

            // a node that links to dimension A should display all of dimension A's signals, so for the purposes of our function, we need to say that dimension A links to dimension A
            for (int dimensionIndex = 0; dimensionIndex < allDimensions.Count(); dimensionIndex++) access[dimensionIndex, dimensionIndex] = true;

            // The actual Floyd-Warshall - determine whether each pair of dimensions link indirectly (eg if A->B->C, then after this step, access[A, C] = true)
            for (int k = 0; k < allDimensions.Count(); k++)
                for (int i = 0; i < allDimensions.Count(); i++)
                    for (int j = 0; j < allDimensions.Count(); j++)
                        if (access[i, k] && access[k, j])
                            access[i, j] = true;

            //
            // Build the list of dimensionName -> List<SignalInfo>
            //

            // this dictionary lists all the signals a given node should have, depending on the dimension it links to
            // ie, if a node links to "dimension1", then that node should spawn all of the signals in the list propogatedSignals["dimension1"]
            _propogatedSignals = new Dictionary<string, List<SignalInfo>>();
            foreach (var dimension in allDimensions)
            {
                _propogatedSignals[dimension.name] = new();
                var dimensionIndex = dimensionNameToIndex[dimension.name];

                foreach (var destinationDimension in allDimensions)
                {
                    if (destinationDimension.Props?.signals == null) continue;

                    var destinationIndex = dimensionNameToIndex[destinationDimension.name];
                    if (access[dimensionIndex, destinationIndex])
                    {
                        _propogatedSignals[dimension.name].AddRange(destinationDimension.Props.signals);
                    }
                }
            }
        }

        private static bool PairEntrance(InnerFogWarpVolume nodeWarp, string destinationName, AstroObject dimensionAO = null)
        {
            Logger.LogWarning($"Pairing node {nodeWarp.gameObject.name} to {destinationName}");

            var destinationAO = dimensionAO ?? AstroObjectLocator.GetAstroObject(destinationName); // find child "Sector/OuterWarp"
            if (destinationAO == null) return false;

            Logger.LogWarning($"Found {destinationName} as gameobject {destinationAO.gameObject.name} (was passed in: {dimensionAO != null})");

            // link the node's warp volume to the destination's
            var destination = GetOuterFogWarpVolumeFromAstroObject(destinationAO.gameObject);
            if (destination == null) return false;

            Logger.LogWarning($"Proceeding with pairing node {nodeWarp.gameObject.name} to {destinationName}. Path to outer fog warp volume: {destination.transform.GetPath()}");

            nodeWarp._linkedOuterWarpVolume = destination;
            destination.RegisterSenderWarp(nodeWarp);

            return true;
        }

        // DB_EscapePodDimension_Body/Sector_EscapePodDimension/Interactables_EscapePodDimension/InnerWarp_ToAnglerNest // need to change the light shaft color
        // DB_ExitOnlyDimension_Body/Sector_ExitOnlyDimension/Interactables_ExitOnlyDimension/InnerWarp_ToExitOnly  // need to change the colors
        // DB_HubDimension_Body/Sector_HubDimension/Interactables_HubDimension/InnerWarp_ToCluster   // need to delete the child "Signal_Harmonica"

        public static void Make(GameObject go, Sector sector, BrambleNodeInfo[] configs, IModBehaviour mod)
        {
            foreach (var config in configs)
            {
                Make(go, sector, config, mod);
            }
        }

        public static GameObject Make(GameObject go, Sector sector, BrambleNodeInfo config, IModBehaviour mod)
        {
            var prefab = SearchUtilities.Find(config.isSeed ? _brambleSeedPrefabPath : _brambleNodePrefabPath);

            // Spawn the bramble node
            var brambleNode = prefab.InstantiateInactive();
            foreach (var collider in brambleNode.GetComponentsInChildren<Collider>(true))
            {
                collider.enabled = true; 
            }

            var innerFogWarpVolume = brambleNode.GetComponent<InnerFogWarpVolume>();
            //brambleNode.AddComponent<FogWarpHUDMarker>();
            var fogLight = brambleNode.GetComponent<FogLight>();

            brambleNode.transform.parent = sector.transform;
            brambleNode.transform.position = go.transform.TransformPoint(config.position);
            brambleNode.transform.rotation = go.transform.TransformRotation(Quaternion.Euler(config.rotation));
            brambleNode.name = "Bramble Node to " + config.linksTo;

            // This node comes with Feldspar's signal, we don't want that though
            GameObject.Destroy(brambleNode.FindChild("Signal_Harmonica"));

            // Fix some components
            fogLight._parentBody = go.GetComponent<OWRigidbody>();
            fogLight._sector = sector;
            fogLight._linkedSector = null;
            fogLight._innerWarp = innerFogWarpVolume;
            fogLight._linkedFogLights = new List<FogLight>();
            fogLight._linkedLightData = new List<FogLight.LightData>();

            sector.RegisterFogLight(fogLight);

            // Set the scale
            brambleNode.transform.localScale = Vector3.one * config.scale;
            innerFogWarpVolume._warpRadius *= config.scale;
            innerFogWarpVolume._exitRadius *= config.scale;

            // Seed fog works differently, so it doesn't need to be fixed (it's also located on a different child path, so the below FindChild calls wouldn't work)
            if (!config.isSeed)
            {
                var fog = brambleNode.FindChild("Effects").FindChild("InnerWarpFogSphere");
                var fogMaterial = fog.GetComponent<MeshRenderer>().sharedMaterial;
                fog.transform.localScale /= config.scale;
                fogMaterial.SetFloat("_Radius", fogMaterial.GetFloat("_Radius") * config.scale);
                fogMaterial.SetFloat("_Density", fogMaterial.GetFloat("_Density") / config.scale);
            }

            // issue: when exiting a scaled bramblenode, the screen fog effect uses the original bramble node radius
            // it's controlled by this class FogWarpEffectBubbleController which is on a game object under Player_Camera

            // found under PlayerFogWarpDetector.LateUpdate()
            // _playerEffectBubbleController.SetFogFade(_fogFraction, _fogColor); // where _fogFraction is basically _targetFogFraction, but lerped into over time

            // found under FogWarpDetector.FixedUpdate()
            // FogWarpVolume fogWarpVolume = _warpVolumes[i];
            // float num2 = Mathf.Abs(fogWarpVolume.CheckWarpProximity(this));
            // float b = Mathf.Clamp01(1f - Mathf.Abs(num2) / fogWarpVolume.GetFogThickness());
            // _targetFogFraction = Mathf.Max(_targetFogFraction, b);

            // this means that either CheckWarpProximity() or GetFogThickness() is incorrect for the InnerWarpFogSpheres.
            // most likely it's CheckWarpProximity()

            // Change the colors
            if (config.isSeed) SetSeedColors(brambleNode, config.fogTint?.ToColor(), config.lightTint?.ToColor());
            else SetNodeColors(brambleNode, config.fogTint?.ToColor(), config.lightTint?.ToColor());

            // Set up warps
            innerFogWarpVolume._sector = sector;
            innerFogWarpVolume._attachedBody = go.GetComponent<OWRigidbody>(); // I don't think this is necessary, it seems to be set correctly on its own
            innerFogWarpVolume._containerWarpVolume = GetOuterFogWarpVolumeFromAstroObject(go); // the OuterFogWarpVolume of the dimension this node is inside of (null if this node is not inside of a bramble dimension (eg it's sitting on a planet or something))
            var success = PairEntrance(innerFogWarpVolume, config.linksTo);
            if (!success) RecordUnpairedNode(innerFogWarpVolume, config.linksTo);

            // Cleanup for dimension exits
            if (config.name != null)
            {
                NamedNodes[config.name] = innerFogWarpVolume;
                BrambleDimensionBuilder.FinishPairingDimensionsForExitNode(config.name);
            }

            // Make signals
            if (_propogatedSignals == null) PropogateSignals();
            foreach (var signalConfig in _propogatedSignals[config.linksTo])
            {
                var signalGO = SignalBuilder.Make(go, sector, signalConfig, mod);
                signalGO.GetComponent<AudioSignal>()._identificationDistance = 0;
                signalGO.GetComponent<AudioSignal>()._sourceRadius = 1;
                signalGO.transform.position = brambleNode.transform.position;
                signalGO.transform.parent = brambleNode.transform;
            }

            // Done!
            brambleNode.SetActive(true);
            return brambleNode;
        }

        public static void SetNodeColors(GameObject brambleNode, Color? fogTint, Color? lightTint)
        {
            if (fogTint != null)
            {
                var fogRenderer = brambleNode.GetComponent<InnerFogWarpVolume>();

                fogRenderer._fogColor = fogTint.Value;
                fogRenderer._useFarFogColor = false;

                var fogBackdrop = brambleNode.FindChild("Terrain_DB_BrambleSphere_Inner_v2")?.FindChild("fogbackdrop_v2");
                if (fogBackdrop != null) fogBackdrop.GetComponent<MeshRenderer>().sharedMaterial.color = (Color)fogTint;
            }

            if (lightTint != null)
            {
                var lightShafts = brambleNode.FindChild("Effects/DB_BrambleLightShafts");

                var lightShaft1 = lightShafts.FindChild("BrambleLightShaft1");
                var mat = lightShaft1.GetComponent<MeshRenderer>().material;
                mat.color = lightTint.Value;

                for (int i = 1; i <= 6; i++)
                {
                    var lightShaft = lightShafts.FindChild($"BrambleLightShaft{i}");
                    lightShaft.GetComponent<MeshRenderer>().sharedMaterial = mat;
                }
            }
        }

        public static void SetSeedColors(GameObject brambleSeed, Color? fogTint, Color? lightTint)
        {
            if (fogTint != null)
            {
                var fogRenderer = brambleSeed.FindChild("VolumetricFogSphere (2)");

                var fogMeshRenderer = fogRenderer.GetComponent<MeshRenderer>();
                var mat = fogMeshRenderer.material;
                mat.color = fogTint.Value;
                fogMeshRenderer.sharedMaterial = mat;
            }

            if (lightTint != null)
            {
                var lightShafts = brambleSeed.FindChild("Terrain_DB_BrambleSphere_Seed_V2 (2)/DB_SeedLightShafts");

                var lightShaft1 = lightShafts.FindChild("DB_SeedLightShafts1");
                var mat = lightShaft1.GetComponent<MeshRenderer>().material;
                mat.color = lightTint.Value;

                for (int i = 1; i <= 6; i++)
                {
                    var lightShaft = lightShafts.FindChild($"DB_SeedLightShafts{i}");
                    lightShaft.GetComponent<MeshRenderer>().sharedMaterial = mat;
                }
            }
        }
    }
}
