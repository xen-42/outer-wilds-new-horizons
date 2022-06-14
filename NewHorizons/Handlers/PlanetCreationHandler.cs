using NewHorizons.Builder.Atmosphere;
using NewHorizons.Builder.Body;
using NewHorizons.Builder.General;
using NewHorizons.Builder.Orbital;
using NewHorizons.Builder.Props;
using NewHorizons.Components;
using NewHorizons.Components.Orbital;
using NewHorizons.External.Modules;
using NewHorizons.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Logger = NewHorizons.Utility.Logger;

namespace NewHorizons.Handlers
{
    public static class PlanetCreationHandler
    {
        public static List<NewHorizonsBody> NextPassBodies = new List<NewHorizonsBody>();

        // I literally forget what this is for
        private static Dictionary<AstroObject, NewHorizonsBody> ExistingAOConfigs;

        private static Dictionary<NHAstroObject, NewHorizonsBody> _dict;

        public static void Init(List<NewHorizonsBody> bodies)
        {
            Main.FurthestOrbit = 30000;

            ExistingAOConfigs = new Dictionary<AstroObject, NewHorizonsBody>();
            _dict = new Dictionary<NHAstroObject, NewHorizonsBody>();

            // Set up stars
            // Need to manage this when there are multiple stars
            var sun = SearchUtilities.Find("Sun_Body");
            var starController = sun.AddComponent<StarController>();
            starController.Light = SearchUtilities.Find("Sun_Body/Sector_SUN/Effects_SUN/SunLight").GetComponent<Light>();
            starController.AmbientLight = SearchUtilities.Find("Sun_Body/AmbientLight_SUN").GetComponent<Light>();
            starController.FaceActiveCamera = SearchUtilities.Find("Sun_Body/Sector_SUN/Effects_SUN/SunLight").GetComponent<FaceActiveCamera>();
            starController.CSMTextureCacher = SearchUtilities.Find("Sun_Body/Sector_SUN/Effects_SUN/SunLight").GetComponent<CSMTextureCacher>();
            starController.ProxyShadowLight = SearchUtilities.Find("Sun_Body/Sector_SUN/Effects_SUN/SunLight").GetComponent<ProxyShadowLight>();
            starController.Intensity = 0.9859f;
            starController.SunColor = new Color(1f, 0.8845f, 0.6677f, 1f);

            var starLightGO = GameObject.Instantiate(sun.GetComponentInChildren<SunLightController>().gameObject);
            foreach (var comp in starLightGO.GetComponents<Component>())
            {
                if (!(comp is SunLightController) && !(comp is SunLightParamUpdater) && !(comp is Light) && !(comp is Transform))
                {
                    GameObject.Destroy(comp);
                }
            }
            GameObject.Destroy(starLightGO.GetComponent<Light>());
            starLightGO.name = "StarLightController";

            starLightGO.AddComponent<StarLightController>();
            StarLightController.AddStar(starController);

            starLightGO.SetActive(true);

            // Load all planets
            var toLoad = bodies.ToList();
            var newPlanetGraph = new PlanetGraphHandler(toLoad);

            foreach (var node in newPlanetGraph)
            {
                LoadBody(node.body);
                toLoad.Remove(node.body);

                if (node is PlanetGraphHandler.FocalPointNode focal)
                {
                    LoadBody(focal.primary.body);
                    LoadBody(focal.secondary.body);

                    toLoad.Remove(focal.primary.body);
                    toLoad.Remove(focal.secondary.body);
                }
            }

            // Remaining planets are orphaned and either are stock bodies or just incorrectly set up
            var planetGraphs = PlanetGraphHandler.ConstructStockGraph(toLoad.ToArray());

            foreach (var planetGraph in planetGraphs)
            {
                foreach (var node in planetGraph)
                {
                    LoadBody(node.body);
                    toLoad.Remove(node.body);

                    if (node is PlanetGraphHandler.FocalPointNode focal)
                    {
                        LoadBody(focal.primary.body);
                        LoadBody(focal.secondary.body);

                        toLoad.Remove(focal.primary.body);
                        toLoad.Remove(focal.secondary.body);
                    }
                }
            }

            // Are there more?
            foreach (var body in toLoad)
            {
                LoadBody(body);
            }

            Logger.Log("Loading Deferred Bodies");

            // Make a copy of the next pass of bodies so that the array can be edited while we load them
            toLoad = NextPassBodies.Select(x => x).ToList();
            while (NextPassBodies.Count != 0)
            {
                foreach (var body in toLoad)
                {
                    LoadBody(body, true);
                }
                toLoad = NextPassBodies;
                NextPassBodies = new List<NewHorizonsBody>();
            }

            Logger.Log("Done loading bodies");

            // Main.Instance.ModHelper.Events.Unity.FireOnNextUpdate(PlanetDestroyer.RemoveAllProxies);

            if (Main.SystemDict[Main.Instance.CurrentStarSystem].Config.destroyStockPlanets) PlanetDestructionHandler.RemoveSolarSystem();
        }

        public static bool LoadBody(NewHorizonsBody body, bool defaultPrimaryToSun = false)
        {
            // I don't remember doing this why is it exceptions what am I doing
            GameObject existingPlanet = null;
            try
            {
                existingPlanet = AstroObjectLocator.GetAstroObject(body.Config.name).gameObject;
            }
            catch (Exception)
            {
                if (body?.Config?.name == null) Logger.LogError($"How is there no name for {body}");
                else existingPlanet = SearchUtilities.Find(body.Config.name.Replace(" ", "") + "_Body", false);
            }

            if (existingPlanet != null)
            {
                try
                {
                    if (body.Config.destroy)
                    {
                        var ao = existingPlanet.GetComponent<AstroObject>();
                        if (ao != null) Main.Instance.ModHelper.Events.Unity.FireInNUpdates(() => PlanetDestructionHandler.RemoveBody(ao), 2);
                        else Main.Instance.ModHelper.Events.Unity.FireInNUpdates(() => existingPlanet.SetActive(false), 2);
                    }
                    else if (body.Config.isQuantumState)
                    {
                        try
                        {
                            var quantumPlanet = existingPlanet.GetComponent<QuantumPlanet>();
                            if (quantumPlanet == null)
                            {
                                // Have to also add the root orbit and sector
                                quantumPlanet = existingPlanet.AddComponent<QuantumPlanet>();
                                var ao = quantumPlanet.GetComponent<NHAstroObject>();

                                var rootSector = quantumPlanet.GetComponentInChildren<Sector>();
                                var groundOrbit = _dict[ao].Config.Orbit;

                                quantumPlanet.groundState = new QuantumPlanet.State(rootSector, groundOrbit);
                                quantumPlanet.states.Add(quantumPlanet.groundState);

                                var visibilityTracker = new GameObject("VisibilityTracker_Sphere");
                                visibilityTracker.transform.parent = existingPlanet.transform;
                                visibilityTracker.transform.localPosition = Vector3.zero;
                                var sphere = visibilityTracker.AddComponent<SphereShape>();
                                sphere.radius = GetSphereOfInfluence(_dict[ao]);
                                var tracker = visibilityTracker.AddComponent<ShapeVisibilityTracker>();
                                quantumPlanet._visibilityTrackers = new VisibilityTracker[] { tracker };
                            }

                            var rb = existingPlanet.GetComponent<OWRigidbody>();

                            var sector = MakeSector.Make(existingPlanet, rb, GetSphereOfInfluence(body));
                            sector.name = $"Sector-{existingPlanet.GetComponentsInChildren<Sector>().Count()}";

                            SharedGenerateBody(body, existingPlanet, sector, rb);

                            // If nothing was generated then forget the sector
                            if (sector.transform.childCount == 0) sector = quantumPlanet.groundState.sector;

                            // If semimajor axis is 0 then forget the orbit
                            var orbit = body.Config.Orbit.semiMajorAxis == 0 ? quantumPlanet.groundState.orbit : body.Config.Orbit;

                            quantumPlanet.states.Add(new QuantumPlanet.State(sector, orbit));
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"Couldn't make quantum state for [{body.Config.name}] : {ex.Message}, {ex.StackTrace}");
                            return false;
                        }
                    }
                    else
                    {
                        UpdateBody(body, existingPlanet);
                    }
                }
                catch (Exception e)
                {
                    Logger.LogError($"Couldn't update body {body.Config?.name}: {e.Message}, {e.StackTrace}");
                    return false;
                }
            }
            else
            {
                if (body.Config.isQuantumState)
                {
                    // If the ground state object isn't made yet do it later
                    NextPassBodies.Add(body);
                }
                else
                {
                    try
                    {
                        GameObject planetObject = GenerateBody(body, defaultPrimaryToSun);
                        if (planetObject == null) return false;
                        planetObject.SetActive(true);
                        _dict.Add(planetObject.GetComponent<NHAstroObject>(), body);
                    }
                    catch (Exception e)
                    {
                        Logger.LogError($"Couldn't generate body {body.Config?.name}: {e.Message}, {e.StackTrace}");
                        return false;
                    }
                }
            }
            return true;
        }

        // Called when updating an existing planet
        public static GameObject UpdateBody(NewHorizonsBody body, GameObject go)
        {
            Logger.Log($"Updating existing Object {go.name}");

            var sector = go.GetComponentInChildren<Sector>();
            var rb = go.GetAttachedOWRigidbody();

            // Since orbits are always there just check if they set a semi major axis
            if (body.Config.Orbit != null && body.Config.Orbit.semiMajorAxis != 0f)
            {
                UpdateBodyOrbit(body, go);
            }

            if (body.Config.removeChildren != null && body.Config.removeChildren.Length > 0)
            {
                foreach (var child in body.Config.removeChildren)
                {
                    Main.Instance.ModHelper.Events.Unity.FireInNUpdates(() => SearchUtilities.Find(go.name + "/" + child)?.SetActive(false), 2);
                }
            }

            // Do stuff that's shared between generating new planets and updating old ones
            go = SharedGenerateBody(body, go, sector, rb);

            return go;
        }

        // Only called when making new planets
        public static GameObject GenerateBody(NewHorizonsBody body, bool defaultPrimaryToSun = false)
        {
            AstroObject primaryBody;
            if (body.Config.Orbit.primaryBody != null)
            {
                primaryBody = AstroObjectLocator.GetAstroObject(body.Config.Orbit.primaryBody);
                if (primaryBody == null)
                {
                    if (defaultPrimaryToSun)
                    {
                        Logger.Log($"Couldn't find {body.Config.Orbit.primaryBody}, defaulting to Sun");
                        primaryBody = AstroObjectLocator.GetAstroObject("Sun");
                    }
                    else
                    {
                        NextPassBodies.Add(body);
                        return null;
                    }
                }
            }
            else
            {
                primaryBody = null;
            }

            Logger.Log($"Begin generation sequence of [{body.Config.name}]");

            var go = new GameObject(body.Config.name.Replace(" ", "").Replace("'", "") + "_Body");
            go.SetActive(false);

            var owRigidBody = RigidBodyBuilder.Make(go, body.Config);
            var ao = AstroObjectBuilder.Make(go, primaryBody, body.Config);

            var sphereOfInfluence = GetSphereOfInfluence(body);

            var sector = MakeSector.Make(go, owRigidBody, sphereOfInfluence * 2f);
            ao._rootSector = sector;

            if (body.Config.Base.surfaceGravity != 0)
            {
                GravityBuilder.Make(go, ao, owRigidBody, body.Config);
            }

            RFVolumeBuilder.Make(go, owRigidBody, sphereOfInfluence, body.Config.ReferenceFrame);

            if (body.Config.Base.hasMapMarker)
            {
                MarkerBuilder.Make(go, body.Config.name, body.Config);
            }

            VolumesBuilder.Make(go, owRigidBody, body.Config, sphereOfInfluence);

            if (body.Config.FocalPoint != null)
            {
                FocalPointBuilder.Make(go, ao, body.Config, body.Mod);
            }

            // Do stuff that's shared between generating new planets and updating old ones
            go = SharedGenerateBody(body, go, sector, owRigidBody);

            body.Object = go;

            // Now that we're done move the planet into place
            UpdatePosition(go, body.Config.Orbit, primaryBody, ao);

            // Have to do this after setting position
            var initialMotion = InitialMotionBuilder.Make(go, primaryBody, ao, owRigidBody, body.Config.Orbit);

            // Spawning on other planets is a bit hacky so we do it last
            if (body.Config.Spawn != null)
            {
                Logger.Log("Doing spawn point thing");
                Main.SystemDict[body.Config.starSystem].SpawnPoint = SpawnPointBuilder.Make(go, body.Config.Spawn, owRigidBody);
            }

            if (body.Config.Orbit.showOrbitLine && !body.Config.Orbit.isStatic)
            {
                Main.Instance.ModHelper.Events.Unity.FireOnNextUpdate(() => OrbitlineBuilder.Make(body.Object, ao as NHAstroObject, body.Config.Orbit.isMoon, body.Config));
            }

            if (!body.Config.Orbit.isStatic)
            {
                DetectorBuilder.Make(go, owRigidBody, primaryBody, ao, body.Config);
            }

            if (ao.GetAstroObjectName() == AstroObject.Name.CustomString)
            {
                AstroObjectLocator.RegisterCustomAstroObject(ao);
            }

            Main.Instance.ModHelper.Events.Unity.FireOnNextUpdate(() =>
            {
                ProxyBuilder.Make(go, body);
            });

            return go;
        }

        private static float GetSphereOfInfluence(NewHorizonsBody body)
        {
            var atmoSize = body.Config.Atmosphere != null ? body.Config.Atmosphere.size : 0f;
            float sphereOfInfluence = Mathf.Max(Mathf.Max(atmoSize, 50), body.Config.Base.surfaceSize * 2f);
            var overrideSOI = body.Config.Base.sphereOfInfluence;
            if (overrideSOI != 0) sphereOfInfluence = overrideSOI;
            return sphereOfInfluence;
        }

        // What is called both on existing planets and new planets
        private static GameObject SharedGenerateBody(NewHorizonsBody body, GameObject go, Sector sector, OWRigidbody rb)
        {
            var sphereOfInfluence = GetSphereOfInfluence(body);

            if (body.Config.Base.ambientLight != 0)
            {
                AmbientLightBuilder.Make(go, sector, sphereOfInfluence, body.Config.Base.ambientLight);
            }

            if (body.Config.Base.groundSize != 0)
            {
                GeometryBuilder.Make(go, sector, body.Config.Base.groundSize);
            }

            if (body.Config.HeightMap != null)
            {
                HeightMapBuilder.Make(go, sector, body.Config.HeightMap, body.Mod);
            }

            if (body.Config.ProcGen != null)
            {
                ProcGenBuilder.Make(go, sector, body.Config.ProcGen);
            }

            if (body.Config.Star != null)
            {
                StarLightController.AddStar(StarBuilder.Make(go, sector, body.Config.Star));
            }

            if (body.Config.Ring != null)
            {
                RingBuilder.Make(go, sector, body.Config.Ring, body.Mod);
            }

            if (body.Config.AsteroidBelt != null)
            {
                AsteroidBeltBuilder.Make(body.Config.name, body.Config, body.Mod);
            }

            if (body.Config.Base.hasCometTail)
            {
                CometTailBuilder.Make(go, sector, body.Config);
            }

            if (body.Config.Lava != null)
            {
                LavaBuilder.Make(go, sector, rb, body.Config.Lava);
            }

            if (body.Config.Water != null)
            {
                WaterBuilder.Make(go, sector, rb, body.Config.Water);
            }

            if (body.Config.Sand != null)
            {
                SandBuilder.Make(go, sector, rb, body.Config.Sand);
            }

            if (body.Config.Atmosphere != null)
            {
                var airInfo = new AtmosphereModule.AirInfo()
                {
                    hasOxygen = body.Config.Atmosphere.hasOxygen,
                    isRaining = body.Config.Atmosphere.hasRain,
                    isSnowing = body.Config.Atmosphere.hasSnow,
                    scale = body.Config.Atmosphere.size
                };

                var surfaceSize = body.Config.Base.surfaceSize;

                AirBuilder.Make(go, sector, airInfo);

                if (!string.IsNullOrEmpty(body.Config.Atmosphere?.clouds?.texturePath))
                {
                    CloudsBuilder.Make(go, sector, body.Config.Atmosphere, body.Mod);
                    SunOverrideBuilder.Make(go, sector, body.Config.Atmosphere, surfaceSize);
                }

                if (body.Config.Atmosphere.hasRain || body.Config.Atmosphere.hasSnow)
                    EffectsBuilder.Make(go, sector, airInfo, surfaceSize);

                if (body.Config.Atmosphere.fogSize != 0)
                    FogBuilder.Make(go, sector, body.Config.Atmosphere);

                AtmosphereBuilder.Make(go, sector, body.Config.Atmosphere, surfaceSize);
            }

            if (body.Config.Props != null)
            {
                PropBuildManager.Make(go, sector, rb, body.Config, body.Mod);
            }

            if (body.Config.Signal != null)
            {
                SignalBuilder.Make(go, sector, body.Config.Signal, body.Mod);
            }

            if (body.Config.Singularity != null)
            {
                SingularityBuilder.Make(go, sector, rb, body.Config);
            }

            if (body.Config.Funnel != null)
            {
                FunnelBuilder.Make(go, go.GetComponentInChildren<ConstantForceDetector>(), rb, body.Config.Funnel);
            }

            // Has to go last probably
            if (body.Config.Cloak != null && body.Config.Cloak.radius != 0f)
            {
                CloakBuilder.Make(go, sector, rb, body.Config.Cloak, !body.Config.ReferenceFrame.hideInMap, body.Mod);
            }

            return go;
        }

        public static void UpdateBodyOrbit(NewHorizonsBody body, GameObject go)
        {
            Logger.Log($"Updating orbit of [{body.Config.name}]");

            try
            {
                var ao = go.GetComponent<AstroObject>();
                var aoName = ao.GetAstroObjectName();
                var aoType = ao.GetAstroObjectType();

                var owrb = go.GetComponent<OWRigidbody>();

                var im = go.GetComponent<InitialMotion>();

                // By default keep it with the same primary body else update to the new one
                var primary = ao._primaryBody;
                if (!string.IsNullOrEmpty(body.Config.Orbit.primaryBody))
                {
                    // If we can't find the new one we want to try again later (return false)
                    primary = AstroObjectLocator.GetAstroObject(body.Config.Orbit.primaryBody);
                    if (primary == null) return;
                }

                // Just destroy the existing AO after copying everything over
                var newAO = AstroObjectBuilder.Make(go, primary, body.Config);
                newAO._gravityVolume = ao._gravityVolume;
                newAO._moon = ao._moon;
                newAO._name = ao._name;
                newAO._owRigidbody = ao._owRigidbody;
                newAO._rootSector = ao._rootSector;
                newAO._sandLevelController = ao._sandLevelController;
                newAO._satellite = ao._satellite;
                newAO._type = ao._type;

                // We need these for later
                var children = AstroObjectLocator.GetChildren(ao).Concat(AstroObjectLocator.GetMoons(ao)).ToArray();
                AstroObjectLocator.DeregisterCustomAstroObject(ao);
                GameObject.Destroy(ao);
                Locator.RegisterAstroObject(newAO);
                AstroObjectLocator.RegisterCustomAstroObject(newAO);

                newAO._primaryBody = primary;

                // Since we destroyed the AO we have to replace links to it in other places
                newAO.gameObject.GetComponentInChildren<ReferenceFrameVolume>()._referenceFrame._attachedAstroObject = newAO;

                GameObject.Destroy(go.GetComponentInChildren<OrbitLine>().gameObject);
                var isMoon = newAO.GetAstroObjectType() == AstroObject.Type.Moon || newAO.GetAstroObjectType() == AstroObject.Type.Satellite;
                if (body.Config.Orbit.showOrbitLine) OrbitlineBuilder.Make(go, newAO, isMoon, body.Config);

                DetectorBuilder.SetDetector(primary, newAO, go.GetComponentInChildren<ConstantForceDetector>());

                // Get ready to move all the satellites
                var relativeMoonPositions = children.Select(x => x.transform.position - go.transform.position).ToArray();

                // If its tidally locked change the alignment
                var alignment = go.GetComponent<AlignWithTargetBody>();
                if (alignment != null)
                {
                    alignment.SetTargetBody(primary.GetComponent<OWRigidbody>());
                }

                // Move the primary
                UpdatePosition(go, body.Config.Orbit, primary, newAO);

                for (int i = 0; i < children.Count(); i++)
                {
                    var child = children[i];

                    // If the child is an AO we do stuff too
                    var childAO = child.GetComponent<NHAstroObject>() ?? child.GetComponent<AstroObject>();
                    if (childAO != null)
                    {
                        if (childAO is NHAstroObject && ExistingAOConfigs.ContainsKey(childAO))
                        {
                            // If it's already and NH object we repeat the whole process else it doesn't work idk
                            NextPassBodies.Add(ExistingAOConfigs[childAO]);
                        }
                        else
                        {
                            foreach (var childChild in AstroObjectLocator.GetChildren(childAO))
                            {
                                if (childChild == null) continue;
                                var dPos = childChild.transform.position - child.transform.position;
                                childChild.transform.position = go.transform.position + relativeMoonPositions[i] + dPos;
                            }
                            // Make sure the moons get updated to the new AO
                            childAO._primaryBody = newAO;
                        }
                    }

                    child.transform.position = go.transform.position + relativeMoonPositions[i];
                }

                // Have to do this after setting position
                InitialMotionBuilder.SetInitialMotion(im, primary, newAO);

                // Have to register this new AO to the locator
                Locator.RegisterAstroObject(newAO);

                ExistingAOConfigs.Add(newAO, body);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Couldn't update orbit of [{body.Config.name}]: {ex.Message}, {ex.StackTrace}");
                // If it doesn't work here there's no point trying again so we'll still return true
            }

            return;
        }

        public static void UpdatePosition(GameObject go, IOrbitalParameters orbit, AstroObject primaryBody, AstroObject secondaryBody)
        {
            Logger.Log($"Placing [{secondaryBody?.name}] around [{primaryBody?.name}]");

            go.transform.parent = Locator.GetRootTransform();

            if (primaryBody != null)
            {
                var primaryGravity = new Gravity(primaryBody.GetGravityVolume());
                var secondaryGravity = new Gravity(secondaryBody.GetGravityVolume());

                go.transform.position = orbit.GetOrbitalParameters(primaryGravity, secondaryGravity, primaryBody, secondaryBody).InitialPosition + primaryBody.transform.position;
            }
            else
            {
                go.transform.position = Vector3.zero;
            }

            if (go.transform.position.magnitude > Main.FurthestOrbit)
            {
                Main.FurthestOrbit = go.transform.position.magnitude + 30000f;
            }
        }
    }
}
