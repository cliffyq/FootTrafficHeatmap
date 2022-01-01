using HarmonyLib;
using UnityEngine;
using Verse;

namespace TrafficHeatmap
{
    [StaticConstructorOnStartup]
    public class Resources
    {
        public static readonly Texture2D Icon = ContentFinder<Texture2D>.Get("icon");

        static Resources()
        {
#if DEBUG
            Harmony.DEBUG = true;
#endif

            Harmony harmony = new Harmony("Cliffyq.TrafficHeatmap");
            harmony.PatchAll();
        }
    }
}
