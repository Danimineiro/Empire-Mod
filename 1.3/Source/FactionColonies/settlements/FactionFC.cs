using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FactionColonies.util;
using HarmonyLib;
using RimWorld;
using RimWorld.BaseGen;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace FactionColonies
{
    public class FactionFC : WorldComponent
    {
        public int eventTimeDue;
        public int taxTimeDue = Find.TickManager.TicksGame;
        public int timeStart = Find.TickManager.TicksGame;
        public int uiTimeUpdate;
        public int dailyTimer = Find.TickManager.TicksGame;
        public int militaryTimeDue;
        public int mercenaryTick;
        public bool factionCreated;

        private int nextUnitId;
        private int nextSquadId;

        public int NextUnitID => ++nextUnitId;
        public int NextSquadID => ++nextSquadId;

        public List<SettlementFC> settlements = new List<SettlementFC>();
        public string name = "PlayerFaction".Translate();
        public string title = "Bastion".Translate();
        public double averageHappiness = 100;
        public double averageLoyalty = 100;
        public double averageUnrest;
        public double averageProsperity = 100;
        public double income;
        public double upkeep;
        public double profit;
        public int capitalLocation = -1;
        public string capitalPlanet;
        public Map taxMap;
        public TechLevel techLevel = TechLevel.Undefined;
        private bool firstTick = true;
        public Texture2D factionIcon = TexLoad.factionIcons.ElementAt(0);
        public string factionIconPath = TexLoad.factionIcons.ElementAt(0).name;


        //New Types of Productions
        public float researchPointPool;
        public float powerPool;
        public ThingWithComps powerOutput;

        public List<FCEvent> events = new List<FCEvent>();
        public List<String> settlementCaravansList = new List<string>(); //list of locations caravans already sent to

        public List<BillFC> OldBills = new List<BillFC>();
        public List<BillFC> Bills = new List<BillFC>();
        public bool autoResolveBills;
        public bool autoResolveBillsChanged = false;

        public List<FCPolicy> policies = new List<FCPolicy>();
        public List<FCTraitEffectDef> traits = new List<FCTraitEffectDef>();
        public List<int> militaryTargets = new List<int>();
        public RaceThingFilter raceFilter;


        //Faction resources
        public ResourceFC food = new ResourceFC(1, ResourceType.Food);
        public ResourceFC weapons = new ResourceFC(1, ResourceType.Weapons);
        public ResourceFC apparel = new ResourceFC(1, ResourceType.Apparel);
        public ResourceFC animals = new ResourceFC(1, ResourceType.Animals);
        public ResourceFC logging = new ResourceFC(1, ResourceType.Logging);

        public ResourceFC mining = new ResourceFC(1, ResourceType.Mining);

        //public ResourceFC research = new ResourceFC("researching", "Researching", 1, ResourceType.Research);
        public ResourceFC power = new ResourceFC(1, ResourceType.Power);
        public ResourceFC medicine = new ResourceFC(1, ResourceType.Medicine);
        public ResourceFC research = new ResourceFC(1, ResourceType.Research);

        //Faction Def
        public FactionFCDef factionDef = new FactionFCDef();

        //Update
        public double updateVersion;
        public int nextSettlementFCID = 1;
        public int nextMercenarySquadID = 1;
        public int nextMercenaryID = 1;
        public int nextTaxID = 1;
        public int nextBillID = 1;
        public int nextEventID = 1;
        public int nextPrisonerID = 1;

        //Military 
        public int nextMilitaryFireSupportID = 1;

        //Military Customization
        public MilitaryCustomizationUtil militaryCustomizationUtil = new MilitaryCustomizationUtil();

        //Sos2 Compatibility
        public Faction factionBackup;
        public int travelTime = 0;
        public string planetName;
        public bool boolChangedPlanet;
        public bool factionUpdated;
        public bool SoSMoving = false;
        public bool SoSShipTaxMap;
        public bool SoSShipCapital;
        public bool SoSShipCapitalMoving = false;
        public List<SettlementSoS2Info> createSettlementQueue = new List<SettlementSoS2Info>();
        public List<SettlementSoS2Info> deleteSettlementQueue = new List<SettlementSoS2Info>();

        //Road builder
        public FCRoadBuilder roadBuilder = new FCRoadBuilder();

        public int traitMilitaristicTickLastUsedExtraSquad = -1;

        //Traits
        public int traitPacifistTickLastUsedDiplomat = -1;
        public int traitExpansionistTickLastUsedSettlementFeeReduction = -1;
        public bool traitExpansionistBoolCanUseSettlementFeeReduction = true;
        public int traitFeudalTickLastUsedMercenary = -1;
        public bool traitFeudalBoolCanUseMercenary = true;
        public int traitMercantileTradeCaravanTickDue = -1;

        //Settlement Leveling
        public int factionLevel = 1;
        public float factionXPCurrent;
        public float factionXPGoal = 100;

        //Random Event
        public float randomEventLastAdded = 0f;

        public List<FCPolicy> factionTraits = new List<FCPolicy>
        {
            new FCPolicy(FCPolicyDefOf.empty), new FCPolicy(FCPolicyDefOf.empty), new FCPolicy(FCPolicyDefOf.empty),
            new FCPolicy(FCPolicyDefOf.empty), new FCPolicy(FCPolicyDefOf.empty)
        };

        public Map TaxMap
        {
            get
            {
                Map map;
                if (taxMap == null)
                {
                    if (Find.WorldObjects
                            .SettlementAt(Find.World.GetComponent<FactionFC>().capitalLocation)?.Map ==
                        null)
                    {
                        //if no tax map or no capital map is valid
                        map = Find.CurrentMap.IsPlayerHome ? Find.CurrentMap : Find.AnyPlayerHomeMap;

                        Log.Message(
                            "Unable to find a player-set tax map or a valid location for the capital. Please open the faction main menu tab and set the capital and tax map. Taxes were sent to the following random PlayerHomeMap " +
                            map.Parent.LabelCap);
                    }
                    else
                    {
                        //if no tax map or no capital map is valid
                        map = Find.CurrentMap.IsPlayerHome ? Find.CurrentMap : Find.AnyPlayerHomeMap;

                        Log.Message(
                            "Unable to find a player-set tax map or a valid location for the capital. Please open the faction main menu tab and set the capital and tax map. Taxes were sent to the following random PlayerHomeMap " +
                            map.Parent.LabelCap);
                    }
                }
                else
                {
                    map = taxMap;
                }

                return map;
            }
        }

        //Research Trading
        public float tradedAmount;

        //Call for aid
        // [HarmonyPatch(typeof(FactionDialogMaker), "CallForAid")]
        // class WorldObjectGizmos
        //{
        //    static void Prefix(Map map, Faction faction)
        //    {

        //    }
        // }


        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref title, "title");
            Scribe_Values.Look(ref capitalLocation, "capitalLocation");
            Scribe_Values.Look(ref capitalPlanet, "capitalPlanet");
            Scribe_References.Look(ref taxMap, "taxMap");
            Scribe_Values.Look(ref factionCreated, "factionCreated");

            Scribe_Values.Look(ref averageHappiness, "averageHappiness");
            Scribe_Values.Look(ref averageLoyalty, "averageLoyalty");
            Scribe_Values.Look(ref averageUnrest, "averageUnrest");
            Scribe_Values.Look(ref averageProsperity, "averageProsperity");

            Scribe_Values.Look(ref income, "income");
            Scribe_Values.Look(ref upkeep, "upkeep");
            Scribe_Values.Look(ref profit, "profit");

            Scribe_Values.Look(ref eventTimeDue, "eventTimeDue");
            Scribe_Values.Look(ref taxTimeDue, "taxTimeDue");
            Scribe_Values.Look(ref timeStart, "timeStart", -1);
            Scribe_Values.Look(ref uiTimeUpdate, "uiTimeUpdate");
            Scribe_Values.Look(ref militaryTimeDue, "militaryTimeDue", -1);
            Scribe_Values.Look(ref dailyTimer, "dailyTimer");
            Scribe_Values.Look(ref techLevel, "techLevel");
            Scribe_Values.Look(ref factionIconPath, "factionIconPath", "Base");

            Scribe_Collections.Look(ref settlements, "settlements", LookMode.Deep);
            Scribe_Collections.Look(ref policies, "factionPolicies", LookMode.Deep);
            Scribe_Collections.Look(ref events, "events", LookMode.Deep);
            Scribe_Collections.Look(ref settlementCaravansList, "settlementCaravansList", LookMode.Value);
            Scribe_Collections.Look(ref traits, "traits", LookMode.Def);
            Scribe_Collections.Look(ref militaryTargets, "militaryTargets", LookMode.Value);

            //New Producitons types
            Scribe_Values.Look(ref researchPointPool, "researchPointPool");
            Scribe_Values.Look(ref powerPool, "powerPool");
            Scribe_References.Look(ref powerOutput, "powerOutput");


            //save resources
            Scribe_Deep.Look(ref food, "food");
            Scribe_Deep.Look(ref weapons, "weapons");
            Scribe_Deep.Look(ref apparel, "apparel");
            Scribe_Deep.Look(ref animals, "animals");
            Scribe_Deep.Look(ref logging, "logging");
            Scribe_Deep.Look(ref mining, "mining");
            Scribe_Deep.Look(ref research, "research");
            Scribe_Deep.Look(ref power, "power");
            Scribe_Deep.Look(ref medicine, "medicine");

            //Faction Def
            Scribe_Deep.Look(ref factionDef, "factionDef");
            Scribe_Deep.Look(ref raceFilter, "raceFilter");

            //Update
            Scribe_Values.Look(ref updateVersion, "updateVersion");
            Scribe_Values.Look(ref nextSettlementFCID, "nextSettlementFCID");

            //Military Customization Util
            Scribe_Deep.Look(ref militaryCustomizationUtil, "militaryCustomizationUtil");
            Scribe_Values.Look(ref nextMilitaryFireSupportID, "nextMilitaryFireSupportID", 1);
            Scribe_Values.Look(ref nextUnitId, "nextUnitID", 1);
            Scribe_Values.Look(ref nextSquadId, "nextSquadID", 1);
            Scribe_Values.Look(ref nextMercenaryID, "nextMercenaryID", 1);
            Scribe_Values.Look(ref nextMercenarySquadID, "nextMercenarySquadID", 1);
            Scribe_Values.Look(ref mercenaryTick, "mercenaryTick", -1);
            Scribe_Values.Look(ref nextPrisonerID, "nextPrisonerID", 1);

            //New Tax Stuff
            Scribe_Values.Look(ref nextTaxID, "nextTaxID", 1);
            Scribe_Values.Look(ref nextBillID, "nextBillID", 1);
            Scribe_Values.Look(ref nextEventID, "nextEventID", 1);


            Scribe_Collections.Look(ref Bills, "Bills", LookMode.Deep);
            Scribe_Collections.Look(ref OldBills, "OldBills", LookMode.Deep);
            Scribe_Values.Look(ref autoResolveBills, "autoResolveBills");

            //Sos2 compatibility
            //Scribe_Deep.Look<Faction>(ref factionBackup, "factionBackup");
            Scribe_Values.Look(ref SoSShipCapital, "SoSShipCapital");
            Scribe_Values.Look(ref SoSShipTaxMap, "SoSShipTaxMap");
            Scribe_Values.Look(ref planetName, "planetName");
            Scribe_Collections.Look(ref createSettlementQueue, "createSettlementQueue", LookMode.Deep);
            Scribe_Collections.Look(ref deleteSettlementQueue, "deleteSettlementQueue", LookMode.Deep);

            //Road builder
            Scribe_Deep.Look(ref roadBuilder, "roadBuilder");

            //Traits
            Scribe_Values.Look(ref traitMilitaristicTickLastUsedExtraSquad, "traitMilitaristicTickLastUsedExtraSquad");
            Scribe_Values.Look(ref traitPacifistTickLastUsedDiplomat, "traitPacifistTickLastUsedDiplomat");
            Scribe_Values.Look(ref traitExpansionistTickLastUsedSettlementFeeReduction,
                "traitExpansionistTickLastUsedSettlementFeeReduction");
            Scribe_Values.Look(ref traitExpansionistBoolCanUseSettlementFeeReduction,
                "traitExpansionistBoolCanUseSettlementReduction");
            Scribe_Values.Look(ref traitFeudalTickLastUsedMercenary, "traitFeudalTickLastUsedMercenary");
            Scribe_Values.Look(ref traitFeudalBoolCanUseMercenary, "traitFeudalBoolCanUseMercenary");
            Scribe_Values.Look(ref traitMercantileTradeCaravanTickDue, "traitMercantileTradeCaravanTickDue");

            //Settlement Leveling
            Scribe_Values.Look(ref factionLevel, "factionLevel");
            Scribe_Values.Look(ref factionXPCurrent, "factionXPCurrent");
            Scribe_Values.Look(ref factionXPGoal, "factionXPGoal");
            Scribe_Collections.Look(ref factionTraits, "factionTraits", LookMode.Deep);

            //Research Trading
            Scribe_Values.Look(ref tradedAmount, "tradedAmount");

            //Random Event
            Scribe_Values.Look(ref randomEventLastAdded, "randomEventLastAddedTick");
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            //Just in case null is saved somehow
            if (raceFilter == null)
            {
                Log.Message("Null raceFilter detected - Recreating");
                raceFilter = new RaceThingFilter(this);
            }
            raceFilter.FinalizeInit(this);
        }

        [HarmonyPatch(typeof(FactionDialogMaker), "RequestMilitaryAidOption")]
        class disableMilitaryAid
        {
            static void Postfix(Map map, Faction faction, Pawn negotiator, ref DiaOption __result)
            {
                if (faction.def.defName != "PColony") return;
                __result = new DiaOption("RequestMilitaryAid".Translate(25));
                __result.Disable("Disabled. Use the settlements military tab.");
            }
        }


        [HarmonyPatch(typeof(WorldPawns), "PassToWorld")]
        class MercenaryPassToWorld
        {
            static bool Prefix(Pawn pawn, PawnDiscardDecideMode discardMode = PawnDiscardDecideMode.Decide)
            {
                FactionFC faction = Find.World.GetComponent<FactionFC>();
                return faction?.militaryCustomizationUtil?.AllMercenaryPawns == null || 
                       !faction.militaryCustomizationUtil.AllMercenaryPawns.Contains(pawn);
            }
        }

        [DebugAction("Empire", "Send Pawn To Settlement", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void sendPawnToSettlement()
        {
            List<Pawn> selected = Find.Selector.SelectedPawns;
            if (!selected.Any())
            {
                Messages.Message("No prisoner selected!", MessageTypeDefOf.RejectInput);
                return;
            }
            List<FloatMenuOption> settlementList = Find.World.GetComponent<FactionFC>()
                .settlements.Select(settlement => new FloatMenuOption(settlement.name + " - Settlement Level : " + 
                                                                      settlement.settlementLevel + " - Prisoners: " + 
                                                                      settlement.prisonerList.Count(), delegate
                {
                    foreach (Pawn pawn in selected)
                    {
                        //disappear colonist
                        FactionColonies.sendPrisoner(pawn, settlement);

                        foreach (var bed in Find.Maps.Where(map => map.IsPlayerHome).SelectMany(map => 
                            map.listerBuildings.allBuildingsColonist).OfType<Building_Bed>())
                        {
                            if (!Enumerable.Any(bed.OwnersForReading, found => found == pawn)) continue;
                            bed.ForPrisoners = false;
                            bed.ForPrisoners = true;
                        }
                    }
                }))
                .ToList();

            FloatMenu floatMenu2 = new FloatMenu(settlementList);
            Find.WindowStack.Add(floatMenu2);
        }

        [HarmonyPatch(typeof(Pawn), "GetGizmos")]
        class PawnGizmos
        {
            static void Postfix(ref Pawn __instance, ref IEnumerable<Gizmo> __result)
            {
                Pawn pawn = __instance;
                if (__instance.guest == null || !__instance.guest.IsPrisoner || !__instance.guest.PrisonerIsSecure ||
                    !Find.World.GetComponent<FactionFC>().settlements.Any()) return;

                __result = __result.Append(new Command_Action
                {
                    defaultLabel = "SendToSettlement".Translate(),
                    defaultDesc = "",
                    icon = TexLoad.iconMilitary,
                    action = delegate
                    {
                        if (pawn.Map.dangerWatcher.DangerRating != StoryDanger.None)
                        {
                            Messages.Message("cantSendWithDangerLevel".Translate(pawn.Map.dangerWatcher.DangerRating.ToString()), MessageTypeDefOf.RejectInput);
                            return;
                        }

                        List<FloatMenuOption> settlementList = Find.World.GetComponent<FactionFC>().settlements.Select(settlement => new FloatMenuOption(settlement.name + " - Settlement Level : " + settlement.settlementLevel + " - Prisoners: " + settlement.prisonerList.Count(), delegate
                        {
                            //disappear colonist
                            FactionColonies.sendPrisoner(pawn, settlement);

                            foreach (var bed in Find.Maps.Where(map => map.IsPlayerHome).SelectMany(map => map.listerBuildings.allBuildingsColonist).OfType<Building_Bed>().Where(bed => bed.OwnersForReading.Any(bedPawn => bedPawn == pawn)))
                            {
                                bed.ForPrisoners = false;
                                bed.ForPrisoners = true;
                            }
                        })).ToList();

                        FloatMenu floatMenu2 = new FloatMenu(settlementList)
                        {
                            vanishIfMouseDistant = true
                        };
                        Find.WindowStack.Add(floatMenu2);
                    }
                });
            }
        }

        //stops friendly faction from being a group source
        [HarmonyPatch(typeof(IncidentWorker_RaidFriendly), "TryResolveRaidFaction")]
        class RaidFriendlyStopSettlementFaction
        {
            static void Postfix(ref IncidentWorker_RaidFriendly __instance, ref bool __result, IncidentParms parms)
            {
                if (parms.faction == FactionColonies.getPlayerColonyFaction())
                {
                    parms.faction = null;
                    __result = false;
                }
            }
        }


        //Faction worldmap gizmos
        //Goodwill by distance to settlement
        [HarmonyPatch(typeof(WorldObject), "GetGizmos")]
        class WorldObjectGizmos
        {
            static void Postfix(ref WorldObject __instance, ref IEnumerable<Gizmo> __result)
            {
                FactionFC worldcomp = Find.World.GetComponent<FactionFC>();
                Faction fact = FactionColonies.getPlayerColonyFaction();
                if (__instance.def.defName != "Settlement") return;
                int tile = __instance.Tile;
                if (__instance.Faction != fact && __instance.Faction != Find.FactionManager.OfPlayer)
                {
                    //if a valid faction to target
                    Faction faction = __instance.Faction;

                    Command_Action actionHostile = new Command_Action
                    {
                        defaultLabel = "AttackSettlement".Translate(),
                        defaultDesc = "",
                        icon = TexLoad.iconMilitary,
                        action = delegate
                        {
                            List<FloatMenuOption> list = new List<FloatMenuOption>();

                            if (!worldcomp.hasPolicy(FCPolicyDefOf.isolationist))
                                list.Add(new FloatMenuOption("CaptureSettlement".Translate(), delegate
                                {
                                    List<FloatMenuOption> settlementList = new List<FloatMenuOption>();

                                    foreach (SettlementFC settlement in worldcomp.settlements)
                                    {
                                        if (settlement.isMilitaryValid())
                                        {
                                            //if military is valid to use.

                                            settlementList.Add(new FloatMenuOption(
                                                settlement.name + " " + "ShortMilitary".Translate() + " " +
                                                settlement.settlementMilitaryLevel + " - " +
                                                "FCAvailable".Translate() + ": " +
                                                (!settlement.isMilitaryBusySilent()).ToString(), delegate
                                                {
                                                    if (settlement.isMilitaryBusy())
                                                    {
                                                    }
                                                    else
                                                    {
                                                        RelationsUtilFC.attackFaction(faction);

                                                        settlement.sendMilitary(tile, Find.World.info.name,
                                                            MilitaryJob.CaptureEnemySettlement, 60000, faction);


                                                        //simulateBattleFC.FightBattle(militaryForce.createMilitaryForceFromSettlement(settlement), militaryForce.createMilitaryForceFromEnemySettlement(faction));
                                                    }
                                                }
                                            ));
                                        }
                                    }

                                    if (settlementList.Count == 0)
                                    {
                                        settlementList.Add(new FloatMenuOption("NoValidMilitaries".Translate(),
                                            null));
                                    }

                                    FloatMenu floatMenu2 = new FloatMenu(settlementList);
                                    floatMenu2.vanishIfMouseDistant = true;
                                    Find.WindowStack.Add(floatMenu2);
                                }));


                            list.Add(new FloatMenuOption("RaidSettlement".Translate(), delegate
                            {
                                List<FloatMenuOption> settlementList = new List<FloatMenuOption>();

                                foreach (SettlementFC settlement in worldcomp.settlements)
                                {
                                    if (settlement.isMilitaryValid())
                                    {
                                        //if military is valid to use.

                                        settlementList.Add(new FloatMenuOption(
                                            settlement.name + " " + "ShortMilitary".Translate() + " " +
                                            settlement.settlementMilitaryLevel + " - " + "FCAvailable".Translate() +
                                            ": " + (!settlement.isMilitaryBusySilent()).ToString(), delegate
                                            {
                                                if (settlement.isMilitaryBusy())
                                                {
                                                    //military is busy
                                                }
                                                else
                                                {
                                                    RelationsUtilFC.attackFaction(faction);

                                                    settlement.sendMilitary(tile, Find.World.info.name,
                                                        MilitaryJob.RaidEnemySettlement, 60000, faction);


                                                    //simulateBattleFC.FightBattle(militaryForce.createMilitaryForceFromSettlement(settlement), militaryForce.createMilitaryForceFromEnemySettlement(faction));
                                                }
                                            }
                                        ));
                                    }
                                }

                                if (settlementList.Count == 0)
                                {
                                    settlementList.Add(new FloatMenuOption("NoValidMilitaries".Translate(), null));
                                }

                                FloatMenu floatMenu2 = new FloatMenu(settlementList);
                                floatMenu2.vanishIfMouseDistant = true;
                                Find.WindowStack.Add(floatMenu2);


                                //set to raid settlement here
                            }));

                            if (worldcomp.hasPolicy(FCPolicyDefOf.authoritarian) &&
                                faction.def.defName != "VFEI_Insect")
                            {
                                list.Add(new FloatMenuOption("EnslavePopulation".Translate(), delegate
                                {
                                    List<FloatMenuOption> settlementList = new List<FloatMenuOption>();

                                    foreach (SettlementFC settlement in worldcomp.settlements)
                                    {
                                        if (settlement.isMilitaryValid())
                                        {
                                            //if military is valid to use.

                                            settlementList.Add(new FloatMenuOption(
                                                settlement.name + " " + "ShortMilitary".Translate() + " " +
                                                settlement.settlementMilitaryLevel + " - " +
                                                "FCAvailable".Translate() + ": " +
                                                (!settlement.isMilitaryBusySilent()).ToString(), delegate
                                                {
                                                    if (settlement.isMilitaryBusy())
                                                    {
                                                        //military is busy
                                                    }
                                                    else
                                                    {
                                                        RelationsUtilFC.attackFaction(faction);

                                                        settlement.sendMilitary(tile, Find.World.info.name,
                                                            MilitaryJob.EnslaveEnemySettlement, 60000, faction);


                                                        //simulateBattleFC.FightBattle(militaryForce.createMilitaryForceFromSettlement(settlement), militaryForce.createMilitaryForceFromEnemySettlement(faction));
                                                    }
                                                }
                                            ));
                                        }
                                    }

                                    if (settlementList.Count == 0)
                                    {
                                        settlementList.Add(new FloatMenuOption("NoValidMilitaries".Translate(),
                                            null));
                                    }

                                    FloatMenu floatMenu2 = new FloatMenu(settlementList);
                                    floatMenu2.vanishIfMouseDistant = true;
                                    Find.WindowStack.Add(floatMenu2);


                                    //set to raid settlement here
                                }));
                            }

                            FloatMenu floatMenu = new FloatMenu(list);
                            floatMenu.vanishIfMouseDistant = true;
                            Find.WindowStack.Add(floatMenu);
                        }
                    };

                    Command_Action actionPeaceful = new Command_Action
                    {
                        defaultLabel = "FCIncreaseRelations".Translate(),
                        defaultDesc = "",
                        icon = TexLoad.iconProsperity,
                        action = delegate { worldcomp.sendDiplomaticEnvoy(faction); }
                    };

                    if (worldcomp.hasPolicy(FCPolicyDefOf.pacifist))
                    {
                        __result = __result.Concat(new[] {actionPeaceful});
                    }
                    else
                    {
                        __result = __result.Concat(new[] {actionHostile});
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Pawn), "Kill")]
        class MercenaryDied
        {
            static bool Prefix(Pawn __instance)
            {
                if (__instance.IsMercenary())
                {
                    if (__instance.Faction != FactionColonies.getPlayerColonyFaction()) __instance.SetFaction(FactionColonies.getPlayerColonyFaction());
                    MercenarySquadFC squad = Find.World.GetComponent<FactionFC>().militaryCustomizationUtil.returnSquadFromUnit(__instance);
                    if (squad != null)
                    {
                        Mercenary merc = Find.World.GetComponent<FactionFC>().militaryCustomizationUtil.returnMercenaryFromUnit(__instance, squad);
                        if (merc != null)
                        {
                            if (squad.settlement != null)
                            {
                                if (FactionColonies.Settings().deadPawnsIncreaseMilitaryCooldown)
                                {
                                    squad.dead += 1;
                                }

                                squad.settlement.happiness -= 1;
                            }

                            squad.PassPawnToDeadMercenaries(merc);
                        }

                        squad.removeDroppedEquipment();
                    }
                    else
                    {
                        Log.Message("Mercenary Errored out. Did not find squad.");
                    }

                    __instance.equipment?.DestroyAllEquipment();
                    __instance.apparel?.DestroyAll();
                    //__instance.Destroy();
                    return true;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(DeathActionWorker_Simple), "PawnDied")]
        class MercenaryAnimalDied
        {
            static bool Prefix(Corpse corpse)
            {
                if (Find.World.GetComponent<FactionFC>().militaryCustomizationUtil.AllMercenaryPawns
                    .Contains(corpse.InnerPawn))
                {
                    //corpse.InnerPawn.SetFaction(FactionColonies.getPlayerColonyFaction());
                    corpse.Destroy();
                    return false;
                }

                return true;
            }
        }

        // [HarmonyPatch(typeof(JobGiver_AnimalFlee), "TryGiveJob")]
        class TryGiveJobFleeAnimal
        {
            static bool Prefix(Pawn pawn)
            {
                if (Find.World.GetComponent<FactionFC>().militaryCustomizationUtil.AllMercenaryPawns.Contains(pawn))
                {
                    return false;
                }

                return true;
            }
        }

        //Goodwill by distance to settlement
        [HarmonyPatch(typeof(SettlementProximityGoodwillUtility), "AppendProximityGoodwillOffsets")]
        class GoodwillPatch
        {
            static void Postfix(int tile, List<Pair<Settlement, int>> outOffsets, bool ignoreIfAlreadyMinGoodwill,
                bool ignorePermanentlyHostile)
            {
                Pair:
                foreach (Pair<Settlement, int> pair in outOffsets)
                {
                    if (pair.First.Faction.def.defName == "PColony")
                    {
                        outOffsets.Remove(pair);
                        goto Pair;
                    }
                }
            }
        }

        //CheckReachNaturalGoodwill()
        [HarmonyPatch(typeof(Faction), "CheckReachNaturalGoodwill")]
        class GoodwillPatchFunctionsGoodwillTendency
        {
            static bool Prefix(ref Faction __instance)
            {
                if (__instance.def.defName == "PColony")
                {
                    return false;
                }

                return true;
            }
        }

        //tryAffectGoodwillWith
        [HarmonyPatch(typeof(Faction), "TryAffectGoodwillWith")]
        class GoodwillPatchFunctionsGoodwillAffect
        {
            static bool Prefix(ref Faction __instance, Faction other, int goodwillChange, bool canSendMessage = true,
                bool canSendHostilityLetter = true, string reason = null, GlobalTargetInfo? lookTarget = null)
            {
                if (__instance.def.defName == "PColony" && other == Find.FactionManager.OfPlayer)
                {
                    if (reason == "GoodwillChangedReason_RequestedTrader".Translate())
                    {
                        return false;
                    }

                    if (reason == "GoodwillChangedReason_ReceivedGift".Translate())
                    {
                        return false;
                    }

                    return true;
                }

                return true;
            }
        }


        //Notify_MemberDied(Pawn member, DamageInfo? dinfo, bool wasWorldPawn, Map map)
        [HarmonyPatch(typeof(Faction), "Notify_MemberDied")]
        class GoodwillPatchFunctionsMemberDied
        {
            static bool Prefix(ref Faction __instance, Pawn member, DamageInfo? dinfo, bool wasWorldPawn, Map map)
            {
                if (member.Faction.def.defName == "PColony" && !wasWorldPawn &&
                    !PawnGenerator.IsBeingGenerated(member) && map != null && map.IsPlayerHome &&
                    !__instance.HostileTo(Faction.OfPlayer))
                {
                    FactionFC faction = Find.World.GetComponent<FactionFC>();
                    if (!faction.hasPolicy(FCPolicyDefOf.pacifist) && dinfo != null)
                    {

                        if (dinfo.Value.Category == DamageInfo.SourceCategory.Collapse)
                        {
                            faction.GainUnrestForReason(new Message("DeathOfFactionPawn".Translate(), MessageTypeDefOf.PawnDeath), 5d);
                            faction.GainHappiness(-5d);
                        }
                        else if (dinfo.Value.Instigator?.Faction != null)
                        {
                            if (dinfo.Value.Instigator is Pawn pawn && !pawn.RaceProps.Animal &&
                                pawn.mindState.mentalStateHandler.CurStateDef != MentalStateDefOf.ManhunterPermanent)
                            {
                                faction.GainUnrestForReason(new Message("DeathOfFactionPawn".Translate(), MessageTypeDefOf.PawnDeath), 5d);
                                faction.GainHappiness(-5d);
                            }
                        }
                        else if (dinfo.Value.Instigator?.Faction == Find.FactionManager.OfPlayer)
                        {
                            faction.GainUnrestForReason(new Message("DeathOfFactionPawn".Translate(), MessageTypeDefOf.PawnDeath), 5d);
                            faction.GainHappiness(-5d);
                        }
                    }

                    //return false to stop from continuing method
                    return false;
                }

                return true;
            }
        }

        private void GainHappiness(double amount)
        {
            foreach (SettlementFC settlement in settlements)
            {
                settlement.happiness += amount *
                                    TraitUtilsFC.cycleTraits(new double(),
                                        "happinessLostMultiplier", settlement.traits,
                                        Operation.Multiplikation) * TraitUtilsFC.cycleTraits(new double(),
                                        "happinessLostMultiplier", traits, Operation.Multiplikation);
            }
        }

        private void GainUnrestForReason(Message msg, double amount)
        {
            Messages.Message(msg);
            foreach (SettlementFC settlement in settlements)
            {
                settlement.unrest += amount *
                                     TraitUtilsFC.cycleTraits(new double(), "unrestGainedMultiplier",
                                         settlement.traits, Operation.Multiplikation) *
                                     TraitUtilsFC.cycleTraits(new double(), "unrestGainedMultiplier",
                                         traits, Operation.Multiplikation);
            }
        }


        //member exit map
        [HarmonyPatch(typeof(Faction), "Notify_MemberExitedMap")]
        class GoodwillPatchFunctionsExitedMap
        {
            static bool Prefix(ref Faction __instance, Pawn member, bool free)
            {
                if (__instance.def.defName == "PColony")
                {
                    return false;
                }

                return true;
            }
        }


        //member took damage
        [HarmonyPatch(typeof(Faction), "Notify_MemberTookDamage")]
        class GoodwillPatchFunctionsTookDamage
        {
            static bool Prefix(ref Faction __instance, Pawn member, DamageInfo dinfo)
            {
                if (__instance.def.defName == "PColony")
                {
                    return false;
                }

                return true;
            }
        }

        //Player traded
        [HarmonyPatch(typeof(Faction), "Notify_PlayerTraded")]
        class GoodwillPatchFunctionsPlayerTraded
        {
            static bool Prefix(ref Faction __instance, float marketValueSentByPlayer, Pawn playerNegotiator)
            {
                if (__instance.def.defName == "PColony")
                {
                    return false;
                }

                return true;
            }
        }

        //Player traded
        [HarmonyPatch(typeof(Faction), "Notify_MemberCaptured")]
        class GoodwillPatchFunctionsCapturedPawn
        {
            static bool Prefix(ref Faction __instance, Pawn member, Faction violator)
            {
                if (__instance.def.defName == "PColony" && violator == Faction.OfPlayer)
                {
                    FactionFC faction = Find.World.GetComponent<FactionFC>();
                    faction.GainUnrestForReason(new Message("CaptureOfFactionPawn".Translate(), MessageTypeDefOf.NegativeEvent), 15d);
                    faction.GainHappiness(-10d);

                    return false;
                }

                return true;
            }
        }

        [HarmonyPatch(typeof(ResearchManager), "FinishProject")]
        class ResearchCompleted
        {
            static void Postfix(ResearchProjectDef proj, bool doCompletionDialog = false, Pawn researcher = null)
            {
                FactionFC fc = Find.World.GetComponent<FactionFC>();
                fc.roadBuilder.CheckForTechChanges();
            }
        }

        // Fix a crash related to a harmony bug on Linux
        // This gets all patches Empire makes, gets the ones that would crash on Linux, and fixes them
        static void FixLinuxHarmonyCrash(Harmony harmony)
        {
            bool WouldCrash(MethodInfo method)
            {
                if (method == null || !method.IsVirtual || method.IsAbstract || method.IsFinal)
                {
                    return false;
                }

                byte[] bytes = method.GetMethodBody()?.GetILAsByteArray();
                if (bytes == null || bytes.Length == 0 || (bytes.Length == 1 && bytes.First() == 0x2A))
                {
                    return true;
                }

                return false;
            }

            var methods = typeof(FactionFC).Assembly.GetTypes()
                    .Where(t => t.IsClass && !typeof(Delegate).IsAssignableFrom(t))
                    .Where(t => t.GetCustomAttributes(typeof(HarmonyPatch)).Any())
                    .SelectMany(t =>
                    {
                        HarmonyPatch patch = (HarmonyPatch) Attribute.GetCustomAttribute(t, typeof(HarmonyPatch));
                        MethodInfo[] m = patch.info.declaringType.GetMethods(BindingFlags.Public |
                                                                             BindingFlags.NonPublic |
                                                                             BindingFlags.Instance |
                                                                             BindingFlags.DeclaredOnly);
                        return m.Where(met => met.Name == patch.info.methodName);
                    })
                    .Where(WouldCrash)
                ;

            foreach (MethodInfo i in methods)
            {
                // Patching methods without any Prefixes/Postfixes before actually patching them fixes it. Idk why
                harmony.Patch(i);
            }
        }

        //CallForAid
        //Remove ability to attack colony.


        public FactionFC(World world) : base(world)
        {
            var harmony = new Harmony("com.Saakra.Empire");

            if (SystemInfo.operatingSystemFamily == OperatingSystemFamily.Linux)
            {
                FixLinuxHarmonyCrash(harmony);
            }

            harmony.PatchAll();

            if (FactionColonies.checkForMod("kentington.saveourship2"))
            {
                SoS2HarmonyPatches.Patch(harmony);
            }

            if (FactionColonies.checkForMod("Krkr.AndroidTiers") || FactionColonies.checkForMod("Atlas.AndroidTiers"))
            {
                //Android_Tiers_Patches.Patch(harmony);
            }


            power.isTithe = true;
            power.isTitheBool = true;
            research.isTithe = true;
            research.isTitheBool = true;
        }

        public List<SettlementFC> settlementsOnPlanet
        {
            get
            {
                List<SettlementFC> list = new List<SettlementFC>();
                foreach (SettlementFC settlement in settlements)
                {
                    if (settlement.planetName == Find.World.info.name)
                    {
                        list.Add(settlement);
                    }
                }

                return list;
            }
        }

        public override void WorldComponentTick()
        {
            base.WorldComponentTick();
            if (firstTick)
            {
                //Log.Message("First Tick");
                FactionColonies.updateChanges();
                if (planetName.NullOrEmpty())
                {
                    planetName = Find.World.info.name;
                }

                roadBuilder.FirstTick();

                Faction FCf = FactionColonies.getPlayerColonyFaction();
                if (FCf != null)
                {
                    FCf.def.techLevel = TechLevel.Undefined;
                    factionIcon = TexLoad.factionIcons.FirstOrFallback(obj => obj.name == factionIconPath,
                        TexLoad.factionIcons.First());
                    updateFactionIcon(ref FCf, "FactionIcons/" + factionIcon.name);
                    factionIconPath = factionIcon.name;
                }
                
                militaryCustomizationUtil.checkMilitaryUtilForErrors();

                factionBackup = FCf;
                firstTick = false;
            }

            FCEventMaker.ProcessEvents(in events);
            billUtility.processBills();

            TickMecernaries();


            //If Player Colony Faction does exists
            Faction faction = FactionColonies.getPlayerColonyFaction();
            if (faction != null)
            {
                roadBuilder.RoadTick();
                TaxTick();
                UITick();
                StatTick();
                MilitaryTick();
                TickActions();
            }
            else if (faction == null && settlements.Count() >= 0 && factionBackup != null)
            {
                //Log.Message("Moved to new planet - Adding faction copy");
                //FactionColonies.createPlayerColonyFaction();
                //FactionColonies.copyPlayerColonyFaction();
            }

            if (boolChangedPlanet)
            {
                // if (!factionUpdated)
                // {
                SoS2HarmonyPatches.updateFactionOnPlanet();
                factionUpdated = false;
                // }
                Reset:
                //Log.Message("New planet-" + Find.World.info.name);
                foreach (SettlementSoS2Info entry in createSettlementQueue)
                {
                    //Log.Message("key for create-" + entry.Key);
                    if (entry.planetName == Find.World.info.name)
                    {
                        //Log.Message("Match");


                        Settlement settlement =
                            (Settlement) WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
                        settlement.SetFaction(faction);
                        settlement.Tile = entry.location;
                        settlement.Name = returnSettlementByLocation(settlement.Tile, Find.World.info.name).name;
                        Find.WorldObjects.Add(settlement);

                        createSettlementQueue.Remove(entry);
                        goto Reset;
                    }
                }

                roadBuilder.CreateRoadQueue(Find.World.info.name);
                roadBuilder.FlagUpdateRoadQueues();
                Reset2:
                foreach (SettlementSoS2Info entry in deleteSettlementQueue)
                {
                    //Log.Message("key for destroy-" + entry.Key);
                    if (entry.planetName != Find.World.info.name) continue;
                    //Log.Message("Match");
                    Find.WorldObjects.Remove(Find.World.worldObjects.WorldObjectAt<WorldSettlementFC>(entry.location));
                    deleteSettlementQueue.Remove(entry);
                    goto Reset2;
                }

                boolChangedPlanet = false;
            }
        }

        public void TickActions()
        {
            int tick = Find.TickManager.TicksGame;
            foreach (SettlementFC settlement in settlements)
            {
                settlement.tickSpecialActions(tick);
            }

            //Feudal
            if (traitFeudalBoolCanUseMercenary == false &&
                (traitFeudalTickLastUsedMercenary + GenDate.TicksPerSeason) <= Find.TickManager.TicksGame)
            {
                traitFeudalBoolCanUseMercenary = true;
                Find.LetterStack.ReceiveLetter("FCActionAvailable".Translate(),
                    "FCActionMercenaryRefreshed".Translate(), LetterDefOf.PositiveEvent);
            }

            //Expansionist
            if (traitExpansionistBoolCanUseSettlementFeeReduction == false &&
                (traitExpansionistTickLastUsedSettlementFeeReduction + GenDate.TicksPerYear) <=
                Find.TickManager.TicksGame)
            {
                traitExpansionistBoolCanUseSettlementFeeReduction = true;
                Find.LetterStack.ReceiveLetter("FCActionAvailable".Translate(),
                    "FCActionSettlementFeeReduction".Translate(), LetterDefOf.PositiveEvent);
            }

            //Mercantile
            if (hasTrait(FCPolicyDefOf.mercantile) && traitMercantileTradeCaravanTickDue <= Find.TickManager.TicksGame)
            {
                IncidentWorker_TraderCaravanArrival worker = new IncidentWorker_TraderCaravanArrival();
                worker.def = IncidentDefOf.TraderCaravanArrival;
                IncidentParms parms =
                    StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.FactionArrival, returnCapitalMap());
                parms.faction = FactionColonies.getPlayerColonyFaction();
                RCellFinder.TryFindRandomPawnEntryCell(out parms.spawnCenter, (Map) parms.target,
                    CellFinder.EdgeRoadChance_Friendly);
                parms.spawnRotation = Rot4.FromAngleFlat((((Map) parms.target).Center - parms.spawnCenter).AngleFlat);
                if (parms.spawnCenter.IsValid)
                    worker.TryExecute(parms);
                else
                    Log.Message("Empire - Mercantile - Spawn Center not valid");


                resetTraitMercantileCaravanTime();
            }
        }

        public void TickMecernaries()
        {
            mercenaryTick++;
            if (mercenaryTick > 120)
            {
                for (int i = 0; i < militaryCustomizationUtil.mercenarySquads.Count(); i++)
                    //foreach (MercenarySquadFC squad in militaryCustomizationUtil.mercenarySquads)
                {
                    MercenarySquadFC squad = militaryCustomizationUtil.mercenarySquads[i];
                    if (squad.isDeployed)
                    {
                        if (squad.DeployedMercenaries.Count > 0 && squad.isDeployed && squad.hasLord == false)
                        {
                            Log.Message("Pawn deployed, creating lord");
                            Faction faction = FactionColonies.getPlayerColonyFaction();
                            List<Pawn> pawns = new List<Pawn>();
                            foreach (Mercenary pawn in squad.DeployedMercenaries)
                            {
                                pawn.pawn.mindState.canFleeIndividual = false;
                                pawns.Add(pawn.pawn);
                            }

                            foreach (Mercenary pawn in squad.DeployedMercenaryAnimals)
                            {
                                pawn.pawn.mindState.canFleeIndividual = false;
                                pawns.Add(pawn.pawn);
                            }


                            Lord lord = LordMaker.MakeNewLord(faction,
                                new LordJob_AssistColony(faction, squad.DeployedMercenaries[0].pawn.DutyLocation()),
                                squad.DeployedMercenaries[0].pawn.Map, pawns);
                            squad.map = squad.DeployedMercenaries[0].pawn.Map;
                            squad.lord = lord;


                            squad.hasLord = true;
                        }

                        if (Find.WindowStack.IsOpen(typeof(EmpireUIMercenaryCommandMenu)) == false)
                        {
                            //Log.Message("Opening Window");
                            // menu.focusWhenOpened = false;

                            Find.WindowStack.Add(new EmpireUIMercenaryCommandMenu());
                        }

                        MilitaryAI.SquadAI(ref squad);
                    }
                }

                mercenaryTick = 0;
            }

            if (militaryCustomizationUtil.fireSupport == null)
            {
                militaryCustomizationUtil.fireSupport = new List<MilitaryFireSupport>();
            }

            //Other functions
            ResetFireSupport:
            foreach (MilitaryFireSupport fireSupport in militaryCustomizationUtil.fireSupport)
            {
                if (fireSupport.ticksTillEnd <= Find.TickManager.TicksGame)
                {
                    militaryCustomizationUtil.fireSupport.Remove(fireSupport);
                    goto ResetFireSupport;
                }

                //process firesupport
                if (fireSupport.fireSupportType == "fireSupport")
                {
                    if ((fireSupport.timeRunning % 15) == 0 && fireSupport.timeRunning >= fireSupport.startupTime) //15
                    {
                        //Log.Message("Boom");
                        IntVec3 spawnCenter =
                            (from x in GenRadial.RadialCellsAround(fireSupport.location, fireSupport.accuracy, true)
                                where x.InBounds(fireSupport.map)
                                select x).RandomElementByWeight(x =>
                                new SimpleCurve {new CurvePoint(0f, 1f), new CurvePoint(fireSupport.accuracy, 0.1f)}
                                    .Evaluate(x.DistanceTo(fireSupport.location)));

                        Map map = fireSupport.map;
                        Thing launcher = null;
                        ProjectileHitFlags hitFlags = ProjectileHitFlags.All;
                        LocalTargetInfo info = new LocalTargetInfo(spawnCenter);
                        ThingDef def = new ThingDef();
                        ThingDef tempDef;
                        if (FactionColonies.checkForMod("CETeam.CombatExtended"))
                        {
                            //if CE is on
                            tempDef = fireSupport.expendProjectile();
                            Type typeDef = FactionColonies.returnUnknownTypeFromName("CombatExtended.AmmoDef");
                            var ammoSetDef = typeDef
                                .GetProperty("AmmoSetDefs", BindingFlags.Public | BindingFlags.Instance)
                                .GetValue(tempDef);
                            Type ammoLink = FactionColonies.returnUnknownTypeFromName("CombatExtended.AmmoLink");
                            var ammoLinkVar = ammoSetDef.GetType().GetProperty("Item")
                                .GetValue(ammoSetDef, new object[] {0});
                            //  Log.Message(ammoLinkVar.ToString());
                            var ammoTypes = ammoLinkVar.GetType()
                                .GetField("ammoTypes", BindingFlags.Public | BindingFlags.Instance)
                                .GetValue(ammoLinkVar);
                            //list of ammotypes
                            int count = (int) ammoTypes.GetType().GetProperty("Count")
                                .GetValue(ammoTypes, new object[] { });
                            for (int k = 0; k < count; k++)
                            {
                                var ammoDefAmmo = ammoTypes.GetType().GetProperty("Item")
                                    .GetValue(ammoTypes, new object[] {k});
                                if (ammoDefAmmo.GetType().GetField("ammo", BindingFlags.Public | BindingFlags.Instance)
                                    .GetValue(ammoDefAmmo).ToString() == tempDef.defName)
                                {
                                    def = (ThingDef) ammoDefAmmo.GetType()
                                        .GetField("projectile", BindingFlags.Public | BindingFlags.Instance)
                                        .GetValue(ammoDefAmmo);
                                    break;
                                }
                            }


                            Type type2 = FactionColonies.returnUnknownTypeFromName("CombatExtended.ProjectileCE");
                            MethodInfo launch = type2.GetMethod("Launch",
                                new[]
                                {
                                    typeof(Thing), typeof(Vector2), typeof(float), typeof(float), typeof(float),
                                    typeof(float), typeof(Thing)
                                });
                            MethodInfo getShotAngle = type2.GetMethod("GetShotAngle",
                                BindingFlags.Public | BindingFlags.Static);
                            Thing thing = GenSpawn.Spawn(def, fireSupport.sourceLocation, map);

                            PropertyInfo gravityProperty = type2.GetProperty("GravityFactor",
                                BindingFlags.NonPublic | BindingFlags.Instance);

                            Vector2 sourceVec = new Vector2(fireSupport.sourceLocation.x, fireSupport.sourceLocation.z);
                            Vector2 destVec = new Vector2(spawnCenter.x, spawnCenter.z);
                            Vector3 finalVector = (destVec - sourceVec);
                            float magnitude = finalVector.magnitude;

                            float gravity = (float) gravityProperty.GetValue(thing); //1.96f * 5;

                            float shotRotation =
                                (-90f + 57.29578f * Mathf.Atan2(finalVector.y, finalVector.x)) %
                                360; //Vector2Utility.AngleTo(sourceVec, destVec);
                            float shotHeight = 10f;
                            float shotSpeed = 100f;
                            float shotAngle = (float) getShotAngle.Invoke(null,
                                BindingFlags.Public | BindingFlags.Static, null,
                                new object[] {shotSpeed, magnitude, shotHeight, true, gravity}, null);


                            launch.Invoke(thing,
                                new object[]
                                {
                                    FactionColonies.getPlayerColonyFaction().leader, sourceVec, shotAngle, shotRotation,
                                    shotHeight, shotSpeed, null
                                });
                        }
                        else
                        {
                            def = fireSupport.expendProjectile().projectileWhenLoaded;
                            Projectile projectile = (Projectile) GenSpawn.Spawn(def, fireSupport.sourceLocation, map);
                            projectile.Launch(launcher, info, info, hitFlags);
                        }
                    }
                }

                // Log.Message("tick - " + fireSupport.timeRunning);
                fireSupport.timeRunning++;
            }
        }


        public int GetNextSettlementFCID()
        {
            nextSettlementFCID++;
            //Log.Message("Returning next settlement FC ID " + nextSettlementFCID);

            return nextSettlementFCID;
        }

        public int GetNextMercenaryID()
        {
            nextMercenaryID++;
            //Log.Message("Returning next mercenary ID " + nextMercenaryID);
            return nextMercenaryID;
        }

        public int GetNextMilitaryFireSupportID()
        {
            nextMilitaryFireSupportID++;
            //Log.Message("Returning next MilitaryFireSupportID " + nextSquadID);

            return nextMilitaryFireSupportID;
        }

        public int GetNextMercenarySquadID()
        {
            nextMercenarySquadID++;
            //Log.Message("Returning next MercenarySquadID " + nextMercenarySquadID);

            return nextMercenarySquadID;
        }

        public int GetNextTaxID()
        {
            nextTaxID++;
            return nextTaxID;
        }

        public int GetNextEventID()
        {
            nextEventID++;
            return nextEventID;
        }

        public int GetNextBillID()
        {
            nextBillID++;
            return nextBillID;
        }

        public int GetNextPrisonerID()
        {
            nextPrisonerID++;
            return nextPrisonerID;
        }

        public List<FCTraitEffectDef> returnListFactionTraits()
        {
            List<FCTraitEffectDef> tmpList = new List<FCTraitEffectDef>();
            foreach (FCTraitEffectDef trait in traits)
            {
                tmpList.Add(trait);
            }

            return tmpList;
        }


        public void setStartTime()
        {
            taxTimeDue = Find.TickManager.TicksGame + LoadedModManager.GetMod<FactionColoniesMod>()
                .GetSettings<FactionColonies>().timeBetweenTaxes;
            dailyTimer = Find.TickManager.TicksGame + 2000;
        }

        //0 defname
        //1 desc
        //2 location
        //3 time till trigger

        public int returnHighestMilitaryLevel()
        {
            int max = 1;
            foreach (SettlementFC settlement in settlements)
            {
                max = Math.Max(max, settlement.settlementMilitaryLevel);
            }

            return max;
        }


        public void updateFaction()
        {
            if (Find.World.GetComponent<FactionFC>().factionDef != null)
            {
            }
            else
            {
                Find.World.GetComponent<FactionFC>().factionDef = new FactionFCDef();
            }

            //load factionfcvalues
            //FactionColonies.getPlayerColonyFaction().def.techLevel = factionDef.techLevel;
            //FactionColonies.getPlayerColonyFaction().def.apparelStuffFilter = factionDef.apparelStuffFilter;
        }

        public void updateFactionRaces()
        {
            Faction faction = FactionColonies.getPlayerColonyFaction();
            // TODO updateFactionRaces()
        }

        public string returnNextTechToLevel()
        {
            switch (techLevel)
            {
                case TechLevel.Ultra:
                    return "ReachedMaxLevel".Translate();
                case TechLevel.Spacer:
                    return "FCShipBasics".Translate();
                case TechLevel.Industrial:
                    return "FCFabrication".Translate();
                case TechLevel.Medieval:
                    return "FCElectricity".Translate();
                case TechLevel.Neolithic:
                    return "FCSmithing".Translate();
                default:
                    return "N/A";
            }
        }

        public void updateTechLevel(ResearchManager researchManager)
        {
            bool medievalOnly = LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>()
                .medievalTechOnly;


            if (!medievalOnly && DefDatabase<ResearchProjectDef>.GetNamed("ShipBasics", false) != null &&
                researchManager.GetProgress(DefDatabase<ResearchProjectDef>.GetNamed("ShipBasics", false)) ==
                DefDatabase<ResearchProjectDef>.GetNamed("ShipBasics", false).baseCost && techLevel < TechLevel.Ultra)
            {
                techLevel = TechLevel.Ultra;
                factionDef.techLevel = TechLevel.Ultra;
                Log.Message("Ultra");
                raceFilter.FinalizeInit(this);
            }
            else if (!medievalOnly && DefDatabase<ResearchProjectDef>.GetNamed("Fabrication", false) != null &&
                     researchManager.GetProgress(DefDatabase<ResearchProjectDef>.GetNamed("Fabrication", false)) ==
                     DefDatabase<ResearchProjectDef>.GetNamed("Fabrication", false).baseCost &&
                     techLevel < TechLevel.Spacer)
            {
                techLevel = TechLevel.Spacer;
                factionDef.techLevel = TechLevel.Spacer;
                Log.Message("Spacer");
                raceFilter.FinalizeInit(this);
            }
            else if (!medievalOnly && DefDatabase<ResearchProjectDef>.GetNamed("Electricity", false) != null &&
                     researchManager.GetProgress(DefDatabase<ResearchProjectDef>.GetNamed("Electricity", false)) ==
                     DefDatabase<ResearchProjectDef>.GetNamed("Electricity", false).baseCost &&
                     techLevel < TechLevel.Industrial)
            {
                techLevel = TechLevel.Industrial;
                factionDef.techLevel = TechLevel.Industrial;
                Log.Message("Industrial");
                raceFilter.FinalizeInit(this);
            }
            else if (DefDatabase<ResearchProjectDef>.GetNamed("Smithing", false) != null &&
                     researchManager.GetProgress(DefDatabase<ResearchProjectDef>.GetNamed("Smithing", false)) ==
                     DefDatabase<ResearchProjectDef>.GetNamed("Smithing", false).baseCost &&
                     techLevel < TechLevel.Medieval)
            {
                techLevel = TechLevel.Medieval;
                factionDef.techLevel = TechLevel.Medieval;
                Log.Message("Medieval");
                raceFilter.FinalizeInit(this);
            }
            else
            {
                if (techLevel < TechLevel.Neolithic)
                {
                    Log.Message("Neolithic");
                    techLevel = TechLevel.Neolithic;
                    raceFilter.FinalizeInit(this);
                }
            }

            //update to player colony faction
            updateFaction();

            Faction playerColonyfaction = FactionColonies.getPlayerColonyFaction();
            if (playerColonyfaction != null && playerColonyfaction.def.techLevel < techLevel)
            {
                Log.Message("Updating Tech Level");
                updateFactionDef(techLevel, ref playerColonyfaction);
            }
            else if (playerColonyfaction.def.techLevel >= techLevel)
            {
                //Log.Message("Tech Level already matches");
            }
            // Check Leader
            if (playerColonyfaction != null)
            {
                if (playerColonyfaction.leader == null || playerColonyfaction.leader.Dead)
                {
                    if (!playerColonyfaction.TryGenerateNewLeader())
                    {
                        Log.Message("Generating Leader failed! Manually Generating . . .");
                        playerColonyfaction.leader = PawnGenerator.GeneratePawn(new PawnGenerationRequest(kind: Faction.OfPlayer.RandomPawnKind(),
                        faction: playerColonyfaction, context: PawnGenerationContext.NonPlayer,
                        forceGenerateNewPawn: true, newborn: false, allowDead: false, allowDowned: false,
                        canGeneratePawnRelations: true, mustBeCapableOfViolence: true, colonistRelationChanceFactor: 0,
                        forceAddFreeWarmLayerIfNeeded: false, worldPawnFactionDoesntMatter: false));
                        if (playerColonyfaction.leader == null)
                        {
                            Log.Warning("That failed, too! Contacting " + playerColonyfaction.Name + " won't work!");
                        }
                    }
                }
            }
        }

        public void updateFactionIcon(ref Faction faction, string iconPath)
        {
            Log.Message("Updated Icon - " + iconPath);
            faction.def.factionIconPath = iconPath;
            if (settlements.Any())
            {
                WorldSettlementFC.traitCachedIcon.SetValue(settlements[0].worldSettlement.def,
                    ContentFinder<Texture2D>.Get(iconPath));
            }

            foreach (SettlementFC settlement in settlements)
            {
                settlement.worldSettlement.def.expandingIconTexture = iconPath;
            }
        }

        public void updateFactionDef(TechLevel tech, ref Faction faction)
        {
            FactionDef replacingDef;
            ThingFilter apparelStuffFilter = new ThingFilter();
            FactionDef def = faction.def;

            switch (tech)
            {
                case TechLevel.Archotech:
                case TechLevel.Ultra:
                case TechLevel.Spacer:
                    replacingDef = DefDatabase<FactionDef>.GetNamedSilentFail("OutlanderCivil");

                    break;
                case TechLevel.Industrial:
                    replacingDef = DefDatabase<FactionDef>.GetNamedSilentFail("OutlanderCivil");
                    break;
                case TechLevel.Medieval:
                    if (FactionColonies.checkForMod("OskarPotocki.VanillaFactionsExpanded.MedievalModule"))
                    {
                        replacingDef = DefDatabase<FactionDef>.GetNamedSilentFail("VFEM_KingdomCivil");
                    }
                    else
                    {
                        replacingDef = DefDatabase<FactionDef>.GetNamedSilentFail("TribeCivil");
                    }

                    break;
                default:
                    replacingDef = DefDatabase<FactionDef>.GetNamedSilentFail("TribeCivil");
                    break;
            }
            //Log.Message("FactionFC.updateFactionDef - switch(tech) passed");

            //Log.Message("2");
            def.caravanTraderKinds = replacingDef.caravanTraderKinds;
            //Log.Message("3");
            if (replacingDef.backstoryFilters != null && replacingDef.backstoryFilters.Count != 0)
                def.backstoryFilters = replacingDef.backstoryFilters;
            //Log.Message("4");
            def.techLevel = tech;
            //Log.Message("5");
            def.basicMemberKind = replacingDef.basicMemberKind;
            //Log.Message("6");
            def.visitorTraderKinds = replacingDef.visitorTraderKinds;
            //Log.Message("7");
            def.baseTraderKinds = replacingDef.baseTraderKinds;
            //Log.Message("8");
            if (replacingDef.apparelStuffFilter != null)
                def.apparelStuffFilter = replacingDef.apparelStuffFilter;


            if (tech >= TechLevel.Spacer && def.apparelStuffFilter != null)
            {
                def.apparelStuffFilter.SetAllow(DefDatabase<StuffCategoryDef>.GetNamedSilentFail("Synthread"), true);
                def.apparelStuffFilter.SetAllow(DefDatabase<StuffCategoryDef>.GetNamedSilentFail("Hyperweave"), true);
                def.apparelStuffFilter.SetAllow(DefDatabase<StuffCategoryDef>.GetNamedSilentFail("Plasteel"), true);
            }
            updateFactionIcon(ref faction, "FactionIcons/" + factionIconPath);

            Log.Message("FactionFC.updateFactionDef - Completed tech update");
        }

        public bool hasPolicy(FCPolicyDef def)
        {
            //Don't game the system
            if (policies.Count() < 2)
            {
                return false;
            }

            foreach (FCPolicy policy in policies)
            {
                if (policy.def == def)
                    return true;
            }

            return false;
        }

        public bool hasTrait(FCPolicyDef def)
        {
            foreach (FCPolicy trait in factionTraits)
            {
                if (trait.def == def)
                    return true;
            }

            return false;
        }

        public bool sendDiplomaticEnvoy(Faction faction)
        {
            FactionFC factionfc = Find.World.GetComponent<FactionFC>();

            if (!faction.def.permanentEnemy)
            {
                if (Find.TickManager.TicksGame >=
                    (factionfc.traitPacifistTickLastUsedDiplomat + GenDate.TicksPerDay * 5))
                {
                    factionfc.traitPacifistTickLastUsedDiplomat = Find.TickManager.TicksGame;
                    int random = Rand.Range(1, 10);
                    if (random > 5)
                    {
                        int relationImprovement = Rand.Range(5, 15);
                        faction.TryAffectGoodwillWith(Find.FactionManager.OfPlayer, relationImprovement);
                        Find.LetterStack.ReceiveLetter("FCRelationImproved".Translate(),
                            "FCRelationImprovedText".Translate(faction.Name, relationImprovement),
                            LetterDefOf.PositiveEvent);
                    }
                    else
                    {
                        Find.LetterStack.ReceiveLetter("FCRelationNotImproved".Translate(),
                            "FCFailedToImproveRelationship".Translate(faction.Name), LetterDefOf.NeutralEvent);
                    }

                    return true;
                }

                Messages.Message(
                    "XDaysToSendDiplomat".Translate(Math.Round(
                        ((factionfc.traitPacifistTickLastUsedDiplomat + GenDate.TicksPerDay * 5) -
                         Find.TickManager.TicksGame).TicksToDays(), 1)), MessageTypeDefOf.RejectInput);
                return false;
            }

            Messages.Message("FCCannotImproveRelationsWithType".Translate(), MessageTypeDefOf.RejectInput);
            return false;
        }

        public void resetRaceFilter()
        {
            raceFilter = new RaceThingFilter(this);
            raceFilter.FinalizeInit(this);
        }

        public void updateAverages()
        {
            int averageHappinessTmp = 0;
            int averageLoyaltyTmp = 0;
            int averageUnrestTmp = 0;
            int averageProsperityTmp = 0;

            if (settlements.Count() > 0)
            {
                foreach (SettlementFC settlement in settlements)
                {
                    averageHappinessTmp += Convert.ToInt32(settlement.happiness);
                    averageLoyaltyTmp += Convert.ToInt32(settlement.loyalty);
                    averageUnrestTmp += Convert.ToInt32(settlement.unrest);
                    averageProsperityTmp += Convert.ToInt32(settlement.prosperity);
                }

                averageHappinessTmp /= settlements.Count();
                averageLoyaltyTmp /= settlements.Count();
                averageUnrestTmp /= settlements.Count();
                averageProsperityTmp /= settlements.Count();
            }

            averageHappiness = averageHappinessTmp;
            averageLoyalty = averageLoyaltyTmp;
            averageUnrest = averageUnrestTmp;
            averageProsperity = averageProsperityTmp;


            if (settlements.Any() && FactionColonies.getPlayerColonyFaction() != null)
            {
                FactionColonies.getPlayerColonyFaction().TryAffectGoodwillWith(Find.FactionManager.OfPlayer,
                    (Convert.ToInt32(averageHappiness) - FactionColonies.getPlayerColonyFaction().PlayerGoodwill));
            }
        }

        public void setName(string name)
        {
            this.name = name;
        }

        public void addSettlement(SettlementFC settlement)
        {
            settlements.Add(settlement);
            uiUpdate();
        }

        public void uiUpdate()
        {
            //Pop UI updates
            updateTotalResources();
            updateTotalProfit();
            updateTechLevel(Find.ResearchManager);
        }

        public double getTotalIncome() //return total income of settlements       ####MAKE UPDATE PER HOUR TICK
        {
            double income = 0;
            for (int i = 0; i < settlements.Count(); i++)
            {
                income += settlements[i].getTotalIncome();
            }

            return income;
        }


        public double getTotalUpkeep() //returns total upkeep of all settlements
        {
            double upkeep = 0;
            for (int i = 0; i < settlements.Count(); i++)
            {
                upkeep += settlements[i].getTotalUpkeep();
            }

            return upkeep;
        }

        public double getTotalProfit() //returns total profit (income - upkeep) of all settlements
        {
            return getTotalIncome() - getTotalUpkeep();
        }

        public void updateTotalProfit()
        {
            income = getTotalIncome();
            upkeep = getTotalUpkeep();
            profit = income - upkeep;
        }

        public void updateTotalResources()
        {
            foreach (ResourceType resourceType in ResourceUtils.resourceTypes)
            {
                int resource = 0;

                for (int k = 0; k < settlements.Count(); k++)
                {
                    resource += (int) settlements[k].getResource(resourceType).endProduction;
                }

                returnResource(resourceType).amount = resource;
                //Log.Message(i + " " + returnResourceByInt(i).amount);  //display total resources by type
            }
        }

        public void updateDailyResearch()
        {
            //Research adding
            if (Find.ResearchManager.currentProj == null && researchPointPool != 0)
            {
                Messages.Message("NoResearchExpended".Translate(Math.Round(researchPointPool)),
                    MessageTypeDefOf.NeutralEvent);
            }
            else if (researchPointPool != 0 && Find.ResearchManager.currentProj != null)
            {
                //Log.Message(researchTotal.ToString());
                float neededPoints;
                neededPoints = (float) Math.Ceiling(Find.ResearchManager.currentProj.CostApparent -
                                                    Find.ResearchManager.currentProj.ProgressApparent);
                Log.Message("Needed points: " + neededPoints);

                float expendedPoints;
                if (researchPointPool >= neededPoints)
                {
                    researchPointPool -= neededPoints;
                    expendedPoints = neededPoints;
                }
                else
                {
                    expendedPoints = researchPointPool;
                    researchPointPool = 0;
                }

                Log.Message("Expended points: " + expendedPoints);

                Find.LetterStack.ReceiveLetter("ResearchPointsExpended".Translate(),
                    "ResearchExpended".Translate(Math.Round(expendedPoints), Find.ResearchManager.currentProj.LabelCap,
                        Math.Round(researchPointPool)), LetterDefOf.PositiveEvent);
                if (Find.ColonistBar.GetColonistsInOrder().Count > 0)
                {
                    Pawn pawn = Find.ColonistBar.GetColonistsInOrder()[0];
                    Find.ResearchManager.ResearchPerformed(
                        (float) Math.Ceiling(
                            ((1 * Find.ResearchManager.currentProj.CostFactor(pawn.Faction.def.techLevel)) /
                             (0.00825 * Find.Storyteller.difficulty.researchSpeedFactor)) * expendedPoints),
                        pawn);
                    Log.Message("Passed to function: " + (float) Math.Ceiling(
                        ((1 * Find.ResearchManager.currentProj.CostFactor(pawn.Faction.def.techLevel)) /
                         (0.00825 * Find.Storyteller.difficulty.researchSpeedFactor)) * expendedPoints));
                }
                else
                {
                    Log.Message("Could not find colonist to research with");
                    Find.ResearchManager.ResearchPerformed(
                        (float) Math.Ceiling((1 / (0.00825 * Find.Storyteller.difficulty.researchSpeedFactor)) *
                                             expendedPoints), null);
                }
            }
        }


        public void addTax(bool isUpdating)
        {
            //if (capitalLocation == -1)
            //{
            //    setCapital();
            //}
            powerPool = 0;

            if (settlements.Count != 0) //if settlements is not zero
            {
                foreach (SettlementFC settlement in settlements)
                {
                    //Start Traits
                    addExperienceToFactionLevel(2f);


                    float trait_Industrious_TaxPercentageBoost = 1;
                    if (hasTrait(FCPolicyDefOf.industrious))
                    {
                        int num = Rand.RangeInclusive(1, 20);
                        if (num == 5)
                        {
                            trait_Industrious_TaxPercentageBoost = 1f + (Rand.RangeInclusive(20, 50) / 100f);
                            Find.LetterStack.ReceiveLetter("FCIdustriousTaxBoost".Translate(),
                                "FCIndustriousPop".Translate(settlement.name,
                                    ((trait_Industrious_TaxPercentageBoost - 1f) * 100f) + "%"),
                                LetterDefOf.PositiveEvent);
                        }
                    }


                    //End Traits

                    List<Thing> list = new List<Thing>();
                    settlement.updateProfitAndProduction();
                    list = settlement.createTithe(trait_Industrious_TaxPercentageBoost);
                    float researchPool = settlement.createResearchPool();
                    float electricityAllotted = settlement.createPowerPool();

                    BillFC bill = new BillFC(settlement); //Create new bill connected to settlement
                    bill.taxes.electricityAllotted = electricityAllotted;
                    bill.taxes.researchCompleted = researchPool;
                    bill.taxes.itemTithes.AddRange(list); //Add tithe to bill's tithes
                    bill.taxes.silverAmount =
                        Convert.ToInt32((settlement.totalIncome * trait_Industrious_TaxPercentageBoost) -
                                        settlement.totalUpkeep) + settlement.returnSilverIncome(true);
                    Bills.Add(bill);

                    FactionColonies.getTownTitle(settlement);
                    TaxTickPrisoner(settlement);
                }


                if (!isUpdating) //if done updating (timeskip) then send goods/silver etc
                {
                    //Messages.Message("TaxesBilled".Translate() + "!", MessageTypeDefOf.PositiveEvent);
                    Find.LetterStack.ReceiveLetter("Taxes Billed", "Taxes from your settlements have been billed",
                        LetterDefOf.PositiveEvent);
                    uiUpdate();

                    //Messages.Message(Find.TickManager.TicksGame.ToString(), MessageTypeDefOf.PositiveEvent);
                }
            }
            else
            {
                Messages.Message("NoSettlementsToTax".Translate(), MessageTypeDefOf.NeutralEvent);
            }
        }

        public float updateFactionLevelGoalXP(int currentLevel)
        {
            float newGoal = 100 + (currentLevel * 150);
            return newGoal;
        }

        public bool addExperienceToFactionLevel(float xp)
        {
            bool leveled = false;
            factionXPCurrent += xp;

            while (factionXPCurrent >= factionXPGoal)
            {
                factionXPCurrent -= factionXPGoal;
                factionLevel += 1;
                Find.LetterStack.ReceiveLetter("FCFactionLevelUp".Translate(),
                    "FCFactionLevelUpDesc".Translate(name, factionLevel), LetterDefOf.PositiveEvent);
                leveled = true;
                factionXPGoal = updateFactionLevelGoalXP(factionLevel);
            }

            return leveled;
        }

        public void addEvent(FCEvent fcevent)
        {
            if (fcevent == null) return;
            //Add event to events
            events.Add(fcevent);

            //check if event has a location, if does, add traits to that specific location;
            if (fcevent.settlementTraitLocations.Count() > 0) //if has specific locations
            {
                foreach (SettlementFC location in fcevent.settlementTraitLocations)
                {
                    location.traits.AddRange(fcevent.def.traits);
                    foreach (FCTraitEffectDef trait in fcevent.def.traits)
                    {
                        //Log.Message(trait.label);
                    }
                }
            }
            else
            {
                //if no specific location then faction wide
                traits.AddRange(fcevent.traits);
            }
        }

        public bool checkSettlementCaravansList(string location) //list of destinations caravans gone to
        {
            for (int i = 0; i < settlementCaravansList.Count(); i++)
            {
                if (location == settlementCaravansList[i] || Find.WorldGrid.IsNeighbor(Convert.ToInt32(location),
                    Convert.ToInt32(settlementCaravansList[i])))
                {
                    return true; // is on list
                }
            }

            return false; //is not on list
        }

        public ResourceFC returnResource(string name) //used to return the correct resource based on string name
        {
            return returnResource(ResourceUtils.getTypeFromName(name));
        }

        public ResourceFC returnResourceByInt(int name) //used to return the correct resource based on string name
        {
            return returnResource(ResourceUtils.resourceTypes[name]);
        }

        public ResourceFC returnResource(ResourceType type)
        {
            switch (type)
            {
                case ResourceType.Food:
                    return food;
                case ResourceType.Weapons:
                    return weapons;
                case ResourceType.Apparel:
                    return apparel;
                case ResourceType.Animals:
                    return animals;
                case ResourceType.Logging:
                    return logging;
                case ResourceType.Mining:
                    return mining;
                case ResourceType.Research:
                    return research;
                case ResourceType.Power:
                    return power;
                case ResourceType.Medicine:
                    return medicine;
            }

            Log.Message("Unable to find resource - returnResourceByInt(int name)");
            return null;
        }

        public void setCapital()
        {
            if (Find.CurrentMap != null && Find.CurrentMap.IsPlayerHome)
            {
                capitalLocation = Find.CurrentMap.Parent.Tile;
                capitalPlanet = Find.World.info.name;

                //Log.Message(Find.CurrentMap.Parent.def.defName);
                if (Find.CurrentMap.Parent.def.defName == "ShipOrbiting")
                {
                    SoSShipCapital = true;
                }
                else
                {
                    SoSShipCapital = false;
                }

                Messages.Message("SetAsFactionCapital".Translate(Find.CurrentMap.Parent.LabelCap),
                    MessageTypeDefOf.NeutralEvent);
            }
            else
            {
                Messages.Message(
                    "Unable to set faction capital on this map. Please go to your capital map and use the Set Capital button or else you may have some bugs soon.",
                    MessageTypeDefOf.NegativeEvent);
            }
        }

        public int returnCapitalMapId()
        {
            for (int i = 0; i < Find.Maps.Count(); i++)
            {
                if (Find.Maps[i].Tile == capitalLocation)
                {
                    return i;
                }
            }

            Log.Message("CouldNotFindMapOfCapital".Translate());
            return -1;
        }

        public Map returnCapitalMap()
        {
            for (int i = 0; i < Find.Maps.Count(); i++)
            {
                if (Find.Maps[i].Tile == capitalLocation)
                {
                    return Find.Maps[i];
                }
            }

            Log.Message("CouldNotFindMapOfCapital".Translate());
            return null;
        }

        public int returnSettlementFCIDByLocation(int location, string planetName)
        {
            for (int i = 0; i < settlements.Count(); i++)
            {
                if (settlements[i].mapLocation == location && settlements[i].planetName == planetName)
                {
                    return i;
                }
            }

            return -1;
        }

        public SettlementFC returnSettlementByLocation(int location, string planetName)
        {
            if (planetName == null)
            {
                Log.Message(
                    "Planet name was null. Please report this as well as the military event that the settlement was used for.");
                planetName = Find.World.info.name;
            }

            for (int i = 0; i < settlements.Count(); i++)
            {
                //Log.Message(settlements[i].planetName);
                if (settlements[i].mapLocation == location && settlements[i].planetName == planetName)
                {
                    return settlements[i];
                }
            }

            return null;
        }

        public string getSettlementName(int location, string planetName)
        {
            int i = returnSettlementFCIDByLocation(location, planetName);
            switch (i)
            {
                case -1:
                    return "Null";

                default:
                    return settlements[returnSettlementFCIDByLocation(location, planetName)].name;
            }
        }

        public SettlementFC getSettlement(int location, string planetName)
        {
            int i = returnSettlementFCIDByLocation(location, planetName);
            switch (i)
            {
                case -1:
                    return null;
                default:
                    return settlements[returnSettlementFCIDByLocation(location, planetName)];
            }
        }


        public void updateSettlementStats()
        {
            foreach (SettlementFC settlement in settlements)
            {
                settlement.updateHappiness();
                settlement.updateLoyalty();
                settlement.updateUnrest();
                settlement.updateProsperity();
            }
        }

        public void TaxTick()
        {
            if (Find.TickManager.TicksGame >= taxTimeDue) // taxTimeDue being used as set interval when skipping time
            {
                while (Find.TickManager.TicksGame >= taxTimeDue) //while updating events
                {
                    //update events in this order: regular events: tax events.


                    if (Find.TickManager.TicksGame > taxTimeDue)
                    {
                        addTax(true);
                    }
                    else
                    {
                        Log.Message(
                            "Empire Mod - TaxTick - Catching Up - Did you skip time? Report this if you did not");
                        addTax(false);
                        //NOT WHERE FINAL UPDATE IS. Go to addTax Function
                    }

                    taxTimeDue += LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>()
                        .timeBetweenTaxes;
                    //Log.Message(Find.TickManager.TicksGame + " vs " + taxTimeDue + " - Taxing");
                }

                //if Autoresolve bills on, attempt to autoresolve
                switch (autoResolveBills)
                {
                    case true:
                        PaymentUtil.autoresolveBills(Bills);
                        break;
                    case false:
                        break;
                }
            }
        }

        public void TaxTickPrisoner(SettlementFC settlement)
        {
            Reset:
            foreach (FCPrisoner prisoner in settlement.prisonerList)
            {
                switch (prisoner.workload)
                {
                    case FCWorkLoad.Heavy:
                        if (prisoner.AdjustHealth(-20))
                            goto Reset;
                        break;
                    case FCWorkLoad.Medium:
                        if (prisoner.AdjustHealth(-10))
                            goto Reset;
                        break;
                    case FCWorkLoad.Light:
                        if (prisoner.AdjustHealth(4))
                            goto Reset;
                        break;
                }
            }
        }

        public void resetTraitMercantileCaravanTime()
        {
            float days = Rand.RangeInclusive(3, 5);
            traitMercantileTradeCaravanTickDue = Find.TickManager.TicksGame + (int) (days * GenDate.TicksPerDay);
        }

        private bool CanMakeRandomEventNow() => Rand.Chance((randomEventLastAdded - LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().minDaysTillRandomEvent) / (LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().maxDaysTillRandomEvent - LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().minDaysTillRandomEvent));

        private bool RandomEventsDisabledOrNoSettlements() => Find.World.GetComponent<FactionFC>().settlements.Count == 0 || FactionColonies.Settings().disableRandomEvents;

        private void MakeRandomEvent()
        {
            if (RandomEventsDisabledOrNoSettlements()) return;

            //Log.Message(tmpNum.ToString());
            if (CanMakeRandomEventNow())
            {
                FCEvent tmpEvt = FCEventMaker.MakeRandomEvent(FCEventMaker.returnRandomEvent(), null);
                //Log.Message(tmpEvt.def.label);
                if (tmpEvt != null)
                {
                    Find.World.GetComponent<FactionFC>().addEvent(tmpEvt);
                    randomEventLastAdded = 0f;

                    //letter code
                    List<string> settlements = new List<string>();
                    tmpEvt.settlementTraitLocations.ForEach(settlement => settlements.Add(settlement.name));
                    string settlementString = string.Join(", ", settlements);

                    if (!settlementString.NullOrEmpty())
                    {
                        Find.LetterStack.ReceiveLetter("Random Event",
                            tmpEvt.def.desc + "\n This event is affecting the following settlements: " +
                            settlementString, LetterDefOf.NeutralEvent);
                    }
                    else
                    {
                        Find.LetterStack.ReceiveLetter("Random Event", tmpEvt.def.desc,
                            LetterDefOf.NeutralEvent);
                    }
                }
                else
                {
                    randomEventLastAdded += 1f;
                }
            }
            else
            {
                randomEventLastAdded += 1f;
            }

        }

        public void StatTick()
        {
            if (Find.TickManager.TicksGame >= dailyTimer) // taxTimeDue being used as set interval when skipping time
            {
                while (Find.TickManager.TicksGame >= dailyTimer) //while updating events
                {
                    //update events in this order: regular events: tax events.

                    // Log.Message("Tick");
                    updateSettlementStats();
                    updateAverages();
                    RelationsUtilFC.resetPlayerColonyRelations();


                    updateDailyResearch();

                    //Random event creation
                    MakeRandomEvent();

                    dailyTimer += GenDate.TicksPerDay;
                    //Log.Message(Find.TickManager.TicksGame + " vs " + taxTimeDue + " - Taxing");
                }
            }
        }

        public void MilitaryTick()
        {
            if (Find.TickManager.TicksGame >= militaryTimeDue)
            {
                if (LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>()
                        .disableHostileMilitaryActions == false &
                    Find.TickManager.TicksGame > (timeStart + GenDate.TicksPerSeason))
                {
                    //if military actions not disabled or game has not passed through the first season
                    //Log.Message("Mil Action debug");


                    //if settlements exist

                    // get list of settlements

                    //if not underattack, add to list

                    //create weight list by settlement military level

                    //choose random

                    if (settlements.Any())
                    {
                        //if settlements exist
                        List<SettlementFC> targets = new List<SettlementFC>();
                        foreach (SettlementFC settlement in settlements)
                        {
                            //create weight list of settlements
                            if (settlement.isUnderAttack == false)
                            {
                                //if not underattack, add to list
                                //get weightvalue of target
                                int weightValue;
                                switch (settlement.settlementMilitaryLevel)
                                {
                                    case 0:
                                    case 1:
                                        weightValue = 10;
                                        break;
                                    case 2:
                                    case 3:
                                        weightValue = 7;
                                        break;
                                    case 4:
                                    case 5:
                                        weightValue = 3;
                                        break;
                                    default:
                                        weightValue = 1;
                                        break;
                                }

                                for (int k = 0; k < weightValue; k++)
                                {
                                    targets.Add(settlement);
                                }
                            }
                        }

                        if (targets.Any())
                        {
                            //List created, pick from list
                            Faction enemy = Find.FactionManager.RandomEnemyFaction();
                            if (enemy != null)
                            {
                                // Limit to settlements on current planet
                                // TODO: Make it compatible with settlements on different planets instead of excluding
                                // them
                                SettlementFC settlement = targets
                                    .Where(s => s.planetName == Find.World.info.name)
                                    .RandomElementWithFallback();
                                
                                if (settlement != null)
                                    MilitaryUtilFC.attackPlayerSettlement(
                                        militaryForce.createMilitaryForceFromFaction(enemy, true),
                                        targets.RandomElement(), enemy);
                            }
                                    
                        }
                    }
                }

                militaryTimeDue = Find.TickManager.TicksGame + (60000 * LoadedModManager.GetMod<FactionColoniesMod>()
                    .GetSettings<FactionColonies>().minMaxDaysTillMilitaryAction.RandomInRange);
                //Log.Message(militaryTimeDue + " - " + Find.TickManager.TicksGame);
//Log.Message((militaryTimeDue - Find.TickManager.TicksGame) / 60000 + " days till next military action");
                //militaryTimeDue =
            }
        }


        public void UITick()
        {
            if (uiTimeUpdate <= 0) //update per time?
            {
                uiTimeUpdate = FactionColonies.updateUiTimer;

                //already built in ui update -.-
                Find.WindowStack.WindowsUpdate();

                //Pop UI updates
                uiUpdate();
            }
            else
            {
                uiTimeUpdate -= 1;
            }
        }
    }
}