﻿using NewHorizons.External.Configs;
using NewHorizons.External.Modules;
using NewHorizons.Handlers;
using NewHorizons.Utility;
using OWML.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Logger = NewHorizons.Utility.Logger;
using Object = UnityEngine.Object;

namespace NewHorizons.Builder.Props
{
    public static class DetailBuilder
    {
        private static readonly Dictionary<PropModule.DetailInfo, GameObject>
            detailInfoToCorrespondingSpawnedGameObject = new Dictionary<PropModule.DetailInfo, GameObject>();

        public static GameObject GetSpawnedGameObjectByDetailInfo(PropModule.DetailInfo detail)
        {
            if (!detailInfoToCorrespondingSpawnedGameObject.ContainsKey(detail)) return null;
            return detailInfoToCorrespondingSpawnedGameObject[detail];
        }

        public static void Make(GameObject go, Sector sector, PlanetConfig config, IModBehaviour mod,
            string uniqueModName, PropModule.DetailInfo detail)
        {
            GameObject detailGO = null;

            if (detail.assetBundle != null)
            {
                var prefab = AssetBundleUtilities.LoadPrefab(detail.assetBundle, detail.path, mod);

                detailGO = MakeDetail(go, sector, prefab, detail.position, detail.rotation, detail.scale,
                    detail.alignToNormal);
            }
            else if (detail.objFilePath != null)
            {
                try
                {
                    var prefab = mod.ModHelper.Assets.Get3DObject(detail.objFilePath, detail.mtlFilePath);
                    AssetBundleUtilities.ReplaceShaders(prefab);
                    prefab.SetActive(false);
                    detailGO = MakeDetail(go, sector, prefab, detail.position, detail.rotation, detail.scale,
                        detail.alignToNormal);
                }
                catch (Exception e)
                {
                    Logger.LogError(
                        $"Could not load 3d object {detail.objFilePath} with texture {detail.mtlFilePath} : {e.Message}");
                }
            }
            else
            {
                detailGO = MakeDetail(go, sector, detail.path, detail.position, detail.rotation, detail.scale,
                    detail.alignToNormal);
            }

            if (detailGO != null && detail.removeChildren != null)
                foreach (var childPath in detail.removeChildren)
                {
                    var childObj = detailGO.transform.Find(childPath);
                    if (childObj != null) childObj.gameObject.SetActive(false);
                    else Logger.LogWarning($"Couldn't find {childPath}");
                }

            if (detailGO != null && detail.removeComponents)
            {
                // Just swap all the children to a new game object
                var newDetailGO = new GameObject(detailGO.name);
                newDetailGO.transform.position = detailGO.transform.position;
                newDetailGO.transform.parent = detailGO.transform.parent;
                // Can't modify parents while looping through children bc idk
                var children = new List<Transform>();
                foreach (Transform child in detailGO.transform) children.Add(child);
                foreach (var child in children) child.parent = newDetailGO.transform;
                Object.Destroy(detailGO);
                detailGO = newDetailGO;
            }

            detailInfoToCorrespondingSpawnedGameObject[detail] = detailGO;
        }

        public static GameObject MakeDetail(GameObject go, Sector sector, string propToClone, MVector3 position,
            MVector3 rotation, float scale, bool alignWithNormal)
        {
            var prefab = SearchUtilities.Find(propToClone);
            if (prefab == null) Logger.LogError($"Couldn't find detail {propToClone}");
            return MakeDetail(go, sector, prefab, position, rotation, scale, alignWithNormal);
        }

        public static GameObject MakeDetail(GameObject planetGO, Sector sector, GameObject prefab, MVector3 position,
            MVector3 rotation, float scale, bool alignWithNormal)
        {
            if (prefab == null) return null;

            var prop = prefab.InstantiateInactive();
            prop.transform.parent = sector?.transform ?? planetGO.transform;
            prop.SetActive(false);

            if (sector != null)
                sector.OnOccupantEnterSector += sd => OWAssetHandler.OnOccupantEnterSector(prop, sd, sector);
            OWAssetHandler.LoadObject(prop);

            foreach (var component in prop.GetComponents<Component>().Concat(prop.GetComponentsInChildren<Component>()))
            {
                // Enable all children or something
                var enabledField = component?.GetType()?.GetField("enabled");
                if (enabledField != null && enabledField.FieldType == typeof(bool))
                    Main.Instance.ModHelper.Events.Unity.FireOnNextUpdate(() => enabledField.SetValue(component, true));

                // Fix a bunch of sector stuff
                if (sector != null)
                {
                    if (component is Sector) (component as Sector)._parentSector = sector;

                    // TODO: Make this work or smthng
                    if (component is GhostIK) (component as GhostIK).enabled = false;
                    if (component is GhostEffects) (component as GhostEffects).enabled = false;

                    if (component is DarkMatterVolume)
                    {
                        var probeVisuals = component.gameObject.transform.Find("ProbeVisuals");
                        if (probeVisuals != null) probeVisuals.gameObject.SetActive(true);
                    }

                    if (component is SectoredMonoBehaviour)
                    {
                        (component as SectoredMonoBehaviour).SetSector(sector);
                    }
                    else
                    {
                        var sectorField = component?.GetType()?.GetField("_sector");
                        if (sectorField != null && sectorField.FieldType == typeof(Sector))
                            Main.Instance.ModHelper.Events.Unity.FireOnNextUpdate(() =>
                                sectorField.SetValue(component, sector));
                    }

                    if (component is AnglerfishController)
                        try
                        {
                            (component as AnglerfishController)._chaseSpeed += OWPhysics
                                .CalculateOrbitVelocity(planetGO.GetAttachedOWRigidbody(),
                                    planetGO.GetComponent<AstroObject>().GetPrimaryBody().GetAttachedOWRigidbody())
                                .magnitude;
                        }
                        catch (Exception e)
                        {
                            Logger.LogError($"Couldn't update AnglerFish chase speed: {e.Message}");
                        }

                    // Fix slide reel
                    if (component is SlideCollectionContainer)
                        sector.OnOccupantEnterSector.AddListener(_ =>
                            (component as SlideCollectionContainer).LoadStreamingTextures());

                    if (component is OWItemSocket) (component as OWItemSocket)._sector = sector;
                }
                else
                {
                    // Remove things that require sectors. Will just keep extending this as things pop up

                    if (component is FogLight || component is SectoredMonoBehaviour)
                    {
                        Object.DestroyImmediate(component);
                        continue;
                    }
                }

                // Fix a bunch of stuff when done loading
                Main.Instance.ModHelper.Events.Unity.RunWhen(() => Main.IsSystemReady, () =>
                {
                    try
                    {
                        if (component is Animator)
                        {
                            (component as Animator).enabled = true;
                        }
                        else if (component is Collider)
                        {
                            (component as Collider).enabled = true;
                        }
                        else if (component is Renderer)
                        {
                            (component as Renderer).enabled = true;
                        }
                        else if (component is Shape)
                        {
                            (component as Shape).enabled = true;
                        }
                        // If it's not a moving anglerfish make sure the anim controller is regular
                        else if (component is AnglerfishAnimController &&
                                 component.GetComponentInParent<AnglerfishController>() == null)
                        {
                            Logger.Log("Enabling anglerfish animation");
                            var angler = component as AnglerfishAnimController;
                            // Remove any reference to its angler
                            if (angler._anglerfishController)
                            {
                                angler._anglerfishController.OnChangeAnglerState -= angler.OnChangeAnglerState;
                                angler._anglerfishController.OnAnglerTurn -= angler.OnAnglerTurn;
                                angler._anglerfishController.OnAnglerSuspended -= angler.OnAnglerSuspended;
                                angler._anglerfishController.OnAnglerUnsuspended -= angler.OnAnglerUnsuspended;
                            }

                            angler.enabled = true;
                            angler.OnChangeAnglerState(AnglerfishController.AnglerState.Lurking);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.LogWarning(
                            $"Exception when modifying component [{component.GetType().Name}] on [{planetGO.name}] : {e.Message}, {e.StackTrace}");
                    }
                });
            }

            prop.transform.position = position == null
                ? planetGO.transform.position
                : planetGO.transform.TransformPoint((Vector3)position);

            var rot = rotation == null ? Quaternion.identity : Quaternion.Euler((Vector3)rotation);
            if (alignWithNormal)
            {
                // Apply the rotation after aligning it with normal
                var up = planetGO.transform.InverseTransformPoint(prop.transform.position).normalized;
                prop.transform.rotation = Quaternion.FromToRotation(Vector3.up, up);
                prop.transform.rotation *= rot;
            }
            else
            {
                prop.transform.rotation = planetGO.transform.TransformRotation(rot);
            }

            prop.transform.localScale = scale != 0 ? Vector3.one * scale : prefab.transform.localScale;

            prop.SetActive(true);

            return prop;
        }
    }
}