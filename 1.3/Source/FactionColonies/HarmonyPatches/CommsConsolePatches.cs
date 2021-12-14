using HarmonyLib;
using RimWorld;
using Verse;

namespace FactionColonies
{
    /// <summary>
 /// This patch disables the military aid a player can receive through the comms console for our faction
 /// </summary>
    [HarmonyPatch(typeof(FactionDialogMaker), "RequestMilitaryAidOption")]
    class DisableMilitaryAid
    {
        static void Postfix(Map map, Faction faction, Pawn negotiator, ref DiaOption __result)
        {
            if (faction.def.defName != "PColony") return;
            __result = new DiaOption("RequestMilitaryAid".Translate(25));
            __result.Disable("Disabled. Use the settlements military tab.");
        }
    }
}
