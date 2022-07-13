using NewHorizons.Builder.Props;
using NewHorizons.External.Configs;
using NewHorizons.External.Modules;
using NewHorizons.Utility.DebugUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using static NewHorizons.External.Modules.PropModule;

namespace NewHorizons.Utility.DebugMenu
{

    /*
     * Strategy:
     * load all existing nomai text and allow the user to select them from a list
     * from there, allow them to edit the placement of the one selected
     */

    class DebugMenuNomaiText : DebugSubmenu
    {
        internal DebugRaycaster _drc;
        internal DebugNomaiTextPlacer _dnp;

        class ConversationMetadata
        {
            public NomaiTextInfo conversation;
            public GameObject conversationGo;
            public PlanetConfig planetConfig;

            public List<SpiralMetadata> spirals;

            public bool collapsed;
        }

        class SpiralMetadata
        {
            public NomaiTextArcInfo spiral;
            public GameObject spiralGo;

            public NomaiTextInfo conversation;
            public PlanetConfig planetConfig;

            public string planetName;
            public int id;
            public int parentID;

            public int pointOnParent = -1;
            public SpiralMetadata parent;
        }

        List<ConversationMetadata> conversations = new List<ConversationMetadata>();

        // menu stuff
        Vector2 conversationsScrollPosition = new Vector2();
        float dx = 0.1f;
        float dy = 0.1f;
        float dT = 1f;

        internal override string SubmenuName()
        {
            return "Text Placer";
        }

        internal override void OnInit(DebugMenu menu)
        {
            _drc = menu.GetComponent<DebugRaycaster>();
            _dnp = menu.GetComponent<DebugNomaiTextPlacer>();
        }

        internal override void OnAwake(DebugMenu menu)
        {
            _drc = menu.GetComponent<DebugRaycaster>();
            _dnp = menu.GetComponent<DebugNomaiTextPlacer>();
        }

        internal override void OnBeginLoadMod(DebugMenu debugMenu) {}

        internal override void GainActive() {} // intentionally blank. do not set `DebugNomaiTextPlacer.active = true;` here

        internal override void LoseActive()
        {
            DebugNomaiTextPlacer.active = false;
        }
        
        internal override void LoadConfigFile(DebugMenu menu, PlanetConfig config)
        {
            if (config?.Props?.nomaiText == null) return;

            foreach(NomaiTextInfo conversation in config.Props.nomaiText)
            {
                ConversationMetadata conversationMetadata = new ConversationMetadata()
                {
                    conversation = conversation,
                    conversationGo = NomaiTextBuilder.GetSpawnedGameObjectByNomaiTextInfo(conversation),
                    planetConfig = config,
                    spirals = new List<SpiralMetadata>(),
                    collapsed = true
                };

                Logger.LogVerbose("Adding conversation Game Object " + conversationMetadata.conversationGo);

                conversations.Add(conversationMetadata);

                var numArcs = conversation.arcInfo == null
                    ? 0
                    : conversation.arcInfo.Length;
                for(int id = 0; id < numArcs; id++)
                {
                    NomaiTextArcInfo arcInfo = conversation.arcInfo[id];

                    SpiralMetadata metadata = new SpiralMetadata()
                    {
                        spiral = arcInfo,
                        spiralGo = NomaiTextBuilder.GetSpawnedGameObjectByNomaiTextArcInfo(arcInfo),
                        conversation = conversation,
                        planetConfig = config,
                        planetName = config.name,
                        id = id
                    };
                    
                    if (metadata.spiralGo != null)
                    {
                        string regex = @"Arc \d+ - Child of (\d*)"; // $"Arc {i} - Child of {parentID}";
                        var parentID = (new Regex(regex)).Matches(metadata.spiralGo.name)[0].Groups[1].Value;
                        metadata.parentID = parentID == "" ? -1 : int.Parse(parentID);

                        Logger.LogVerbose("Parent id for '" + metadata.spiralGo.name + "' : " + parentID);
                    }

                    conversationMetadata.spirals.Add(metadata);
                }
            }
        }


        /*
           Conversations:                                                
           +---------------------------+                            
           |1) comment                 |
           |   |o Visible| |Place|     | // replace is greyed out if the user is currently replacing this plane (replacing is done after the user presses G
           |2) ...                     |                                        
           +---------------------------+
                                                                    
           Spirals:                                
           +---------------------------+                     
           | v Planet - Comment        |
           | +----------------------+  | 
           | | v ID                 |  |                            
           | |                      |  |                            
           | | | > Surface 2 |      |  |                            
           | | x: 5  ---o---        |  |                            
           | | y: 2  ---o---        |  |                            
           | | theta: 45 ---o---    |  |                            
           | |                      |  |                            
           | | o Child o Adult o ...|  |                            
           | | variation: o1 o2 o3..|  |                            
           | +----------------------+  |                            
           |                           |                            
           | +----------------------+  |                            
           | | > ID                 |  |                            
           | +----------------------+  |                            
           |                           |                            
           | ...                       |                            
           +---------------------------+ 
        
           +---------------------------+                                                        
           | > Planet - Comment        |                            
           +---------------------------+   
           ...
                                                                    
         */
        internal override void OnGUI(DebugMenu menu)
        {
            conversationsScrollPosition = GUILayout.BeginScrollView(conversationsScrollPosition);
            
            for(int i = 0; i < conversations.Count(); i++)
            {
                ConversationMetadata conversationMeta = conversations[i];

                GUILayout.BeginHorizontal();
                    GUILayout.Space(5);        

                    GUILayout.BeginVertical(menu._editorMenuStyle);
                        
                        var arrow = conversationMeta.collapsed ? " > " : " v ";
                        if (GUILayout.Button(arrow + conversationMeta.planetConfig.name + " - " + i, menu._submenuStyle)) 
                        {
                            conversationMeta.collapsed = !conversationMeta.collapsed;
                            Logger.LogVerbose("BUTTON " + i);
                        }

                        if (!conversationMeta.collapsed)
                        {
                            GUILayout.Space(5);
                            // button to set this one to place with a raycast
                            GUILayout.Label(conversationMeta.conversation.type.ToString());
                            
                            // only show the button if this conversation is a wall text, do not show it if it is a scroll text or something
                            if (
                                //conversationMeta.conversation.type == PropModule.NomaiTextInfo.NomaiTextType.Wall &&
                                GUILayout.Button("Place conversation with G")
                            ) {
                                Logger.LogVerbose(conversationMeta.conversationGo+" 0");
                                DebugNomaiTextPlacer.active = true;
                                _dnp.onRaycast = (DebugRaycastData data) =>
                                {
                                    // I think this should be looking for AstroObject and then doing ._rootSector
                                    var sectorObject = data.hitBodyGameObject.GetComponentInChildren<Sector>()?.gameObject;
                                    if (sectorObject == null) sectorObject = data.hitBodyGameObject.GetComponentInParent<Sector>()?.gameObject;

                                    if (conversationMeta.conversation.type == NomaiTextInfo.NomaiTextType.Wall)
                                    {
                                        conversationMeta.conversation.position = data.pos;
                                        conversationMeta.conversation.normal   = data.norm;
                                        conversationMeta.conversation.rotation = null;
                                        UpdateConversationTransform(conversationMeta, sectorObject);
                                    }
                                    else if (conversationMeta.conversationGo != null)
                                    {
                                        conversationMeta.conversationGo.transform.localPosition = data.pos;
                                        DebugPropPlacer.SetGameObjectRotation(conversationMeta.conversationGo, data, _dnp.gameObject.transform.position);
                                        
                                        conversationMeta.conversation.position = conversationMeta.conversationGo.transform.localPosition;
                                        conversationMeta.conversation.rotation = conversationMeta.conversationGo.transform.localEulerAngles;
                                    }

                                    DebugNomaiTextPlacer.active = false;
                                };
                            }

                            //
                            // spirals
                            //

                            for(int j = 0; j < conversationMeta.spirals.Count(); j++)
                            {
                                var spiralMeta = conversationMeta.spirals[j];
                                bool changed = false;
                                GUILayout.BeginHorizontal();
                                    GUILayout.Space(5);        
                                    GUILayout.BeginVertical(menu._submenuStyle);

                                        // spiral controls
                                        GUILayout.Label("Spiral");

                                        // TODO: implement disabled feature: change spiral type and variation
                                        GUILayout.Label("Type");
                                            GUILayout.BeginHorizontal();
                                            GUI.enabled = spiralMeta.spiral.type != NomaiTextArcInfo.NomaiTextArcType.Adult;
                                            if (GUILayout.Button("Adult")) { spiralMeta.spiral.type = NomaiTextArcInfo.NomaiTextArcType.Adult; changed = true; }
                                            GUI.enabled = spiralMeta.spiral.type != NomaiTextArcInfo.NomaiTextArcType.Child;
                                            if (GUILayout.Button("Child")) { spiralMeta.spiral.type = NomaiTextArcInfo.NomaiTextArcType.Child; changed = true; }
                                            GUI.enabled = spiralMeta.spiral.type != NomaiTextArcInfo.NomaiTextArcType.Stranger;
                                            if (GUILayout.Button("Stranger")) { spiralMeta.spiral.type = NomaiTextArcInfo.NomaiTextArcType.Stranger; changed = true; }
                                            GUI.enabled = true;
                                            GUILayout.EndHorizontal();
                                        
                                        GUILayout.Label("Variation");
                                            GUILayout.BeginHorizontal();
                                            var varietyCount = GetVarietyCountForType(spiralMeta.spiral.type);
                                            //var newVariation = int.Parse(GUILayout.TextField(spiralMeta.spiral.variation+""));
                                            //newVariation = Mathf.Min(Mathf.Max(0, newVariation), varietyCount);
                                            //if (newVariation != spiralMeta.spiral.variation) changed = true;
                                            //spiralMeta.spiral.variation = newVariation;
                                            GUI.enabled = spiralMeta.spiral.variation > 0;
                                            if (GUILayout.Button("-", GUILayout.ExpandWidth(false))) { spiralMeta.spiral.variation--; changed=true; } 
                                            GUILayout.Label(spiralMeta.spiral.variation+"", GUILayout.ExpandWidth(false));
                                            GUI.enabled = spiralMeta.spiral.variation < varietyCount-1;
                                            if (GUILayout.Button("+", GUILayout.ExpandWidth(false))) { spiralMeta.spiral.variation++; changed=true; } 

                                            GUILayout.EndHorizontal();
        

                                        // TODO: debug disabled feature: place spiral point on parent
                                        //var shouldBeInManual = spiralMeta.parentID < 0 || GUILayout.Toggle(spiralMeta.pointOnParent < 0, "Manual Positioning");
                                        //if ( shouldBeInManual && spiralMeta.pointOnParent >= 0) spiralMeta.pointOnParent = -1;
                                        //if (!shouldBeInManual && spiralMeta.pointOnParent < 0 ) spiralMeta.pointOnParent = 0;

                                        var shouldBeInManual = true;

                                        if (shouldBeInManual)
                                        {
                                            // x
                                            GUILayout.BeginHorizontal();
                                                var positionX = (spiralMeta.spiral.position?.x ?? 0);
                                                GUILayout.Label("x:     ", GUILayout.Width(50));
                                                float deltaX = float.Parse(GUILayout.TextField(positionX+"", GUILayout.Width(50))) - positionX;
                                                if (GUILayout.Button("+", GUILayout.ExpandWidth(false))) deltaX += dx;
                                                if (GUILayout.Button("-", GUILayout.ExpandWidth(false))) deltaX -= dx;
                                                dx = float.Parse(GUILayout.TextField(dx+"", GUILayout.Width(100)));
                                            GUILayout.EndHorizontal();
                                            // y
                                            GUILayout.BeginHorizontal();
                                                var positionY = (spiralMeta.spiral.position?.y ?? 0);
                                                GUILayout.Label("y:     ", GUILayout.Width(50));
                                                float deltaY = float.Parse(GUILayout.TextField(positionY+"", GUILayout.Width(50))) - positionY;
                                                if (GUILayout.Button("+", GUILayout.ExpandWidth(false))) deltaY += dy;
                                                if (GUILayout.Button("-", GUILayout.ExpandWidth(false))) deltaY -= dy;
                                                dy = float.Parse(GUILayout.TextField(dy+"", GUILayout.Width(100)));
                                            GUILayout.EndHorizontal();
                                            // theta
                                            GUILayout.BeginHorizontal();
                                                var theta = spiralMeta.spiral.zRotation;
                                                GUILayout.Label("theta: ", GUILayout.Width(50));
                                                float deltaTheta = float.Parse(GUILayout.TextField(theta+"", GUILayout.Width(50))) - theta;
                                                if (GUILayout.Button("+", GUILayout.ExpandWidth(false))) deltaTheta += dT;
                                                if (GUILayout.Button("-", GUILayout.ExpandWidth(false))) deltaTheta -= dT;
                                                dT = float.Parse(GUILayout.TextField(dT+"", GUILayout.Width(100)));
                                            GUILayout.EndHorizontal();

                                            if (deltaX != 0 || deltaY != 0 || deltaTheta != 0)
                                            {
                                                spiralMeta.spiral.position = (spiralMeta.spiral.position??Vector2.zero) + new Vector2(deltaX, deltaY);
                                                spiralMeta.spiral.zRotation += deltaTheta; 
        
                                                spiralMeta.spiralGo.transform.localPosition = new Vector3(spiralMeta.spiral.position.x, spiralMeta.spiral.position.y, 0);
                                                spiralMeta.spiralGo.transform.localRotation = Quaternion.Euler(0, 0, spiralMeta.spiral.zRotation);

                                                spiralMeta.pointOnParent = -1; // since tweaks have been made, we're setting this to manual mode
                                            
                                                UpdateChildrenLocations(spiralMeta);
                                            }
                                        }
                                        else
                                        {
                                            var parent = GetParent(spiralMeta);
                                            var numPoints = 10; // parent.spiralGo.GetComponent<NomaiTextLine>()._points.Count();
                                            
                                            var newPointOnParent = Mathf.RoundToInt(GUILayout.HorizontalSlider(spiralMeta.pointOnParent, 0, numPoints)); // stepped horizontal slider
                                            
                                            if (spiralMeta.pointOnParent != newPointOnParent)
                                            {
                                                spiralMeta.pointOnParent = newPointOnParent;
                                                UpdateSpiralLocationByPointOnParent(spiralMeta);
                                            }
                                        }

                                        var newMirror = GUILayout.Toggle(spiralMeta.spiral.mirror, "Mirror");
                                        if (newMirror != spiralMeta.spiral.mirror)
                                        {
                                            spiralMeta.spiral.mirror = newMirror;
                                            spiralMeta.spiralGo.transform.localScale = new Vector3(newMirror ? -1 : 1, 1, 1);
                                        }
                                    GUILayout.EndVertical();
                                    GUILayout.Space(5);
                                GUILayout.EndHorizontal();
                
                                if (changed)
                                { 
                                    // cache required stuff, destroy spiralMeta.go, call NomaiTextBuilder.MakeArc using spiralMeta.spiral and cached stuff
                                    // PropModule.NomaiTextArcInfo arcInfo, GameObject conversationZone, GameObject parent, int textEntryID, int i
                                    var conversationZone = spiralMeta.spiralGo.transform.parent.gameObject;
                                    var wallTextComponent = conversationZone.GetComponent<NomaiWallText>();
                                    var oldTextLineComponent = spiralMeta.spiralGo.GetComponent<NomaiTextLine>();
                                    var indexInParent = 0;
                                    for (indexInParent = 0; indexInParent < wallTextComponent._textLines.Length; indexInParent++) if (oldTextLineComponent == wallTextComponent._textLines[indexInParent]) break;
                                    var textEntryId = oldTextLineComponent._entryID;
                                    GameObject.Destroy(spiralMeta.spiralGo);
                                    spiralMeta.spiralGo = NomaiTextBuilder.MakeArc(spiralMeta.spiral, conversationZone, null, textEntryId);
                                    wallTextComponent._textLines[indexInParent] = spiralMeta.spiralGo.GetComponent<NomaiTextLine>();

                                    spiralMeta.spiralGo.name = "Brandnewspiral";

                                    var s = AddDebugShape.AddSphere(wallTextComponent._sector.gameObject, 0.1f, Color.green);
                                    s.transform.localPosition = wallTextComponent._sector.transform.InverseTransformPoint(spiralMeta.spiralGo.transform.position);                        

                                    //var conversationZone = spiralMeta.spiralGo.transform.parent.gameObject;
                                    //var wallTextComponent = conversationZone.GetComponent<NomaiWallText>();
                                    //foreach(NomaiTextLine l in wallTextComponent._textLines) GameObject.Destroy(l.gameObject);
                                    //NomaiTextBuilder.RefreshArcs(wallTextComponent, conversationZone, spiralMeta.conversation);
                                    
                                    foreach(NomaiTextLine line in wallTextComponent._textLines)
                                    {
                                        line.GetComponent<NomaiTextLine>()._active = true;
                                        line.GetComponent<NomaiTextLine>().SetTranslatedState(true);
                                        line.GetComponent<NomaiTextLine>().SetTranslatedState(false);
                                    }

                                    //spiralMeta.spiralGo.GetComponent<NomaiTextLine>()._active = true;
                                    //spiralMeta.spiralGo.GetComponent<NomaiTextLine>().SetTranslatedState(true);
                                    ////wallTextComponent.ShowImmediate();

                                    Locator.GetPlayerBody().gameObject.GetComponentInChildren<DebugArrow>().target = spiralMeta.spiralGo.transform;
                                }

                                GUILayout.Space(10);
                            }
                        }

                    GUILayout.EndVertical();

                    GUILayout.Space(5);
                GUILayout.EndHorizontal();
                
                GUILayout.Space(10);
            }

            GUILayout.EndScrollView();
        }

        private SpiralMetadata GetParent(SpiralMetadata child)
        {
            if (child.parent != null || child.parentID < 0) return child.parent;

            
            ConversationMetadata convoMeta = conversations.Where(m => m.conversation == child.conversation).First();
            SpiralMetadata parentSpiralMeta = convoMeta.spirals.Where(potentialParentMeta => potentialParentMeta.id == child.id).First();
            child.parent = parentSpiralMeta;
            
            return child.parent;
        }

        private void UpdateSpiralLocationByPointOnParent(SpiralMetadata spiralMetadata, bool updateChildren = true)
        {
            Logger.Log("Updating spiral " + spiralMetadata.id);
            Logger.Log("Setting point on parent " + spiralMetadata.pointOnParent);
            if (spiralMetadata.pointOnParent < 0) return;

            SpiralMetadata parentSpiralMeta = GetParent(spiralMetadata);
            var parentPoints = parentSpiralMeta.spiralGo.GetComponent<NomaiTextLine>()._points;
            
            Logger.Log("Got parent and parent points");

            var prevPointOnParent = parentPoints[Mathf.Max(0,                      spiralMetadata.pointOnParent-1)];
            var nextPointOnParent = parentPoints[Mathf.Min(parentPoints.Count()-1, spiralMetadata.pointOnParent+1)];
            var delta = nextPointOnParent - prevPointOnParent;
            var parentTangent = Mathf.Rad2Deg * Mathf.Atan2(delta.y, delta.x);
            var newRotationRelativeToParent = parentTangent + 90;
            spiralMetadata.spiral.zRotation = parentSpiralMeta.spiral.zRotation + newRotationRelativeToParent;

            Logger.Log("Got zRotation: " + newRotationRelativeToParent + " -=- " + spiralMetadata.spiral.zRotation);

            var pointOnParent = parentPoints[spiralMetadata.pointOnParent];
            var selfBasePoint = spiralMetadata.spiralGo.GetComponent<NomaiTextLine>()._points[0];
            var newPointRelativeToParent = -selfBasePoint + pointOnParent;
            spiralMetadata.spiral.position = parentSpiralMeta.spiral.position + new Vector2(newPointRelativeToParent.x, newPointRelativeToParent.y);

            Logger.Log("Got position " + newPointRelativeToParent + " -=- " + spiralMetadata.spiral.position);

            spiralMetadata.spiralGo.transform.localPosition = new Vector3(spiralMetadata.spiral.position.x, spiralMetadata.spiral.position.y, 0);
            spiralMetadata.spiralGo.transform.localEulerAngles = new Vector3(0, 0, spiralMetadata.spiral.zRotation);
            

            if (updateChildren) UpdateChildrenLocations(spiralMetadata);
        }

        private void UpdateChildrenLocations(SpiralMetadata parentSprialMetadata)
        {
            Logger.Log("Updating children");
            ConversationMetadata convoMeta = conversations.Where(m => m.conversation == parentSprialMetadata.conversation).First();
            Logger.Log("Got conversation metadata");

            convoMeta.spirals
                .Where(spiralMeta => spiralMeta.parentID == parentSprialMetadata.id && spiralMeta.id != parentSprialMetadata.id)
                .ToList()
                .ForEach(spiralMeta => {
                    if (spiralMeta.pointOnParent == -1) return; // this spiral is positioned manually, skip

                    UpdateSpiralLocationByPointOnParent(spiralMeta);
                });
        }

        private int GetVarietyCountForType(NomaiTextArcInfo.NomaiTextArcType type)
        {
            switch(type)
            {
                case NomaiTextArcInfo.NomaiTextArcType.Stranger: 
                    return NomaiTextBuilder.GetGhostArcPrefabs().Count();
                case NomaiTextArcInfo.NomaiTextArcType.Child: 
                    return NomaiTextBuilder.GetChildArcPrefabs().Count();
                default:
                case NomaiTextArcInfo.NomaiTextArcType.Adult: 
                    return NomaiTextBuilder.GetArcPrefabs().Count();
            }
        }

        void UpdateConversationTransform(ConversationMetadata conversationMetadata, GameObject sectorParent)
        {
            var nomaiWallTextObj = conversationMetadata.conversationGo;
            if (nomaiWallTextObj == null) return;

            var planetGO = sectorParent;
            var info = conversationMetadata.conversation;
        
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
        }

        internal override void PreSave(DebugMenu menu)
        {
            conversations.ForEach(metadata =>
            {
                if (metadata.conversationGo == null) return;
                metadata.conversation.position = metadata.conversationGo.transform.localPosition;
                metadata.conversation.rotation = metadata.conversationGo.transform.localEulerAngles;           
            });

            // Spirals' configs do not need to be updated. They are always up to date
            //spirals.ForEach(metadata =>
            //{
            //    metadata.spiral.position = metadata.spiral.position;
            //    metadata.spiral.zRotation = metadata.spiral.zRotation;
            //});
        }
    }
}
