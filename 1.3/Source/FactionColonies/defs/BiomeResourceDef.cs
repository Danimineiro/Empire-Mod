using System.Collections.Generic;
using RimWorld;
using Verse;

namespace FactionColonies
{
    public class BiomeResourceDef : Def, IExposable
    {
        public List<double> BaseProductionAdditive = new List<double>();
        public List<double> BaseProductionMultiplicative = new List<double>();
        public bool canSettle;

        public void ExposeData()
        {
            Scribe_Collections.Look(ref BaseProductionAdditive, "BaseProductionAdditive", LookMode.Value);
            Scribe_Collections.Look(ref BaseProductionAdditive, "BaseProductionMultiplicative", LookMode.Value);
            Scribe_Values.Look(ref canSettle, "canSettle");
        }
    }


    [DefOf]
    public class BiomeResourceDefOf
    {
        public static BiomeResourceDef defaultBiome;
        static BiomeResourceDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(BiomeResourceDefOf));
        }
    }
}
