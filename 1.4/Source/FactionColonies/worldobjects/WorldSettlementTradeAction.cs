using System.Collections.Generic;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace FactionColonies
{
    public class WorldSettlementTradeAction : CaravanArrivalAction
    {
        private WorldSettlementFC settlement;

        public override string Label => "TradeWithSettlement".Translate((NamedArgument)settlement.Label);

        public override string ReportString => "CaravanTrading".Translate((NamedArgument)settlement.Label);

        public WorldSettlementTradeAction()
        {
        }

        public WorldSettlementTradeAction(WorldSettlementFC settlement) => this.settlement = settlement;

        public override FloatMenuAcceptanceReport StillValid(
          Caravan caravan,
          int destinationTile)
        {
            FloatMenuAcceptanceReport acceptanceReport = base.StillValid(caravan, destinationTile);
            if (!acceptanceReport)
                return acceptanceReport;
            return settlement != null && settlement.Tile != destinationTile
              ? (FloatMenuAcceptanceReport)false
              : CanTradeWith(caravan, settlement);
        }

        public override void Arrived(Caravan caravan)
        {
            CameraJumper.TryJumpAndSelect((GlobalTargetInfo)(WorldObject)caravan);
            Find.WindowStack.Add(new Dialog_Trade(
              BestCaravanPawnUtility.FindBestNegotiator(caravan, settlement.Faction, settlement.TraderKind), settlement));
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref settlement, "settlement");
        }

        public static FloatMenuAcceptanceReport CanTradeWith(
          Caravan caravan,
          WorldSettlementFC settlement)
        {
            return settlement != null && settlement.Spawned && (!settlement.HasMap) &&
                   (settlement.CanTradeNow) && HasNegotiator(caravan, settlement);
        }

        private static bool HasNegotiator(Caravan caravan, WorldSettlementFC settlement)
        {
            Pawn bestNegotiator =
              BestCaravanPawnUtility.FindBestNegotiator(caravan, settlement.Faction, settlement.TraderKind);
            return bestNegotiator != null && !bestNegotiator.skills.GetSkill(SkillDefOf.Social).TotallyDisabled;
        }

        private static FloatMenuAcceptanceReport CheckSettlementAndNegotiator(WorldSettlementFC worldSettlementFC, Pawn bestNegotiator)
        {
            return worldSettlementFC.Spawned && worldSettlementFC.CanTradeNow && (!bestNegotiator?.skills.GetSkill(SkillDefOf.Social).TotallyDisabled ?? false);
        }

        public static IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan, WorldSettlementFC settlement)
        {
            Pawn bestNegotiator = BestCaravanPawnUtility.FindBestNegotiator(caravan, settlement.Faction, settlement.TraderKind);
            foreach (FloatMenuOption option in CaravanArrivalActionUtility.GetFloatMenuOptions(() => CheckSettlementAndNegotiator(settlement, bestNegotiator), () => new WorldSettlementTradeAction(settlement), "TradeWith".Translate(settlement.Label), caravan, settlement.Tile, settlement))
            {
                yield return option;
            }
        }
    }
}