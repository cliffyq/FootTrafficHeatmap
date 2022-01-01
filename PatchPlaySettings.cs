using HarmonyLib;
using RimWorld;
using Verse;

namespace TrafficHeatmap
{
    [HarmonyPatch(typeof(PlaySettings), "DoPlaySettingsGlobalControls")]
    static class PatchPlaySettings
    {
        [HarmonyPostfix]
        static void PostFix(WidgetRow row, bool worldView)
        {
            if (worldView)
                return;

            if (row == null || Resources.Icon == null)
                return;

            row.ToggleableIcon(ref TrafficHeatmap.ShowHeatMap, Resources.Icon, "String_ShowHeatMap1", SoundDefOf.Mouseover_ButtonToggle);

            row.ToggleableIcon(ref TrafficHeatmap.ShowHeatMapCost, Resources.Icon, "String_ShowHeatMapCost1", SoundDefOf.Mouseover_ButtonToggle);

            row.ToggleableIcon(ref TrafficHeatmapMinMax.ShowHeatMap, Resources.Icon, "String_ShowHeatMap2", SoundDefOf.Mouseover_ButtonToggle);

            row.ToggleableIcon(ref TrafficHeatmapMinMax.ShowHeatMapCost, Resources.Icon, "String_ShowHeatMapCost2", SoundDefOf.Mouseover_ButtonToggle);

        }
    }
}
