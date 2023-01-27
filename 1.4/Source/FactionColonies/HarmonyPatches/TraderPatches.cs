using HarmonyLib;
using RimWorld;
using Verse;

namespace FactionColonies
{
    //Remove checking for silver
    [HarmonyPatch(typeof(TradeDeal), "DoesTraderHaveEnoughSilver")]
    class TradeHarmonyPatchEnoughSilver
    {
        static void Postfix(ref bool __result)
        {
            if (TradeSession.trader.TraderKind.defName == "FCResearchTrader")
            {
                __result = true;
            }
        }
    }

    //Price in research trading
    [HarmonyPatch(typeof(Tradeable), "GetPriceFor")]
    class TradeHarmonyPatchItemPrice
    {
        static void Postfix(ref Tradeable __instance, ref float __result)
        {
            if (TradeSession.trader.TraderKind.defName == "FCResearchTrader")
            {
                __result = __instance.AnyThing.MarketValue;
            }
        }
    }

    //Player traded
    [HarmonyPatch(typeof(TradeDeal), "TryExecute")]
    class TradeHarmonyPatchFinishedTrade
    {
        static void Postfix(ref TradeDeal __instance, ref bool __result)
        {

            if (TradeSession.trader.TraderKind.defName == "FCResearchTrader")
            {
                FactionFC faction = Find.World.GetComponent<FactionFC>();
                if (__result == true && faction.tradedAmount != 0)
                {
                    Find.LetterStack.ReceiveLetter("FCFactionResearch".Translate(), TranslatorFormattedStringExtensions.Translate("PointsAddedToResearchPool", faction.tradedAmount), LetterDefOf.PositiveEvent);
                    faction.tradedAmount = 0;
                }
            }
        }
    }
}
