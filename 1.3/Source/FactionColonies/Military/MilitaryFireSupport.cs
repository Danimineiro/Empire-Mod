using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using UnityEngine;
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

        public bool ShouldBeOver => ticksTillEnd <= Find.TickManager.TicksGame;

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

        private bool ShouldFire => (timeRunning % 15) == 0 && timeRunning >= startupTime;

        private IntVec3 SemiRandomSpawnCenter => (from x in GenRadial.RadialCellsAround(location, accuracy, true)where x.InBounds(map)select x).RandomElementByWeight(x =>new SimpleCurve { new CurvePoint(0f, 1f), new CurvePoint(accuracy, 0.1f) }.Evaluate(x.DistanceTo(location)));

        private void DoCombatExtendedLaunch(IntVec3 spawnCenter, ThingDef def)
        {   
            //if CE is on
            ThingDef tempDef = expendProjectile();
            Type typeDef = FactionColonies.returnUnknownTypeFromName("CombatExtended.AmmoDef");
            var ammoSetDef = typeDef.GetProperty("AmmoSetDefs", BindingFlags.Public | BindingFlags.Instance).GetValue(tempDef);
            Type ammoLink = FactionColonies.returnUnknownTypeFromName("CombatExtended.AmmoLink");
            var ammoLinkVar = ammoSetDef.GetType().GetProperty("Item").GetValue(ammoSetDef, new object[] { 0 });
            //  Log.Message(ammoLinkVar.ToString());
            var ammoTypes = ammoLinkVar.GetType().GetField("ammoTypes", BindingFlags.Public | BindingFlags.Instance).GetValue(ammoLinkVar);
            //list of ammotypes
            int count = (int) ammoTypes.GetType().GetProperty("Count").GetValue(ammoTypes, new object[] { });
            for (int k = 0; k < count; k++)
            {
                var ammoDefAmmo = ammoTypes.GetType().GetProperty("Item").GetValue(ammoTypes, new object[] { k });
                if (ammoDefAmmo.GetType().GetField("ammo", BindingFlags.Public | BindingFlags.Instance).GetValue(ammoDefAmmo).ToString() == tempDef.defName)
                {
                    def = (ThingDef)ammoDefAmmo.GetType().GetField("projectile", BindingFlags.Public | BindingFlags.Instance).GetValue(ammoDefAmmo);
                    break;
                }
            }

            Type type2 = FactionColonies.returnUnknownTypeFromName("CombatExtended.ProjectileCE");
            MethodInfo launch = type2.GetMethod("Launch", new[]
            {
                typeof(Thing),
                typeof(Vector2),
                typeof(float),
                typeof(float),
                typeof(float),
                typeof(float),
                typeof(Thing)
            });

            MethodInfo getShotAngle = type2.GetMethod("GetShotAngle", BindingFlags.Public | BindingFlags.Static);
            Thing thing = GenSpawn.Spawn(def, sourceLocation, map);

            PropertyInfo gravityProperty = type2.GetProperty("GravityFactor", BindingFlags.NonPublic | BindingFlags.Instance);

            Vector2 sourceVec = new Vector2(sourceLocation.x, sourceLocation.z);
            Vector2 destVec = new Vector2(spawnCenter.x, spawnCenter.z);
            Vector3 finalVector = (destVec - sourceVec);
            float magnitude = finalVector.magnitude;

            float gravity = (float)gravityProperty.GetValue(thing); //1.96f * 5;

            float shotRotation = (-90f + 57.29578f * Mathf.Atan2(finalVector.y, finalVector.x)) % 360; //Vector2Utility.AngleTo(sourceVec, destVec);
            float shotHeight = 10f;
            float shotSpeed = 100f;
            float shotAngle = (float)getShotAngle.Invoke(null, BindingFlags.Public | BindingFlags.Static, null, new object[] { shotSpeed, magnitude, shotHeight, true, gravity }, null);

            launch.Invoke(thing, new object[]
            {
                FactionColonies.getPlayerColonyFaction().leader,
                sourceVec,
                shotAngle,
                shotRotation,
                shotHeight,
                shotSpeed,
                null
            });
        }

        public void Process()
        {
            if (fireSupportType == "fireSupport")
            {
                if (ShouldFire)
                {
                    IntVec3 spawnCenter = SemiRandomSpawnCenter;
                    LocalTargetInfo info = new LocalTargetInfo(spawnCenter);
                    ThingDef def = new ThingDef();
                    if (FactionColonies.IsModLoaded("CETeam.CombatExtended")) 
                    {
                        DoCombatExtendedLaunch(spawnCenter, def);
                    }
                    else
                    {
                        def = expendProjectile().projectileWhenLoaded;
                        Projectile projectile = (Projectile)GenSpawn.Spawn(def, sourceLocation, map);
                        projectile.Launch(null, info, info, ProjectileHitFlags.All);
                    }
                }
            }

            timeRunning++;
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