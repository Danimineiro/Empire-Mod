using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    [StaticConstructorOnStartup]
    public static class FactionColoniesMilitary
    {
        private static List<SavedUnitFC> savedUnits = new List<SavedUnitFC>();
        private static List<SavedSquadFC> savedSquads = new List<SavedSquadFC>();
        public static IEnumerable<SavedSquadFC> SavedSquads => savedSquads;
        public static IEnumerable<SavedUnitFC> SavedUnits => savedUnits;

        public static string EmpireConfigFolderPath;
        public static string EmpireMilitaryUnitFolder;
        public static string EmpireMilitarySquadFolder;

        static FactionColoniesMilitary()
        {
            EmpireConfigFolderPath = Path.Combine(GenFilePaths.SaveDataFolderPath, "Empire");
            EmpireMilitarySquadFolder = Path.Combine(EmpireConfigFolderPath, "Squads");
            EmpireMilitaryUnitFolder = Path.Combine(EmpireConfigFolderPath, "Units");
            if (!Directory.Exists(EmpireConfigFolderPath) ||
                !Directory.Exists(EmpireMilitarySquadFolder) ||
                !Directory.Exists(EmpireMilitaryUnitFolder))
            {
                Directory.CreateDirectory(EmpireConfigFolderPath);
                Directory.CreateDirectory(EmpireMilitarySquadFolder);
                Directory.CreateDirectory(EmpireMilitaryUnitFolder);
            }

            Read();
        }

        public static SavedSquadFC GetSquad(string name) => savedSquads.FirstOrFallback(s => s.name == name);
        public static SavedUnitFC GetUnit(string name) => savedUnits.FirstOrFallback(u => u.name == name);

        public static void RemoveSquad(string name)
        {
            savedSquads.RemoveAll(squad => squad.name == name);
            File.Delete(GetSquadPath(name));
        }
        
        public static void RemoveSquad(SavedSquadFC squad)
        {
            savedSquads.Remove(squad);
            File.Delete(GetSquadPath(squad.name));
        }

        public static void RemoveUnit(string name)
        {
            savedSquads.RemoveAll(unit => unit.name == name);
            File.Delete(GetUnitPath(name));
        }
        
        public static void RemoveUnit(SavedUnitFC unit)
        {
            savedUnits.Remove(unit);
            File.Delete(GetUnitPath(unit.name));
        }

        [DebugAction("Empire", "Reload Saved Military")]
        public static void Read()
        {
            if (Scribe.mode != LoadSaveMode.Inactive)
                throw new Exception("Empire - Attempt to load saved military while scribe is active");
            
            savedSquads.Clear();
            savedUnits.Clear();
            foreach (string path in Directory.EnumerateFiles(EmpireMilitarySquadFolder))
            {
                try
                {
                    SavedSquadFC squad = new SavedSquadFC();
                    Scribe.loader.InitLoading(path);
                    squad.ExposeData();
                    savedSquads.Add(squad);
                }
                catch (Exception e)
                {
                    Log.Error("Failed to load squad at path " + path);
                }
                finally
                {
                    Scribe.loader.FinalizeLoading();
                }
            }

            foreach (string path in Directory.EnumerateFiles(EmpireMilitaryUnitFolder))
            {
                try
                {
                    SavedUnitFC unit = new SavedUnitFC();
                    Scribe.loader.InitLoading(path);
                    unit.ExposeData();
                    savedUnits.Add(unit);
                }
                catch (Exception e)
                {
                    Log.Error("Failed to load unit at path " + path);
                }
                finally
                {
                    Scribe.loader.FinalizeLoading();
                }
            }
        }

        public static string GetUnitPath(string name) => Path.Combine(EmpireMilitaryUnitFolder, $"{name}.xml");
        public static string GetSquadPath(string name) => Path.Combine(EmpireMilitarySquadFolder, $"{name}.xml");

        public static void SaveSquad(SavedSquadFC squad)
        {
            if (Scribe.mode != LoadSaveMode.Inactive)
            {
                throw new Exception("Empire - Attempt to save squad while scribe is active");
            }

            string path = GetSquadPath(squad.name);
            try
            {
                Scribe.saver.InitSaving(path, "squad");
                int version = 0;
                Scribe_Values.Look(ref version, "version");
                squad.ExposeData();
            }
            catch (Exception e)
            {
                Log.Error($"Failed to save squad {squad.name} {e}");
            }
            finally
            {
                Scribe.saver.FinalizeSaving();
            }

            savedSquads.RemoveAll(s => s.name == squad.name);
            savedSquads.Add(squad);
        }

        public static void SaveUnit(SavedUnitFC unit)
        {
            if (Scribe.mode != LoadSaveMode.Inactive)
            {
                throw new Exception("Empire - Attempt to save unit while scribe is active");
            }

            string path = GetUnitPath(unit.name);
            try
            {
                Scribe.saver.InitSaving(path, "unit");
                int version = 0;
                Scribe_Values.Look(ref version, "version");
                unit.ExposeData();
            }
            catch (Exception e)
            {
                Log.Error($"Failed to save unit {unit.name} {e}");
            }
            finally
            {
                Scribe.saver.FinalizeSaving();
            }
            savedUnits.RemoveAll(u => u.name == unit.name);
            savedUnits.Add(unit);
        }

        public static void SaveAllUnits() => savedUnits.ForEach(SaveUnit);
        public static void SaveAllSquads() => savedSquads.ForEach(SaveSquad);
    }

    public class SavedUnitFC : IExposable
    {
        public string name;
        public bool isTrader;
        public bool isCivilian;
        public PawnKindDef animal;
        public PawnKindDef pawnKind;
        public SavedThing weapon;
        public List<SavedThing> apparel;

        public SavedUnitFC() {}

        public SavedUnitFC(MilUnitFC unit)
        {
            Pawn pawn = unit.defaultPawn;
            name = unit.name;
            if (pawn.equipment?.Primary != null)
                weapon = new SavedThing(pawn.equipment.Primary);
            
            apparel = pawn.apparel.WornApparel.Select(a => new SavedThing(a)).ToList();
            isTrader = unit.isTrader;
            isCivilian = unit.isCivilian;
            animal = unit.animal;
            pawnKind = unit.pawnKind;
        }

        public MilUnitFC CreateMilUnit()
        {
            MilUnitFC unit = new MilUnitFC(false)
            {
                name = name,
                isCivilian = isCivilian,
                isTrader = isTrader,
                animal = animal,
                pawnKind = pawnKind
            };
            if (!Find.World.GetComponent<FactionFC>().raceFilter.Allows(pawnKind.race))
            {
                unit.pawnKind = FactionColonies.getPlayerColonyFaction().RandomPawnKind();
            }
            unit.generateDefaultPawn();

            if (weapon.thing != null)
                unit.equipWeapon((ThingWithComps) weapon.CreateThing());

            apparel.ForEach(a =>
            {
                if (a.thing != null)
                {
                    unit.wearEquipment((Apparel) a.CreateThing(), true);
                }
            });

            unit.changeTick();
            unit.updateEquipmentTotalCost();

            return unit;
        }

        public MilUnitFC Import()
        {
            FactionFC fc = Find.World.GetComponent<FactionFC>();
            MilUnitFC unit = this.CreateMilUnit();
            fc.militaryCustomizationUtil.units.Add(unit);
            return unit;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref isTrader, "isTrader");
            Scribe_Values.Look(ref isCivilian, "isCivilian");
            Scribe_Defs.Look(ref animal, "animal");
            Scribe_Defs.Look(ref pawnKind, "pawnKind");
            Scribe_Deep.Look(ref weapon, "weapon");
            Scribe_Collections.Look(ref apparel, "apparel", LookMode.Deep);
        }
    }

    public class SavedSquadFC : IExposable
    {
        public string name;
        public List<SavedUnitFC> unitTemplates = new List<SavedUnitFC>();
        public List<int> units = new List<int>(30);
        public bool isTraderCaravan;
        public bool isCivilian;
        public SavedSquadFC() {}

        public SavedSquadFC(MilSquadFC squad)
        {
            name = squad.name;
            isTraderCaravan = squad.isTraderCaravan;
            isCivilian = squad.isCivilian;

            // Dont store blank units
            var squadTemplates = squad.units.Distinct().Where(u => !u.isBlank).ToList();
            
            unitTemplates = squadTemplates.Select(unit => new SavedUnitFC(unit)).ToList();
            units = squad.units.Select(unit => squadTemplates.IndexOf(unit)).ToList();
        }

        public MilSquadFC CreateMilSquad()
        {
            MilSquadFC squad = new MilSquadFC(true);
            squad.name = name;
            squad.isCivilian = isCivilian;
            squad.isTraderCaravan = isTraderCaravan;

            FactionFC fc = Find.World.GetComponent<FactionFC>();

            var milUnits = unitTemplates.Select(unit => unit.CreateMilUnit()).ToList();

            foreach (int i in units)
            {
                if(i == -1)
                    squad.units.Add(fc.militaryCustomizationUtil.blankUnit);
                else
                    squad.units.Add(milUnits[i]);
            }

            return squad;
        }
        public MilSquadFC Import()
        {
            FactionFC fc = Find.World.GetComponent<FactionFC>();
            MilSquadFC squad = this.CreateMilSquad();
            foreach (MilUnitFC unit in squad.units.Distinct().Where(unit => !unit.isBlank))
            {
                fc.militaryCustomizationUtil.units.Add(unit);
            }
            fc.militaryCustomizationUtil.squads.Add(squad);
            return squad;
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref isCivilian, "isCivilian");
            Scribe_Values.Look(ref isTraderCaravan, "isTraderCaravan");
            Scribe_Collections.Look(ref unitTemplates, "unitTemplates", LookMode.Deep);
            Scribe_Collections.Look(ref units, "units", LookMode.Value);
        }
    }
    
    public struct SavedThing : IExposable
    {
        public ThingDef thing;
        public ThingDef stuff;

        public SavedThing(Thing thing)
        {
            this.thing = thing.def;
            this.stuff = thing.Stuff;
        }
        public Thing CreateThing() => thing != null ? ThingMaker.MakeThing(thing, stuff) : null;
        public void ExposeData()
        {
            Scribe_Defs.Look(ref thing, "thing");
            Scribe_Defs.Look(ref stuff, "stuff");
        }
    }
}
