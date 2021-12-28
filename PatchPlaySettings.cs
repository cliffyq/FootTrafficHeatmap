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

            row.ToggleableIcon(ref TrafficHeatmap.ShowHeatMap, Icon, "String_ShowHeatMap", SoundDefOf.Mouseover_ButtonToggle);
#if DEBUG
            row.ToggleableIcon(ref TrafficHeatmap.ShowHeatMapCost, Icon, "String_ShowHeatMapCost", SoundDefOf.Mouseover_ButtonToggle);
#endif
        }
    }
}
