using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld.Planet;
using RimWorld;
using UnityEngine;
using Verse;
using HarmonyLib;

namespace FactionColonies
{
    public class FCRoadBuilder : IExposable
    {
        public List<FCPlanetRoadQueue> roadQueues = new List<FCPlanetRoadQueue>();
        public RoadDef roadDef;

        public int daysBetweenTicks = 3;
        public bool roadBuildingEnabled = true;
        public bool wasRoadBuildingDisabled = true;
        bool roadBuilders;

        public FCRoadBuilder()
        {
        }

        public void ExposeData()
        {
            Scribe_Defs.Look<RoadDef>(ref roadDef, "roadDef");
            Scribe_Values.Look<int>(ref daysBetweenTicks, "daysBetweenTicks");
            Scribe_Values.Look<bool>(ref roadBuildingEnabled, "roadBuildingEnabled");
            Scribe_Values.Look<bool>(ref wasRoadBuildingDisabled, "wasRoadBuildingDisabled");
            Scribe_Collections.Look<FCPlanetRoadQueue>(ref roadQueues, "roadQueues", LookMode.Deep, new object[] { Find.World.info.name, this.roadDef, this.daysBetweenTicks });
        }

        public void RemoveInvalidQueues()
        {
            this.roadQueues.RemoveAll(rq => rq == null || rq.planetName.NullOrEmpty());
        }

        public void FirstTick()
        {
            this.RemoveInvalidQueues();
            this.CheckForTechChanges();
            this.CreateRoadQueue(Find.World.info.name, false);
            this.FlagUpdateRoadQueues();

            if (this.daysBetweenTicks == 0)
            {
                Log.Message("Empire - Resetting daysBetweenTicks");
                int days = this.roadBuilders ? 1 : 3;
                this.daysBetweenTicks = days;
                foreach (FCPlanetRoadQueue queue in this.roadQueues)
                {
                    queue.daysBetweenTicks = days;
                }
            }
        }

        public void RoadTick()
        {
            if (this.roadDef == null || !this.roadBuildingEnabled)
            {
                this.wasRoadBuildingDisabled = true;
                return;
            }

            // Every 20 ticks causes a slight stutter, but the game is still playable
            // TODO: Make this a config option
            if(Find.TickManager.TicksGame % 20 == 0)
            {
                FactionFC faction = Find.World.GetComponent<FactionFC>();
                FCPlanetRoadQueue queue = this.GetRoadQueue(Find.World.info.name);

                if (!roadBuilders && faction.hasTrait(FCPolicyDefOf.roadBuilders))
                {
                    foreach (FCPlanetRoadQueue prq in this.roadQueues)
                    {
                        prq.shouldUpdateSettlementsToProcess = true;
                        prq.daysBetweenTicks = 1;
                    }
                    this.roadBuilders = true;
                    this.daysBetweenTicks = 1;
                }

                // If road building was disabled, then set the next tick to make a road
                // to the correct time
                if (this.wasRoadBuildingDisabled)
                {
                    this.wasRoadBuildingDisabled = false;
                    foreach (FCPlanetRoadQueue prq in this.roadQueues)
                    {
                        prq.nextRoadTick = Find.TickManager.TicksGame + GenDate.TicksPerDay * prq.daysBetweenTicks;
                    }
                }

                queue.ProcessOnePath();
                queue.BuildRoadSegments();
            }
        }


        /// <summary>
        /// Gets the road queue specified by planetName.
        /// </summary>
        /// <returns>The road queue. May be null.</returns>
        /// <param name="planetName">Planet name.</param>
        public FCPlanetRoadQueue GetRoadQueue(string planetName)
        {
            return roadQueues.FirstOrFallback(rq => rq.planetName == planetName, null);
        }

        // Returns whether or not a settlement would be built to.
        public static bool IsValidRoadTarget(Settlement settlement)
        {
            FactionFC fC = Find.World.GetComponent<FactionFC>();

            // If faction exists and is either player or player has roadBuilders and the faction is an ally
            if (settlement.Faction != null)
                if (settlement.Faction.IsPlayer || (fC.hasTrait(FCPolicyDefOf.roadBuilders) && settlement.Faction.PlayerRelationKind == FactionRelationKind.Ally))
                    return true;

            foreach (SettlementFC settlementFC in fC.settlements)
            {
                if (settlementFC.planetName == Find.World.info.name && settlementFC.mapLocation == settlement.Tile)
                    return true;
            }

            return false;
        }

        public FCPlanetRoadQueue CreateRoadQueue(string planetName, bool logFailure = true)
        {
            FCPlanetRoadQueue queue = GetRoadQueue(planetName);
            if (queue != null) 
            {
                if(logFailure)
                    Log.Message("Empire - Road queue for " + planetName + " already exists.");

                return queue;
            }
            queue = new FCPlanetRoadQueue(planetName, this.roadDef, this.daysBetweenTicks);
            roadQueues.Add(queue);
            return queue;
        }

        public void CheckForTechChanges()
        {
            FactionFC faction = Find.World.GetComponent<FactionFC>();
            RoadDef def = this.roadDef;

            if (DefDatabase<ResearchProjectDef>.GetNamed("FCRoadBuildingHighway", false).IsFinished)
            {
                def = RoadDefOf.AncientAsphaltHighway;
            }
            else if (DefDatabase<ResearchProjectDef>.GetNamed("FCRoadBuildingRoad", false).IsFinished)
            {
                def = RoadDefOf.AncientAsphaltRoad;

            }
            else if (DefDatabase<ResearchProjectDef>.GetNamed("FCRoadBuildingDirt", false).IsFinished)
            {
                def = RoadDefOf.DirtRoad;
            }

            if (this.roadDef != def)
            {
                this.roadDef = def;

                foreach (FCPlanetRoadQueue queue in this.roadQueues)
                {
                    queue.RoadDef = def;
                }
            }
        }

        public void DrawPaths()
        {
            GetRoadQueue(Find.World.info.name).DrawPaths();
        }

        /// <summary>
        /// Flags all road queues to update whenever they are able.
        /// </summary>
        public void FlagUpdateRoadQueues()
        {
            foreach (FCPlanetRoadQueue queue in this.roadQueues)
            {
                queue.shouldUpdateSettlementsToProcess = true;
            }
        }
    }

    public class FCPlanetRoadQueue : IExposable
    {
        public string planetName;
        public int nextRoadTick;
        public int daysBetweenTicks;
        protected RoadDef roadDef;

        public bool shouldUpdateSettlementsToProcess = true;

        public List<int> settlementsFromTiles = new List<int>();
        public List<int> settlementsToTiles = new List<int>();
        IEnumerator<FCRoadPath> roadPathIterator;

        public RoadDef RoadDef {
            get {
                return roadDef;
            }
            set
            {
                roadDef = value;
                ResetPaths();
            }
        }

        public List<FCRoadPath> roadPaths = new List<FCRoadPath>();

        public void ExposeData()
        {
            Scribe_Values.Look<string>(ref planetName, "planetName");
            Scribe_Values.Look<int>(ref nextRoadTick, "nextRoadTick");
            Scribe_Values.Look<int>(ref daysBetweenTicks, "daysBetweenTicks");
            Scribe_Defs.Look<RoadDef>(ref roadDef, "roadDef");
        }

        public FCPlanetRoadQueue(string planetName, RoadDef roadDef, int daysBetweenTicks)
        {
            this.planetName = planetName ?? Find.World.info.name;
            this.roadDef = roadDef;
            this.daysBetweenTicks = daysBetweenTicks;
            this.nextRoadTick = this.nextRoadTick == 0 ? Find.TickManager.TicksGame : this.nextRoadTick;
        }

        public void AddPath(FCRoadPath path)
        {
            this.roadPaths.Add(path);
        }

        /// <summary>
        /// Updates processed settlements if needed, checks if the current planet is correct, 
        /// and that it is time to build the segements, then builds the segments
        /// </summary>
        public bool BuildRoadSegments()
        {
            if (this.shouldUpdateSettlementsToProcess)
            {
                this.UpdateSettlementsToProcess();
                this.shouldUpdateSettlementsToProcess = false;
            }
            if (Find.World.info.name != this.planetName || this.nextRoadTick > Find.TickManager.TicksGame)
                return false;

            this.nextRoadTick += GenDate.TicksPerDay * this.daysBetweenTicks;
            return this.ForceBuildRoadSegments();
        }

        bool ForceBuildRoadSegments()
        {
            bool built = false;
            foreach (FCRoadPath path in roadPaths)
            {
                built |= path.BuildSegment(this.roadDef);
            }
            if(built)
            {
                Find.World.renderer.SetDirty<WorldLayer_Roads>();
                Find.World.renderer.SetDirty<WorldLayer_Paths>();
            }
            return built;
        }

        public void DrawPaths()
        {
            foreach (FCRoadPath path in roadPaths)
            {
                path.DrawPath();
            }
        }

        IEnumerator<FCRoadPath> ProcessPath()
        {
            foreach (int from in this.settlementsFromTiles)
            {
                foreach (int to in this.settlementsToTiles)
                {
                    if (this.roadPaths.Any(path => path.From == from && path.To == to))
                        continue;

                    if (from != to)
                        yield return new FCRoadPath(from, to);
                }
            }
        }

        public void UpdateSettlementsToProcess()
        {
            if (Find.World.info.name != this.planetName)
            {
                Log.Error("Empire - Attempt to UpdateSettlementsToProcess on wrong planet. Report this.");
                return;
            }

            this.settlementsFromTiles.Clear();
            this.settlementsToTiles.Clear();

            FactionFC fC = Find.World.GetComponent<FactionFC>();
            foreach (SettlementFC settlement in fC.settlements)
            {
                if(settlement.planetName == this.planetName)
                    this.settlementsFromTiles.Add(settlement.mapLocation);
            }
            foreach (Settlement settlement in Find.World.worldObjects.Settlements)
            {
                if (FCRoadBuilder.IsValidRoadTarget(settlement))
                {
                    this.settlementsToTiles.Add(settlement.Tile);
                }
            }

            this.roadPathIterator = ProcessPath();
        }

        public void ProcessOnePath()
        {
            if (this.roadPathIterator == null)
                this.roadPathIterator = ProcessPath();

            if (this.roadPathIterator.MoveNext())
                this.roadPaths.Add(this.roadPathIterator.Current);
        }

        /// <summary>
        /// Resets the paths progress. Does not recalculate the paths.
        /// </summary>
        public void ResetPaths()
        {
            foreach (FCRoadPath path in this.roadPaths)
            {
                path.ResetProgress();
            }
        }
    }

    public class FCRoadPath
    {
        public WorldPath Path { get; protected set; }
        public int From { get; protected set; }
        public int To { get; protected set; }

        public FCRoadPath(Settlement from, Settlement to)
        {
            if (from.Tile == to.Tile)
            {
                Log.Error("Empire - Attempt to create road path to the same tile");
            }
            this.SetupPath(from.Tile, to.Tile);
        }

        public FCRoadPath(int from, int to)
        {
            if (from == to)
            {
                Log.Error("Empire - Attempt to create road path to the same tile");
            }
            this.SetupPath(from, to);
        }

        void SetupPath(int from, int to)
        {
            WorldPath path = Find.World.pathFinder.FindPath(from, to, null);

            // path belongs to a WorldPathPool that gets very vocal in the error log
            // when theres more WorldPaths than caravans. The workaround to this error
            // is to copy the path to a new WorldPath object that is not a part of 
            // the pool and Dispose of the one that is
            this.Path = new WorldPath();
            foreach (int node in path.NodesReversed)
            {
                this.Path.AddNodeAtStart(node);
            }
            this.Path.SetupFound(path.TotalCost);
            this.Path.inUse = true;
            path.Dispose();
        }

        /// <summary>
        ///  Builds 1 segment of road. Returns if a segment was built
        /// </summary>
        /// <returns> this.IsCompleted </returns>
        /// <param name="roadDef">Road def.</param>
        public bool BuildSegment(RoadDef roadDef)
        {
            start:
            if (!this.Path.Found || this.IsCompleted)
                return false;

            int tile = this.Path.ConsumeNextNode();
            int lastTile = this.Path.Peek(-1);

            WorldGrid grid = Find.WorldGrid;

            RoadDef existingRoad = grid.GetRoadDef(lastTile, tile);
            if (IsNewRoadBetter(existingRoad, roadDef))
            {
                // Replaces the road if this.Road.priority > the existing road's priority
                grid.OverlayRoad(lastTile, tile, roadDef);
                Find.WorldPathGrid.RecalculatePerceivedMovementDifficultyAt(lastTile);
                Find.WorldPathGrid.RecalculatePerceivedMovementDifficultyAt(tile);
            }
            else
            {
                goto start;
            }
            return true;
        }


        public bool IsCompleted
        {
            get
            {
                return this.Path.NodesLeftCount == 1;
            }
        }

        public static bool IsNewRoadBetter(RoadDef oldRoad, RoadDef newRoad)
        {
            if (newRoad == null)
                return false;

            if (oldRoad == null)
                return true;

            return newRoad.priority > oldRoad.priority;
        }

        public void DrawPath()
        {
            this.Path.DrawPath(null);
        }

        public void ResetProgress()
        {
            Traverse.Create(this.Path).Field("curNodeIndex").SetValue(this.Path.NodesReversed.Count - 1);
        }
    }
}
