using NewHorizons.Builder.Props;
using NewHorizons.Components.Orbital;
using NewHorizons.Handlers;
using NewHorizons.Utility;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NewHorizons.Builder.Body
{
    public static class BrambleDimensionBuilder
    {
        public static readonly float BASE_DIMENSION_RADIUS = 1705f;

        // location of all vanilla bramble dimensions
        //-9116.795 -19873.44 2480.327
        //-8460.688 -19873.44 6706.444
        //-5015.165 -19873.44 4142.816
        //-8993.414 -17059.44 4521.747
        //-7044.813 -17135.44 3272.149
        //-6904.48  -17048.44 5574.479
        //-11096.95 -22786.44 4657.534
        //-8716.807 -22786.44 4496.394


        // keys are all node names that have been referenced as an exit by at least one dimension but do not (yet) exist
        // values are all dimensions' warp controllers that link to a given dimension
        // unpairedNodes[name of node that doesn't exist yet] => List{warp controller for dimension that exits to that node, ...}
        private static Dictionary<string, List<OuterFogWarpVolume>> _unpairedDimensions;

        public static void Init()
        {
            // Just in case something went wrong and a dimension never got paired last time
            _unpairedDimensions = new();
        }

        public static GameObject Make(NewHorizonsBody body, GameObject go, NHAstroObject ao, Sector sector, OWRigidbody owRigidBody)
        {
            var config = body.Config.Bramble.dimension;

            ao.IsDimension = true;
            sector._name = Sector.Name.BrambleDimension;

            var atmo = SearchUtilities.Find("DB_HubDimension_Body/Sector_HubDimension/Atmosphere_HubDimension").InstantiateInactive();
            var volumes = SearchUtilities.Find("DB_HubDimension_Body/Sector_HubDimension/Volumes_HubDimension").InstantiateInactive();
            var effects = SearchUtilities.Find("DB_HubDimension_Body/Sector_HubDimension/Effects_HubDimension").InstantiateInactive();
            var geometry = DetailBuilder.MakeDetail(go, sector, "DB_HubDimension_Body/Sector_HubDimension/Geometry_HubDimension", Vector3.zero, Vector3.zero, 1, false);
            var exitWarps = SearchUtilities.Find("DB_HubDimension_Body/Sector_HubDimension/OuterWarp_Hub").InstantiateInactive();
            var repelVolume = SearchUtilities.Find("DB_HubDimension_Body/BrambleRepelVolume").InstantiateInactive();

            atmo.name = "Atmosphere";
            atmo.transform.parent = sector.transform;
            atmo.transform.localPosition = Vector3.zero;

            volumes.name = "Volumes";
            volumes.transform.parent = sector.transform;
            volumes.transform.localPosition = Vector3.zero;

            effects.name = "Effects";
            effects.transform.parent = sector.transform;
            effects.transform.localPosition = Vector3.zero;

            geometry.name = "Geometry";
            geometry.transform.parent = sector.transform;
            geometry.transform.localPosition = Vector3.zero;

            exitWarps.name = "OuterWarp";
            exitWarps.transform.parent = sector.transform;
            exitWarps.transform.localPosition = Vector3.zero;

            repelVolume.name = "BrambleRepelVolume";
            repelVolume.transform.parent = sector.transform;
            repelVolume.transform.localPosition = Vector3.zero;

            // Set up warps
            var outerFogWarpVolume = exitWarps.GetComponent<OuterFogWarpVolume>();
            outerFogWarpVolume._senderWarps = new List<InnerFogWarpVolume>();
            outerFogWarpVolume._linkedInnerWarpVolume = null;
            outerFogWarpVolume._name = OuterFogWarpVolume.Name.None;
            outerFogWarpVolume._sector = sector;

            PairExit(config.linksTo, outerFogWarpVolume);

            // Change fog color
            if (body.Config.Bramble.dimension.fogTint != null)
            {
                var fogGO = atmo.FindChild("FogSphere_Hub");
                var fog = fogGO.GetComponent<PlanetaryFogController>();
                fog.fogTint = body.Config.Bramble.dimension.fogTint.ToColor();
            }

            // Set up repel volume to only contain this dimension
            // The base game one is on the HUB dimension and encompasses all bramble dimensions and their sectors
            var cloak = repelVolume.gameObject.GetComponentInChildren<DarkBrambleCloakSphere>();
            cloak.transform.localScale = Vector3.one * 4000f;
            cloak._sectors = new Sector[] { sector };

            atmo.SetActive(true);
            volumes.SetActive(true);
            effects.SetActive(true);
            geometry.SetActive(true);
            exitWarps.SetActive(true);
            repelVolume.SetActive(true);

            return go;
        }

        public static void PairExit(string exitName, OuterFogWarpVolume warpController)
        {
            if (!BrambleNodeBuilder.NamedNodes.ContainsKey(exitName))
            {
                if (!_unpairedDimensions.ContainsKey(exitName)) _unpairedDimensions[exitName] = new();
                _unpairedDimensions[exitName].Add(warpController);
                return;
            }
            
            warpController._linkedInnerWarpVolume = BrambleNodeBuilder.NamedNodes[exitName];
        }

        public static void FinishPairingDimensionsForExitNode(string nodeName)
        {
            if (!_unpairedDimensions.ContainsKey(nodeName)) return;

            var warpControllers = _unpairedDimensions[nodeName].ToList();
            foreach (var dimensionWarpController in warpControllers)
            {
                PairExit(nodeName, dimensionWarpController);    
            }

            //unpairedDimensions.Remove(nodeName);
        }

    }
}
