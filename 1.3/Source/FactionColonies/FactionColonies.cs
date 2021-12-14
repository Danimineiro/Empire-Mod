using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FactionColonies.util;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace FactionColonies
{
    public class FactionColonies : ModSettings
    {
        public static void UpdateChanges()
        {
            FactionFC factionFC = Find.World.GetComponent<FactionFC>();

            Log.Message("Updating Empire to Latest Version");
            //NEW PLACE FOR UPDATE VERSIONS

            //I think this does things necessary for SOS so I'm gonna keep it
            if (factionFC.factionBackup == null)
            {
                factionFC.factionBackup = new Faction();
                factionFC.factionBackup = getPlayerColonyFaction();
                if (getPlayerColonyFaction() != null)
                {
                    Log.Message("Faction created");
                    factionFC.factionCreated = true;
                }

                factionFC.capitalPlanet = Find.World.info.name;

                SoS2HarmonyPatches.ResetFactionLeaders();
            }

            Log.Message("Empire - Testing for traits with no tie");
            verifyTraits();

            MessagePlayerAboutConfigErrors(factionFC);

            Log.Message("Empire - Testing for update change");

            if (Settings().updateVersion < 0.370)
            {
                Find.LetterStack.ReceiveLetter("FCManualDefenseWarningLabel".Translate(), "FCManualDefenseWarningDesc".Translate(), LetterDefOf.NewQuest);
            }

            double newVersion = PatchNoteDef.GetLatestForMod("saakra.empire").ToOldEmpireVersion;
            //Add update letter/checker here!!
            if (Settings().updateVersion < newVersion)
            {
                DebugActionsMisc.PatchNotesDisplayWindow();

                Settings().updateVersion = newVersion;
                Settings().settlementsAutoBattle = true;
                Settings().Write();
            }
        }

        private static void MessagePlayerAboutConfigErrors(FactionFC factionFC)
        {
            Log.Message("Empire - Testing for invalid capital map");
            //Check for an invalid capital map
            if (Find.WorldObjects.SettlementAt(factionFC.capitalLocation) == null && factionFC.SoSShipCapital == false)
            {
                Messages.Message("FCResetCapitalLocationWarning".Translate(), MessageTypeDefOf.NegativeEvent);
            }

            if (factionFC.taxMap == null)
            {
                Messages.Message("FCTaxMapNotSetWarning".Translate(), MessageTypeDefOf.CautionInput);
            }

            if (factionFC.policies.Count() < 2)
            {
                Find.LetterStack.ReceiveLetter("FCTraits".Translate(), "FCSelectYourTraits".Translate(), LetterDefOf.NeutralEvent);
            }

            if (!Settings().settlementsAutoBattle)
            {
                Messages.Message("FCAutoResolveDisabledWarning".Translate(), MessageTypeDefOf.RejectInput);
            }
        }

        public static void verifyTraits()
        {
            //make new list for factionfc traits
            //loop through events and add traits
            //loop through
            List<FCTraitEffectDef> factionTraits = new List<FCTraitEffectDef>();

            foreach (FCEvent evt in Find.World.GetComponent<FactionFC>().events)
            {
                if (evt.settlementTraitLocations.Count() <= 0)
                {
                    factionTraits.AddRange(evt.traits);
                }
            }

            Find.World.GetComponent<FactionFC>().traits = factionTraits;

            //go through each settlement and make new list for each settlement
            //loop through each active event and add settlement traits
            //loop through buildings and add traits

            foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
            {
                List<FCTraitEffectDef> settlementsTraits = new List<FCTraitEffectDef>();

                foreach (FCEvent evt in Find.World.GetComponent<FactionFC>().events)
                {
                    if (evt.settlementTraitLocations.Any())
                    {
                        //ignore
                        if (evt.settlementTraitLocations.Contains(settlement))
                        {
                            settlementsTraits.AddRange(evt.traits);
                        }
                    }
                }

                foreach (BuildingFCDef building in settlement.buildings)
                {
                    settlementsTraits.AddRange(building.traits);
                }

                settlement.traits = settlementsTraits;
            }
        }

        public static bool IsModLoaded(string packageID) => LoadedModManager.RunningModsListForReading.Any(mod => mod.PackageIdPlayerFacing == packageID);

        public static Type returnUnknownTypeFromName(string name)
        {
            foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = a.GetType(name);
                if (type != null)
                    return type;
            }

            return null;
        }

        public static double calculateMilitaryLevelPoints(int MilitaryLevel)
        {
            double points = 500; //starting points at mil level 0
            for (int i = 1; i <= MilitaryLevel; i++)
            {
                points += (500 * MilitaryLevel);
            }

            return points;
        }

        public static bool canCraftItem(ThingDef thing, bool includeSingleUse = false)
        {
            bool canCraft = true;
            if (thing.recipeMaker != null)
            {
                if (thing.recipeMaker.researchPrerequisites != null)
                {
                    foreach (ResearchProjectDef research in thing.recipeMaker.researchPrerequisites)
                    {
                        if (!(Find.ResearchManager.GetProgress(research) >= research.baseCost))
                        {
                            //research is not good
                            canCraft = false;
                        }
                    }
                }

                if (thing.recipeMaker.researchPrerequisite != null)
                {
                    if (!(Find.ResearchManager.GetProgress(thing.recipeMaker.researchPrerequisite) >=
                          thing.recipeMaker.researchPrerequisite.baseCost))
                    {
                        //research is not good
                        canCraft = false;
                    }
                }
            }
            else
            {
                if (Find.World.GetComponent<FactionFC>().techLevel < thing.techLevel)
                {
                    canCraft = false;
                }
            }

            if (thing.thingSetMakerTags != null && thing.thingSetMakerTags.Contains("SingleUseWeapon") &&
                !includeSingleUse)
            {
                canCraft = false;
            }


            return canCraft;
        }

        public static Faction getPlayerColonyFaction()
        {
            return Find.FactionManager.FirstFactionOfDef(DefDatabase<FactionDef>.GetNamed("PColony"));
        }


        //<DevAdd>   Create new seperate function to create a faction
        public static WorldSettlementFC createPlayerColonySettlement(int tile, bool createWorldObject, string planetName)
        {
            //Log.Message("boop");
            StringBuilder reason = new StringBuilder();
            if (!TileFinder.IsValidTileForNewSettlement(tile, reason))
            {
                //Log.Message("Invalid Tile");
                //Alert Error to User
                Messages.Message(reason.ToString(), MessageTypeDefOf.NegativeEvent);


                return null;
                //create alert with reason
                //AlertsReadout alert = new AlertsReadout()
            }

            //Log.Message("Colony is being created");
            Faction faction = getPlayerColonyFaction();

            FactionFC worldcomp = Find.World.GetComponent<FactionFC>();
            if (!worldcomp.settlements.Any())
            {
                Find.World.GetComponent<FactionFC>().timeStart = Find.TickManager.TicksGame;
            }

            //Log.Message(faction.Name);

            SettlementFC settlementfc;
            WorldSettlementFC settlement = null;
            if (createWorldObject)
            {
                settlementfc = new SettlementFC(getName(faction), tile);
                settlement = (WorldSettlementFC) WorldObjectMaker.MakeWorldObject(
                    DefDatabase<WorldObjectDef>.GetNamed("FactionBaseGenerator"));
                settlement.Tile = tile;

                List<String> used = new List<string>();
                List<Settlement> settlements = Find.WorldObjects.Settlements;
                foreach (Settlement found in settlements)
                {
                    used.Add(found.Name);
                }
                
                settlement.settlement = settlementfc;
                settlement.Name = 
                    NameGenerator.GenerateName(faction.def.factionNameMaker, used, true);
                
                settlement.SetFaction(faction);
                Find.WorldObjects.Add(settlement);
                settlementfc.worldSettlement = settlement;
            }
            else
            {
                settlementfc = new SettlementFC("Settlement", tile);
            }

            //create settlement data for world object
            settlementfc.power.isTithe = true;
            settlementfc.power.isTitheBool = true;
            settlementfc.research.isTithe = true;
            settlementfc.research.isTitheBool = true;
            settlementfc.planetName = planetName;
            if (worldcomp.hasPolicy(FCPolicyDefOf.militaristic))
                settlementfc.constructBuilding(DefDatabase<BuildingFCDef>.GetNamed("barracks"), 0);
            if (worldcomp.hasPolicy(FCPolicyDefOf.authoritarian))
                settlementfc.loyalty = 70;
            if (worldcomp.hasPolicy(FCPolicyDefOf.egalitarian))
                settlementfc.happiness = 60;
            if (worldcomp.hasPolicy(FCPolicyDefOf.expansionist) && settlementfc.settlementLevel == 1)
                settlementfc.upgradeSettlement();

            worldcomp.addSettlement(settlementfc);
            if (createWorldObject)
            {
                worldcomp.roadBuilder.FlagUpdateRoadQueues();
            }

            Find.LetterStack.ReceiveLetter("FCSettlementFormed".Translate(),
                "TheSettlement".Translate() + " " + settlementfc.name + "HasBeenFormed".Translate() + "!",
                LetterDefOf.PositiveEvent);

            //Example to grab settlement data from FC
            //Log.Message(settlementfc.ReturnFCSettlement().Name.ToString());


            return settlement;
        }

        private static readonly List<string> usedNames = new List<string>();
        
        private static string getName(Faction faction)
        {
            if (faction?.def.settlementNameMaker == null)
            {
                return "Settlement";
            }

            RulePackDef rulePack = faction.def.settlementNameMaker;
            usedNames.Clear();
            List<Settlement> settlements = Find.WorldObjects.Settlements;
            for (int index = 0; index < settlements.Count; ++index)
            {
                Settlement settlement = settlements[index];
                if (settlement.Name != null)
                    usedNames.Add(settlement.Name);
            }

            return NameGenerator.GenerateName(rulePack, usedNames, true);
        }

        [DebugAction("Empire", "View Events and ticks till", allowedGameStates = AllowedGameStates.Playing)]
        private static void ViewEventsAndLog()
        {
            Find.World.GetComponent<FactionFC>().events.ForEach(delegate(FCEvent e)
            {
                Log.Message(e.def.defName + " with cooldown: " + (e.timeTillTrigger - Find.TickManager.TicksGame));
            });
        }

        [DebugAction("Empire", "Increment Time 5 Days", allowedGameStates = AllowedGameStates.Playing)]
        private static void incrementTimeFiveDays()
        {
            //Log.Message("Debug - Increment Time 5 Days");
            Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + 300000);
        }

        [DebugAction("Empire", "Increment Time 1 Year", allowedGameStates = AllowedGameStates.Playing)]
        private static void incrementTimeOneYear()
        {
            //Log.Message("Debug - Increment Time 5 Days");
            Find.TickManager.DebugSetTicksGame(Find.TickManager.TicksGame + GenDate.TicksPerYear);
        }
        
        [DebugAction("Empire", "Print Races", allowedGameStates = AllowedGameStates.Playing)]
        private static void PrintRaces()
        {
            getPlayerColonyFaction().def.pawnGroupMakers.ForEach(maker =>
            {
                Log.Message("Traders: " + maker.traders.Count);
                foreach (PawnGenOption option in maker.options)
                {
                    Log.Message("Race: " + option.kind.race.defName + ", " + option.kind.defName + ", " + 
                                option.kind.isFighter + ", " + option.kind.trader + " for " + maker.kindDef);   
                }
            });
        }

        [DebugAction("Empire", "Reset All Military Squad Assignments", allowedGameStates = AllowedGameStates.Playing)]
        private static void resetAllMilitarySquads()
        {
            Log.Message("Debug - Reset All Military Squad Assignments");
            MilitaryCustomizationUtil util = Find.World.GetComponent<FactionFC>().militaryCustomizationUtil;
            for (int i = util.AllMercenaries.Count - 1; i >= 0; i--)
            {
                if (util.AllMercenaries[i].squad.hasLord)
                {
                    util.AllMercenaries[i].squad.map.lordManager.RemoveLord(util.AllMercenaries[i].squad.lord);
                }

                util.AllMercenaries[i].pawn.Destroy();
                util.AllMercenaries[i].squad.mercenaries.Remove(util.AllMercenaries[i]);
            }

            for (int k = util.mercenarySquads.Count() - 1; k >= 0; k--)
            {
                util.mercenarySquads[k].settlement.militarySquad = null;
                util.mercenarySquads.RemoveAt(k);
            }


            util.checkMilitaryUtilForErrors();
        }


        [DebugAction("Empire", "Make Random Event", allowedGameStates = AllowedGameStates.Playing)]
        private static void makeRandomEvent()
        {
            List<DebugMenuOption> list = new List<DebugMenuOption>();
            foreach (FCEventDef evtDef in DefDatabase<FCEventDef>.AllDefsListForReading)
            {
                if (evtDef.isRandomEvent)
                    list.Add(new DebugMenuOption(evtDef.label, DebugMenuOptionMode.Action, delegate
                        {
                            Log.Message("Debug - Make Random Event - " + evtDef.label);
                            FCEvent evt = FCEventMaker.MakeRandomEvent(evtDef, null);
                            if (evtDef.activateAtStart == false)
                            {
                                FCEventMaker.MakeRandomEvent(evtDef, null);
                                Find.World.GetComponent<FactionFC>().addEvent(evt);
                            }

                            //letter code
                            string settlementString = evt.settlementTraitLocations.Join((settlement) => $" {settlement.name}", "\n");

                            if (!settlementString.NullOrEmpty()) Find.LetterStack.ReceiveLetter("Random Event", $"{evt.def.desc}\n{"EventAffectingSettlements".Translate()}\n{settlementString}", LetterDefOf.NeutralEvent);
                        }
                    ));
            }

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
        }

        [DebugAction("Empire", "Proc MilitaryTimeDue", allowedGameStates = AllowedGameStates.Playing)]
        private static void procMilitaryTimeDue()
        {
            Log.Message("Debug - Proc MilitaryTimeDue");
            Find.World.GetComponent<FactionFC>().militaryTimeDue = Find.TickManager.TicksGame + 1;
        }

        [DebugAction("Empire", "Fix Missing Settlements", allowedGameStates = AllowedGameStates.Playing)]
        private static void checkForMissingSettlements()
        {
            Log.Message("Debug - Proc MilitaryTimeDue");

            FactionFC factionfc = Find.World.GetComponent<FactionFC>();

            foreach (SettlementFC settlement in factionfc.settlementsOnPlanet)
            {
                if (Find.WorldObjects.AnyWorldObjectAt(settlement.mapLocation) == false)
                {
                    createPlayerColonySettlement(settlement.mapLocation, true, Find.World.info.name);
                }
            }
        }


        [DebugAction("Empire", "Reset Faction Leaders", allowedGameStates = AllowedGameStates.Playing)]
        private static void resetFactionLeadeers()
        {
            Log.Message("Debug - Reset Faction Leaders");
            SoS2HarmonyPatches.ResetFactionLeaders();
        }

        [DebugAction("Empire", "Attack Player Settlement", allowedGameStates = AllowedGameStates.Playing)]
        private static void attackPlayerSettlement()
        {
            List<DebugMenuOption> list = new List<DebugMenuOption>();
            foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
            {
                list.Add(new DebugMenuOption(settlement.name, DebugMenuOptionMode.Action, delegate
                    {
                        Log.Message("Debug - Attack Player Settlement - " + settlement.name);
                        Faction enemyFaction = Find.FactionManager.RandomEnemyFaction();
                        MilitaryUtilFC.attackPlayerSettlement(
                            militaryForce.createMilitaryForceFromFaction(enemyFaction, true), settlement, enemyFaction);
                    }
                ));
            }

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
        }


        [DebugAction("Empire", "Change Settlement Defending Force", allowedGameStates = AllowedGameStates.Playing)]
        private static void ChangeAttackPlayerSettlementMilitaryForce()
        {
            FactionFC worldcomp = Find.World.GetComponent<FactionFC>();
            List<DebugMenuOption> list = new List<DebugMenuOption>();
            foreach (FCEvent evt in worldcomp.events)
            {
                if (evt.def == FCEventDefOf.settlementBeingAttacked)
                {
                    list.Add(new DebugMenuOption(
                        worldcomp.returnSettlementByLocation(evt.location, evt.planetName).name,
                        DebugMenuOptionMode.Action, delegate
                        {
                            //when event is selected, select defending force to replace it with

                            List<DebugMenuOption> list2 = new List<DebugMenuOption>();
                            foreach (SettlementFC settlement in worldcomp.settlements)
                            {
                                if (settlement.isMilitaryValid() && settlement.name != evt.settlementFCDefending.name)
                                {
                                    list2.Add(new DebugMenuOption(
                                        settlement.name + " - " + settlement.settlementMilitaryLevel + " - Busy: " +
                                        settlement.isMilitaryBusySilent(), DebugMenuOptionMode.Action, delegate
                                        {
                                            if (settlement.isMilitaryBusy() == false)
                                            {
                                                Log.Message("Debug - Change Player Settlement - " +
                                                            evt.militaryForceDefending.homeSettlement.name + " to " +
                                                            settlement.name);
                                                MilitaryUtilFC.changeDefendingMilitaryForce(evt, settlement);
                                            }
                                        }
                                    ));
                                }
                            }

                            Find.WindowStack.Add(new Dialog_DebugOptionListLister(list2));
                        }
                    ));
                    Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
                }
            }
        }

        [DebugAction("Empire", "Upgrade Player Settlement", allowedGameStates = AllowedGameStates.Playing)]
        private static void UpgradePlayerSettlementx1() => UpgradePlayerSettlement();

        [DebugAction("Empire", "Upgrade Player Settlement x5", allowedGameStates = AllowedGameStates.Playing)]
        private static void UpgradePlayerSettlementx5() => UpgradePlayerSettlement(5);

        private static void UpgradePlayerSettlement(int times = 1)
        {
            List<DebugMenuOption> list = new List<DebugMenuOption>();
            foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
            {
                list.Add(new DebugMenuOption(settlement.name, DebugMenuOptionMode.Action, delegate
                {
                    if (times > 0)
                    {
                        Log.Message("Debug - Upgrade Player Settlement x" + times + "- " + settlement.name);
                    }
                    else
                    {
                        Log.Message("Debug - Downgrade Player Settlement x" + times + "- " + settlement.name);
                    }
                    settlement.upgradeSettlement(times);
                }
                ));
            }

            Find.WindowStack.Add(new Dialog_DebugOptionListLister(list));
        }

        [DebugAction("Empire", "Test Function", allowedGameStates = AllowedGameStates.Playing)]
        private static void testVariable()
        {
            Log.Message("Debug - Test Function - ");
            Find.World.GetComponent<FactionFC>().roadBuilder.FlagUpdateRoadQueues();
        }

        [DebugAction("Empire", "De-Level Player Settlement", allowedGameStates = AllowedGameStates.Playing)]
        private static void DelevelPlayerSettlement() => UpgradePlayerSettlement(-1);

        [DebugAction("Empire", "Reset Military Squads Cooldowns", allowedGameStates = AllowedGameStates.Playing)]
        private static void ResetMilitarySquads()
        {
            Find.World.GetComponent<FactionFC>().militaryCustomizationUtil.mercenarySquads =
                new List<MercenarySquadFC>();
            Log.Message("Debug - Reset Military Squad Cooldowns");
            foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
            {
                settlement.returnMilitary(false);
            }
        }

        [DebugAction("Empire", "Clear Old Bills", allowedGameStates = AllowedGameStates.Playing)]
        private static void clearOldBills()
        {
            Find.World.GetComponent<FactionFC>().OldBills = new List<BillFC>();
        }

        [DebugAction("Empire", "Clear All Events", allowedGameStates = AllowedGameStates.Playing)]
        private static void clearAllEvents()
        {
            Find.World.GetComponent<FactionFC>().events = new List<FCEvent>();
        }

        [DebugAction("Empire", "Clear All Bills", allowedGameStates = AllowedGameStates.Playing)]
        private static void clearAllBills()
        {
            Find.World.GetComponent<FactionFC>().Bills = new List<BillFC>();
        }

        [DebugAction("Empire", "Place 500 Silver", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void placeSilverFC() => silverPlacer(500);

        [DebugAction("Empire", "Place 50000 Silver", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void placeALotOfSilverFC() => silverPlacer(50000);

        private static void silverPlacer(int amount = 500)
        {
            DebugTool tool = null;
            IntVec3 DropPosition;
            Map map;
            tool = new DebugTool("Select Drop Position", delegate
            {
                DropPosition = UI.MouseCell();
                map = Find.CurrentMap;


                Thing silver = ThingMaker.MakeThing(ThingDefOf.Silver);
                silver.stackCount = amount;
                GenPlace.TryPlaceThing(silver, DropPosition, map, ThingPlaceMode.Near);
            });
            DebugTools.curTool = tool;
        }

        /// <summary>
        /// Internal method used to spawn a <paramref name="settlement"/>'s squad for military deployment
        /// </summary>
        /// <param name="settlement"></param>
        /// <param name="squad"></param>
        /// <param name="dropPosition"></param>
        /// <param name="DropPod"></param>
        private static void SpawnSquad(SettlementFC settlement, MercenarySquadFC squad, IntVec3 dropPosition, bool DropPod)
        {
            IncidentParms parms = new IncidentParms
            {
                target = Find.CurrentMap,
                faction = getPlayerColonyFaction(),
                podOpenDelay = 140,
                points = 999,
                raidArrivalModeForQuickMilitaryAid = true,
                raidNeverFleeIndividual = true,
                raidForceOneIncap = true,
                raidArrivalMode = PawnsArrivalModeDefOf.CenterDrop,
                raidStrategy = RaidStrategyDefOf.ImmediateAttackFriendly
            };

            if (DropPod)
            {
                parms.spawnCenter = dropPosition;
                PawnsArrivalModeWorkerUtility.DropInDropPodsNearSpawnCenter(parms, squad.AllEquippedMercenaryPawns);
            }
            else
            {
                PawnsArrivalModeWorker_EdgeWalkIn worker = new PawnsArrivalModeWorker_EdgeWalkIn();
                worker.TryResolveRaidSpawnCenter(parms);
                worker.Arrive(squad.AllEquippedMercenaryPawns, parms);
            }

            squad.AllEquippedMercenaryPawns.ForEach(pawn => pawn.ApplyIdeologyRitualWounds());
            squad.isDeployed = true;
            squad.orderLocation = dropPosition;
            squad.timeDeployed = Find.TickManager.TicksGame;
            Find.LetterStack.ReceiveLetter("deploymentSuccessLabel".Translate(), "deploymentSuccessDesc".Translate(settlement.name, Find.CurrentMap.Parent.LabelCap), LetterDefOf.NeutralEvent, new LookTargets(squad.AllEquippedMercenaryPawns));

            settlement.SendMilitary(Find.CurrentMap.Index, Find.World.info.name, MilitaryJob.Deploy, 1, null);
            LordMaker.MakeNewLord(getPlayerColonyFaction(), new LordJob_DeployMilitary(dropPosition, squad), Find.CurrentMap, squad.AllEquippedMercenaryPawns);

            if (settlement.militarySquad != squad)
            {
                Find.World.GetComponent<FactionFC>().traitMilitaristicTickLastUsedExtraSquad = Find.TickManager.TicksGame;
            }
        }

        /// <summary>
        /// Deploys a <paramref name="settlement"/>'s main force, takes silver if there is an <paramref name="overrideSquad"/>
        /// </summary>
        /// <param name="settlement"></param>
        /// <param name="DropPod"></param>
        /// <param name="overrideSquad"></param>
        public static void CallinAlliedForces(SettlementFC settlement, bool DropPod, MercenarySquadFC overrideSquad = null)
        {
            MercenarySquadFC squad = overrideSquad ?? settlement.militarySquad;

            if (Find.CurrentMap.Parent is WorldSettlementFC)
            {
                Messages.Message("FCMilitaryTriedDeployingToSettlementFC".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            squad.updateSquadStats(settlement.settlementMilitaryLevel);
            squad.resetNeeds();

            IntVec3 dropPosition;
            DebugTool tool = new DebugTool("selectDeploymentPosition".Translate(), delegate
            {
                dropPosition = UI.MouseCell();
                Map curMap = Find.CurrentMap;

                if (!dropPosition.InBounds(curMap)) 
                { 
                    Messages.Message("selectedPosOutOfBounds".Translate(), MessageTypeDefOf.RejectInput);
                    return;
                }
                if (dropPosition.CloseToEdge(curMap, 10))
                {
                    Messages.Message("selectedPosTooCloseToEdge".Translate(), MessageTypeDefOf.RejectInput);
                    return;
                }

                if (overrideSquad != null) PaymentUtil.paySilver((int)Math.Round(settlement.militarySquad.outfit.updateEquipmentTotalCost() * .2));
                SpawnSquad(settlement, squad, dropPosition, DropPod);
                DebugTools.curTool = null;
            });
            DebugTools.curTool = tool;

            //UI.UIToMapPosition(UI.MousePositionOnUI).ToIntVec3();
        }

        /// <summary>
        /// Deploys the secondary military of the empire from a <paramref name="settlement"/> 
        /// </summary>
        /// <param name="settlement"></param>
        /// <param name="DropPod"></param>
        /// <param name="cost"></param>
        public static void CallinExtraForces(SettlementFC settlement, bool DropPod)
        {
            MercenarySquadFC squad = Find.World.GetComponent<FactionFC>().militaryCustomizationUtil.createMercenarySquad(settlement, true);
            squad.OutfitSquad(squad.settlement.militarySquad.outfit);

            CallinAlliedForces(settlement, DropPod, squad);
        }

        public static void FireSupport(SettlementFC settlement, MilitaryFireSupport support)
        {
            DebugTool tool = null;
            IntVec3 DropPosition;
            tool = new DebugTool("FCFireSupportSelectPosition".Translate(), delegate
            {
                float cost = support.returnTotalCost();
                if (PaymentUtil.getSilver() > cost)
                {
                    PaymentUtil.paySilver((int) Math.Round(cost));
                    DropPosition = UI.MouseCell();
                    IntVec3 spawnCenter = DropPosition;
                    Map map = Find.CurrentMap;
                    //Make new list
                    List<ThingDef> projectiles = new List<ThingDef>();
                    projectiles.AddRange(support.projectiles);
                    MilitaryFireSupport fireSupport = new MilitaryFireSupport("fireSupport", map, spawnCenter,
                        projectiles.Count() * 15, 600, support.accuracy, projectiles);
                    Find.World.GetComponent<FactionFC>().militaryCustomizationUtil.fireSupport.Add(fireSupport);

                    Messages.Message("FCFireSupportNameWillBeFiredOnPosition".Translate(support.name), MessageTypeDefOf.ThreatSmall);
                    settlement.artilleryTimer = Find.TickManager.TicksGame + 60000;
                }
                else
                {
                    Messages.Message("FCFireSupportNoSilver".Translate(), MessageTypeDefOf.RejectInput);
                }


                DebugTools.curTool = null;
            }, delegate { GenDraw.DrawRadiusRing(UI.MouseCell(), support.accuracy, Color.red); });
            DebugTools.curTool = tool;
        }

        /// <summary>
        /// Debug function. Calls in Allied Forces. Doesn't need translations
        /// </summary>
        private static void CallInAlliedForcesSelect()
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
            {
                if (settlement.militarySquad != null)
                {
                    list.Add(new FloatMenuOption(settlement.name, delegate
                        {
                            IncidentParms parms = new IncidentParms();
                            parms.target = Find.CurrentMap;
                            parms.faction = getPlayerColonyFaction();
                            parms.podOpenDelay = 140;
                            parms.points = 999;
                            parms.raidArrivalModeForQuickMilitaryAid = true;
                            parms.raidNeverFleeIndividual = true;
                            parms.raidForceOneIncap = true;
                            parms.raidArrivalMode = PawnsArrivalModeDefOf.CenterDrop;
                            parms.raidStrategy = RaidStrategyDefOf.ImmediateAttackFriendly;
                            parms.raidArrivalModeForQuickMilitaryAid = true;

                            settlement.militarySquad.updateSquadStats(settlement.settlementMilitaryLevel);


                            DebugTool tool = null;
                            IntVec3 DropPosition;
                            tool = new DebugTool("Select Drop Position", delegate
                            {
                                DropPosition = UI.MouseCell();
                                parms.spawnCenter = DropPosition;

                                //List<Pawn> list2 = parms.raidStrategy.Worker.SpawnThreats(parms);
                                //parms.raidArrivalMode.Worker.Arrive(list2, parms);
                                settlement.militarySquad.isDeployed = true;
                                settlement.militarySquad.orderLocation = DropPosition;
                                settlement.militarySquad.timeDeployed = Find.TickManager.TicksGame;


                                PawnsArrivalModeWorkerUtility.DropInDropPodsNearSpawnCenter(parms,
                                    settlement.militarySquad.AllEquippedMercenaryPawns);
                                settlement.militarySquad.AllEquippedMercenaryPawns.ForEach(pawn => pawn.ApplyIdeologyRitualWounds());
                                settlement.militarySquad.isDeployed = true;
                                DebugTools.curTool = null;
                            });
                            DebugTools.curTool = tool;

                            //UI.UIToMapPosition(UI.MousePositionOnUI).ToIntVec3();
                        }
                    ));
                }
            }

            Find.WindowStack.Add(new FloatMenu(list));
        }


        [DebugAction("Empire", "Call In Allied Forces", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void CallInAlliedForcesDebug() => CallInAlliedForcesSelect();


        [DebugAction("Empire", "Level Up Faction", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void LevelUpFaction()
        {
            FactionFC faction = Find.World.GetComponent<FactionFC>();
            faction.addExperienceToFactionLevel(faction.factionXPGoal);
        }

        public static bool returnIsResearched(ResearchProjectDef def)
        {
            if (def == null)
            {
                return false;
            }

            return Math.Abs(Find.ResearchManager.GetProgress(def) - def.baseCost) < .1;
        }

        public static void removePlayerSettlement(SettlementFC settlement)
        {
            settlement.PrepareDestroyWorldObject();
            FactionFC faction = Find.World.GetComponent<FactionFC>();
            faction.settlements.Remove(settlement);
            Messages.Message("SettlementRemoved".Translate(settlement.name), MessageTypeDefOf.NegativeEvent);

            if (Find.World.info.name == settlement.planetName)
            {
                Find.WorldObjects.Remove(Find.World.worldObjects.WorldObjectOfDefAt(DefDatabase<WorldObjectDef>
                    .GetNamed("FactionBaseGenerator"), settlement.mapLocation));
            }
            else
            {
                faction.deleteSettlementQueue.Add(new SettlementSoS2Info(settlement.planetName,
                    settlement.mapLocation));
            }

            //clear military events
            settlement.returnMilitary(false);

            HashSet<FCEvent> toRemove = new HashSet<FCEvent>();

            foreach (FCEvent evt in faction.events)
            {
                //military event removal
                if (evt.def == FCEventDefOf.captureEnemySettlement || evt.def == FCEventDefOf.raidEnemySettlement)
                {
                    if (evt.militaryForceAttacking.homeSettlement == settlement)
                    {
                        toRemove.Add(evt);
                    }
                }

                if (evt.def == FCEventDefOf.settlementBeingAttacked)
                {
                    if (evt.militaryForceDefending.homeSettlement == settlement)
                    {
                        if (evt.settlementFCDefending == settlement)
                        {
                            toRemove.Add(evt);
                        }

                        //if not defending settlement
                        MilitaryUtilFC.changeDefendingMilitaryForce(evt, evt.settlementFCDefending);
                    }
                    else
                    {
                        //if force belongs to other settlement
                        evt.militaryForceDefending.homeSettlement.cooldownMilitary();

                        toRemove.Add(evt);
                    }
                }


                //settlement event removal
                if (evt.def == FCEventDefOf.constructBuilding || evt.def == FCEventDefOf.enactSettlementPolicy ||
                    evt.def == FCEventDefOf.upgradeSettlement || evt.def == FCEventDefOf.cooldownMilitary)
                {
                    if (evt.source == settlement.mapLocation)
                    {
                        toRemove.Add(evt);
                    }
                }

                if (evt.def.isRandomEvent && evt.settlementTraitLocations.Count() > 0)
                {
                    if (evt.settlementTraitLocations.Contains(settlement))
                    {
                        evt.settlementTraitLocations.Remove(settlement);
                        if (evt.settlementTraitLocations.Count() == 0)
                        {
                            toRemove.Add(evt);
                        }
                    }
                }
            }

            foreach(FCEvent evt in toRemove)
            {
                faction.events.Remove(evt);
            }
        }

        public static int CompareFloatMenuOption(FloatMenuOption x, FloatMenuOption y)
        {
            return String.Compare(x.Label, y.Label);
        }

        public static int CompareBuildingDef(BuildingFCDef x, BuildingFCDef y)
        {
            return string.Compare(x.label, y.label);
        }

        public static int CompareSettlementName(SettlementFC x, SettlementFC y)
        {
            return string.Compare(x.name, y.name);
        }

        public static int CompareSettlementLevel(SettlementFC x, SettlementFC y)
        {
            return y.settlementLevel.CompareTo(x.settlementLevel);
        }

        public static int CompareSettlementMilitaryLevel(SettlementFC x, SettlementFC y)
        {
            return y.settlementMilitaryLevel.CompareTo(x.settlementMilitaryLevel);
        }

        public static int CompareSettlementFreeWorkers(SettlementFC x, SettlementFC y)
        {
            return ((y.workersUltraMax - y.getTotalWorkers()).CompareTo((x.workersUltraMax - x.getTotalWorkers())));
        }

        public static int CompareSettlementUnrest(SettlementFC x, SettlementFC y)
        {
            return x.unrest.CompareTo(y.unrest);
        }

        public static int CompareSettlementLoyalty(SettlementFC x, SettlementFC y)
        {
            return y.loyalty.CompareTo(x.loyalty);
        }

        public static int CompareSettlementHappiness(SettlementFC x, SettlementFC y)
        {
            return y.happiness.CompareTo(x.happiness);
        }

        public static int CompareSettlementProsperity(SettlementFC x, SettlementFC y)
        {
            return y.prosperity.CompareTo(x.prosperity);
        }

        public static int CompareSettlementProfit(SettlementFC x, SettlementFC y)
        {
            return y.getTotalProfit().CompareTo(x.getTotalProfit());
        }

        public static int ReturnTicksToArrive(int currentTile, int destinationTile)
        {
            bool tilesInShuttleRange = (currentTile, destinationTile).AreTilesInAnyShuttleRange();
            bool medievalOnly = LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().medievalTechOnly;
            bool podsResearched = DefDatabase<ResearchProjectDef>.GetNamed("TransportPod", false)?.IsFinished ?? false;

            if (!medievalOnly)
            {
                if (!(currentTile, destinationTile).AreValidTiles()) return podsResearched ? 30000 : 600000;
                if (podsResearched) return Find.WorldGrid.TraversalDistanceBetween(currentTile, destinationTile) * (tilesInShuttleRange ? 5 : 10);
            }

            using (WorldPath tempPath = Find.WorldPathFinder.FindPath(currentTile, destinationTile, null))
            {
                if (tempPath == WorldPath.NotFound) return 600000;

                return CaravanArrivalTimeEstimator.EstimatedTicksToArrive(currentTile, destinationTile, tempPath, 0f, CaravanTicksPerMoveUtility.GetTicksPerMove(null), Find.TickManager.TicksAbs);
            }
        }

        public static void sendPrisoner(Pawn prisoner, SettlementFC settlement)
        {
            settlement.addPrisoner(prisoner);
            prisoner.DeSpawn();
        }

        public static Faction copyPlayerColonyFaction()
        {
            FactionFC worldcomp = Find.World.GetComponent<FactionFC>();

            worldcomp.setCapital();

            FactionDef facDef = new FactionDef();


            facDef = DefDatabase<FactionDef>.GetNamed("PColony");
            Faction faction = new Faction();
            faction.def = facDef;
            faction.def.techLevel = worldcomp.factionBackup.def.techLevel;
            faction.loadID = Find.UniqueIDsManager.GetNextFactionID();
            faction.colorFromSpectrum = worldcomp.factionBackup.colorFromSpectrum;
            faction.Name = worldcomp.factionBackup.Name;
            faction.centralMelanin = worldcomp.factionBackup.centralMelanin;
            //<DevAdd> Copy player faction relationships  
            foreach (Faction other in Find.FactionManager.AllFactionsListForReading)
            {
                faction.TryMakeInitialRelationsWith(other);
            }

            //faction.GenerateNewLeader();
            faction.TryGenerateNewLeader();

            //Log.Message(Find.FactionManager.AllFactions.Contains(faction).ToString());

            //Find.FactionManager.Add(faction);

            //check if SoS2 is enabled
            if (IsModLoaded("kentington.saveourship2"))
            {
                Log.Message("SoS2 running - planet changed");
                //SoS2 is loaded

                Type typ = returnUnknownTypeFromName("SaveOurShip2.WorldSwitchUtility");
                Type typ2 = returnUnknownTypeFromName("SaveOurShip2.WorldFactionList");

                var mainclass = Traverse.CreateWithType(typ.ToString());
                var dict = mainclass.Property("PastWorldTracker").Field("WorldFactions").GetValue();

                var planetfactiondict = Traverse.Create(dict);
                var unknownclass = planetfactiondict.Property("Item", new object[] {Find.World.info.name}).GetValue();

                var factionlist = Traverse.Create(unknownclass);
                var list = factionlist.Field("myFactions").GetValue();
                List<String> modifiedlist = (List<String>) list;
                modifiedlist.Add(faction.GetUniqueLoadID());
                factionlist.Field("myFactions").SetValue(modifiedlist);
                //Log.Message("Added faction to world list");

                foreach (Faction other in Find.FactionManager.AllFactionsVisibleInViewOrder)
                {
                    faction.TryMakeInitialRelationsWith(other);
                }

                Find.FactionManager.Add(faction);
            }


            return faction;
        }

        public static void debugMarker(ref int i)
        {
            Log.Message(i.ToString());
            i = i + 1;
        }

        public static Faction createPlayerColonyFaction()
        {
            FactionFC worldcomp = Find.World.GetComponent<FactionFC>();
            //Log.Message("Creating new faction");
            //Set start time for world component to start tracking your faction;
            worldcomp.setCapital();

            //Log.Message("Faction is being created");
            FactionDef facDef = DefDatabase<FactionDef>.GetNamed("PColony");
            Faction faction = new Faction();
            faction.def = facDef;
            faction.def.techLevel = TechLevel.Undefined;
            faction.loadID = Find.UniqueIDsManager.GetNextFactionID();
            faction.colorFromSpectrum = FactionGenerator.NewRandomColorFromSpectrum(faction);
            faction.Name = "PlayerColony".Translate();
            faction.centralMelanin = Rand.Value;
            faction.def.classicIdeo = Faction.OfPlayer.def.classicIdeo;
            faction.ideos = Faction.OfPlayer.ideos;
            //<DevAdd> Copy player faction relationships  
            foreach (Faction other in Find.FactionManager.AllFactionsListForReading)
            {
                faction.TryMakeInitialRelationsWith(other);
            }
            // Set starting goodwill to Player
            faction.TryAffectGoodwillWith(Faction.OfPlayer, 200);

            // Generate Leader
            if(!faction.TryGenerateNewLeader())
            {
                Log.Message("Generating Leader failed! Manually Generating . . .");
                faction.leader = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind: Faction.OfPlayer.RandomPawnKind(),
                faction: faction, context: PawnGenerationContext.NonPlayer, 
                forceGenerateNewPawn: true, newborn: false, allowDead: false, allowDowned: false,
                canGeneratePawnRelations: true, mustBeCapableOfViolence: true, colonistRelationChanceFactor: 0,
                forceAddFreeWarmLayerIfNeeded: false,  worldPawnFactionDoesntMatter: false));
                if(faction.leader == null)
                {
                    Log.Warning("That failed, too! Contacting " + faction.Name + " won't work!");
                }
            }
            worldcomp.factionBackup = faction;
            Find.FactionManager.Add(faction);

            Find.World.GetComponent<FactionFC>().updateTechLevel(Find.ResearchManager);
            return faction;
        }

        public static void changePlayerColonyFaction(Faction faction)
        {
            faction = createPlayerColonyFaction();
            Log.Message("Faction was updated - " + faction.Name);
        }


        private static List<float> getAttackPoints()
        {
            List<float> list = new List<float>();
            for (int i = -Convert.ToInt32(plusOrMinusRandomAttackValue * 10);
                i < plusOrMinusRandomAttackValue * 10;
                i++)
            {
                list.Add((i / 10));
            }

            return list;
        }

        public static float randomAttackModifier()
        {
            float y = (from x in getAttackPoints()
                select x).RandomElementByWeight(x =>
                new SimpleCurve
                        {new CurvePoint(0f, 1f), new CurvePoint(plusOrMinusRandomAttackValue, .1f)}
                    .Evaluate(Math.Abs(x) - 2));
            return y;
        }


        public static string FloorStat(double stat)
        {
            return Convert.ToString(Math.Floor((stat * 100)) / 100);
        }

        public static FactionColonies Settings() => LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>();

        public static string getTownTitle(SettlementFC settlement)
        {
            double highest = 0;
            ResourceType? resourceKey = null;
            int level;
            if (settlement.settlementLevel <= 3)
            {
                level = 1;
            }
            else if (settlement.settlementLevel <= 6)
            {
                level = 2;
            }
            else
            {
                level = 3;
            }

            foreach (ResourceType resourceType in ResourceUtils.resourceTypes)
            {
                ResourceFC resource = settlement.getResource(resourceType);
                if (resource.endProduction > highest)
                {
                    highest = resource.endProduction;
                    resourceKey = resourceType;
                }
            }

            return ("FCTitle_" + resourceKey + "_" + level).Translate();
        }

        public int silverPerResource = 100;
        public static double silverToCreateSettlement = 1000;
        public int timeBetweenTaxes = GenDate.TicksPerTwelfth;
        public static int updateUiTimer = 150;
        public int productionTitheMod = 25;
        public static int productionResearchBase = 100;
        public static int storeReportCount = 4;
        public int workerCost = 100;


        public static double unrestBaseGain = 0;
        public static double unrestBaseLost = 1;

        public static double loyaltyBaseGain = 1;
        public static double loyaltyBaseLost = 0;

        public static double happinessBaseGain = 1;
        public static double happinessBaseLost = 0;

        public static double prosperityBaseRecovery = 1;

        public double settlementBaseUpgradeCost = 1000;
        public int settlementMaxLevel = 10;

        public bool medievalTechOnly;
        public bool disableHostileMilitaryActions;
        public bool disableRandomEvents;
        public bool disableForcedPausingDuringEvents = true;
        public bool deadPawnsIncreaseMilitaryCooldown;
        public bool settlementsAutoBattle = true;
        public TaxDeliveryMode forcedTaxDeliveryMode;

        public int minDaysTillMilitaryAction = 4;
        public int maxDaysTillMilitaryAction = 10;

        public int minDaysTillRandomEvent = 0;
        public int maxDaysTillRandomEvent = 6;
        public IntRange minMaxDaysTillMilitaryAction = new IntRange(4, 10);

        private static float plusOrMinusRandomAttackValue = 2;
        public static double militaryAnimalCostMultiplier = 1.5;
        public static double militaryRaceCostMultiplier = .15;

        public double updateVersion = 0;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref silverPerResource, "silverPerResource");
            Scribe_Values.Look(ref timeBetweenTaxes, "timeBetweenTaxes");
            Scribe_Values.Look(ref productionTitheMod, "productionTitheMod");
            Scribe_Values.Look(ref workerCost, "workerCost");
            Scribe_Values.Look(ref settlementMaxLevel, "settlementMaxLevel");
            Scribe_Values.Look(ref medievalTechOnly, "medievalTechOnly");
            Scribe_Values.Look(ref disableHostileMilitaryActions, "disableHostileMilitaryActions");
            Scribe_Values.Look(ref disableRandomEvents, "disableRandomEvents");
            Scribe_Values.Look(ref forcedTaxDeliveryMode, "forcedTaxDeliveryMode", default);
            Scribe_Values.Look(ref deadPawnsIncreaseMilitaryCooldown, "deadPawnsIncreaseMilitaryCooldown");
            Scribe_Values.Look(ref settlementsAutoBattle, "settlementsAutoBattle");
            Scribe_Values.Look(ref minDaysTillMilitaryAction, "minDaysTillMilitaryAction");
            Scribe_Values.Look(ref maxDaysTillMilitaryAction, "maxDaysTillMilitaryAction");
            Scribe_Values.Look(ref minDaysTillRandomEvent, "minDaysTillRandomEvent", 0);
            Scribe_Values.Look(ref maxDaysTillRandomEvent, "maxDaysTillRandomEvent", 6);
            Scribe_Values.Look(ref updateVersion, "updateVersion");
        }
    }

    
    public class FactionColoniesMod : Mod
    {
        public FactionColonies settings = new FactionColonies();

        public FactionColoniesMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<FactionColonies>();
        }

        string silverPerResource;
        string timeBetweenTaxes;
        string productionTitheMod;
        string workerCost;
        string settlementMaxLevel;
        int daysBetweenTaxes;
        IntRange minMaxDaysTillMilitaryAction = new IntRange(4, 10);
        IntRange minMaxDaysTillRandomEvent = new IntRange(0, 6);

        private Vector2 scrollVector = new Vector2();
        private float viewRectHeight = -1f;

        private bool firstRun = true;
        private bool fixDone = false;

        /// <summary>
        /// Creates an option for the list of ForcedTaxDeliveryOptions. Shuttles may not be used if royality is inactive
        /// </summary>
        private FloatMenuOption ShuttleOption
        {
            get
            {
                if (ModsConfig.RoyaltyActive)
                {
                    return new FloatMenuOption("taxDeliveryModeShuttleDesc".Translate(), delegate () {settings.forcedTaxDeliveryMode = TaxDeliveryMode.Shuttle;});
                }
                else 
                { 
                    return new FloatMenuOption("taxDeliveryModeShuttleUnavailableDesc".Translate(), null); 
                }
            }
        }

        /// <summary>
        /// Creates a list of options for forced tax delivery
        /// </summary>
        private List<FloatMenuOption> ForcedTaxDeliveryOptions
        {
            get
            {
                return new List<FloatMenuOption>() 
                {
                    new FloatMenuOption("taxDeliveryModeDefaultDesc".Translate(), delegate() {settings.forcedTaxDeliveryMode = default;}),
                    new FloatMenuOption("taxDeliveryModeTaxSpotDesc".Translate(), delegate() {settings.forcedTaxDeliveryMode = TaxDeliveryMode.TaxSpot;}),
                    new FloatMenuOption("taxDeliveryModeCaravanDesc".Translate(), delegate() {settings.forcedTaxDeliveryMode = TaxDeliveryMode.Caravan;}),
                    new FloatMenuOption("taxDeliveryModeDropPodDesc".Translate(), delegate() {settings.forcedTaxDeliveryMode = TaxDeliveryMode.DropPod;}),
                    ShuttleOption
                };
            }
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            silverPerResource = settings.silverPerResource.ToString();
            timeBetweenTaxes = (settings.timeBetweenTaxes / 60000).ToString();
            productionTitheMod = settings.productionTitheMod.ToString();
            workerCost = settings.workerCost.ToString();
            settlementMaxLevel = settings.settlementMaxLevel.ToString();
            daysBetweenTaxes = settings.timeBetweenTaxes / 60000;

            minMaxDaysTillMilitaryAction = new IntRange(settings.minDaysTillMilitaryAction, settings.maxDaysTillMilitaryAction);
            minMaxDaysTillRandomEvent = new IntRange(settings.minDaysTillRandomEvent, settings.maxDaysTillRandomEvent);

            viewRectHeight = viewRectHeight == -1f ? float.MaxValue : viewRectHeight;
            Rect viewRect = new Rect(inRect.x, inRect.y, inRect.width - 17f, viewRectHeight);

            Widgets.BeginScrollView(inRect, ref scrollVector, viewRect);
            Listing_Standard ls = new Listing_Standard();
            ls.Begin(viewRect);
            ls.Label("FCSettingSilverPerResource".Translate());
            ls.IntEntry(ref settings.silverPerResource, ref silverPerResource);
            ls.Label("FCSettingDaysBetweenTax".Translate());
            ls.IntEntry(ref daysBetweenTaxes, ref timeBetweenTaxes);
            settings.timeBetweenTaxes = Math.Max(1, daysBetweenTaxes) * 60000;
            ls.Label("FCSettingProductionTitheMod".Translate());
            ls.IntEntry(ref settings.productionTitheMod, ref productionTitheMod);
            ls.Label("FCSettingWorkerCost".Translate());
            ls.IntEntry(ref settings.workerCost, ref workerCost);
            ls.Label("FCSettingMaxSettlementLevel".Translate());
            ls.IntEntry(ref settings.settlementMaxLevel, ref settlementMaxLevel);
            ls.CheckboxLabeled("MedievalTechOnly".Translate(), ref settings.medievalTechOnly);
            ls.CheckboxLabeled("FCSettingDisableHostileMilActions".Translate(), ref settings.disableHostileMilitaryActions);
            ls.CheckboxLabeled("FCSettingDisableRandomEvents".Translate(), ref settings.disableRandomEvents);
            ls.CheckboxLabeled("FCSettingDeadPawnsIncreaseMilCooldown".Translate(), ref settings.deadPawnsIncreaseMilitaryCooldown);
            ls.CheckboxLabeled("FCSettingForcedPausing".Translate(), ref settings.disableForcedPausingDuringEvents);
            ls.CheckboxLabeled("FCSettingAutoResolveBattles".Translate(), ref settings.settlementsAutoBattle);
            if (ls.ButtonText("selectTaxDeliveryModeButton".Translate() + settings.forcedTaxDeliveryMode)) Find.WindowStack.Add(new FloatMenu(ForcedTaxDeliveryOptions));

            ls.Label("FCSettingMinMaxMilitaryAction".Translate());
            ls.IntRange(ref minMaxDaysTillMilitaryAction, 1, 20);
            settings.minDaysTillMilitaryAction = minMaxDaysTillMilitaryAction.min;
            settings.maxDaysTillMilitaryAction = Math.Max(1, minMaxDaysTillMilitaryAction.max);

            ls.Label("FCSettingMinMaxRandomEvent".Translate());
            ls.IntRange(ref minMaxDaysTillRandomEvent, 0, 20);
            settings.minDaysTillRandomEvent = minMaxDaysTillRandomEvent.min;
            settings.maxDaysTillRandomEvent = Math.Max(1, minMaxDaysTillRandomEvent.max);

            if (ls.ButtonText("FCOpenPatchNotes".Translate())) DebugActionsMisc.PatchNotesDisplayWindow();

            if (ls.ButtonText("FCSettingResetButton".Translate()))
            {
                FactionColonies blank = new FactionColonies();
                settings.silverPerResource = blank.silverPerResource;
                settings.timeBetweenTaxes = blank.timeBetweenTaxes;
                settings.productionTitheMod = blank.productionTitheMod;
                settings.workerCost = blank.workerCost;
                settings.medievalTechOnly = blank.medievalTechOnly;
                settings.settlementMaxLevel = blank.settlementMaxLevel;
                settings.minDaysTillMilitaryAction = blank.minDaysTillMilitaryAction;
                settings.maxDaysTillMilitaryAction = blank.maxDaysTillMilitaryAction;
                settings.minDaysTillRandomEvent = blank.minDaysTillRandomEvent;
                settings.maxDaysTillRandomEvent = blank.maxDaysTillRandomEvent;
                settings.disableRandomEvents = blank.disableRandomEvents;
                settings.deadPawnsIncreaseMilitaryCooldown = blank.deadPawnsIncreaseMilitaryCooldown;
                settings.settlementsAutoBattle = blank.settlementsAutoBattle;
                settings.disableForcedPausingDuringEvents = blank.disableForcedPausingDuringEvents;
                settings.forcedTaxDeliveryMode = blank.forcedTaxDeliveryMode;
            }

            FixScrollingBug(ls);
            ls.End();

            Widgets.EndScrollView();
            base.DoSettingsWindowContents(inRect);
        }

        private void FixScrollingBug(Listing_Standard ls)
        {
            if (fixDone) return;

            if (!firstRun)
            {
                viewRectHeight = ls.CurHeight + 5f;
                fixDone = true;
            }
            else
            {
                viewRectHeight = float.MaxValue;
                firstRun = false;
            }
        }

        public override string SettingsCategory()
        {
            return "Empire";
        }

        public override void WriteSettings()
        {
            LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().timeBetweenTaxes = daysBetweenTaxes * 60000;
            base.WriteSettings();
        }
    }
}
