using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI.Group;

namespace FactionColonies
{
    public class WorldSettlementDefendAction : CaravanArrivalAction
    {
        private WorldSettlementFC settlement;

        //For saving
        public WorldSettlementDefendAction()
        {
        }
        
        public WorldSettlementDefendAction(WorldSettlementFC settlement)
        {
            this.settlement = settlement;
        }

        public override void Arrived(Caravan caravan) => settlement.startDefence(MilitaryUtilFC.returnMilitaryEventByLocation(settlement.settlement.mapLocation),() => settlement.CaravanDefend(caravan));
        
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref settlement, "settlement");
        }
        
        public override string Label => "DefendColony".Translate();

        public override string ReportString => "DefendColonyDesc".Translate();
        
        public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(
            Caravan caravan,
            WorldSettlementFC settlement)
        {
            return CaravanArrivalActionUtility.GetFloatMenuOptions(
                () => settlement.Spawned && settlement.settlement.isUnderAttack,
                () => new WorldSettlementDefendAction(settlement),
                "DefendColony".Translate(), caravan,
                settlement.Tile, settlement);
        }
    }
}