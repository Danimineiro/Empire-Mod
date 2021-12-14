using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FactionColonies.util
{
    /// <summary>
    /// This class provides extentions for Tiles, Cells and Maps
    /// </summary>
    static class MapCellAndTileExtensions
    {
        /// <summary>
        /// Checks if a given <paramref name="targetCell"/> on a <paramref name="map"/> is blocked
        /// </summary>
        /// <param name="targetCell"></param>
        /// <param name="map"></param>
        /// <returns>true if it is blocked, false otherwise</returns>
        public static bool CellBlockedByThing(this IntVec3 targetCell, Map map)
        {
            foreach (Thing thing in targetCell.GetThingList(map))
            {
                if (thing is IActiveDropPod || thing is Skyfaller || thing.def.category == ThingCategory.Building || thing.def.category == ThingCategory.Item) return true;

                PlantProperties plant = thing.def.plant;
                if (plant != null && plant.IsTree) return true;
            }

            return false;
        }
        /// <summary>
        /// Checks if a <paramref name="targetCell"/> is naturally or mountain roofed
        /// </summary>
        /// <param name="targetCell"></param>
        /// <param name="map"></param>
        /// <returns>true if natural or mountain roofed, false otherwise</returns>
        public static bool CellIsRoofedNaturalOrThick(this IntVec3 targetCell, Map map) => targetCell.GetRoof(map) is RoofDef roof && (roof.isNatural || roof.isThickRoof);

        /// <summary>
        /// Checks if a given space with size <paramref name="spaceSize"/> at <paramref name="targetCell"/> fulfills the space requirements of a SkyFaller
        /// </summary>
        /// <param name="targetCell"></param>
        /// <param name="spaceSize"></param>
        /// <param name="map"></param>
        /// <returns>true if there is enough space, false otherwise</returns>
        public static bool CellFulfilsSpaceRequirementForSkyFaller(this IntVec3 targetCell, IntVec2 spaceSize, Map map, bool ignoreRoofing = false)
        {
            foreach (IntVec3 cell in GenAdj.OccupiedRect(targetCell, Rot4.North, spaceSize))
            {
                return !(!cell.InBounds(map) || !cell.Walkable(map) || (targetCell.CellIsRoofedNaturalOrThick(map) && !ignoreRoofing) || CellBlockedByThing(targetCell, map));
            }
            return true;
        }

        /// <param name="map"></param>
        /// <param name="faction"></param>
        /// <returns>true if the <paramref name="map"/> contains downed pawns for a <paramref name="faction"/>, false if it doesn't</returns>
        public static bool HasDownedForFaction(this Map map, Faction faction) => map.mapPawns.SpawnedDownedPawns.Any(pawn => pawn.Faction == faction && !pawn.IsPrisoner);

        /// <summary>
        /// Checks if a <paramref name="tile"/> is valid
        /// </summary>
        /// <param name="tile"></param>
        /// <returns>true if the tile >= 0, false otherwise</returns>
        public static bool IsValidTile(this int tile) => tile >= 0;

        /// <summary>
        /// Checks if all <paramref name="tiles"/> in a touple are valid tiles
        /// </summary>
        /// <param name="tiles"></param>
        /// <returns>true if all tiles are >= 0, false otherwise</returns>
        public static bool AreValidTiles(this (int, int) tiles) => tiles.Item1.IsValidTile() && tiles.Item2.IsValidTile();

        /// <summary>
        /// Checks if the given <paramref name="tile"/> is inside a shuttle ports range
        /// </summary>
        /// <param name="tile"></param>
        /// <returns>true if it is, false otherwise</returns>
        public static bool IsInAnyShuttleRange(this int tile) => tile.IsValidTile() && Find.World.GetComponent<FactionFC>().settlements.Any(settlement => settlement.buildings.Contains(BuildingFCDefOf.shuttlePort) && Find.WorldGrid.TraversalDistanceBetween(settlement.worldSettlement.Tile, tile) <= ShuttleSender.ShuttleRange);

        /// <summary>
        /// Checks if a touple of tiles is inside any shuttle ports range
        /// </summary>
        /// <param name="tiles"></param>
        /// <returns>true if they are, false otherwise</returns>
        public static bool AreTilesInAnyShuttleRange(this (int, int) tiles) => tiles.Item1.IsInAnyShuttleRange() && tiles.Item2.IsInAnyShuttleRange();
    }
}
