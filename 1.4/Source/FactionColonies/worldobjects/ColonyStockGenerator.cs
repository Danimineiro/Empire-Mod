using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace FactionColonies
{
    public class ColonyStockGenerator : StockGenerator
    {
        private StockGenerator parent;
        private FactionFC faction = Find.World.GetComponent<FactionFC>();

        public ColonyStockGenerator(StockGenerator parent)
        {
            this.parent = parent;
        }
        
        public override IEnumerable<Thing> GenerateThings(int forTile, Faction faction = null)
        {
            return parent.GenerateThings(forTile, faction).Where(thing => HandlesThingDef(thing.def));
        }

        public override bool HandlesThingDef(ThingDef thingDef)
        {
            return thingDef.techLevel <= faction.techLevel || thingDef is StockGenerator_Techprints && 
                   (!thingDef.tradeTags?.Contains("ExoticMisc") ?? true) && 
                   parent.HandlesThingDef(thingDef);
        }


        public override IEnumerable<string> ConfigErrors(TraderKindDef parentDef)
        {
            return parent.ConfigErrors(parentDef);
        }
        
        public override void ResolveReferences(TraderKindDef trader)
        {
            parent.ResolveReferences(trader);
        }
    }
}