using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FactionColonies.util
{
    static class MapCellAndTileExtensions
    {
        public static bool CellBlockedByThing(this IntVec3 targetCell, Map map)
        {
            foreach (Thing thing in targetCell.GetThingList(map))
            {
                if (thing is IActiveDropPod || thing is Skyfaller || thing.def.category == ThingCategory.Building)
                {
                    return true;
                }
                if (thing.def.category == ThingCategory.Item)
                {
                    return true;
                }
                PlantProperties plant = thing.def.plant;
                if (plant != null && plant.IsTree)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool CellFullFillsSpaceRequirement(this IntVec3 targetCell, IntVec2 intVec2, Map map)
        {
            foreach (IntVec3 cell in GenAdj.OccupiedRect(targetCell, Rot4.North, intVec2))
            {
                RoofDef roof = cell.GetRoof(map);
                return !(!cell.InBounds(map) || !cell.Walkable(map) || (roof != null && (roof.isNatural || roof.isThickRoof)) || CellBlockedByThing(targetCell, map));
            }
            return true;
        }

        public static bool HasWoundedForFaction(this Map map, Faction forFaction) => map.mapPawns.SpawnedDownedPawns.Any(pawn => pawn.Faction == forFaction && !pawn.IsPrisoner);

        public static bool IsValidTile(this int tile) => tile >= 0;

        public static bool AreValidTiles(this (int, int) tiles) => tiles.Item1.IsValidTile() && tiles.Item2.IsValidTile();

        public static bool IsInAnyShuttleRange(this int tile) => tile.IsValidTile() && Find.World.GetComponent<FactionFC>().settlements.Any(settlement => settlement.buildings.Contains(BuildingFCDefOf.shuttlePort) && Find.WorldGrid.TraversalDistanceBetween(settlement.worldSettlement.Tile, tile) <= ShuttleSender.ShuttleRange);

        public static bool AreTilesInAnyShuttleRange(this (int, int) tiles) => tiles.Item1.IsInAnyShuttleRange() && tiles.Item2.IsInAnyShuttleRange();
    }
}
