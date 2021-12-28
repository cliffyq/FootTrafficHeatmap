using HarmonyLib;
using Verse;

namespace TrafficHeatmap
{
    [StaticConstructorOnStartup]
    public class Test
    {
        static Test()
        {
#if DEBUG
            Harmony.DEBUG = true;
#endif

            Harmony harmony = new Harmony("Cliffyq.TrafficHeatmap");
            harmony.PatchAll();
        }
    }
}
