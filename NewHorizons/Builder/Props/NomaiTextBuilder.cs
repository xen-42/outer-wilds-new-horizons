using NewHorizons.External.Modules;
using NewHorizons.Handlers;
using NewHorizons.Utility;
using OWML.Common;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using UnityEngine;
using Logger = NewHorizons.Utility.Logger;
using Random = UnityEngine.Random;
namespace NewHorizons.Builder.Props
{
    public static class NomaiTextBuilder
    {
        private static List<GameObject> _arcPrefabs;
        private static List<GameObject> _childArcPrefabs;
        private static List<GameObject> _ghostArcPrefabs;
        private static GameObject _scrollPrefab;
        private static GameObject _computerPrefab;
        private static GameObject _cairnPrefab;
        private static GameObject _recorderPrefab;
        private static GameObject _preCrashRecorderPrefab;

        private static Dictionary<PropModule.NomaiTextArcInfo, GameObject> arcInfoToCorrespondingSpawnedGameObject = new Dictionary<PropModule.NomaiTextArcInfo, GameObject>();
        public static GameObject GetSpawnedGameObjectByNomaiTextArcInfo(PropModule.NomaiTextArcInfo arc)
        {
            if (!arcInfoToCorrespondingSpawnedGameObject.ContainsKey(arc)) return null;
            return arcInfoToCorrespondingSpawnedGameObject[arc];
        }

        private static Dictionary<PropModule.NomaiTextInfo, GameObject> conversationInfoToCorrespondingSpawnedGameObject = new Dictionary<PropModule.NomaiTextInfo, GameObject>();
        
        public static GameObject GetSpawnedGameObjectByNomaiTextInfo(PropModule.NomaiTextInfo convo)
        {
            Logger.LogVerbose("Retrieving wall text obj for " + convo);
            if (!conversationInfoToCorrespondingSpawnedGameObject.ContainsKey(convo)) return null;
            return conversationInfoToCorrespondingSpawnedGameObject[convo];
        }

        public static List<GameObject> GetArcPrefabs() { return _arcPrefabs; }
        public static List<GameObject> GetChildArcPrefabs() { return _childArcPrefabs; }
        public static List<GameObject> GetGhostArcPrefabs() { return _ghostArcPrefabs; }

        private static void InitPrefabs()
        {
            // Just take every scroll and get the first arc
            var existingArcs = GameObject.FindObjectsOfType<ScrollItem>().Select(x => x?._nomaiWallText?.gameObject?.transform?.Find("Arc 1")?.gameObject).Where(x => x != null).ToArray();
            _arcPrefabs = new List<GameObject>();
            _childArcPrefabs = new List<GameObject>();
            foreach (var existingArc in existingArcs)
            {
                if (existingArc.GetComponent<MeshRenderer>().material.name.Contains("Child"))
                {
                    var arc = existingArc.InstantiateInactive();
                    arc.name = "Arc (Child)";
                    _childArcPrefabs.Add(arc);
                }
                else
                {
                    var arc = existingArc.InstantiateInactive();
                    arc.name = "Arc";
                    _arcPrefabs.Add(arc);
                }
            }

            var existingGhostArcs = GameObject.FindObjectsOfType<GhostWallText>().Select(x => x?._textLine?.gameObject).Where(x => x != null).ToArray();
            _ghostArcPrefabs = new List<GameObject>();
            foreach (var existingArc in existingGhostArcs)
            {
                var arc = existingArc.InstantiateInactive();
                arc.name = "Arc";
                _ghostArcPrefabs.Add(arc);
            }

            _scrollPrefab = SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Sector_NorthHemisphere/Sector_NorthPole/Sector_HangingCity/Sector_HangingCity_District2/Interactables_HangingCity_District2/Prefab_NOM_Scroll").InstantiateInactive();
            _scrollPrefab.name = "Prefab_NOM_Scroll";

            _computerPrefab = SearchUtilities.Find("VolcanicMoon_Body/Sector_VM/Interactables_VM/Prefab_NOM_Computer").InstantiateInactive();
            _computerPrefab.name = "Prefab_NOM_Computer";
            _computerPrefab.transform.rotation = Quaternion.identity;

            _cairnPrefab = SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Sector_Crossroads/Interactables_Crossroads/Trailmarkers/Prefab_NOM_BH_Cairn_Arc (1)").InstantiateInactive();
            _cairnPrefab.name = "Prefab_NOM_Cairn";
            _cairnPrefab.transform.rotation = Quaternion.identity;

            _recorderPrefab = SearchUtilities.Find("Comet_Body/Prefab_NOM_Shuttle/Sector_NomaiShuttleInterior/Interactibles_NomaiShuttleInterior/Prefab_NOM_Recorder").InstantiateInactive();
            _recorderPrefab.name = "Prefab_NOM_Recorder";
            _recorderPrefab.transform.rotation = Quaternion.identity;

            _preCrashRecorderPrefab = SearchUtilities.Find("BrittleHollow_Body/Sector_BH/Sector_EscapePodCrashSite/Sector_CrashFragment/Interactables_CrashFragment/Prefab_NOM_Recorder").InstantiateInactive();
            _preCrashRecorderPrefab.name = "Prefab_NOM_Recorder_Vessel";
            _preCrashRecorderPrefab.transform.rotation = Quaternion.identity;
        }

        public static void Make(GameObject planetGO, Sector sector, PropModule.NomaiTextInfo info, IModBehaviour mod)
        {
            if (_scrollPrefab == null) InitPrefabs();

            var xmlPath = System.IO.File.ReadAllText(mod.ModHelper.Manifest.ModFolderPath + info.xmlFile);

            switch (info.type)
            {
                case PropModule.NomaiTextInfo.NomaiTextType.Wall:
                    {
                        var nomaiWallTextObj = MakeWallText(planetGO, sector, info, xmlPath).gameObject;

                        nomaiWallTextObj.transform.parent = sector?.transform ?? planetGO.transform;
                        nomaiWallTextObj.transform.position = planetGO.transform.TransformPoint(info.position);
                        if (info.normal != null)
                        {
                            // In global coordinates (normal was in local coordinates)
                            var up = (nomaiWallTextObj.transform.position - planetGO.transform.position).normalized;
                            var forward = planetGO.transform.TransformDirection(info.normal).normalized;

                            nomaiWallTextObj.transform.up = up;
                            nomaiWallTextObj.transform.forward = forward;
                        }
                        if (info.rotation != null)
                        {
                            nomaiWallTextObj.transform.rotation = planetGO.transform.TransformRotation(Quaternion.Euler(info.rotation));
                        }

                        nomaiWallTextObj.SetActive(true);
                        conversationInfoToCorrespondingSpawnedGameObject[info] = nomaiWallTextObj;
                        break;
                    }
                case PropModule.NomaiTextInfo.NomaiTextType.Scroll:
                    {
                        var customScroll = _scrollPrefab.InstantiateInactive();

                        var nomaiWallText = MakeWallText(planetGO, sector, info, xmlPath);
                        nomaiWallText.transform.parent = customScroll.transform;
                        nomaiWallText.transform.localPosition = Vector3.zero;
                        nomaiWallText.transform.localRotation = Quaternion.identity;

                        nomaiWallText._showTextOnStart = false;

                        // Don't want to be able to translate until its in a socket
                        nomaiWallText.GetComponent<Collider>().enabled = false;

                        nomaiWallText.gameObject.SetActive(true);

                        var scrollItem = customScroll.GetComponent<ScrollItem>();

                        // Idk why this thing is always around
                        GameObject.Destroy(customScroll.transform.Find("Arc_BH_City_Forum_2").gameObject);

                        // This variable is the bane of my existence i dont get it
                        scrollItem._nomaiWallText = nomaiWallText;

                        // Because the scroll was already awake it does weird shit in Awake and makes some of the entries in this array be null
                        scrollItem._colliders = new OWCollider[] { scrollItem.GetComponent<OWCollider>() };

                        // Else when you put them down you can't pick them back up
                        customScroll.GetComponent<OWCollider>()._physicsRemoved = false;

                        // Place scroll
                        customScroll.transform.parent = sector?.transform ?? planetGO.transform;
                        customScroll.transform.position = planetGO.transform.TransformPoint(info.position ?? Vector3.zero);

                        var up = planetGO.transform.InverseTransformPoint(customScroll.transform.position).normalized;
                        customScroll.transform.rotation = Quaternion.FromToRotation(customScroll.transform.up, up) * customScroll.transform.rotation;

                        customScroll.SetActive(true);

                        // Enable the collider and renderer
                        Main.Instance.ModHelper.Events.Unity.RunWhen(
                            () => Main.IsSystemReady,
                            () =>
                            {
                                Logger.LogVerbose("Fixing scroll!");
                                scrollItem._nomaiWallText = nomaiWallText;
                                scrollItem.SetSector(sector);
                                customScroll.transform.Find("Props_NOM_Scroll/Props_NOM_Scroll_Geo").GetComponent<MeshRenderer>().enabled = true;
                                customScroll.transform.Find("Props_NOM_Scroll/Props_NOM_Scroll_Collider").gameObject.SetActive(true);
                                nomaiWallText.gameObject.GetComponent<Collider>().enabled = false;
                                customScroll.GetComponent<CapsuleCollider>().enabled = true;
                            }
                        );
                        conversationInfoToCorrespondingSpawnedGameObject[info] = customScroll;
                        break;
                    }
                case PropModule.NomaiTextInfo.NomaiTextType.Computer:
                    {
                        var computerObject = _computerPrefab.InstantiateInactive();

                        computerObject.transform.parent = sector?.transform ?? planetGO.transform;
                        computerObject.transform.position = planetGO.transform.TransformPoint(info?.position ?? Vector3.zero);

                        var up = computerObject.transform.position - planetGO.transform.position;
                        if (info.normal != null) up = planetGO.transform.TransformDirection(info.normal);
                        computerObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, up) * computerObject.transform.rotation;

                        var computer = computerObject.GetComponent<NomaiComputer>();
                        computer.SetSector(sector);

                        computer._dictNomaiTextData = MakeNomaiTextDict(xmlPath);
                        computer._nomaiTextAsset = new TextAsset(xmlPath);
                        computer._nomaiTextAsset.name = Path.GetFileNameWithoutExtension(info.xmlFile);
                        AddTranslation(xmlPath);

                        // Make sure the computer model is loaded
                        StreamingHandler.SetUpStreaming(computerObject, sector);

                        computerObject.SetActive(true);
                        conversationInfoToCorrespondingSpawnedGameObject[info] = computerObject;
                        break;
                    }
                case PropModule.NomaiTextInfo.NomaiTextType.Cairn:
                    {
                        var cairnObject = _cairnPrefab.InstantiateInactive();

                        cairnObject.transform.parent = sector?.transform ?? planetGO.transform;
                        cairnObject.transform.position = planetGO.transform.TransformPoint(info?.position ?? Vector3.zero);

                        if (info.rotation != null)
                        {
                            cairnObject.transform.rotation = planetGO.transform.TransformRotation(Quaternion.Euler(info.rotation));
                        }
                        else
                        {
                            // By default align it to normal
                            var up = (cairnObject.transform.position - planetGO.transform.position).normalized;
                            cairnObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, up) * cairnObject.transform.rotation;
                        }

                        // Idk do we have to set it active before finding things?
                        cairnObject.SetActive(true);

                        // Make it do the thing when it finishes being knocked over
                        foreach (var rock in cairnObject.GetComponent<NomaiCairn>()._rocks)
                        {
                            rock._returning = false;
                            rock._owCollider.SetActivation(true);
                            rock.enabled = false;
                        }

                        // So we can actually knock it over
                        cairnObject.GetComponent<CapsuleCollider>().enabled = true;

                        var nomaiWallText = cairnObject.transform.Find("Props_TH_ClutterSmall/Arc_Short").GetComponent<NomaiWallText>();
                        nomaiWallText.SetSector(sector);

                        nomaiWallText._dictNomaiTextData = MakeNomaiTextDict(xmlPath);
                        nomaiWallText._nomaiTextAsset = new TextAsset(xmlPath);
                        nomaiWallText._nomaiTextAsset.name = Path.GetFileNameWithoutExtension(info.xmlFile);
                        AddTranslation(xmlPath);

                        // Make sure the computer model is loaded
                        StreamingHandler.SetUpStreaming(cairnObject, sector);
                        conversationInfoToCorrespondingSpawnedGameObject[info] = cairnObject;
                        break;
                    }
                case PropModule.NomaiTextInfo.NomaiTextType.PreCrashRecorder:
                case PropModule.NomaiTextInfo.NomaiTextType.Recorder:
                    {
                        var recorderObject = (info.type == PropModule.NomaiTextInfo.NomaiTextType.PreCrashRecorder ? _preCrashRecorderPrefab : _recorderPrefab).InstantiateInactive();

                        recorderObject.transform.parent = sector?.transform ?? planetGO.transform;
                        recorderObject.transform.position = planetGO.transform.TransformPoint(info?.position ?? Vector3.zero);

                        if (info.rotation != null)
                        {
                            recorderObject.transform.rotation = planetGO.transform.TransformRotation(Quaternion.Euler(info.rotation));
                        }
                        else
                        {
                            var up = recorderObject.transform.position - planetGO.transform.position;
                            recorderObject.transform.rotation = Quaternion.FromToRotation(Vector3.up, up) * recorderObject.transform.rotation;
                        }

                        var nomaiText = recorderObject.GetComponentInChildren<NomaiText>();
                        nomaiText.SetSector(sector);

                        nomaiText._dictNomaiTextData = MakeNomaiTextDict(xmlPath);
                        nomaiText._nomaiTextAsset = new TextAsset(xmlPath);
                        nomaiText._nomaiTextAsset.name = Path.GetFileNameWithoutExtension(info.xmlFile);
                        AddTranslation(xmlPath);

                        // Make sure the recorder model is loaded
                        StreamingHandler.SetUpStreaming(recorderObject, sector);

                        recorderObject.SetActive(true);

                        recorderObject.transform.Find("InteractSphere").gameObject.GetComponent<SphereShape>().enabled = true;
                        conversationInfoToCorrespondingSpawnedGameObject[info] = recorderObject;
                        break;
                    }
                default:
                    Logger.LogError($"Unsupported NomaiText type {info.type}");
                    break;
            }
        }

        private static NomaiWallText MakeWallText(GameObject go, Sector sector, PropModule.NomaiTextInfo info, string xmlPath)
        {
            GameObject nomaiWallTextObj = new GameObject("NomaiWallText");
            nomaiWallTextObj.SetActive(false);

            var box = nomaiWallTextObj.AddComponent<BoxCollider>();
            box.center = new Vector3(-0.0643f, 1.1254f, 0f);
            box.size = new Vector3(6.1424f, 5.2508f, 0.5f);

            box.isTrigger = true;

            nomaiWallTextObj.AddComponent<OWCollider>();
            var nomaiWallText = nomaiWallTextObj.AddComponent<NomaiWallText>();

            var text = new TextAsset(xmlPath);

            // Text assets need a name to be used with VoiceMod
            text.name = Path.GetFileNameWithoutExtension(info.xmlFile);

            BuildArcs(xmlPath, nomaiWallText, nomaiWallTextObj, info);
            AddTranslation(xmlPath);
            nomaiWallText._nomaiTextAsset = text;

            nomaiWallText.SetTextAsset(text);

            return nomaiWallText;
        }

        internal static void BuildArcs(string xml, NomaiWallText nomaiWallText, GameObject conversationZone, PropModule.NomaiTextInfo info)
        {
            var dict = MakeNomaiTextDict(xml);

            nomaiWallText._dictNomaiTextData = dict;

            RefreshArcs(nomaiWallText, conversationZone, info);
        }

        internal static void RefreshArcs(NomaiWallText nomaiWallText, GameObject conversationZone, PropModule.NomaiTextInfo info)
        {
            var dict = nomaiWallText._dictNomaiTextData;
            Random.InitState(info.seed);

            var arcsByID = new Dictionary<int, GameObject>();

            if (info.arcInfo != null && info.arcInfo.Count() != dict.Values.Count())
            {
                Logger.LogError($"Can't make NomaiWallText, arcInfo length [{info.arcInfo.Count()}] doesn't equal text entries [{dict.Values.Count()}]");
                return;
            }

            var i = 0;
            foreach (var textData in dict.Values)
            {
                var arcInfo = info.arcInfo?.Length > i ? info.arcInfo[i] : null;
                var textEntryID = textData.ID;
                var parentID = textData.ParentID;

                var parent = parentID == -1 ? null : arcsByID[parentID];

                GameObject arc = MakeArc(arcInfo, conversationZone, parent, textEntryID);
                arc.name = $"Arc {i} - Child of {parentID}";

                arcsByID.Add(textEntryID, arc);

                i++;
            }
        }

        internal static GameObject MakeArc(PropModule.NomaiTextArcInfo arcInfo, GameObject conversationZone, GameObject parent, int textEntryID)
        {
            GameObject arc;
            var type = arcInfo != null ? arcInfo.type : PropModule.NomaiTextArcInfo.NomaiTextArcType.Adult;
            var variation = arcInfo.variation;
            switch (type)
            {
                case PropModule.NomaiTextArcInfo.NomaiTextArcType.Child:
                    variation = arcInfo.variation < 0
                        ? Random.Range(0, _childArcPrefabs.Count())
                        : (arcInfo.variation % _childArcPrefabs.Count());
                    arc = _childArcPrefabs[variation].InstantiateInactive();
                    break;
                case PropModule.NomaiTextArcInfo.NomaiTextArcType.Stranger when _ghostArcPrefabs.Any():
                    variation = arcInfo.variation < 0
                        ? Random.Range(0, _ghostArcPrefabs.Count())
                        : (arcInfo.variation % _ghostArcPrefabs.Count());
                    arc = _ghostArcPrefabs[variation].InstantiateInactive();
                    break;
                case PropModule.NomaiTextArcInfo.NomaiTextArcType.Adult:
                default:
                    variation = arcInfo.variation < 0
                        ? Random.Range(0, _arcPrefabs.Count())
                        : (arcInfo.variation % _arcPrefabs.Count());
                    arc = _arcPrefabs[variation].InstantiateInactive();
                    break;
            }
            arcInfo.variation = variation;

            arc.transform.parent = conversationZone.transform;
            arc.GetComponent<NomaiTextLine>()._prebuilt = false;

            if (arcInfo != null)
            {
                var a = arcInfo;
                if (a.position == null) arc.transform.localPosition = Vector3.zero;
                else arc.transform.localPosition = new Vector3(a.position.x, a.position.y, 0);

                arc.transform.localRotation = Quaternion.Euler(0, 0, a.zRotation);

                if (a.mirror) arc.transform.localScale = new Vector3(-1, 1, 1);
            }
            // Try auto I guess
            else
            {
                if (parent == null)
                {
                    arc.transform.localPosition = Vector3.zero;
                }
                else
                {
                    var points = parent.GetComponent<NomaiTextLine>().GetPoints();
                    var point = points[points.Count() / 2];

                    arc.transform.localPosition = point;
                    arc.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(0, 360));
                }
            }

            arc.GetComponent<NomaiTextLine>().SetEntryID(textEntryID);
            arc.GetComponent<MeshRenderer>().enabled = false;

            arc.SetActive(true);

            arcInfoToCorrespondingSpawnedGameObject[arcInfo] = arc;

            return arc;
        }

        private static Dictionary<int, NomaiText.NomaiTextData> MakeNomaiTextDict(string xmlPath)
        {
            var dict = new Dictionary<int, NomaiText.NomaiTextData>();

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xmlPath);
            XmlNode rootNode = xmlDocument.SelectSingleNode("NomaiObject");

            foreach (object obj in rootNode.SelectNodes("TextBlock"))
            {
                XmlNode xmlNode = (XmlNode)obj;

                int textEntryID = -1;
                int parentID = -1;

                XmlNode textNode = xmlNode.SelectSingleNode("Text");
                XmlNode entryIDNode = xmlNode.SelectSingleNode("ID");
                XmlNode parentIDNode = xmlNode.SelectSingleNode("ParentID");

                if (entryIDNode != null && !int.TryParse(entryIDNode.InnerText, out textEntryID))
                {
                    Logger.LogError($"Couldn't parse int ID in [{entryIDNode?.InnerText}] for [{xmlPath}]");
                    textEntryID = -1;
                }

                if (parentIDNode != null && !int.TryParse(parentIDNode.InnerText, out parentID))
                {
                    Logger.LogError($"Couldn't parse int ParentID in [{parentIDNode?.InnerText}] for [{xmlPath}]");
                    parentID = -1;
                }

                NomaiText.NomaiTextData value = new NomaiText.NomaiTextData(textEntryID, parentID, textNode, false, NomaiText.Location.UNSPECIFIED);
                dict.Add(textEntryID, value);
            }
            return dict;
        }

        private static void AddTranslation(string xmlPath)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xmlPath);

            XmlNode xmlNode = xmlDocument.SelectSingleNode("NomaiObject");
            XmlNodeList xmlNodeList = xmlNode.SelectNodes("TextBlock");

            foreach (object obj in xmlNodeList)
            {
                XmlNode xmlNode2 = (XmlNode)obj;
                var text = xmlNode2.SelectSingleNode("Text").InnerText;
                TranslationHandler.AddDialogue(text);
            }
        }
    }
}
