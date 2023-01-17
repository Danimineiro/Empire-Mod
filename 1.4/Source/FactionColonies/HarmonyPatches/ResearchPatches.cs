using HarmonyLib;
using RimWorld;
using Verse;

namespace FactionColonies
{
    [HarmonyPatch(typeof(ResearchManager), "FinishProject")]
    class ResearchCompleted
    {
        static void Postfix(ResearchProjectDef proj, bool doCompletionDialog = false, Pawn researcher = null)
        {
            FactionFC fc = Find.World.GetComponent<FactionFC>();
            fc.roadBuilder.CheckForTechChanges();
        }
    }
}
