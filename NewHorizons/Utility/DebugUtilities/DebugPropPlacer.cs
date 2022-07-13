using NewHorizons.Builder.Props;
using NewHorizons.External.Configs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using static NewHorizons.External.Modules.PropModule;

namespace NewHorizons.Utility.DebugUtilities
{

    //
    // The prop placer. Doesn't interact with any files, just places and tracks props.
    //

    [RequireComponent(typeof(DebugRaycaster))]
    class DebugPropPlacer : MonoBehaviour
    {
        private struct PropPlacementData
        {
            public AstroObject body;
            public string system;
            public GameObject gameObject;
            public DetailInfo detailInfo;
        }

        // VASE
        public static readonly string DEFAULT_OBJECT = "BrittleHollow_Body/Sector_BH/Sector_NorthHemisphere/Sector_NorthPole/Sector_HangingCity/Sector_HangingCity_District1/Props_HangingCity_District1/OtherComponentsGroup/Props_HangingCity_Building_10/Prefab_NOM_VaseThin";

        public string currentObject { get; private set; } // path of the prop to be placed
        private bool hasAddedCurrentObjectToRecentsList = false;
        private List<PropPlacementData> props = new List<PropPlacementData>();
        private List<PropPlacementData> deletedProps = new List<PropPlacementData>();
        private DebugRaycaster _rc;

        public static HashSet<string> RecentlyPlacedProps = new HashSet<string>();

        public static bool active = false;
        public GameObject mostRecentlyPlacedPropGO { get { return props.Count() <= 0 ? null : props[props.Count() - 1].gameObject; } }
        public string mostRecentlyPlacedPropPath { get { return props.Count() <= 0 ? "" : props[props.Count() - 1].detailInfo.path; } }

        private void Awake()
        {
            _rc = this.GetRequiredComponent<DebugRaycaster>();
            currentObject = DEFAULT_OBJECT;
        }

        private void Update()
        {
            if (!Main.Debug) return;
            if (!active) return;

            if (Keyboard.current[Key.G].wasReleasedThisFrame)
            {
                PlaceObject();
            }

            if (Keyboard.current[Key.Minus].wasReleasedThisFrame)
            {
                DeleteLast();
            }

            if (Keyboard.current[Key.Equals].wasReleasedThisFrame)
            {
                UndoDelete();
            }
        }

        public void SetCurrentObject(string s)
        {
            currentObject = s;
            hasAddedCurrentObjectToRecentsList = false;
        }

        internal void PlaceObject()
        {
            DebugRaycastData data = _rc.Raycast();
            PlaceObject(data, this.gameObject.transform.position);

            if (!hasAddedCurrentObjectToRecentsList)
            {
                hasAddedCurrentObjectToRecentsList = true;

                if (!RecentlyPlacedProps.Contains(currentObject))
                {
                    RecentlyPlacedProps.Add(currentObject);
                }
            }
        }

        public void PlaceObject(DebugRaycastData data, Vector3 playerAbsolutePosition)
        {
            // TODO: implement sectors
            // if this hits a sector, store that sector and add a config file option for it

            if (!data.hitBodyGameObject.name.EndsWith("_Body"))
            {
                Logger.LogWarning("Cannot place object on non-body object: " + data.hitBodyGameObject.name);
            }

            try
            {
                if (currentObject == "" || currentObject == null)
                {
                    SetCurrentObject(DEFAULT_OBJECT);
                }

                GameObject prop = DetailBuilder.MakeDetail(data.hitBodyGameObject, data.hitBodyGameObject.GetComponentInChildren<Sector>(), currentObject, data.pos, data.norm, 1, false);
                PropPlacementData propData = RegisterProp_WithReturn(data.hitBodyGameObject.GetComponent<AstroObject>(), prop);

                SetGameObjectRotation(prop, data, playerAbsolutePosition);
            }
            catch
            {
                Logger.LogError($"Failed to place object {currentObject} on body ${data.hitBodyGameObject} at location ${data.pos}.");
            }
        }

        public static void SetGameObjectRotation(GameObject prop, DebugRaycastData data, Vector3 playerAbsolutePosition)
        {
            // align with surface normal
            Vector3 alignToSurface = (Quaternion.LookRotation(data.norm) * Quaternion.FromToRotation(Vector3.up, Vector3.forward)).eulerAngles;
            prop.transform.localEulerAngles = alignToSurface;

            // rotate facing dir towards player
            GameObject g = new GameObject("DebugProp");
            g.transform.parent = prop.transform.parent;
            g.transform.localPosition = prop.transform.localPosition;
            g.transform.localRotation = prop.transform.localRotation;

            prop.transform.parent = g.transform;

            var dirTowardsPlayer = prop.transform.parent.transform.InverseTransformPoint(playerAbsolutePosition) - prop.transform.localPosition;
            dirTowardsPlayer.y = 0;
            float rotation = Quaternion.LookRotation(dirTowardsPlayer).eulerAngles.y;
            prop.transform.localEulerAngles = new Vector3(0, rotation, 0);

            prop.transform.parent = g.transform.parent;
            GameObject.Destroy(g);
        }

        public static string GetAstroObjectName(string bodyName)
        {
            var astroObject = AstroObjectLocator.GetAstroObject(bodyName);
            if (astroObject == null) return null;

            var astroObjectName = astroObject.name;

            return astroObjectName;
        }

        public void FindAndRegisterPropsFromConfig(PlanetConfig config, List<string> pathsList = null)
        {
            if (config.starSystem != Main.Instance.CurrentStarSystem) return;

            AstroObject planet = AstroObjectLocator.GetAstroObject(config.name);

            if (planet == null) return;
            if (config.Props == null || config.Props.details == null) return;

            var astroObject = AstroObjectLocator.GetAstroObject(config.name);

            foreach (var detail in config.Props.details)
            {
                GameObject spawnedProp = DetailBuilder.GetSpawnedGameObjectByDetailInfo(detail);

                if (spawnedProp == null)
                {
                    Logger.LogError("No spawned prop found for " + detail.path);
                    continue;
                }

                PropPlacementData data = RegisterProp_WithReturn(astroObject, spawnedProp, detail.path, detail);

                // note: we do not support placing props from assetbundles, so they will not be added to the
                // selectable list of placed props
                if (detail.assetBundle == null && !RecentlyPlacedProps.Contains(data.detailInfo.path))
                {
                    if (pathsList != null) pathsList.Add(data.detailInfo.path);
                }
            }
        }

        public void RegisterProp(AstroObject body, GameObject prop)
        {
            RegisterProp_WithReturn(body, prop);
        }

        private PropPlacementData RegisterProp_WithReturn(AstroObject body, GameObject prop, string propPath = null, DetailInfo detailInfo = null)
        {
            if (Main.Debug)
            {
                // TOOD: make this prop an item
            }

            //var body = AstroObjectLocator.GetAstroObject(bodyGameObjectName);

            Logger.LogVerbose($"Adding prop to {Main.Instance.CurrentStarSystem}::{body.name}");


            detailInfo = detailInfo == null ? new DetailInfo() : detailInfo;
            detailInfo.path = propPath == null ? currentObject : propPath;

            PropPlacementData data = new PropPlacementData
            {
                body = body,
                gameObject = prop,
                system = Main.Instance.CurrentStarSystem,
                detailInfo = detailInfo
            };

            props.Add(data);
            return data;
        }

        public Dictionary<AstroObject, DetailInfo[]> GetPropsConfigByBody()
        {
            var groupedProps = props
                .GroupBy(p => p.system + "." + p.body)
                .Select(grp => grp.ToList())
                .ToList();

            Dictionary<AstroObject, DetailInfo[]> propConfigs = new Dictionary<AstroObject, DetailInfo[]>();

            foreach (List<PropPlacementData> bodyProps in groupedProps)
            {
                if (bodyProps == null || bodyProps.Count == 0) continue;
                if (bodyProps[0].body == null) continue;
                var body = bodyProps[0].body;
                Logger.LogVerbose("getting prop group for body " + body.name);
                //string bodyName = GetAstroObjectName(bodyProps[0].body);

                DetailInfo[] infoArray = new DetailInfo[bodyProps.Count];
                propConfigs[body] = infoArray;

                for (int i = 0; i < bodyProps.Count; i++)
                {
                    var prop = bodyProps[i];
                    var rootTransform = prop.gameObject.transform.root;

                    // Objects are parented to the sector and not to the planet
                    // However, raycasted positions are reported relative to the root game object
                    // Normally these two are the same, but there are some notable exceptions (ex, floating islands)
                    // So we can't use local position/rotation here, we have to inverse transform the global position/rotation relative to root object
                    prop.detailInfo.position = rootTransform.InverseTransformPoint(prop.gameObject.transform.position);
                    prop.detailInfo.scale = prop.gameObject.transform.localScale.x;
                    if (!prop.detailInfo.alignToNormal) prop.detailInfo.rotation = rootTransform.InverseTransformRotation(prop.gameObject.transform.rotation).eulerAngles;

                    infoArray[i] = prop.detailInfo;
                }
            }

            return propConfigs;
        }

        public void DeleteLast()
        {
            if (props.Count <= 0) return;

            PropPlacementData last = props[props.Count - 1];
            props.RemoveAt(props.Count - 1);

            last.gameObject.SetActive(false);

            deletedProps.Add(last);
        }

        public void UndoDelete()
        {
            if (deletedProps.Count <= 0) return;

            PropPlacementData last = deletedProps[deletedProps.Count - 1];
            deletedProps.RemoveAt(deletedProps.Count - 1);

            last.gameObject.SetActive(true);

            props.Add(last);
        }
    }
}
