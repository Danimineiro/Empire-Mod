using Verse;
using Verse.AI.Group;

namespace FactionColonies
{
    class LordJob_ColonistsIdle : LordJob
    {
        public override bool AddFleeToil => false;
        public override bool AllowStartNewGatherings => false;
        public override bool AlwaysShowWeapon => true;

        public override void LordJobTick()
        {
            base.LordJobTick();
        }

        public override StateGraph CreateGraph()
        {
            StateGraph stateGraph = new StateGraph();
            stateGraph.AddToil(new LordToil_IdleNearby());
            return stateGraph;
        }

        public override void Notify_PawnLost(Pawn pawn, PawnLostCondition condition)
        {
            FactionFC faction = Find.World.GetComponent<FactionFC>();

            //Check if a settlement battle ended
            SettlementFC settlement = faction.getSettlement(pawn.Tile, Find.World.info.name);
            settlement?.worldSettlement.removeDefender(pawn);
        }
    }
}
