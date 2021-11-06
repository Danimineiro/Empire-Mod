using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public class BuildingFCDef : Def, IExposable
    {
        public void ExposeData()
        {
            Scribe_Values.Look(ref desc, "desc");
            Scribe_Values.Look(ref cost, "cost");
            Scribe_Values.Look(ref techLevel, "techLevel");
            Scribe_Values.Look(ref constructionDuration, "constructionDuration");
            Scribe_Collections.Look(ref traits, "traits", LookMode.Def);
            Scribe_Collections.Look(ref applicableBiomes, "applicableBiomes", LookMode.Value);
            Scribe_Values.Look(ref upkeep, "upkeep");
            Scribe_Values.Look(ref iconPath, "iconPath");
            Scribe_Values.Look(ref shuttleUses, "shuttleUses");
            Scribe_Values.Look(ref requiresRoyality, "requiresRoyality");
            Scribe_Values.Look(ref requiresIdeology, "requiresIdeology");
            Scribe_Collections.Look(ref requiredModsID, "requiredMods", LookMode.Value);
        }

        public string desc;
        public double cost;
        public int constructionDuration;
        public TechLevel techLevel = TechLevel.Undefined;
        public List<FCTraitEffectDef> traits;
        public List<string> applicableBiomes = new List<string>();
        public int upkeep = 0;
        public string iconPath = "GUI/unrest";
        public Texture2D iconLoaded;
        public int shuttleUses = 0;
        public bool requiresRoyality = false;
        public bool requiresIdeology = false;
        public List<string> requiredModsID = new List<string>();
        //public required research

        public Texture2D icon
        {
            get
            {
                if (iconLoaded == null)
                {
                    if (!iconPath.NullOrEmpty()) {
                        iconLoaded = ContentFinder<Texture2D>.Get(iconPath);
                    } else
                    {
                        Log.Message("Failed to load icon");
                        iconLoaded = TexLoad.questionmark;
                    }
                }
                return iconLoaded;
            }
        }

        public bool RequiredModsLoaded => (ModsConfig.RoyaltyActive || !requiresRoyality) && (ModsConfig.IdeologyActive || !requiresIdeology) && requiredModsID.TrueForAll(mod => ModsConfig.IsActive(mod));
    }

    [DefOf]
    public class BuildingFCDefOf
    {
        public static BuildingFCDef Empty;
        public static BuildingFCDef Construction;
        public static BuildingFCDef artilleryOutpost;
        public static BuildingFCDef shuttlePort;
        static BuildingFCDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(BuildingFCDefOf));
        }
    }

}
