using RimWorld;
using RimWorld.BaseGen;
using Verse;
using Verse.AI.Group;

namespace FactionColonies
{
    public class SymbolResolver_Colony : SymbolResolver
    {
        public override void Resolve(ResolveParams rp)
        {
            int dist = 0;
            if (rp.edgeDefenseWidth.HasValue)
                dist = rp.edgeDefenseWidth.Value;
            else if (rp.rect.Width >= 20 && rp.rect.Height >= 20 &&
                     (rp.faction.def.techLevel >= TechLevel.Industrial || Rand.Bool))
                dist = Rand.Bool ? 2 : 4;
            
            if (rp.faction.def.techLevel >= TechLevel.Industrial)
            {
                BaseGen.symbolStack.Push("outdoorLighting", rp);
                int num = Rand.Chance(0.75f) ? GenMath.RoundRandom(rp.rect.Area / 400f) : 0;
                for (int index = 0; index < num; ++index)
                {
                    ResolveParams resolveParams2 = rp;
                    resolveParams2.faction = rp.faction;
                   BaseGen.symbolStack.Push("firefoamPopper", resolveParams2);
                }
            }

            bool? nullable1;
            if (dist > 0)
            {
                ResolveParams resolveParams2 = rp;
                resolveParams2.faction = rp.faction;
                resolveParams2.edgeDefenseWidth = dist;
                ref ResolveParams local = ref resolveParams2;
                nullable1 = rp.edgeThingMustReachMapEdge;
                bool? nullable2 = !nullable1.HasValue || nullable1.GetValueOrDefault();
                local.edgeThingMustReachMapEdge = nullable2;
                BaseGen.symbolStack.Push("edgeDefense", resolveParams2);
            }

            ResolveParams resolveParams3 = rp;
            resolveParams3.rect = rp.rect.ContractedBy(dist);
            resolveParams3.faction = rp.faction;
            BaseGen.symbolStack.Push("ensureCanReachMapEdge", resolveParams3);
            ResolveParams resolveParams4 = rp;
            resolveParams4.rect = rp.rect.ContractedBy(dist);
            resolveParams4.faction = rp.faction;
            ref ResolveParams local1 = ref resolveParams4;
            nullable1 = rp.floorOnlyIfTerrainSupports;
            bool? nullable3 = !nullable1.HasValue || nullable1.GetValueOrDefault();
            local1.floorOnlyIfTerrainSupports = nullable3;
            BaseGen.symbolStack.Push("basePart_outdoors", resolveParams4);
            ResolveParams resolveParams5 = rp;
            resolveParams5.floorDef = TerrainDefOf.Bridge;
            ref ResolveParams local2 = ref resolveParams5;
            nullable1 = rp.floorOnlyIfTerrainSupports;
            bool? nullable4 = !nullable1.HasValue || nullable1.GetValueOrDefault();
            local2.floorOnlyIfTerrainSupports = nullable4;
            ref ResolveParams local3 = ref resolveParams5;
            nullable1 = rp.allowBridgeOnAnyImpassableTerrain;
            bool? nullable5 = !nullable1.HasValue || nullable1.GetValueOrDefault();
            local3.allowBridgeOnAnyImpassableTerrain = nullable5;
            BaseGen.symbolStack.Push("floor", resolveParams5);
        }
    }
}