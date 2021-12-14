using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FactionColonies.util;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;
using Verse.Sound;

namespace FactionColonies
{
    /// <summary>
    ///     WorldObject that in many ways re-implements Settlement.cs from Rimworld.Planet. May cause compatibility issues with
    ///     other mods that rely on finding Settlement objects on the world map. Recommend testing this extensively with mods
    ///     like SoS2, RimWar, or any mods that modify, collect, or deep save world objects before publishing changes
    /// </summary>
    public class WorldSettlementFC : Settlement
    {
        public static readonly FieldInfo traitCachedIcon = typeof(WorldObjectDef).GetField("expandingIconTextureInt",
            BindingFlags.NonPublic | BindingFlags.Instance);

        public static readonly FieldInfo traitCachedMaterial = typeof(WorldObjectDef).GetField("material",
            BindingFlags.NonPublic | BindingFlags.Instance);

        public militaryForce attackerForce;
        public List<Pawn> attackers = new List<Pawn>();
        public militaryForce defenderForce;
        public List<Pawn> defenders = new List<Pawn>();

        /// <summary>
        ///     A flag meant to indicate whether or not this settlement is meant for actual destruction; used to override
        ///     WorldObject.Destroy() for compatibility purposes
        /// </summary>
        private bool destroyFlag;

        public SettlementFC settlement;
        public int shuttleUsesRemaining;
        public List<CaravanSupporting> supporting = new List<CaravanSupporting>();

        public new WorldSettlementTraderTracker trader;

        public new string Name
        {
            get
            {
                if (settlement == null) return "";

                return settlement.name ?? (settlement.name = "");
            }
            set => settlement.name = value;
        }

        public override string Label => Name;


        public new TraderKindDef TraderKind
        {
            get
            {
                if (trader.settlement == null) trader.settlement = this;
                return trader?.TraderKind;
            }
        }

        public new IEnumerable<Thing> Goods => trader?.StockListForReading;

        public new int RandomPriceFactorSeed => trader?.RandomPriceFactorSeed ?? 0;

        public new string TraderName => trader?.TraderName;

        public new bool CanTradeNow => trader != null && trader.CanTradeNow;

        public new float TradePriceImprovementOffsetForPlayer => trader?.TradePriceImprovementOffsetForPlayer ?? 0.0f;

        public new TradeCurrency TradeCurrency => TraderKind.tradeCurrency;

        public new bool EverVisited => trader.EverVisited;

        public new bool RestockedSinceLastVisit => trader.RestockedSinceLastVisit;

        public new int NextRestockTick => trader.NextRestockTick;

        private Command_Action DefendColonyAction => new Command_Action
        {
            defaultLabel = "DefendColony".Translate(),
            defaultDesc = "DefendColonyDesc".Translate(),
            icon = TexLoad.iconMilitary,
            action = delegate
            {
                if (FactionColonies.Settings().settlementsAutoBattle)
                    Messages.Message("autoBattleEnabledNoManualFight".Translate(), MessageTypeDefOf.RejectInput);
                else
                    startDefence(MilitaryUtilFC.returnMilitaryEventByLocation(settlement.mapLocation), () => { });
            }
        };

        private Command_Action ChangeDefenderAction => new Command_Action
        {
            defaultLabel = "DefendSettlement".Translate(),
            defaultDesc = "",
            icon = TexLoad.iconCustomize,
            action = delegate
            {
                var list = new List<FloatMenuOption>();
                var evt = MilitaryUtilFC.returnMilitaryEventByLocation(settlement.mapLocation);
                if (evt == null) return;

                list.Add(new FloatMenuOption("SettlementDefendingInformation".Translate(
                        evt.militaryForceDefending.homeSettlement.name,
                        evt.militaryForceDefending.militaryLevel), null,
                    MenuOptionPriority.High));

                list.Add(new FloatMenuOption("ChangeDefendingForce".Translate(),
                    () => ChangeDefendingForceAction(evt)));

                var floatMenu = new FloatMenu(list)
                {
                    vanishIfMouseDistant = true
                };
                Find.WindowStack.Add(floatMenu);
            }
        };

        public Command_Action RequestShuttleAction => new Command_Action
        {
            defaultLabel = "shuttlePortCallShuttleLabel".Translate(),
            defaultDesc = "shuttlePortCallShuttleDesc".Translate(shuttleUsesRemaining, ShuttleSender.cost),
            icon = ContentFinder<Texture2D>.Get("UI/Commands/CallShuttle"),
            action = delegate
            {
                if (shuttleUsesRemaining < ShuttleSender.cost)
                {
                    Messages.Message("notEnoughShuttleUsesRemaining".Translate(), MessageTypeDefOf.RejectInput);
                    return;
                }

                Find.WorldSelector.ClearSelection();
                var sender = new ShuttleSender(Tile, this);
                Find.WorldTargeter.BeginTargeting(sender.PerformActionWithTarget, true,
                    CompLaunchable.TargeterMouseAttachment, false, sender.DrawWorldRadiusRing,
                    sender.DisplayTargetInformation, sender.ChoseWorldTarget);
            }
        };

        public Command_Action RequestShuttleForCaravanAction => new Command_Action
        {
            defaultLabel = "shuttlePortCallShuttleForCaravanLabel".Translate(),
            defaultDesc = "shuttlePortCallShuttleDesc".Translate(shuttleUsesRemaining, ShuttleSender.cost),
            icon = ContentFinder<Texture2D>.Get("UI/Commands/CallShuttle"),

            action = delegate
            {
                if (shuttleUsesRemaining < ShuttleSender.cost)
                {
                    Messages.Message("noShuttleUsesRemaining".Translate(), MessageTypeDefOf.RejectInput);
                    return;
                }

                var caravans = Find.World.worldObjects.Caravans.Where(caravan => caravan.Faction == Faction.OfPlayer)
                    .ToList();
                var options = new List<FloatMenuOption>();

                caravans.ForEach(caravan => options.Add(new FloatMenuOption(caravan.Label, delegate
                {
                    var sender = new ShuttleSenderCaravan(caravan.Tile, caravan, this);

                    CameraJumper.TryJump(caravan);
                    Find.WorldSelector.ClearSelection();
                    var tile = caravan.Tile;
                    Find.WorldTargeter.BeginTargeting(sender.ChoseWorldTarget, true,
                        CompLaunchable.TargeterMouseAttachment, false,
                        delegate { GenDraw.DrawWorldRadiusRing(tile, ShuttleSender.ShuttleRange); },
                        target => sender.TargetingLabelGetter(target, tile, ShuttleSender.ShuttleRange,
                            Gen.YieldSingle(caravan), sender.Launch));
                })));

                if (options.Count == 0) options.Add(new FloatMenuOption("noCaravansToSendShuttleTo".Translate(), null));

                Find.WindowStack.Add(new FloatMenu(options));
            }
        };

        public Command_Action OpenSettlementWindowAction => new Command_Action
        {
            defaultLabel = "openSettlementWindowDefaultLabel".Translate(),
            defaultDesc = "openSettlementWindowDefaultDesc".Translate(),
            icon = ContentFinder<Texture2D>.Get("UI/Icons/QuestionMark"),
            action = delegate { Find.WindowStack.Add(new SettlementWindowFc(settlement)); }
        };

        /// <summary>
        ///     Indicate that this should be destroyed when WorldObject.Destroy() is called
        /// </summary>
        public void PrepareDestroy()
        {
            destroyFlag = true;
        }

        /// <summary>
        ///     Compatibility focused: this object should only be destroyed very deliberately, else another object is likely trying
        ///     to handle negative combat resolution against this settlement.
        /// </summary>
        public override void Destroy()
        {
            if (destroyFlag)
                base.Destroy();

            else
                endBattle(false, 0);
        }

        public IEnumerable<Thing> ColonyThingsWillingToBuy(Pawn playerNegotiator)
        {
            return trader?.ColonyThingsWillingToBuy(playerNegotiator);
        }

        public void GiveSoldThingToTrader(Thing toGive, int countToGive, Pawn playerNegotiator)
        {
            trader.GiveSoldThingToTrader(toGive, countToGive, playerNegotiator);
        }

        public void GiveSoldThingToPlayer(Thing toGive, int countToGive, Pawn playerNegotiator)
        {
            trader.GiveSoldThingToPlayer(toGive, countToGive, playerNegotiator);
        }

        private string FoundSettlementString(SettlementFC s)
        {
            return s.name + " " + "ShortMilitary".Translate() + " " + s.settlementMilitaryLevel +
                   " - " + "FCAvailable".Translate() + ": " + (!s.isMilitaryBusySilent()).ToString();
        }

        private void ChangeDefendingForceAction(FCEvent evt)
        {
            var faction = Find.World.GetComponent<FactionFC>();
            var settlementList = new List<FloatMenuOption>
            {
                new FloatMenuOption
                (
                    "ResetToHomeSettlement".Translate(settlement.settlementMilitaryLevel),
                    delegate { MilitaryUtilFC.changeDefendingMilitaryForce(evt, settlement); },
                    MenuOptionPriority.High
                )
            };


            settlementList.AddRange
            (
                from foundSettlement in faction.settlements
                where foundSettlement.isMilitaryValid() && foundSettlement != settlement
                select new FloatMenuOption
                (
                    FoundSettlementString(foundSettlement),
                    delegate
                    {
                        if (!foundSettlement.isMilitaryBusy())
                            MilitaryUtilFC.changeDefendingMilitaryForce(evt, foundSettlement);
                    }
                )
            );

            if (settlementList.Count == 0)
                settlementList.Add(new FloatMenuOption("NoValidMilitaries".Translate(), null));

            var floatMenu2 = new FloatMenu(settlementList)
            {
                vanishIfMouseDistant = true
            };
            Find.WindowStack.Add(floatMenu2);
        }

        public override void PostMake()
        {
            trader = new WorldSettlementTraderTracker(this);

            updateTechIcon();
            def.expandingIconTexture = "FactionIcons/" + Find.World.GetComponent<FactionFC>().factionIconPath;
            traitCachedIcon.SetValue(def, ContentFinder<Texture2D>.Get(def.expandingIconTexture));
            base.PostMake();

            attackers = new List<Pawn>();
            defenders = new List<Pawn>();
            supporting = new List<CaravanSupporting>();
        }

        public void updateTechIcon()
        {
            var techLevel = Find.World.GetComponent<FactionFC>().techLevel;
            Log.Message("Got tech level " + techLevel);
            if (techLevel == TechLevel.Animal || techLevel == TechLevel.Neolithic)
                def.texture = "World/WorldObjects/TribalSettlement";
            else
                def.texture = "World/WorldObjects/DefaultSettlement";

            traitCachedMaterial.SetValue(def, MaterialPool.MatFrom(def.texture,
                ShaderDatabase.WorldOverlayTransparentLit, WorldMaterials.WorldObjectRenderQueue));
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref settlement, "settlement");
            Scribe_Collections.Look(ref attackers, "attackers", LookMode.Reference);
            Scribe_Collections.Look(ref defenders, "defenders", LookMode.Reference);
            Scribe_Collections.Look(ref supporting, "supporting", LookMode.Reference);
            Scribe_Deep.Look(ref defenderForce, "defenderForce");
            Scribe_Deep.Look(ref attackerForce, "attackerForce");
            Scribe_Deep.Look(ref trader, "trader");
            Scribe_Values.Look(ref shuttleUsesRemaining, "shuttleUsesRemaining");
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            foreach (var gizmo in base.GetGizmos()) yield return gizmo;
            yield return OpenSettlementWindowAction;
            if (settlement.isUnderAttack) yield return DefendColonyAction;
            if (settlement.isUnderAttack && !attackers.Any()) yield return ChangeDefenderAction;
            var containsShuttlePort = settlement.buildings.Contains(BuildingFCDefOf.shuttlePort);
            if (containsShuttlePort) yield return RequestShuttleAction;
            if (containsShuttlePort) yield return RequestShuttleForCaravanAction;
        }

        public void CaravanDefend(Caravan caravan)
        {
            var pawns = caravan.pawns.InnerListForReading.ListFullCopy();
            AddToDefenceFromList(pawns, caravan.Tile);

            if (!caravan.Destroyed) caravan.Destroy();
            var enterCell = FindNearEdgeCell(Map);
            foreach (var pawn in pawns)
            {
                var loc =
                    CellFinder.RandomSpawnCellForPawnNear(enterCell, Map);
                GenSpawn.Spawn(pawn, loc, Map, Rot4.Random);
            }
        }

        public void AddToDefenceFromList(List<Pawn> pawns, int destinationTile)
        {
            if (pawns.NullOrEmpty())
            {
                Log.Error("Tried to add an empty list of pawns to an FCEvent");
                return;
            }

            startDefence(
                MilitaryUtilFC.returnMilitaryEventByLocation(destinationTile), () =>
                {
                    foreach (var pawn in pawns)
                    {
                        if (defenders.Contains(pawn)) return;
                        if (defenders.Any())
                            defenders[0].GetLord().AddPawn(pawn);
                        else
                            LordMaker.MakeNewLord(FactionColonies.getPlayerColonyFaction(), new LordJob_ColonistsIdle(),
                                Map, pawns);
                    }

                    var caravanSupporting = new CaravanSupporting
                    {
                        pawns = pawns
                    };

                    supporting.Add(caravanSupporting);

                    defenders.AddRange(caravanSupporting.pawns);
                });
        }

        public override IEnumerable<Gizmo> GetCaravanGizmos(Caravan caravan)
        {
            if (settlement.isUnderAttack)
            {
                yield return new Command_Action
                {
                    defaultLabel = "DefendColony".Translate(),
                    defaultDesc = "DefendColonyDesc".Translate(),
                    icon = TexLoad.iconMilitary,
                    action = () =>
                    {
                        startDefence(MilitaryUtilFC.returnMilitaryEventByLocation(settlement.mapLocation),
                            () => CaravanDefend(caravan));
                    }
                };
            }
            else
            {
                trader.settlement = trader.settlement ?? settlement.worldSettlement;
                var kindDef = trader.TraderKind;
                var action = (Command_Action) CaravanVisitUtility.TradeCommand(caravan, Faction, kindDef);

                var bestNegotiator = BestCaravanPawnUtility.FindBestNegotiator(caravan, Faction, kindDef);
                action.action = () =>
                {
                    if (!CanTradeNow)
                        return;
                    Find.WindowStack.Add(new Dialog_Trade(bestNegotiator, this));
                    PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter_Send(Goods.OfType<Pawn>(),
                        "LetterRelatedPawnsTradingWithSettlement"
                            .Translate((NamedArgument) Faction.OfPlayer.def.pawnsPlural), LetterDefOf.NeutralEvent);
                };

                yield return action;
            }
        }

        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Caravan caravan)
        {
            if (!settlement.isUnderAttack)
                foreach (var option in WorldSettlementTradeAction.GetFloatMenuOptions(caravan, this))
                    yield return option;
            else
                foreach (var option in WorldSettlementDefendAction.GetFloatMenuOptions(caravan, this))
                    yield return option;
        }

        public override IEnumerable<FloatMenuOption> GetTransportPodsFloatMenuOptions(IEnumerable<IThingHolder> pods,
            CompLaunchable representative)
        {
            foreach (var floatMenuOption in base.GetTransportPodsFloatMenuOptions(pods, representative))
            {
                yield return floatMenuOption;
            }
            if (TransportPodsArrivalAction_LandInSpecificCell.CanLandInSpecificCell(pods, this))


                yield return new FloatMenuOption("LandInExistingMap".Translate(Label), delegate
                {
                    var myMap = representative.parent.Map;
                    var map = Map;
                    Current.Game.CurrentMap = map;
                    CameraJumper.TryHideWorld();
                    var targeter = Find.Targeter;
                    var targetParams = TargetingParameters.ForDropPodsDestination();

                    void action(LocalTargetInfo targetInfo)
                    {
                        representative.TryLaunch(Tile,
                            new WorldSettlementTransportPodDefendAction(this, targetInfo.Cell,
                                representative.parent.TryGetComp<CompShuttle>() != null));
                    }

                    targeter.BeginTargeting(targetParams, action, null, delegate
                    {
                        if (Find.Maps.Contains(myMap)) Current.Game.CurrentMap = myMap;
                    }, CompLaunchable.TargeterMouseAttachment);
                });
        }

        private void deleteMap()
        {
            if (Map == null) return;
            Map.lordManager.lords.Clear();

            CameraJumper.TryJump(settlement.mapLocation);
            //Prevent player from zooming back into the settlement
            Current.Game.CurrentMap = Find.AnyPlayerHomeMap;

            //Ignore any empty caravans
            var AllDowned = supporting.All(supporting => supporting.pawns.All(pawn => !pawn.Downed || !pawn.Dead));
            foreach (var caravanSupporting in supporting.Where(supporting => supporting.pawns.Any(
                pawn => !pawn.Downed && !pawn.Dead)))
                CaravanFormingUtility.FormAndCreateCaravan(caravanSupporting.pawns.Where(pawn => pawn.Spawned),
                    Faction.OfPlayer, settlement.mapLocation, settlement.mapLocation, -1);

            if (AllDowned && defenders.Any())
            {
                var pawns = new HashSet<Thing>();
                foreach (var caravanSupporting in supporting)
                foreach (var pawn in caravanSupporting.pawns)
                    if (!pawn.Dead)
                    {
                        pawn.DeSpawn();
                        pawns.Add(pawn);
                    }

                foreach (Pawn pawn in pawns)
                    if (!pawn.Dead)
                    {
                        var num2 = 0;
                        while (pawn.health.HasHediffsNeedingTend())
                        {
                            num2++;
                            if (num2 > 10000)
                            {
                                Log.Error("Too many iterations.");
                                return;
                            }

                            TendUtility.DoTend(null, pawn, null);
                        }
                    }

                var eventParams = new FCEvent
                {
                    location = Find.AnyPlayerHomeMap.Tile,
                    planetName = settlement.planetName,
                    source = settlement.mapLocation,
                    goods = pawns.ToList(),
                    customDescription = DeliveryEvent.ShuttleEventInjuredString,
                    timeTillTrigger = Find.TickManager.TicksGame +
                                      FactionColonies.ReturnTicksToArrive(Tile, Find.AnyPlayerHomeMap.Tile)
                };

                if (pawns.Any()) DeliveryEvent.CreateDeliveryEvent(eventParams);
            }

            if (Map.mapPawns?.AllPawnsSpawned == null) return;

            //Despawn removes them from AllPawnsSpawned, so we copy it
            foreach (var pawn in Map.mapPawns.AllPawnsSpawned.ListFullCopy()) pawn.DeSpawn();
        }

        public override bool ShouldRemoveMapNow(out bool removeWorldObject)
        {
            removeWorldObject = false;
            return !defenders.Any() && !attackers.Any();
        }

        public void startDefence(FCEvent evt, Action after)
        {
            if (FactionColonies.Settings().settlementsAutoBattle)
            {
                var won = SimulateBattleFc.FightBattle(evt.militaryForceAttacking, evt.militaryForceDefending) == 1;
                endBattle(won, (int) evt.militaryForceDefending.forceRemaining);
                return;
            }

            if (defenderForce == null)
            {
                endBattle(false, 0);
                return;
            }

            LongEventHandler.QueueLongEvent(() =>
                {
                    if (Map == null)
                        MapGenerator.GenerateMap(new IntVec3(70 + settlement.settlementLevel * 10,
                                1, 70 + settlement.settlementLevel * 10), this,
                            MapGeneratorDef, ExtraGenStepDefs).mapDrawer.RegenerateEverythingNow();

                    zoomIntoTile(evt);
                    after.Invoke();
                },
                "GeneratingMap", false, GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap);
        }

        public override void Tick()
        {
            base.Tick();
            trader?.TraderTrackerTick();
        }

        private void zoomIntoTile(FCEvent evt)
        {
            SoundDefOf.Tick_High.PlayOneShotOnCamera();
            if (Current.Game.CurrentMap != Map && !defenders.Any())
            {
                if (evt == null)
                {
                    Log.Warning("Aborting defense, null FCEvent!");
                    return;
                }

                evt.timeTillTrigger = Find.TickManager.TicksGame;
                var force = MilitaryUtilFC.returnDefendingMilitaryForce(evt);
                if (force == null) return;

                force.homeSettlement.militaryBusy = true;

                foreach (var building in Map.listerBuildings.allBuildingsColonist)
                    FloodFillerFog.FloodUnfog(building.InteractionCell, Map);

                generateFriendlies(force);
            }

            if (Current.Game.CurrentMap == Map && Find.World.renderer.wantedMode != WorldRenderMode.Planet) return;

            if (defenders.Any())
                CameraJumper.TryJump(new GlobalTargetInfo(defenders[0]));
            else if (Map.mapPawns.AllPawnsSpawned.Any())
                CameraJumper.TryJump(new GlobalTargetInfo(Map.mapPawns.AllPawnsSpawned[0]));
            else
                CameraJumper.TryJump(new IntVec3(Map.Size.x / 2, 0, Map.Size.z / 2), Map);
        }

        public override void Notify_CaravanFormed(Caravan caravan)
        {
            var foundCaravan = new List<CaravanSupporting>();
            foreach (var found in caravan.pawns)
            {
                if (found.GetLord() != null) found.GetLord().ownedPawns.Remove(found);

                foreach (var caravanSupporting in
                    supporting.Where(caravanSupporting => caravanSupporting.pawns.Contains(found)))
                {
                    foundCaravan.Add(caravanSupporting);
                    caravanSupporting.pawns.Remove(found);
                    break;
                }
            }

            foreach (var caravanSupporting in foundCaravan.Where(caravanSupporting =>
                    caravanSupporting.pawns.Find(pawn => !pawn.Downed &&
                                                         !pawn.Dead && !pawn.AnimalOrWildMan()) == null))
                //Prevent removing while creating end battle caravans
                if (settlement.isUnderAttack)
                    supporting.Remove(caravanSupporting);
            /*It appears vanilla handles this automatically
                foreach (Pawn animal in caravanSupporting.supporting.FindAll(pawn => pawn.AnimalOrWildMan()))
                {
                    animal.holdingOwner = null;
                    animal.DeSpawn();
                    Find.WorldPawns.PassToWorld(animal);
                    caravan.pawns.TryAdd(animal);
                }*/

            //Appears to not happen sometimes, no clue why
            foreach (var pawn in caravan.pawns) Map.reservationManager.ReleaseAllClaimedBy(pawn);

            base.Notify_CaravanFormed(caravan);
        }

        public static IntVec3 FindNearEdgeCell(Map map)
        {
            bool BaseValidator(IntVec3 x)
            {
                return x.Standable(map) && !x.Fogged(map);
            }

            var hostFaction = map.ParentFaction;
            if (CellFinder.TryFindRandomEdgeCellWith(x =>
            {
                if (!BaseValidator(x))
                    return false;
                if (hostFaction != null && map.reachability.CanReachFactionBase(x, hostFaction))
                    return true;
                return hostFaction == null && map.reachability.CanReachBiggestMapEdgeDistrict(x);
            }, map, CellFinder.EdgeRoadChance_Neutral, out var result))
                return CellFinder.RandomClosewalkCellNear(result, map, 5);
            if (CellFinder.TryFindRandomEdgeCellWith(BaseValidator, map, CellFinder.EdgeRoadChance_Neutral, out result))
                return CellFinder.RandomClosewalkCellNear(result, map, 5);
            Log.Warning("Could not find any valid edge cell.");
            return CellFinder.RandomCell(map);
        }

        private void generateFriendlies(militaryForce force)
        {
            var points = (float) (force.militaryLevel * force.militaryEfficiency * 100);
            List<Pawn> friendlies;
            var riders = new Dictionary<Pawn, Pawn>();
            if (force.homeSettlement.militarySquad != null &&
                force.homeSettlement.militarySquad.mercenaries.Any())
            {
                var squad = force.homeSettlement.militarySquad;

                squad.OutfitSquad(squad.settlement.militarySquad.outfit);
                squad.updateSquadStats(squad.settlement.settlementMilitaryLevel);
                squad.resetNeeds();

                friendlies = squad.AllEquippedMercenaryPawns.ToList();

                foreach (var animal in squad.animals) riders.Add(animal.handler.pawn, animal.pawn);
            }
            else
            {
                var parms = new IncidentParms
                {
                    target = Map,
                    faction = FactionColonies.getPlayerColonyFaction(),
                    generateFightersOnly = true,
                    raidStrategy = RaidStrategyDefOf.ImmediateAttackFriendly
                };
                parms.points = IncidentWorker_Raid.AdjustedRaidPoints(points,
                    PawnsArrivalModeDefOf.EdgeWalkIn, parms.raidStrategy,
                    parms.faction, PawnGroupKindDefOf.Combat);
                friendlies = PawnGroupMakerUtility.GeneratePawns(
                    IncidentParmsUtility.GetDefaultPawnGroupMakerParms(
                        PawnGroupKindDefOf.Combat, parms, true)).ToList();
                if (!friendlies.Any()) Log.Error("Got no pawns spawning raid from parms " + parms);
            }

            void tryFindLoc(out IntVec3 loc, Pawn friendly)
            {
                var min = (70 + settlement.settlementLevel * 10) / 2 - 5 - 5 * settlement.settlementLevel;
                var size = 10 + settlement.settlementLevel * 10;
                CellFinder.TryFindRandomCellInsideWith(new CellRect(min, min, size, size),
                    testing => testing.Standable(Map) && Map.reachability.CanReachMapEdge(testing,
                        TraverseParms.For(TraverseMode.PassDoors)), out loc);
                if (loc.x == -1000)
                {
                    Log.Message("Failed with " + friendly + ", " + loc);
                    CellFinder.TryFindRandomCellNear(new IntVec3(min + 10 + settlement.settlementLevel, 1,
                            min + 10 + settlement.settlementLevel), Map, 75,
                        testing => testing.Standable(Map), out loc);
                }
            }

            foreach (var friendly in friendlies)
            {
                if (friendly.IsWildMan()) continue;

                friendly.ApplyIdeologyRitualWounds();

                IntVec3 loc;
                if (friendly.AnimalOrWildMan())
                {
                    if (riders.Count > 0)
                    {
                        try
                        {
                            var owner = riders.First(pair => pair.Value.thingIDNumber == friendly.thingIDNumber).Key;
                            CellFinder.TryFindRandomCellInsideWith(new CellRect((int) owner.DrawPos.x - 5,
                                    (int) owner.DrawPos.z - 5, 10, 10),
                                testing => testing.Standable(Map) && Map.reachability.CanReachMapEdge(testing,
                                    TraverseParms.For(TraverseMode.PassDoors)), out loc);
                        }
                        catch
                        {
                            var isAnimal = friendly.RaceProps.Animal ? "animal" : "human";
                            Log.Error("No pair found for " + isAnimal + ": " + friendly.thingIDNumber +
                                      ", and riders dictionary is not empty!");
                            continue;
                        }
                    }
                    else
                    {
                        Log.Error("Rider Dictionary is empty but animal was still generated?");
                        continue;
                    }
                }
                else
                {
                    tryFindLoc(out loc, friendly);
                }

                GenSpawn.Spawn(friendly, loc, Map, new Rot4());
                friendly.drafter = new Pawn_DraftController(friendly);


                Map.mapPawns.RegisterPawn(friendly);
                friendly.drafter.Drafted = true;
            }

            LordMaker.MakeNewLord(FactionColonies.getPlayerColonyFaction(), new LordJob_DefendColony(riders), Map,
                friendlies);

            defenders = friendlies;
        }

        private void endBattle(bool won, int remaining)
        {
            var faction = Find.World.GetComponent<FactionFC>();

            // Log.Message("Handling combat resolution...");
            try
            {
                if (won)
                {
                    WinBattle(faction);
                }
                else
                {
                    LoseBattle(faction);
                }
                // Log.Message("Handling foreign defenders...");
                CooldownMilitary(remaining);
            }
            catch (Exception e)
            {
                Log.Error($"Encountered an error while trying to resolve combat in Empire{System.Environment.NewLine}{e}");
            }
            settlement.isUnderAttack = false;
        }

        private void CooldownMilitary(int remaining)
        {
            if (defenderForce?.homeSettlement == settlement)
            {
                defenderForce?.homeSettlement?.cooldownMilitary();
            }
            else if (defenderForce == null)
            {
                // Log.Message("Defending force not set-- if the attack came from another mod, this is fine.");
            }
            else
            {
                // if not the home settlement defending
                if (remaining >= 7)
                {
                    Find.LetterStack.ReceiveLetter("OverwhelmingVictory".Translate(),
                        "OverwhelmingVictoryDesc".Translate(), LetterDefOf.PositiveEvent);
                    defenderForce.homeSettlement.returnMilitary(true);
                }
                else
                {
                    defenderForce.homeSettlement.cooldownMilitary();
                }
            }
        }

        private void LoseBattle(FactionFC faction)
        {
            //get multipliers
            var happinessLostMultiplier =
                TraitUtilsFC.cycleTraits("happinessLostMultiplier",
                    settlement.traits, Operation.Multiplication) *
                TraitUtilsFC.cycleTraits("happinessLostMultiplier", faction.traits, Operation.Multiplication);
            var loyaltyLostMultiplier =
                TraitUtilsFC.cycleTraits("loyaltyLostMultiplier", settlement.traits,
                    Operation.Multiplication) * TraitUtilsFC.cycleTraits("loyaltyLostMultiplier",
                    faction.traits, Operation.Multiplication);

            var muliplier = 1;
            if (faction.hasPolicy(FCPolicyDefOf.feudal))
                muliplier = 2;
            float prosperityMultiplier = 1;
            var canDestroyBuildings = true;
            if (faction.hasTrait(FCPolicyDefOf.resilient))
            {
                prosperityMultiplier = .5f;
                canDestroyBuildings = false;
            }

            // Log.Message("Determined Multipliers for loss penalty");
            // if winner are enemies
            settlement.prosperity -= 20 * prosperityMultiplier;
            settlement.happiness -= 25 * happinessLostMultiplier;
            settlement.loyalty -= 15 * loyaltyLostMultiplier * muliplier;

            string str = "DefenseFailureFull".Translate(settlement.name);


            for (var k = 0; k < 4; k++)
            {
                var deconstructRoll = new IntRange(0, 10).RandomInRange;
                var deconstructChance = 7;
                if (deconstructRoll < deconstructChance || settlement.buildings[k].defName == "Empty" ||
                    settlement.buildings[k].defName == "Construction" || !canDestroyBuildings) continue;
                str += "\n" +
                       "BuildingDestroyedInRaid".Translate(settlement.buildings[k].label);
                settlement.deconstructBuilding(k);
            }

            // Log.Message("Building deconstruction handled");
            // level remover checker
            if (settlement.settlementLevel > 1 && canDestroyBuildings)
            {
                var num = new IntRange(0, 10).RandomInRange;
                if (num >= 7)
                {
                    str += "\n\n" + "SettlementDeleveledRaid".Translate();
                    settlement.delevelSettlement();
                }
            }

            // Log.Message("Settlement deleveling handled");
            Find.LetterStack.ReceiveLetter("DefenseFailure".Translate(), str, LetterDefOf.Death,
                new LookTargets(this));
        }

        private void WinBattle(FactionFC faction)
        {
            faction.addExperienceToFactionLevel(5f);
            Find.LetterStack.ReceiveLetter("DefenseSuccessful".Translate(),
                "DefenseSuccessfulFull".Translate(settlement.name),
                LetterDefOf.PositiveEvent, new LookTargets(this));
        }

        private void endAttack()
        {
            endBattle(defenders.Any(), defenders.Count);
            deleteMap();

            supporting.Clear();
            defenders.Clear();
            defenderForce = null;
            attackers.Clear();
            attackerForce = null;
        }

        public void removeAttacker(Pawn downed)
        {
            attackers.Remove(downed);
            if (attackers.Any()) return;
            LongEventHandler.QueueLongEvent(endAttack,
                "EndingAttack", false, error =>
                {
                    DelayedErrorWindowRequest.Add("ErrorEndingAttack".Translate(),
                        "ErrorEndingAttackDescription".Translate());
                    Log.Error(error.Message);
                });
        }

        public void removeDefender(Pawn defender)
        {
            defenders.Remove(defender);
            if (defenders.Any()) return;
            LongEventHandler.QueueLongEvent(endAttack,
                "EndingAttack", false, error =>
                {
                    DelayedErrorWindowRequest.Add("ErrorEndingAttack".Translate(),
                        "ErrorEndingAttackDescription".Translate());
                    Log.Error(error.Message);
                });
        }
    }

    [HarmonyPatch(typeof(Pawn), "GetGizmos")]
    internal class PawnGizmos
    {
        private static void Postfix(ref Pawn __instance, ref IEnumerable<Gizmo> __result)
        {
            var output = __result.ToList();
            if (__result == null || __instance?.Faction == null || !output.Any() ||
                !(__instance.Map.Parent is WorldSettlementFC))
                return;

            var found = __instance;
            var pawnDraftController = __instance.drafter ?? new Pawn_DraftController(__instance);

            var settlementFc = (WorldSettlementFC) __instance.Map.Parent;
            if (__instance.Faction.Equals(FactionColonies.getPlayerColonyFaction()))
            {
                var draftColonists = new Command_Toggle
                {
                    hotKey = KeyBindingDefOf.Command_ColonistDraft,
                    isActive = () => false,
                    toggleAction = () =>
                    {
                        if (pawnDraftController.pawn.Faction.Equals(Faction.OfPlayer)) return;
                        pawnDraftController.pawn.SetFaction(Faction.OfPlayer);
                        pawnDraftController.Drafted = true;
                    },
                    defaultDesc = "CommandToggleDraftDesc".Translate(),
                    icon = TexCommand.Draft,
                    turnOnSound = SoundDefOf.DraftOn,
                    groupKey = 81729172,
                    defaultLabel = "CommandDraftLabel".Translate()
                };
                if (pawnDraftController.pawn.Downed)
                    draftColonists.Disable("IsIncapped".Translate(
                        (NamedArgument) pawnDraftController.pawn.LabelShort,
                        (NamedArgument) pawnDraftController.pawn));
                draftColonists.tutorTag = "Draft";
                output.Add(draftColonists);
            }
            else if (__instance.Faction.Equals(Faction.OfPlayer) && __instance.Drafted &&
                     !settlementFc.supporting.Any(caravan => caravan.pawns.Any(pawn => pawn.Equals(found))))
            {
                foreach (Command_Toggle action in output.Where(gizmo => gizmo is Command_Toggle))
                {
                    if (action.hotKey != KeyBindingDefOf.Command_ColonistDraft) continue;

                    var index = output.IndexOf(action);
                    action.toggleAction = () =>
                    {
                        found.SetFaction(FactionColonies.getPlayerColonyFaction());
                        //settlementFc.worldSettlement.defenderLord.AddPawn(__instance);
                    };
                    output[index] = action;
                    break;
                }
            }

            __result = output;
        }
    }
}