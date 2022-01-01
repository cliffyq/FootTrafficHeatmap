using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace TrafficHeatmap
{
    [HarmonyPatch(typeof(Pawn_PathFollower))]
    internal class Patch
    {
        [HarmonyPostfix]
        [HarmonyPatch("SetupMoveIntoNextCell")]
        static void Postfix(Pawn ___pawn, bool ___moving, float ___nextCellCostLeft, IntVec3 ___lastCell, IntVec3 ___nextCell, float ___nextCellCostTotal)
        {
            if (___pawn.IsColonist && !___pawn.Dead && !___pawn.Downed && ___pawn.Awake())
            {
                if (___pawn.CurJob != null)
                {
                    var locomotionUrgencySameAs = ___pawn.jobs.curDriver.locomotionUrgencySameAs;
                    var urgency = ___pawn.jobs.curJob.locomotionUrgency;
                    if ((locomotionUrgencySameAs != null && locomotionUrgencySameAs != ___pawn && locomotionUrgencySameAs.Spawned) ||
                        (urgency != LocomotionUrgency.Amble && urgency != LocomotionUrgency.Walk))
                    {
                        var heatmap = ___pawn.Map.GetComponent<TrafficHeatmap>();
                        var heatmapInstance = TrafficHeatmap.Instances[___pawn.Map];
                        if (heatmapInstance != heatmap)
                        {
                            Log.Error("2 instances?");
                        }
                        heatmap.Update(___pawn, ___nextCellCostTotal);
                        //var heatmap1 = ___pawn.Map.GetComponent<TrafficHeatmapSqrt>();
                        //heatmap1.Update(___pawn, ___nextCellCostTotal);

                        Log.Message($"SetupMoveIntoNextCell. Pawn: {___pawn}, heatmap: {heatmap.ToString()}");
                    }
                }
            }
        }

        static string GetLocomotionUrgency(Pawn pawn)
        {
            if (pawn.CurJob == null)
            {
                return "NoCurJob";
            }
            Pawn locomotionUrgencySameAs = pawn.jobs.curDriver.locomotionUrgencySameAs;
            if (locomotionUrgencySameAs != null && locomotionUrgencySameAs != pawn && locomotionUrgencySameAs.Spawned)
            {
                return $"(Same as {locomotionUrgencySameAs.ToString()}){GetLocomotionUrgency(locomotionUrgencySameAs)}";
            }
            return pawn.jobs.curJob.locomotionUrgency.ToString();
        }
    }
}
