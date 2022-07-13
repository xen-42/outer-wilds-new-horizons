using HarmonyLib;

namespace NewHorizons.Patches 
{ 
    [HarmonyPatch]
    public class AutoSlideProjectorPatches
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(AutoSlideProjector), nameof(AutoSlideProjector.Play))]
        public static void AutoSlideProjector_Play(ref SlideCollectionContainer ____slideCollectionItem)
        {
            ____slideCollectionItem.enabled = true;
        }
    }
}