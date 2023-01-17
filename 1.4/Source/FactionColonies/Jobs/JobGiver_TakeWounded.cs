using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace FactionColonies
{
    class JobGiver_TakeWounded : ThinkNode_JobGiver
    {
		protected override Job TryGiveJob(Pawn searcher)
		{
            if (!RCellFinder.TryFindBestExitSpot(searcher, out IntVec3 cell, TraverseMode.ByPawn))
            {
                return null;
            }

            Pawn wounded = ReachableWounded(searcher);
			if (wounded == null)
			{
				return null;
			}
			Job job = JobMaker.MakeJob(JobDefOf.Kidnap);
			job.targetA = wounded;
			job.targetB = cell;
			job.count = 1;
			return job;
		}

		private static Pawn ReachableWounded(Pawn searcher)
        {
			List<Pawn> list = searcher.Map.mapPawns.SpawnedPawnsInFaction(searcher.Faction);
			for (int i = 0; i < list.Count; i++)
			{
				Pawn pawn = list[i];
				if (!pawn.IsPrisoner && pawn.Downed && searcher.CanReserveAndReach(pawn, PathEndMode.OnCell, Danger.Some, 1, -1, null, false))
				{
					return pawn;
				}
			}
			return null;
		}

		protected Danger maxDanger = Danger.Some;
	}
}
