using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public class WorldSettlementTraderTracker : IThingHolder, IExposable
    {
        private static List<TraderKindDef> baseTraderKinds;

        public static List<TraderKindDef> BaseTraderKinds
        {
            get
            {
                if (baseTraderKinds != null) return baseTraderKinds;
                baseTraderKinds = DefDatabase<FactionDef>.GetNamed("PColony").pawnGroupMakers
                    .SelectMany(maker => maker.options).Select(option => option.kind.defaultFactionType)
                    .Where(faction => faction?.baseTraderKinds != null)
                    .SelectMany(faction =>
                        faction.baseTraderKinds).Distinct()
                    .Where(trader => trader.tradeCurrency == TradeCurrency.Silver &&
                                     trader.TitleRequiredToTrade == null &&
                                     trader.permitRequiredForTrading == null)
                    .Select(kind =>
                    {
                        TraderKindDef temp = new TraderKindDef
                        {
                            faction = DefDatabase<FactionDef>.GetNamed("PColony"),
                            category = kind.category,
                            commonality = kind.commonality,
                            tradeCurrency = TradeCurrency.Silver,
                            commonalityMultFromPopulationIntent = kind.commonalityMultFromPopulationIntent,
                            hideThingsNotWillingToTrade = true
                        };
                        foreach (StockGenerator generator in 
                            kind.stockGenerators.Where(generator => !generator.GetType().Name.Equals("StockGenerator_Techprints")))
                        {
                            temp.stockGenerators.Add(new ColonyStockGenerator(generator));
                        }

                        return temp;
                    }).ToList();

                return baseTraderKinds;
            }
        }

        public static void reloadTraderKind()
        {
            baseTraderKinds = null;
        }
        
        public WorldSettlementFC settlement;
        private ThingOwner<Thing> stock;
        private int lastStockGenerationTicks = -1;
        private bool everGeneratedStock;
        private List<Pawn> tmpSavedPawns = new List<Pawn>();

        protected virtual int RegenerateStockEveryDays => 30;

        public IThingHolder ParentHolder => settlement;

        public List<Thing> StockListForReading
        {
            get
            {
                if (stock == null)
                    RegenerateStock();
                return stock.InnerListForReading;
            }
        }

        [CanBeNull]
        public TraderKindDef TraderKind
        {
            get
            {
                return BaseTraderKinds.Any() ? BaseTraderKinds[Mathf.Abs(settlement.HashOffset()) % BaseTraderKinds.Count] : null;
            }
        }

        public int RandomPriceFactorSeed => Gen.HashCombineInt(settlement.ID, 1933327354);

        public bool EverVisited => everGeneratedStock;

        public bool RestockedSinceLastVisit => everGeneratedStock && stock == null;

        public int NextRestockTick => stock == null || !everGeneratedStock
            ? -1
            : (lastStockGenerationTicks == -1 ? 0 : lastStockGenerationTicks) +
              RegenerateStockEveryDays * 60000;

        public virtual string TraderName => settlement.Faction == null
            ? settlement.LabelCap
            : (string) "SettlementTrader".Translate((NamedArgument) settlement.LabelCap,
                (NamedArgument) settlement.Faction.Name);

        private bool HasStockTraderKindWillTrade => stock == null || stock.InnerListForReading.Any(x => TraderKind.WillTrade(x.def));

        public virtual bool CanTradeNow
        {
            get
            {
                if (TraderKind == null) return false;
                return HasStockTraderKindWillTrade;
            }
        }

        public virtual float TradePriceImprovementOffsetForPlayer => 0.02f;

        public WorldSettlementTraderTracker()
        {
        }

        public WorldSettlementTraderTracker(WorldSettlementFC settlement) => this.settlement = settlement;

        public virtual void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                tmpSavedPawns.Clear();
                if (stock != null)
                {
                    for (int index = stock.Count - 1; index >= 0; --index)
                    {
                        if (!(stock[index] is Pawn pawn4)) continue;
                        stock.Remove(pawn4);
                        tmpSavedPawns.Add(pawn4);
                    }
                }
            }

            Scribe_Collections.Look(ref tmpSavedPawns, "tmpSavedPawns", LookMode.Reference,
                Array.Empty<object>());
            Scribe_Deep.Look(ref stock, "stock", Array.Empty<object>());
            Scribe_Values.Look(ref lastStockGenerationTicks, "lastStockGenerationTicks");
            Scribe_Values.Look(ref everGeneratedStock, "wasStockGeneratedYet");
            Scribe_References.Look(ref settlement, "settlement");
            if (Scribe.mode != LoadSaveMode.PostLoadInit && Scribe.mode != LoadSaveMode.Saving)
                return;
            foreach (Pawn pawn in tmpSavedPawns)
            {
                stock.TryAdd(pawn, false);
            }

            tmpSavedPawns.Clear();
        }

        public virtual IEnumerable<Thing> ColonyThingsWillingToBuy(Pawn playerNegotiator)
        {
            Caravan caravan = playerNegotiator.GetCaravan();
            foreach (Thing allInventoryItem in CaravanInventoryUtility.AllInventoryItems(caravan))
                yield return allInventoryItem;
            List<Pawn> pawns = caravan.PawnsListForReading;
            foreach (Pawn pawn in pawns.Where(pawn => !caravan.IsOwner(pawn)))
            {
                yield return pawn;
            }
        }

        public virtual void GiveSoldThingToTrader(Thing toGive, int countToGive, Pawn playerNegotiator)
        {
            if (stock == null)
                RegenerateStock();
            Caravan caravan = playerNegotiator.GetCaravan();
            Thing thing = toGive.SplitOff(countToGive);
            thing.PreTraded(TradeAction.PlayerSells, playerNegotiator, settlement);
            if (toGive is Pawn from)
            {
                CaravanInventoryUtility.MoveAllInventoryToSomeoneElse(from, caravan.PawnsListForReading);
                if (from.RaceProps.Humanlike || stock.TryAdd(from, false))
                    return;
                from.Destroy();
            }
            else
            {
                if (stock.TryAdd(thing, false))
                    return;
                thing.Destroy();
            }
        }

        public virtual void GiveSoldThingToPlayer(Thing toGive, int countToGive, Pawn playerNegotiator)
        {
            Caravan caravan = playerNegotiator.GetCaravan();
            Thing thing = toGive.SplitOff(countToGive);
            thing.PreTraded(TradeAction.PlayerBuys, playerNegotiator, settlement);
            if (thing is Pawn p)
            {
                caravan.AddPawn(p, true);
            }
            else
            {
                Pawn toMoveInventoryTo =
                    CaravanInventoryUtility.FindPawnToMoveInventoryTo(thing, caravan.PawnsListForReading,
                        null);
                if (toMoveInventoryTo == null)
                {
                    Log.Error("Could not find any pawn to give sold thing to.");
                    thing.Destroy();
                }
                else
                {
                    if (toMoveInventoryTo.inventory.innerContainer.TryAdd(thing))
                        return;
                    Log.Error("Could not add sold thing to inventory.");
                    thing.Destroy();
                }
            }
        }

        public virtual void TraderTrackerTick()
        {
            if (stock == null)
                return;
            if (Find.TickManager.TicksGame - lastStockGenerationTicks > RegenerateStockEveryDays * 60000)
            {
                TryDestroyStock();
            }
            else
            {
                for (int index = stock.Count - 1; index >= 0; --index)
                {
                    if (stock[index] is Pawn pawn3 && pawn3.Destroyed)
                        stock.Remove(pawn3);
                }

                for (int index = stock.Count - 1; index >= 0; --index)
                {
                    if (!(stock[index] is Pawn p3) || p3.IsWorldPawn()) continue;
                    Log.Error("Faction base has non-world-pawns in its stock. Removing...");
                    stock.Remove(p3);
                }
            }
        }

        public void TryDestroyStock()
        {
            if (stock == null)
                return;
            for (int index = stock.Count - 1; index >= 0; --index)
            {
                Thing thing = stock[index];
                stock.Remove(thing);
                if (!(thing is Pawn) && !thing.Destroyed)
                    thing.Destroy();
            }

            stock = null;
        }

        public bool ContainsPawn(Pawn p) => stock != null && stock.Contains(p);

        protected virtual void RegenerateStock()
        {
            TryDestroyStock();
            stock = new ThingOwner<Thing>(this);
            everGeneratedStock = true;
            if (settlement.Faction == null || !settlement.Faction.IsPlayer)
                stock.TryAddRangeOrTransfer(ThingSetMakerDefOf.TraderStock.root.Generate(
                    new ThingSetMakerParams
                    {
                        traderDef = TraderKind,
                        tile = settlement.Tile,
                        makingFaction = settlement.Faction
                    }));
            foreach (Thing thing in stock)
            {
                if (thing is Pawn pawn1)
                    Find.WorldPawns.PassToWorld(pawn1);
            }

            lastStockGenerationTicks = Find.TickManager.TicksGame;
        }

        public ThingOwner GetDirectlyHeldThings() => stock;

        public void GetChildHolders(List<IThingHolder> outChildren) =>
            ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }
}