using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace FactionColonies
{
    [HarmonyPatch]
    public class WorldSettlementTransportPodDefendAction : TransportPodsArrivalAction_LandInSpecificCell
    {
    private readonly IntVec3 cell;
    private readonly MapParent mapParent;
    private readonly bool landInShuttle;

    public WorldSettlementTransportPodDefendAction(WorldSettlementFC mapParent, IntVec3 cell, bool landInShuttle)
    {
        this.mapParent = mapParent;
        this.cell = cell;
        this.landInShuttle = landInShuttle;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TransportPodsArrivalAction_LandInSpecificCell), "Arrived")]
    private static void ArrivePatch(TransportPodsArrivalAction_LandInSpecificCell __instance, List<ActiveDropPodInfo> pods, int tile)
    {
        if (Traverse.Create(__instance).Field("mapParent").GetValue() is WorldSettlementFC settlement)
        {
            List<Pawn> pawns = new List<Pawn>();
            bool hasAnyPawns = false;

            foreach (ActiveDropPodInfo activeDropPodInfo in pods)
            {
                foreach (Thing thing in activeDropPodInfo.innerContainer)
                {
                    if (thing is Pawn pawn)
                    {
                        hasAnyPawns = true;
                        pawns.Add(pawn);
                    }
                }
            }

            if (hasAnyPawns) settlement.AddToDefenceFromList(pawns, tile);
        }
    }
    }
}
