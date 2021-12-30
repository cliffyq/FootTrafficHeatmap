using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TrafficHeatmap
{
    [HarmonyPatch(typeof(PlaySettings), "DoPlaySettingsGlobalControls")]
    static class PatchPlaySettings
    {
        static readonly Texture2D Icon = ContentFinder<Texture2D>.Get("icon");

        [HarmonyPostfix]
        static void PostFix(WidgetRow row, bool worldView)
        {
            if (worldView)
                return;

            if (row == null || Icon == null)
                return;

            //row.ToggleableIcon(ref TrafficHeatmap.ShowHeatMap, Icon, "String_ShowHeatMap1", SoundDefOf.Mouseover_ButtonToggle);

            //row.ToggleableIcon(ref TrafficHeatmap.ShowHeatMapCost, Icon, "String_ShowHeatMapCost1", SoundDefOf.Mouseover_ButtonToggle);

            row.ToggleableIcon(ref TrafficHeatmapFixedSampleRate.ShowHeatMap, Icon, "String_ShowHeatMap2", SoundDefOf.Mouseover_ButtonToggle);

            row.ToggleableIcon(ref TrafficHeatmapFixedSampleRate.ShowHeatMapCost, Icon, "String_ShowHeatMapCost2", SoundDefOf.Mouseover_ButtonToggle);

        }
    }
}
