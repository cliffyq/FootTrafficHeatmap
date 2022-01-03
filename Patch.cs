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
        private static void Postfix(Pawn ___pawn, float ___nextCellCostTotal)
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
                        var heatmap = ___pawn.Map.GetComponent<FootTrafficHeatmap>();
                        heatmap.Update(___pawn, ___nextCellCostTotal);
                    }
                }
            }
        }
    }
}