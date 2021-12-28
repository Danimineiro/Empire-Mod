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

        public static void updateFactionOnPlanet(FactionFC worldComp)
        {
            Faction faction1 = FactionColonies.getPlayerColonyFaction();
            Log.Message($"Null check on faction while updating: {faction1 == null}");
            if (faction1 == null && worldComp.factionCreated == true)
            {
                Log.Message("Moved to new planet - Adding faction copy");
                //FactionColonies.createPlayerColonyFaction();
                faction1 = FactionColonies.copyPlayerColonyFaction(worldComp);
                Log.Message($"Rechecking faction for null: {faction1 == null}");
                // faction1 = FactionColonies.getPlayerColonyFaction();
                // List<SettlementFC> newWorldSettlements = new List<SettlementFC>();
                // foreach (Settlement s in Find.World.worldObjects.Settlements.Where(settlement =>
                //     settlement.Faction == faction1))
                // {
                //     Log.Message($"Found settlement owned by Empire faction {s.Name}");
                //     if (s.GetType() == typeof(WorldSettlementFC))
                //     {
                //         SettlementFC settlementFc = worldComp.returnSettlementByLocation(s.Tile, Find.World.info.name);
                //         newWorldSettlements.Add(settlementFc);
                //     }
                //     else
                //     {
                //         WorldSettlementFC wsFC =
                //             FactionColonies.createPlayerColonySettlement(s.Tile, true, Find.World.info.name);
                //         SettlementFC settlementFc = worldComp.returnSettlementByLocation(s.Tile, Find.World.info.name);
                //         settlementFc.loyalty = 15;
                //         settlementFc.happiness = 25;
                //         settlementFc.unrest = 20;
                //         settlementFc.prosperity = 70;
                //         newWorldSettlements.Add(settlementFc);
                //         s.Destroy();
                //     }
                // }
                // worldComp.settlements = newWorldSettlements;
            }
            //Log.Message(((bool)(faction1 != null)).ToString());
            foreach (Faction factionOther in Find.FactionManager.AllFactionsListForReading)
            {
                //Log.Message(factionOther.def.defName);
                if (factionOther != faction1 && faction1.RelationWith(factionOther, true) == null)
                {
                    try
                    {
                        faction1.TryMakeInitialRelationsWith(factionOther);
                    }
                    catch (Exception e)
                    {
                        Log.ErrorOnce($"Failed making faction relations with {faction1.Name} and {factionOther.Name}{System.Environment.NewLine}", 1);
                    }
                }
            }
            worldComp.factionUpdated = true;
            worldComp.updateFactionIcon(ref faction1, "FactionIcons/" + worldComp.factionIconPath);

            //foreach (SettlementFC settlement in worldComp.settlements)
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


            //FactionFC worldComp = Find.World.GetComponent<FactionFC>();
            if (worldcomp != null && worldcomp.planetName != null && worldcomp.planetName != Find.World.info.name && Find.TickManager.TicksGame > 600)
            {
                Log.Message("Executing Empire - SoS2 postfix");
                Faction faction1 = FactionColonies.getPlayerColonyFaction();
                updateFactionOnPlanet(worldcomp);

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
                Log.Message("Toggling change planet bool");
                worldcomp.boolChangedPlanet = true;
                worldcomp.planetName = Find.World.info.name;
            }
        }



        public static void Patch(Harmony harmony)
        {

            Type typ = FactionColonies.returnUnknownTypeFromName("SaveOurShip2.WorldSwitchUtility");
            Type typ2 = FactionColonies.returnUnknownTypeFromName("SaveOurShip2.FixOutdoorTemp");
            Type typ3 = FactionColonies.returnUnknownTypeFromName("SaveOurShip2.SelectiveWorldGeneration");


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
            MethodInfo originalpost2 = typ3.GetMethod("Replace", BindingFlags.Static | BindingFlags.Public);



            var prefix = typeof(SoS2HarmonyPatches).GetMethod("Prefix");
            var postfix = typeof(SoS2HarmonyPatches).GetMethod("Postfix");
            harmony.Patch(originalpre, prefix: new HarmonyMethod(prefix));
            harmony.Patch(originalpost, postfix: new HarmonyMethod(postfix));
            harmony.Patch(originalpost2, postfix: new HarmonyMethod(postfix));
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

     //    [HarmonyPatch(typeof(FactionManager), "Add")]
     //
     //    class AddPatch
     //    {
     //        static void Prefix(FactionManager __instance, Faction faction)
     //        {
     //            if (__instance.allFactions.Contains(faction))
     //                    return;
     //                __instance.allFactions.Add(faction);
     //                __instance.RecacheFactions();
     //            }
     //    }
     //    
     //    [HarmonyPatch(typeof(Faction), "TryGenerateNewLeader")]
	    // class TryGenerateNewLeaderPatch
	    // {
		   //  static bool Prefix(Faction __instance, ref bool __result)
		   //  {
			  //   Pawn pawn = __instance.leader;
			  //   __instance.leader = null;
     //            Log.Message($"leader props set for {__instance.Name}");
			  //   if (__instance.def.generateNewLeaderFromMapMembersOnly)
			  //   {
     //                Log.Message("Generating from map members only...");
				 //    for (int i = 0; i < Find.Maps.Count; i++)
				 //    {
					//     Map map = Find.Maps[i];
					//     for (int j = 0; j < map.mapPawns.AllPawnsCount; j++)
					//     {
					// 	    if (map.mapPawns.AllPawns[j] != pawn && !map.mapPawns.AllPawns[j].Destroyed && map.mapPawns.AllPawns[j].HomeFaction == __instance)
					// 	    {
					// 		    __instance.leader = map.mapPawns.AllPawns[j];
					// 	    }
					//     }
				 //    }
			  //   }
			  //   else if (__instance.def.pawnGroupMakers != null)
			  //   {
     //                Log.Message("Generating from pawn group makers");
				 //    List<PawnKindDef> list = new List<PawnKindDef>();
				 //    foreach (PawnGroupMaker pawnGroupMaker in from x in __instance.def.pawnGroupMakers where x.kindDef == PawnGroupKindDefOf.Combat select x)
				 //    {
     //                    Log.Message($"checking pawn group maker");
					//     foreach (PawnGenOption pawnGenOption in pawnGroupMaker.options)
					//     {
					// 	    if (pawnGenOption.kind.factionLeader)
					// 	    {
     //                            Log.Message($"Adding {pawnGenOption.kind.defName}");
					// 		    list.Add(pawnGenOption.kind);
					// 	    }
					//     }
				 //    }
				 //    if (__instance.def.fixedLeaderKinds != null)
				 //    {
     //                    Log.Message("Adding fixed leaderkinds");
					//     list.AddRange(__instance.def.fixedLeaderKinds);
				 //    }
     //                if (list.TryRandomElement(out PawnKindDef kind))
     //                {
     //                    PawnGenerationRequest request = new PawnGenerationRequest(kind, __instance, PawnGenerationContext.NonPlayer, -1, __instance.def.leaderForceGenerateNewPawn, false, false, false, true, false, 1f, false, true, true, true, false, false, false, false, 0f, 0f, null, 1f, null, null, null, null, null, null, null, null, null, null, null, null, null, false, false, false);
     //                    Gender supremeGender = __instance.ideos.PrimaryIdeo.SupremeGender;
     //                    if (supremeGender != Gender.None)
     //                    {
     //                        request.FixedGender = new Gender?(supremeGender);
     //                    }
     //                    Log.Message("Requesting pawn from generator...");
     //                    __instance.leader = PawnGenerator.GeneratePawn(request);
     //                    if (__instance.leader.RaceProps.IsFlesh)
     //                    {
     //                        __instance.leader.relations.everSeenByPlayer = true;
     //                    }
     //                    if (!Find.WorldPawns.Contains(__instance.leader))
     //                    {
     //                        Find.WorldPawns.PassToWorld(__instance.leader, PawnDiscardDecideMode.KeepForever);
     //                    }
     //                }
     //            }
     //
			  //   __result = __instance.leader != null;
     //
			  //   Log.Message($"Generated Leader for {__instance.Name}: {__instance.leader?.Name?.ToString() ?? "null"}");
			  //   return false;
		   //  }
	    // }
    }
}
