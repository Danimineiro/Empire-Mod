using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace FactionColonies
{
    public class LordToil_HuntColonists : LordToil
    {
        public override bool ForceHighStoryDanger => true;

        public override bool AllowSatisfyLongNeeds => false;

        public override void Init()
        {
            base.Init();
            LessonAutoActivator.TeachOpportunity(ConceptDefOf.Drafting, OpportunityType.Critical);
        }

        public override void UpdateAllDuties()
        {
            Find.SignalManager.SendSignal(new Signal("startAssault"));
            Messages.Message(new Message("The assault is beginning!", MessageTypeDefOf.ThreatSmall));
            foreach (Pawn pawn in lord.ownedPawns)
            {
                pawn.mindState.duty = new PawnDuty(DutyDefOf.AssaultColony); //new PawnDuty(DefDatabase<DutyDef>.GetNamed("HuntColonists"));
                pawn.mindState.canFleeIndividual = false;
            }
        }
    }
}