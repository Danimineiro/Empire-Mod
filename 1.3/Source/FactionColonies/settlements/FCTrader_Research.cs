using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using RimWorld.Planet;
using UnityEngine;
using HarmonyLib;

namespace FactionColonies
{
    public class FCTrader_Research : ITrader
    {

        FactionFC factionfc;

        public FCTrader_Research()
        {
            factionfc = Find.World.GetComponent<FactionFC>();
        }







        public IEnumerable<Thing> ColonyThingsWillingToBuy(Pawn playerNegotiator)
        {
            foreach (Map map in Find.Maps)
            {
                if (map.IsPlayerHome)
                {
                    foreach (Thing thing in map.listerThings.AllThings)
                    {
                        if (thing.IsInAnyStorage() == true && thing.def.category == ThingCategory.Item && TradeUtility.PlayerSellableNow(thing, this) && !FactionColonies.canCraftItem(thing.def, true))
                        {
                            yield return thing;
                        }
                    }
                }
            }

        }

        public void GiveSoldThingToPlayer(Thing toGive, int countToGive, Pawn playerNegotiator)
        {
            //Nothing to give dude
        }

        public void GiveSoldThingToTrader(Thing toGive, int countToGive, Pawn playerNegotiator)
        {
            Thing thing = toGive.SplitOff(countToGive);

            Log.Message(thing.MarketValue + " added to research pool");
            factionfc.researchPointPool += thing.MarketValue;
            factionfc.tradedAmount += thing.MarketValue;
            thing.Destroy(DestroyMode.Vanish);

        }

        public IEnumerable<Thing> Goods
        {
            get
            { //Possibly set to empty list?
                return Enumerable.Empty<Thing>();
            }
        }

        public TraderKindDef TraderKind
        {
            get
            {
                return DefDatabase<TraderKindDef>.GetNamed("FCResearchTrader");
            }
        }

        public int RandomPriceFactorSeed
        {
            get
            {
                return 0;
            }
        }

        public string TraderName
        {
            get
            {
                return "Faction Researcher";
            }
        }

        public bool CanTradeNow
        {
            get
            {
                return true;
            }
        }

        public float TradePriceImprovementOffsetForPlayer
        {
            get
            {
                return 0f;
            }
        }

        public Faction Faction
        {
            get
            {
                return FactionColonies.getPlayerColonyFaction();
            }
        }

        public TradeCurrency TradeCurrency
        {
            get
            {
                return TradeCurrency.Silver;
            }
        }

    }

    public class StockGenerator_BuyResearchable : StockGenerator
    {
        public override IEnumerable<Thing> GenerateThings(int forTile, Faction faction = null)
        {
            yield break;
        }

        public override bool HandlesThingDef(ThingDef thingDef)
        {
            FactionFC factionfc = Find.World.GetComponent<FactionFC>();
            return !FactionColonies.canCraftItem(thingDef, true);
        }
    }
}
