using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace FactionColonies
{
    [HarmonyPatch(typeof(WorldPawns), "PassToWorld")]
    class MercenaryPassToWorld
    {
        static bool Prefix(Pawn pawn, PawnDiscardDecideMode discardMode = PawnDiscardDecideMode.Decide)
        {
            FactionFC faction = Find.World.GetComponent<FactionFC>();
            return faction?.militaryCustomizationUtil?.AllMercenaryPawns == null || !faction.militaryCustomizationUtil.AllMercenaryPawns.Contains(pawn);
        }
    }
}
