using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FactionColonies
{
    
    public class MilitaryFireSupport : IExposable, ILoadReferenceable
    {
        public int loadID = -1;
        public string name;
        public float totalCost;
        public int timeRunning;
        public int ticksTillEnd;
        public float accuracy = 15;
        public string fireSupportType;
        public Map map;
        public IntVec3 location;
        public int startupTime;
        public IntVec3 sourceLocation;
        public List<ThingDef> projectiles;

        public MilitaryFireSupport()
        {
        }

        public MilitaryFireSupport(string fireSupportType, Map map, IntVec3 location, int ticksTillEnd, int startupTime,
            float accuracy, List<ThingDef> projectiles = null)
        {
            this.fireSupportType = fireSupportType;
            this.ticksTillEnd = Find.TickManager.TicksGame + ticksTillEnd + startupTime;
            this.accuracy = accuracy;
            this.map = map;
            this.location = location;
            this.startupTime = startupTime;
            sourceLocation = CellFinder.RandomEdgeCell(map);
            this.projectiles = projectiles;
        }

        public string GetUniqueLoadID()
        {
            return $"MilitaryFireSupport_{loadID}";
        }

        public void setLoadID()
        {
            loadID = Find.World.GetComponent<FactionFC>().GetNextMilitaryFireSupportID();
        }

        public float returnAccuracyCostPercentage()
        {
            return (float) Math.Round((Math.Max(0, 15 - accuracy) / 15) * 100);
        }

        public float returnTotalCost()
        {
            float cost = 0;
            foreach (ThingDef def in projectiles)
            {
                cost += def.BaseMarketValue * 1.5f * (1 + returnAccuracyCostPercentage() / 100);
            }

            totalCost = (float) Math.Round(cost);
            return totalCost;
        }

        public void delete()
        {
            Find.World.GetComponent<FactionFC>().militaryCustomizationUtil.fireSupportDefs.Remove(this);
        }

        public ThingDef expendProjectile()
        {
            if (!projectiles.Any()) return null;
            ThingDef projectile = projectiles[0];
            projectiles.RemoveAt(0);
            return projectile;
        }

        public List<ThingDef> returnFireSupportOptions()
        {
            // return list of thingdefs that can be used as fire support
            ThingSetMaker thingSetMaker = new ThingSetMaker_Count();
            ThingSetMakerParams param = new ThingSetMakerParams();
            param.filter = new ThingFilter();
            param.techLevel = Find.World.GetComponent<FactionFC>().techLevel;

            param.filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("MortarShells"), true);
            if (DefDatabase<ThingCategoryDef>.GetNamedSilentFail("AmmoShells") != null)
                param.filter.SetAllow(DefDatabase<ThingCategoryDef>.GetNamedSilentFail("AmmoShells"), true);
            List<ThingDef> list = thingSetMaker.AllGeneratableThingsDebug(param).ToList();
            return list;
        }


        public void ExposeData()
        {
            Scribe_Values.Look(ref loadID, "loadID");
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref timeRunning, "timeRunning");
            Scribe_Values.Look(ref ticksTillEnd, "ticksTillEnd");
            Scribe_Values.Look(ref accuracy, "accuracy");
            Scribe_Values.Look(ref fireSupportType, "fireSupportType");
            Scribe_References.Look(ref map, "map");
            Scribe_Values.Look(ref location, "location");
            Scribe_Values.Look(ref location, "sourceLocation");
            Scribe_Values.Look(ref startupTime, "startupTime");
            Scribe_Collections.Look(ref projectiles, "projectiles", LookMode.Def);
        }
    }
}