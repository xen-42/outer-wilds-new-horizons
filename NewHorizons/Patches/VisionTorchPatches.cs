using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using NewHorizons.Builder.Props;
using UnityEngine;

namespace NewHorizons.Patches
{
	
    [HarmonyPatch]
    public static class MindProjectorTriggerPatches
    {
        [HarmonyPrefix]
        [HarmonyPatch(typeof(MindProjectorTrigger), nameof(MindProjectorTrigger.OnTriggerVolumeEntry))]
        public static bool MindProjectorTrigger_OnTriggerVolumeEntry(MindProjectorTrigger __instance, GameObject hitObj)
        {
			var t = hitObj.GetComponent<VisionTorchTarget>();
            if (t != null) //(hitObj.CompareTag("PrisonerDetector"))
		    {
                __instance._mindProjector.OnProjectionComplete += t.onSlidesComplete;
                __instance._mindProjector.SetMindSlideCollection(t.slideCollection);

			    __instance.OnBeamStartHitPrisoner.Invoke();
			    __instance._mindProjector.Play(reset: true);
			    __instance._mindProjector.OnProjectionStart += __instance.OnProjectionStart;
			    __instance._mindProjector.OnProjectionComplete += __instance.OnProjectionComplete;
         
			    Locator.GetPlayerTransform().GetComponent<PlayerLockOnTargeting>().LockOn(hitObj.transform, Vector3.zero);
			    __instance._playerLockedOn = true;
                return false;
            }

            return true;
        }
    }

	[HarmonyPatch]
	public static class VisionTorchItemPatches
    {
		// This is some dark magic
		// this creates a method called base_DropItem that basically just calls OWItem.PickUpItem whenever it (VisionTorchItemPatches.base_PickUpItem) is called
		[HarmonyReversePatch]
		[HarmonyPatch(typeof(OWItem), nameof(OWItem.DropItem))]
		private static void base_DropItem(OWItem instance, Vector3 position, Vector3 normal, Transform parent, Sector sector, IItemDropTarget customDropTarget) { }


        // Make the vision torch droppable. In the base game you can only drop it if you're in the dream world.
        [HarmonyPrefix]
        [HarmonyPatch(typeof(VisionTorchItem), nameof(VisionTorchItem.DropItem))]
		public static bool VisionTorchItem_DropItem(VisionTorchItem __instance, Vector3 position, Vector3 normal, Transform parent, Sector sector, IItemDropTarget customDropTarget)
		{
			if (!Locator.GetDreamWorldController().IsInDream())
			{
				base_DropItem(__instance, position, normal, parent, sector, customDropTarget);
			}

			return true;
		}
	}
}
