using HarmonyLib;
using RimWorld;
using Verse;

namespace TrafficHeatmap
{
    [HarmonyPatch(typeof(PlaySettings), "DoPlaySettingsGlobalControls")]
    internal static class PatchPlaySettings
    {
        [HarmonyPostfix]
        private static void PostFix(WidgetRow row, bool worldView)
        {
            if (worldView)
                return;

            if (row == null || Resources.Icon == null)
                return;

            row.ToggleableIcon(ref FootTrafficHeatmap.ShowHeatMap, Resources.Icon, "Show Foot Traffic Heatmap", SoundDefOf.Mouseover_ButtonToggle);
#if DEBUG
            row.ToggleableIcon(ref TrafficHeatmap.ShowHeatMapCost, Resources.Icon, "Show Cell Cost", SoundDefOf.Mouseover_ButtonToggle);
#endif
        }
    }
}