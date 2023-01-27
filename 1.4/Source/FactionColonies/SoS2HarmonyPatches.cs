using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using System.Reflection;
using RimWorld.Planet;
using RimWorld.QuestGen;

namespace FactionColonies
{

    public class SoS2HarmonyPatches
    {
        //member took damage
        //[HarmonyPatch(typeof(WorldLayer_Hills), "Regenerate")]
        class testfix
        {
            static void Prefix(ref WorldLayer __instance)
            {
                Log.Message("World grid exists: " + Find.WorldGrid);
                Log.Message("world grid tile count" + Find.WorldGrid.tiles.Count().ToString());
                Log.Message("tile 5 info" + ((bool)(Find.WorldGrid.tiles[5] != null)).ToString());
                //foreach (Tile tile in Find.WorldGrid.tiles)
                // {
                //Log.Message("tile info - " + Find.WorldGrid.tiles.IndexOf(tile));
                //}
                Log.Message("world grid hilliness of tile 5" + Find.WorldGrid.tiles[5].hilliness);
            }
        }


        //
        public static void Prefix()
        {
            FactionFC worldcomp = Find.World.GetComponent<FactionFC>();
            worldcomp.travelTime = Find.TickManager.TicksGame;
            worldcomp.SoSMoving = true;
            if (worldcomp.taxMap != null && worldcomp.taxMap.Parent != null && worldcomp.taxMap.Parent.def.defName == "ShipOrbiting")
            {
                worldcomp.SoSShipTaxMap = true;
            }
            if (worldcomp.SoSShipCapital == true)
            {
                worldcomp.SoSShipCapitalMoving = true;
            }

        }
        //


        public static List<string> returnPlanetFactionLoadIds()
        {
            //List<Faction> finalList = new List<Faction>();

            Type typ = FactionColonies.returnUnknownTypeFromName("SaveOurShip2.WorldSwitchUtility");
            Type typ2 = FactionColonies.returnUnknownTypeFromName("SaveOurShip2.WorldFactionList");

            var mainclass = Traverse.CreateWithType(typ.ToString());
            var dict = mainclass.Property("PastWorldTracker").Field("WorldFactions").GetValue();

            var planetfactiondict = Traverse.Create(dict);
            var unknownclass = planetfactiondict.Property("Item", new object[] { Find.World.info.name }).GetValue();

            var factionlist = Traverse.Create(unknownclass);
            var list = factionlist.Field("myFactions").GetValue();
            List<String> modifiedlist = (List<String>)list;
            return modifiedlist;
        }

        public static bool checkOnPlanet(Faction faction)
        {
            bool match = false;
            foreach (string str in returnPlanetFactionLoadIds())
            {
                if (faction.GetUniqueLoadID() == str)
                {
                    match = true;
                }
            }

            return match;
        }

        public static void updateFactionOnPlanet()
        {
            FactionFC worldcomp = Find.World.GetComponent<FactionFC>();
            Faction faction1 = FactionColonies.getPlayerColonyFaction();
            //Log.Message((faction1 != null).ToString());
            if (faction1 == null && worldcomp.factionCreated == true)
            {
                Log.Message("Moved to new planet - Adding faction copy");
                //FactionColonies.createPlayerColonyFaction();
                FactionColonies.copyPlayerColonyFaction();
                faction1 = FactionColonies.getPlayerColonyFaction();
            }
            //Log.Message(((bool)(faction1 != null)).ToString());
            foreach (Faction factionOther in Find.FactionManager.AllFactionsListForReading)
            {
                //Log.Message(factionOther.def.defName);
                if (factionOther != faction1 && faction1.RelationWith(factionOther, true) == null)
                {

                    faction1.TryMakeInitialRelationsWith(factionOther);
                }
            }
            worldcomp.updateFactionIcon(ref faction1, "FactionIcons/" + worldcomp.factionIconPath);
            worldcomp.factionUpdated = true;
            //foreach (SettlementFC settlement in worldcomp.settlements)
            //{
            //    Settlement obj = Find.WorldObjects.SettlementAt(settlement.mapLocation);
            //    if (obj != null && obj.Faction != faction1)
            //    {
            //        obj.SetFaction(faction1);
            //    }
            //}
        }
        //
        //[HarmonyPatch(typeof(Scenario), "PostWorldGenerate")]
        public static void Postfix()
        {




            FactionFC worldcomp = Find.World.GetComponent<FactionFC>();


            //FactionFC worldcomp = Find.World.GetComponent<FactionFC>();
            if (worldcomp != null && worldcomp.planetName != null && worldcomp.planetName != Find.World.info.name && Find.TickManager.TicksGame > 60000)
            {
                Faction faction1 = FactionColonies.getPlayerColonyFaction();
                updateFactionOnPlanet();

                if (worldcomp.SoSMoving == true)
                {
                    int difference = Find.TickManager.TicksGame - worldcomp.travelTime;
                    worldcomp.taxTimeDue = worldcomp.taxTimeDue + difference;
                    worldcomp.dailyTimer = worldcomp.dailyTimer + difference;
                    worldcomp.militaryTimeDue = worldcomp.militaryTimeDue + difference;
                    worldcomp.SoSMoving = false;
                }
                if (worldcomp.SoSShipTaxMap == true)
                {
                    worldcomp.taxMap = Find.CurrentMap;
                    Log.Message("Updated Tax map to ship");
                    Log.Message(worldcomp.taxMap.Parent.Label);
                }
                if (worldcomp.SoSShipCapitalMoving == true)
                {
                    worldcomp.SoSShipCapitalMoving = false;
                    worldcomp.setCapital();
                }

                ///
                worldcomp.boolChangedPlanet = true;
                worldcomp.planetName = Find.World.info.name;
            }
            


            //ResetFactionLeaders();



        }



        public static void Patch(Harmony harmony)
        {

            Type typ = FactionColonies.returnUnknownTypeFromName("SaveOurShip2.WorldSwitchUtility");
            Type typ2 = FactionColonies.returnUnknownTypeFromName("SaveOurShip2.FixOutdoorTemp");



            //Get type inside of type
            Type[] types = typ2.GetNestedTypes(BindingFlags.Public | BindingFlags.Static);
            foreach (Type t in types)
            {
                //Log.Message( t.ToString());
                if (t.ToString() == "SaveOurShip2.FixOutdoorTemp+SelectiveWorldGeneration")
                {
                    typ2 = t;
                    //Log.Message("found" + t.ToString());
                    break;
                }
            }


            MethodInfo originalpre = typ.GetMethod("KillAllColonistsNotInCrypto", BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo originalpost = typ.GetMethod("DoWorldSwitch", BindingFlags.Static | BindingFlags.NonPublic);
            // MethodInfo originalpost2 = typ2.GetMethod("Replace", BindingFlags.Static | BindingFlags.Public);



            var prefix = typeof(SoS2HarmonyPatches).GetMethod("Prefix");
            var postfix = typeof(SoS2HarmonyPatches).GetMethod("Postfix");
            harmony.Patch(originalpre, prefix: new HarmonyMethod(prefix));
            harmony.Patch(originalpost, postfix: new HarmonyMethod(postfix));
            // harmony.Patch(originalpost2, postfix: new HarmonyMethod(postfix));
            Log.Message("Finished patching Empire and SoS2");
        }
        //

        public static void ResetFactionLeaders(bool planet = false)
        {
            List<Faction> list;
            
            if (planet)
            {
                list = Find.FactionManager.AllFactionsListForReading;
            }
            else
            {
                var factions = Find.FactionManager.GetType().GetField("allFactions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Find.FactionManager);
                list = (List<Faction>)factions;
            }
            // Log.Message(list.Count().ToString());
            Log.Message("Resetting faction leaders");
            // List<Faction> list = (List<Faction>)mainclass                //mainclass.Field("allFactions", ).GetValue();
            foreach (Faction faction in list)
            {
                if (faction.leader == null && faction.def.leaderTitle != null && faction.def.leaderTitle != "")
                {
                    try
                    {
                        faction.TryGenerateNewLeader();
                    }
                    catch (NullReferenceException e) 
                    {
                        Log.Message("Empire - Error trying to generate leader for " + faction.Name);
                    }
                    // Log.Message("Generated new leader for " + faction.Name);
                }

            }
        }
    }

    public class SettlementSoS2Info : IExposable
    {
        public SettlementSoS2Info()
        {

        }

        public SettlementSoS2Info(string planetName, int location, string settlementName = null)
        {
            this.planetName = planetName;
            this.location = location;
            this.settlementName = settlementName;
        }

        public string planetName;
        public string settlementName;
        public int location;

        public void ExposeData()
        {
            Scribe_Values.Look<string>(ref planetName, "planetName");
            Scribe_Values.Look<string>(ref settlementName, "settlementName");
            Scribe_Values.Look<int>(ref location, "location");

        }
    }
}
