using System.Collections.Generic;
using System.Linq;
using Verse;
using Verse.AI;

namespace FactionColonies
{
    class JobDriver_FollowCloseAndCarry : JobDriver_Follow
    {
        protected override IEnumerable<Toil> MakeNewToils()
        {
            List<Toil> toils = base.MakeNewToils().ToList();

            toils.ForEach(toil => toil.AddFailCondition(delegate
            {
                return ((Pawn)job.GetTarget(TargetIndex.A).Thing).carryTracker.CarriedThing == null;
            }));

            return toils;
        }
    }
}
