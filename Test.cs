using HarmonyLib;
using Verse;
using Verse.AI;

namespace TrafficHeatmap
{
    //[StaticConstructorOnStartup]
    [HarmonyPatch(typeof(Pawn_PathFollower))]
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

        [HarmonyPostfix]
        [HarmonyPatch(nameof(Pawn_PathFollower.PatherTick))]
        static void Postfix1(Pawn ___pawn, bool ___moving, float ___nextCellCostLeft, IntVec3 ___lastCell)
        {
            if (___pawn.IsColonist)
            {
                Log.Message($"PatherTick. Pawn: {___pawn.ToString()}, pawn position : {___pawn.Position}, las cell: {___lastCell}, is moving: {___moving}, next cell cost left: {___nextCellCostLeft}, tick:{Find.TickManager.TicksGame}");
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch("TryEnterNextPathCell")]
        static void Postfix2(Pawn ___pawn, bool ___moving, float ___nextCellCostLeft, IntVec3 ___lastCell)
        {
            if (___pawn.IsColonist)
            {
                Log.Message($"TryEnterNextPathCell. Pawn: {___pawn.ToString()}, pawn position : {___pawn.Position}, las cell: {___lastCell}, is moving: {___moving}, next cell cost left: {___nextCellCostLeft}, tick:{Find.TickManager.TicksGame}");
            }
        }
    }
}
