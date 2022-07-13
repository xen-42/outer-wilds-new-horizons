using NewHorizons.External.Configs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace NewHorizons.Utility.DebugMenu
{
    class DebugMenuShipLogs : DebugSubmenu
    {
        string entryPositionsText = "";

        internal override void GainActive()
        {
        
        }
        internal override void LoseActive()
        {
            
        }

        internal override void LoadConfigFile(DebugMenu menu, PlanetConfig config)
        {
        
        }


        internal override void OnAwake(DebugMenu menu)
        {
        }

        internal override void OnBeginLoadMod(DebugMenu debugMenu)
        {
        }

        internal override void OnGUI(DebugMenu menu)
        {
            if (GUILayout.Button("Print Ship Log Positions"))
            {
                entryPositionsText = String.Join("\n", 
                    Resources
                        .FindObjectsOfTypeAll<ShipLogEntryCard>()
                        .ToList()
                        .Select(go => 
                            ("{ \"id\": \"" +go.name+ "\", \"position\": {\"x\": "+go.transform.localPosition.x+", \"y\": "+go.transform.localPosition.y+" } ")
                        )
                        .ToList()
                );
            }

            GUILayout.TextArea(entryPositionsText);
        }

        internal override void OnInit(DebugMenu menu)
        {
        
        }

        internal override void PreSave(DebugMenu menu)
        {
        
        }

        internal override string SubmenuName()
        {
            return "Ship Log";
        }
    }
}
