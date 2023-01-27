using FactionColonies.util;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace FactionColonies
{
    [HarmonyPatch(typeof(JobDriver_Goto), "TryExitMap")]
    public class Patch
    {
        static bool Prefix(ref JobDriver_Goto __instance)
        {
            Pawn pawn = __instance.pawn;
            return !(pawn.IsMercenary() && pawn.Map.Parent is WorldSettlementFC);
        }
    }
}
