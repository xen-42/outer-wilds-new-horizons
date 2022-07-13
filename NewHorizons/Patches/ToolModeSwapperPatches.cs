using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace NewHorizons.Patches
{
    [HarmonyPatch]
    public static class ToolModeSwapperPatches
    {

        // Patches ToolModeSwapper.EquipToolMode(ToolMode mode) to deny swaps if you're holding a vision torch.
        // This is critical for preventing swapping to the scout launcher (causes memory slides to fail) but it
        // just doesn't look right when you switch to other stuff (eg the signalscope), so I'm disabling swapping tools entirely

        // the correct way to do this is to patch ToolModeSwapper.Update to be exactly the same as it is now, but change the below line
        // to include a check for "is holding vision torch", but I'm not copy/pasting an entire function, no sir
        // if (((_currentToolMode == ToolMode.None || _currentToolMode == ToolMode.Item) && Locator.GetPlayerSuit().IsWearingSuit(includeTrainingSuit: false)) || ((_currentToolMode == ToolMode.None || _currentToolMode == ToolMode.SignalScope) && OWInput.IsInputMode(InputMode.ShipCockpit)))
        [HarmonyPrefix]
        [HarmonyPatch(typeof(ToolModeSwapper), nameof(ToolModeSwapper.EquipToolMode))]
        public static bool ToolModeSwapper_EquipToolMode(ToolModeSwapper __instance, ToolMode mode)
        {
            var isHoldingVisionTorch = __instance.GetItemCarryTool()?.GetHeldItemType() == ItemType.VisionTorch;
            var swappingToRestrictedTool = 
                mode == ToolMode.Probe || 
                mode == ToolMode.SignalScope || 
                mode == ToolMode.Translator;
            var isInShip = UnityEngine.GameObject.FindObjectOfType<ShipCockpitController>()?._playerAtFlightConsole ?? false;

            if (!isInShip && isHoldingVisionTorch && swappingToRestrictedTool) return false;

            return true;
        }
    }
}
