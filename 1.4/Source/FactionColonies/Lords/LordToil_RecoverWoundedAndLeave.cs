using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace FactionColonies
{
    class LordToil_RecoverWoundedAndLeave : LordToil
    {
        protected LordToilData_ExitMap Data
        {
            get
            {
                return (LordToilData_ExitMap) data;
            }
        }

        public LordToil_RecoverWoundedAndLeave(LordToilData_ExitMap data)
        {
            this.data = data;
        }

        public override bool AllowSatisfyLongNeeds
        {
            get
            {
                return false;
            }
        }

        public override bool AllowSelfTend
        {
            get
            {
                return false;
            }
        }

        public override void UpdateAllDuties()
        {
            for (int i = 0; i < lord.ownedPawns.Count; i++)
            {
                lord.ownedPawns[i].mindState.duty = new PawnDuty(DefDatabase<DutyDef>.GetNamed("FCTakeWoundedAndLeave")) { locomotion = Data.locomotion, canDig = Data.canDig };

                if (Data.interruptCurrentJob && lord.ownedPawns[i].jobs.curJob != null) lord.ownedPawns[i].jobs.EndCurrentJob(JobCondition.InterruptForced);
            }
        }
    }
}
