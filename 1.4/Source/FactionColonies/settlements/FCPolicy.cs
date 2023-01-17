using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public enum FCPolicyCategory : byte
    {
        Undefined = 0,
        Trait = 1,
        Core = 2,
        Tax = 3,
        Military = 4
    }

    public class FCPolicy : IExposable
    {
        public FCPolicy()
        {

        }
        public FCPolicy( FCPolicyDef def)
        {
            FactionFC faction = Find.World.GetComponent<FactionFC>();
            this.def = def;
            timeEnacted = Find.TickManager.TicksGame;


            //Road Builder Trait
            if (def == FCPolicyDefOf.roadBuilders)
            {
                ResearchProjectDef researchdef = DefDatabase<ResearchProjectDef>.GetNamed("FCRoadBuildingDirt", false);
                if (researchdef == null)
                    Log.Message("Empire Error - Road research returned Null");
                if (!(Find.ResearchManager.GetProgress(researchdef) == researchdef.baseCost))
                {
                    Find.ResearchManager.FinishProject(researchdef);
                }

            }

            //Mercantile Trait
            if (def == FCPolicyDefOf.mercantile)
            {
                faction.resetTraitMercantileCaravanTime();
            }
        }

        public FCPolicyDef def;
        public int timeEnacted;

        public void ExposeData()
        {
            Scribe_Defs.Look(ref def, "def");
            Scribe_Values.Look(ref timeEnacted, "timeEnacted");
            
        }


    }

    public class FCPolicyDef : Def, IExposable
    {
        public void ExposeData()
        {
            Scribe_Values.Look(ref factionLevelRequirement, "factionLevelRequirement");
            Scribe_Values.Look(ref techLevelRequirement, "techLevelRequirement");
            Scribe_Values.Look(ref desc, "desc");
            Scribe_Values.Look(ref category, "category");
            Scribe_Collections.Look(ref positiveEffects, "positiveEffects", LookMode.Value);
            Scribe_Collections.Look(ref negativeEffects, "negativeEffects", LookMode.Value);
        }

        public string desc;
        public FCPolicyCategory category;
        public TechLevel techLevelRequirement;
        public int factionLevelRequirement;
        public List<string> positiveEffects;
        public List<string> negativeEffects;

        public Texture2D IconLight
        {
            get
            {
                switch (defName)
                {
                    case "militaristic":
                        return TexLoad.traitMilitaristicLight;
                    case "pacifist":
                        return TexLoad.traitPacifistLight;
                    case "authoritarian":
                        return TexLoad.traitAuthoritarianLight;
                    case "egalitarian":
                        return TexLoad.traitEgalitarianLight;
                    case "isolationist":
                        return TexLoad.traitIsolationistLight;
                    case "expansionist":
                        return TexLoad.traitExpansionistLight;
                    case "technocratic":
                        return TexLoad.traitTechnocraticLight;
                    case "feudal":
                        return TexLoad.traitFeudalLight;
                    default:
                        Log.Message("Could not find icon for " + defName);
                        return null;
                }
            }
        }

        public Texture2D IconDark
        {
            get
            {
                switch (defName)
                {
                    case "militaristic":
                        return TexLoad.traitMilitaristicDark;
                    case "pacifist":
                        return TexLoad.traitPacifistDark;
                    case "authoritarian":
                        return TexLoad.traitAuthoritarianDark;
                    case "egalitarian":
                        return TexLoad.traitEgalitarianDark;
                    case "isolationist":
                        return TexLoad.traitIsolationistDark;
                    case "expansionist":
                        return TexLoad.traitExpansionistDark;
                    case "technocratic":
                        return TexLoad.traitTechnocraticDark;
                    case "feudal":
                        return TexLoad.traitFeudalDark;
                    default:
                        Log.Message("Could not find icon for " + defName);
                        return null;
                }
            }
        }
    }

    [DefOf]
    public class FCPolicyDefOf
    {
        //Faction Traits
        public static FCPolicyDef empty;

        public static FCPolicyDef resilient;
        public static FCPolicyDef raiders;
        public static FCPolicyDef defenseInDepth;
        public static FCPolicyDef industrious;
        public static FCPolicyDef roadBuilders;
        public static FCPolicyDef mercantile;
        public static FCPolicyDef innovative;
        public static FCPolicyDef lucky;

        //Policies
        public static FCPolicyDef militaristic;
        public static FCPolicyDef pacifist;
        public static FCPolicyDef authoritarian;
        public static FCPolicyDef egalitarian;
        public static FCPolicyDef isolationist;
        public static FCPolicyDef expansionist;
        public static FCPolicyDef technocratic;
        public static FCPolicyDef feudal;

        static FCPolicyDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(FCPolicyDefOf));
        }
    }

}
