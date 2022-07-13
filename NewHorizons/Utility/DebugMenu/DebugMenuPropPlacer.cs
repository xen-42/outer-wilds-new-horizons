using NewHorizons.External.Configs;
using NewHorizons.External.Modules;
using NewHorizons.Utility.DebugUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace NewHorizons.Utility.DebugMenu
{
    class DebugMenuPropPlacer : DebugSubmenu
    {
        private HashSet<string> favoriteProps = new HashSet<string>();
        private List<string> propsLoadedFromConfig = new List<string>();
        public static readonly char separatorCharacter = '☧'; // since no chars are illegal in game object names, I picked one that's extremely unlikely to be used to be a separator
        private static readonly string favoritePropsPlayerPrefKey = "FavoriteProps";

        internal DebugPropPlacer _dpp;
        internal DebugRaycaster _drc;

        // misc
        private GameObject mostRecentlyPlacedProp;
        private Vector3 mostRecentlyPlacedPropSphericalPos;

        // menu params
        private Vector2 recentPropsScrollPosition = Vector2.zero;
        private bool propsCollapsed = false;
        private bool propPositioningCollapsed = false;
        private Vector3 propPosDelta = new Vector3(0.1f, 0.1f, 0.1f);
        private Vector3 propRotDelta = new Vector3(0.1f, 0.1f, 0.1f);
        private Vector3 propSphericalPosDelta = new Vector3(0.1f, 0.1f, 0.1f);
        private float propRotationAboutLocalUpDelta = 0.1f;
        private float propScaleDelta = 0.1f;

        internal override string SubmenuName()
        {
            return "Prop Placer";
        }

        internal override void OnInit(DebugMenu menu)
        {
            _dpp = menu.GetComponent<DebugPropPlacer>();
            _drc = menu.GetComponent<DebugRaycaster>();
        }

        internal override void OnAwake(DebugMenu menu)
        {
            _dpp = menu.GetComponent<DebugPropPlacer>();
            _drc = menu.GetComponent<DebugRaycaster>();
            LoadFavoriteProps();
        }

        internal override void OnBeginLoadMod(DebugMenu debugMenu)
        {

        }

        internal override void GainActive()
        {
            DebugPropPlacer.active = true;
        }

        internal override void LoseActive()
        {
            DebugPropPlacer.active = false;
        }

        internal override void LoadConfigFile(DebugMenu menu, PlanetConfig config)
        {
            _dpp.FindAndRegisterPropsFromConfig(config, propsLoadedFromConfig);
        }

        private void LoadFavoriteProps()
        {
            string favoritePropsPlayerPref = PlayerPrefs.GetString(favoritePropsPlayerPrefKey);

            if (favoritePropsPlayerPref == null || favoritePropsPlayerPref == "") return;

            var favoritePropPaths = favoritePropsPlayerPref.Split(separatorCharacter);
            foreach (string favoriteProp in favoritePropPaths)
            {
                DebugPropPlacer.RecentlyPlacedProps.Add(favoriteProp);
                this.favoriteProps.Add(favoriteProp);
            }
        }

        internal override void OnGUI(DebugMenu menu)
        {
            //
            // DebugPropPlacer
            // 
            GUILayout.Label("Currently placing: ");
            _dpp.SetCurrentObject(GUILayout.TextArea(_dpp.currentObject));

            GUILayout.Space(5);
            GUILayout.Space(5);


            var arrow = propsCollapsed ? " > " : " v ";
            if (GUILayout.Button(arrow + "Recently placed objects", menu._tabBarStyle)) propsCollapsed = !propsCollapsed;
            if (!propsCollapsed) DrawPropsList(menu);
            GUILayout.Space(5);

            if (_dpp.mostRecentlyPlacedPropGO != null)
            {
                arrow = propPositioningCollapsed ? " > " : " v ";
                if (GUILayout.Button(arrow + "Position last placed prop", menu._tabBarStyle)) propPositioningCollapsed = !propPositioningCollapsed;
                if (!propPositioningCollapsed) DrawPropsAdustmentControls(menu);
            }
        }

        private void DrawPropsAdustmentControls(DebugMenu menu)
        {
            var propPath = _dpp.mostRecentlyPlacedPropPath;
            var propPathElements = propPath[propPath.Length - 1] == '/'
                ? propPath.Substring(0, propPath.Length - 1).Split('/')
                : propPath.Split('/');
            string propName = propPathElements[propPathElements.Length - 1];
            GUILayout.Label($"Reposition {propName}: ");


            Vector3 latestPropPosDelta = VectorInput(_dpp.mostRecentlyPlacedPropGO.transform.localPosition, propPosDelta, out propPosDelta, "x", "y", "z");
            _dpp.mostRecentlyPlacedPropGO.transform.localPosition += latestPropPosDelta;
            if (latestPropPosDelta != Vector3.zero) mostRecentlyPlacedPropSphericalPos = DeltaSphericalPosition(mostRecentlyPlacedProp, Vector3.zero);

            //GUILayout.Space(5);
            //Vector3 latestPropRotDelta = VectorInput(_dpp.mostRecentlyPlacedPropGO.transform.localEulerAngles, propRotDelta, out propRotDelta, "x", "y", "z");
            //_dpp.mostRecentlyPlacedPropGO.transform.localEulerAngles += latestPropRotDelta;

            GUILayout.Space(5);
            GUILayout.Space(5);


            if (mostRecentlyPlacedProp != _dpp.mostRecentlyPlacedPropGO)
            {
                mostRecentlyPlacedProp = _dpp.mostRecentlyPlacedPropGO;
                mostRecentlyPlacedPropSphericalPos = DeltaSphericalPosition(mostRecentlyPlacedProp, Vector3.zero);
            }

            Vector3 latestPropSphericalPosDelta = VectorInput(mostRecentlyPlacedPropSphericalPos, propSphericalPosDelta, out propSphericalPosDelta, "lat   ", "lon   ", "height");
            if (latestPropSphericalPosDelta != Vector3.zero)
            {
                SetSphericalPosition(mostRecentlyPlacedProp, mostRecentlyPlacedPropSphericalPos + latestPropSphericalPosDelta);
                mostRecentlyPlacedPropSphericalPos = mostRecentlyPlacedPropSphericalPos + latestPropSphericalPosDelta;
            }

            GUILayout.Space(5);
            GUILayout.Space(5);


            GUILayout.BeginHorizontal();
            GUILayout.Label("Rotate about up: ", GUILayout.Width(50));
            float deltaRot = 0;
            if (GUILayout.Button("+", GUILayout.ExpandWidth(false))) deltaRot += propRotationAboutLocalUpDelta;
            if (GUILayout.Button("-", GUILayout.ExpandWidth(false))) deltaRot -= propRotationAboutLocalUpDelta;
            propRotationAboutLocalUpDelta = float.Parse(GUILayout.TextField(propRotationAboutLocalUpDelta + "", GUILayout.Width(100)));

            if (deltaRot != 0)
            {
                Transform astroObject = mostRecentlyPlacedProp.transform.parent.parent;
                mostRecentlyPlacedProp.transform.RotateAround(mostRecentlyPlacedProp.transform.position, mostRecentlyPlacedProp.transform.up, deltaRot);
            }
            GUILayout.EndHorizontal();


            GUILayout.BeginHorizontal();
            GUILayout.Label("scale: ", GUILayout.Width(50));
            var scaleString = mostRecentlyPlacedProp.transform.localScale.x + "";
            var newScaleString = GUILayout.TextField(scaleString, GUILayout.Width(50));
            var parsedScaleString = mostRecentlyPlacedProp.transform.localScale.x; try { parsedScaleString = float.Parse(newScaleString); } catch { }
            float deltaScale = scaleString == newScaleString ? 0 : parsedScaleString - mostRecentlyPlacedProp.transform.localScale.x;
            if (GUILayout.Button("+", GUILayout.ExpandWidth(false))) deltaScale += propScaleDelta;
            if (GUILayout.Button("-", GUILayout.ExpandWidth(false))) deltaScale -= propScaleDelta;
            propScaleDelta = float.Parse(GUILayout.TextField(propScaleDelta + "", GUILayout.Width(100)));

            if (deltaScale != 0)
            {
                float newScale = mostRecentlyPlacedProp.transform.localScale.x + deltaScale;
                mostRecentlyPlacedProp.transform.localScale = new Vector3(newScale, newScale, newScale);
            }
            GUILayout.EndHorizontal();
        }
        private Vector3 DeltaSphericalPosition(GameObject prop, Vector3 deltaSpherical)
        {
            Transform astroObject = prop.transform.parent.parent;
            Transform sector = prop.transform.parent;
            Vector3 originalLocalPos = astroObject.InverseTransformPoint(prop.transform.position); // parent is the sector, this gives localPos relative to the astroobject (what the DetailBuilder asks for)
            Vector3 sphericalPos = CoordinateUtilities.CartesianToSpherical(originalLocalPos);

            if (deltaSpherical == Vector3.zero) return sphericalPos;
            Vector3 newSpherical = sphericalPos + deltaSpherical;

            Vector3 finalLocalPosition = CoordinateUtilities.SphericalToCartesian(newSpherical);
            Vector3 finalAbsolutePosition = astroObject.TransformPoint(finalLocalPosition);
            prop.transform.localPosition = prop.transform.parent.InverseTransformPoint(finalAbsolutePosition);
            // prop.transform.rotation = Quaternion.FromToRotation(originalLocalPos.normalized, finalLocalPosition.normalized) * prop.transform.rotation;

            // first, rotate the object by the astroObject's rotation, that means anything afterwards is relative to this rotation (ie, we can pretend the astroObject has 0 rotation)
            // then, rotate by the difference in position, basically accounting for the curvature of a sphere
            // then re-apply the local rotations of the hierarchy down to the prop (apply the sector local rotation, then the prop local rotation)

            // since we're doing all rotation relative to the astro object, we start with its absolute rotation
            // then we apply the rotation about the astroobject using FromTooRotation
            // then we reapply the local rotations down through the hierarchy
            prop.transform.rotation = astroObject.rotation * Quaternion.FromToRotation(originalLocalPos.normalized, finalLocalPosition.normalized) * sector.localRotation * prop.transform.localRotation;

            return newSpherical;
        }

        // DB_EscapePodDimension_Body/Sector_EscapePodDimension/Interactables_EscapePodDimension/InnerWarp_ToAnglerNest
        // DB_ExitOnlyDimension_Body/Sector_ExitOnlyDimension/Interactables_ExitOnlyDimension/InnerWarp_ToExitOnly  // need to change the colors
        // DB_HubDimension_Body/Sector_HubDimension/Interactables_HubDimension/InnerWarp_ToCluster   // need to delete the child "Signal_Harmonica"

        private Vector3 SetSphericalPosition(GameObject prop, Vector3 newSpherical)
        {
            Transform astroObject = prop.transform.parent.parent;
            Transform sector = prop.transform.parent;
            Vector3 originalLocalPos = astroObject.InverseTransformPoint(prop.transform.position); // parent is the sector, this gives localPos relative to the astroobject (what the DetailBuilder asks for)
            Vector3 sphericalPos = CoordinateUtilities.CartesianToSpherical(originalLocalPos);

            if (newSpherical == sphericalPos) return sphericalPos;

            Vector3 finalLocalPosition = CoordinateUtilities.SphericalToCartesian(newSpherical);
            Vector3 finalAbsolutePosition = astroObject.TransformPoint(finalLocalPosition);
            prop.transform.localPosition = prop.transform.parent.InverseTransformPoint(finalAbsolutePosition);
            Logger.Log("new position: " + prop.transform.localPosition);

            var onlyChangingRAndRIsNegative = false;

            // first, rotate the object by the astroObject's rotation, that means anything afterwards is relative to this rotation (ie, we can pretend the astroObject has 0 rotation)
            // then, rotate by the difference in position, basically accounting for the curvature of a sphere
            // then re-apply the local rotations of the hierarchy down to the prop (apply the sector local rotation, then the prop local rotation)

            // since we're doing all rotation relative to the astro object, we start with its absolute rotation
            // then we apply the rotation about the astroobject using FromTooRotation
            // then we reapply the local rotations down through the hierarchy
            Vector3 originalLocalPos_ForcedPositiveR = CoordinateUtilities.SphericalToCartesian(new Vector3(sphericalPos.x, sphericalPos.y, Mathf.Abs(sphericalPos.z)));
            Vector3 finalLocalPos_ForcedPositiveR    = CoordinateUtilities.SphericalToCartesian(new Vector3(newSpherical.x, newSpherical.y, Mathf.Abs(newSpherical.z)));
            if (!onlyChangingRAndRIsNegative) prop.transform.rotation = astroObject.rotation * Quaternion.FromToRotation(originalLocalPos_ForcedPositiveR.normalized, finalLocalPos_ForcedPositiveR.normalized) * sector.localRotation * prop.transform.localRotation;

            return newSpherical;
        }

        private Vector3 VectorInput(Vector3 input, Vector3 deltaControls, out Vector3 deltaControlsOut, string labelX, string labelY, string labelZ)
        {
            var dx = deltaControls.x;
            var dy = deltaControls.y;
            var dz = deltaControls.z;

            // x
            GUILayout.BeginHorizontal();
            GUILayout.Label(labelX + ":     ", GUILayout.Width(50));
            var xString = input.x + "";
            var newXString = GUILayout.TextField(xString, GUILayout.Width(50));
            var parsedXString = input.x; try { parsedXString = float.Parse(newXString); } catch { }
            float deltaX = xString == newXString ? 0 : parsedXString - input.x;
            if (GUILayout.Button("+", GUILayout.ExpandWidth(false))) deltaX += dx;
            if (GUILayout.Button("-", GUILayout.ExpandWidth(false))) deltaX -= dx;
            dx = float.Parse(GUILayout.TextField(dx + "", GUILayout.Width(100)));
            GUILayout.EndHorizontal();

            // y
            GUILayout.BeginHorizontal();
            GUILayout.Label(labelY + ":     ", GUILayout.Width(50));
            var yString = input.y + "";
            var newYString = GUILayout.TextField(yString, GUILayout.Width(50));
            var parsedYString = input.y; try { parsedYString = float.Parse(newYString); } catch { }
            float deltaY = yString == newYString ? 0 : parsedYString - input.y;
            if (GUILayout.Button("+", GUILayout.ExpandWidth(false))) deltaY += dy;
            if (GUILayout.Button("-", GUILayout.ExpandWidth(false))) deltaY -= dy;
            dy = float.Parse(GUILayout.TextField(dy + "", GUILayout.Width(100)));
            GUILayout.EndHorizontal();

            // z
            GUILayout.BeginHorizontal();
            GUILayout.Label(labelZ + ":     ", GUILayout.Width(50));
            var zString = input.z + "";
            var newZString = GUILayout.TextField(zString, GUILayout.Width(50));
            var parsedZString = input.z; try { parsedZString = float.Parse(newZString); } catch { }
            float deltaZ = zString == newZString ? 0 : parsedZString - input.z;
            if (GUILayout.Button("+", GUILayout.ExpandWidth(false))) deltaZ += dz;
            if (GUILayout.Button("-", GUILayout.ExpandWidth(false))) deltaZ -= dz;
            dz = float.Parse(GUILayout.TextField(dz + "", GUILayout.Width(100)));
            GUILayout.EndHorizontal();

            deltaControlsOut = new Vector3(dx, dy, dz);
            return new Vector3(deltaX, deltaY, deltaZ);
        }

        private void DrawPropsList(DebugMenu menu)
        {
            // List of recently placed objects
            recentPropsScrollPosition = GUILayout.BeginScrollView(recentPropsScrollPosition, GUILayout.Width(menu.EditorMenuSize.x), GUILayout.Height(500));
            foreach (string propPath in DebugPropPlacer.RecentlyPlacedProps)
            {
                GUILayout.BeginHorizontal();

                var propPathElements = propPath[propPath.Length - 1] == '/'
                    ? propPath.Substring(0, propPath.Length - 1).Split('/')
                    : propPath.Split('/');
                string propName = propPathElements[propPathElements.Length - 1];

                string favoriteButtonIcon = favoriteProps.Contains(propPath) ? "★" : "☆";
                if (GUILayout.Button(favoriteButtonIcon, GUILayout.ExpandWidth(false)))
                {
                    if (favoriteProps.Contains(propPath))
                    {
                        favoriteProps.Remove(propPath);
                    }
                    else
                    {
                        favoriteProps.Add(propPath);
                    }

                    string[] favoritePropsArray = favoriteProps.ToArray<string>();
                    PlayerPrefs.SetString(favoritePropsPlayerPrefKey, string.Join(separatorCharacter + "", favoritePropsArray));
                }

                if (GUILayout.Button(propName))
                {
                    _dpp.SetCurrentObject(propPath);
                }

                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
        }

        internal override void PreSave(DebugMenu menu)
        {
            UpdateLoadedConfigsForRecentSystem(menu);
        }

        private void UpdateLoadedConfigsForRecentSystem(DebugMenu menu)
        {
            var newDetails = _dpp.GetPropsConfigByBody();

            var newDetailsCountsByPlanet = string.Join(", ", newDetails.Keys.Select(x => $"{x.name} ({newDetails[x].Length})"));
            Logger.Log($"Updating config files. New Details Counts by planet: {newDetailsCountsByPlanet}");

            var planetToConfigPath = new Dictionary<AstroObject, string>();

            // Get all configs
            foreach (var filePath in menu.loadedConfigFiles.Keys)
            {
                Logger.LogVerbose($"Potentially updating copy of config at {filePath}");
                Logger.LogVerbose($"{menu.loadedConfigFiles[filePath].name} {AstroObjectLocator.GetAstroObject(menu.loadedConfigFiles[filePath].name)?.name}");
                Logger.LogVerbose($"{menu.loadedConfigFiles[filePath].name}");

                if (menu.loadedConfigFiles[filePath].starSystem != Main.Instance.CurrentStarSystem) return;
                if (menu.loadedConfigFiles[filePath].name == null || AstroObjectLocator.GetAstroObject(menu.loadedConfigFiles[filePath].name) == null) 
                { 
                    Logger.LogWarning("Failed to update copy of config at " + filePath); 
                    continue; 
                }

                var astroObject = AstroObjectLocator.GetAstroObject(menu.loadedConfigFiles[filePath].name);
                planetToConfigPath[astroObject] = filePath;

                if (!newDetails.ContainsKey(astroObject)) continue;

                if (menu.loadedConfigFiles[filePath].Props == null) menu.loadedConfigFiles[filePath].Props = new PropModule();
                menu.loadedConfigFiles[filePath].Props.details = newDetails[astroObject];

                Logger.Log($"Successfully updated copy of config file for {astroObject.name}");
            }

            // find all new planets that do not yet have config paths
            var planetsThatDoNotHaveConfigFiles = newDetails.Keys.Where(x => !planetToConfigPath.ContainsKey(x)).ToList();
            foreach (var astroObject in planetsThatDoNotHaveConfigFiles)
            {
                Logger.Log("Fabricating new config file for " + astroObject.name);

                var filepath = $"planets/{Main.Instance.CurrentStarSystem}/{astroObject.name}.json";
                
                var config = new PlanetConfig();
                config.starSystem = Main.Instance.CurrentStarSystem;
                config.name = astroObject._name == AstroObject.Name.CustomString ? astroObject.GetCustomName() : astroObject._name.ToString();
                config.Props = new PropModule();
                config.Props.details = newDetails[astroObject];

                menu.loadedConfigFiles[filepath] = config;
            }
        }
    }
}
