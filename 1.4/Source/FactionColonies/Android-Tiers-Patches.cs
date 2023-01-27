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

    public class Android_Tiers_Patches
    {


        //
        public static bool Prefix(PawnGroupMakerParms parms, bool warnOnZeroResults, ref IEnumerable<Pawn> __result)
        {
           if (parms.faction.def.defName == "PColony")
            {
                return false;
            } else
            {
                return true;
            }

        }
        //




        public static void Patch(Harmony harmony)
        {

            Type typ = FactionColonies.returnUnknownTypeFromName("AndroidTiers.PawnGroupMakerUtility_Patch");




            //Get type inside of type
            Type[] types = typ.GetNestedTypes(BindingFlags.Public | BindingFlags.Static);
            foreach (Type t in types)
            {
                Log.Message( t.ToString());
                if (t.ToString() == "AndroidTiers.PawnGroupMakerUtility_Patch+GeneratePawns_Patch")
                {
                    typ = t;
                    //Log.Message("found" + t.ToString());
                    break;
                }
            }


            MethodInfo originalpre = typ.GetMethod("Listener", BindingFlags.Static | BindingFlags.NonPublic);

            Log.Message("2");
            //var prefix = typeof(Android_Tiers_Patches).GetMethod("Prefix");
            MethodInfo prefix = typeof(Android_Tiers_Patches).GetMethod("Prefix");
            // List<MethodInfo> list = typeof(Android_Tiers_Patches).GetMethods();
            foreach (MethodInfo info in typeof(Android_Tiers_Patches).GetMethods())
                if (info.Name == "Prefix")
                    prefix = info;
            Log.Message("2");
            harmony.Patch(originalpre, prefix: new HarmonyMethod(prefix));


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
            //List<Faction> list = (List<Faction>)mainclass                //mainclass.Field("allFactions", ).GetValue();
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
                    //Log.Message("Generated new leader for " + faction.Name);
                }

            }
        }
    }


}
