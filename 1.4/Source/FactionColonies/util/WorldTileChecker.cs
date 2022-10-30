using System.Linq;
using System.Text;
using RimWorld.Planet;
using Verse;

namespace FactionColonies.util
{
    public static class WorldTileChecker
    {
        public static bool IsValidTileForNewSettlement(int tile, StringBuilder reason = null)
        {
            if (tile == -1)
            {
                reason?.Append("selectedInvalidTile".Translate());
                return false;
            }

            if (!TileFinder.IsValidTileForNewSettlement(tile, reason)) return false;

            foreach (WorldSettlementFC settlement in Find.WorldObjects.AllWorldObjects.Where(obj => obj.GetType() == typeof(WorldSettlementFC)))
            {
                if (Find.WorldGrid.IsNeighborOrSame(settlement.Tile, tile))
                {
                    reason?.Append("FactionBaseAdjacent".Translate());
                    return false;
                }
            }

            return true;
        }
    }
}
