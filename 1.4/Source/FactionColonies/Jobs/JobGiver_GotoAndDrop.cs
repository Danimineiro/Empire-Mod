using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace FactionColonies
{
    class JobGiver_GotoAndDrop : ThinkNode_JobGiver
    {
        protected override Job TryGiveJob(Pawn pawn)
        {
            LocalTargetInfo targetInfo = pawn.mindState.duty.focus;
            if (!pawn.CanReach(targetInfo, PathEndMode.OnCell, PawnUtility.ResolveMaxDanger(pawn, maxDanger), false, false, TraverseMode.ByPawn))
            {
                return null;
            }

            if (pawn.carryTracker.CarriedThing == null)
            {
                pawn.GetLord().Notify_ReachedDutyLocation(pawn);
                return null;
            }

            if (targetInfo.Pawn is Pawn target && target.carryTracker.CarriedThing != null)
            {
                return new Job(DefDatabase<JobDef>.GetNamed("FCFollowCloseAndCarry"), targetInfo)
                {
                    followRadius = 4,
                };
            }

            return new Job(DefDatabase<JobDef>.GetNamed("FCGotoAndDrop"), targetInfo);
        }

        protected Danger maxDanger = Danger.Some;
    }
}
