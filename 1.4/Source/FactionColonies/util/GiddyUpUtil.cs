using System;
using System.Reflection;
using RimWorld;
using Verse;
using Verse.AI;

namespace FactionColonies.util
{
    [StaticConstructorOnStartup]
    public class GiddyUpUtil
    {
        private static readonly JobDef Mounting;
        
        //TODO Swap to https://github.com/rheirman/GiddyUpCore/blob/master/Source/Giddy-up-Core/Utilities/NPCMountUtility.cs
        static GiddyUpUtil()
        {
            Mounting = DefDatabase<JobDef>.GetNamedSilentFail("Mount");
        }
        
        public static void Mount(Pawn rider, Pawn animal)
        {
            if (Mounting == null) return;
            Job mountJob = new Job(Mounting,
                new LocalTargetInfo(animal)) {count = 1};
            rider.jobs.StartJob(mountJob);
        }
    }
}