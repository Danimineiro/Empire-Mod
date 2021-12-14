using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace FactionColonies.util
{
	class DeliveryEvent
	{
		public static TraverseParms DeliveryTraverseParms => new TraverseParms()
		{
			canBashDoors = false,
			canBashFences = false,
			alwaysUseAvoidGrid = false,
			fenceBlocked = false,
			maxDanger = Danger.Deadly,
			mode = TraverseMode.ByPawn
		};

		public static void CreateDeliveryEvent(List<Thing> things, int source, Letter let = null, Message msg = null)
		{
			CreateDeliveryEvent(new FCEvent()
			{
				source = source,
				goods = things,
				customDescription = "",
				timeTillTrigger = Find.TickManager.TicksGame + 10,
				let = let,
				msg = msg
			}); 
		}

		public static void Action(FCEvent evt)
		{
			Action(evt, Find.World.GetComponent<FactionFC>().settlements.FirstOrFallback(settlement => settlement.mapLocation == evt.source)?.traits.Contains(FCTraitEffectDefOf.shuttlePort) ?? false);
		}

		public static void Action(FCEvent evt, Letter let = null, Message msg = null, bool CanUseShuttle = false)
		{
			evt.let = let;
			evt.msg = msg;
			Action(evt, CanUseShuttle || (Find.World.GetComponent<FactionFC>().settlements.FirstOrFallback(settlement => settlement.mapLocation == evt.source)?.traits.Contains(FCTraitEffectDefOf.shuttlePort) ?? false));
		}

		private static void MakeDeliveryLetterAndMessage(FCEvent evt)
		{
			try
			{
				if (evt.let != null)
				{
					evt.let.lookTargets = evt.goods;
					Find.LetterStack.ReceiveLetter(evt.let);
				}
				else
				{
					Find.LetterStack.ReceiveLetter("GoodsReceivedFollowing".Translate(evt.def.label.ToLower()), evt.goods.ToLetterString(), LetterDefOf.PositiveEvent, evt.goods);
				}

				if (evt.msg != null)
				{
					evt.msg.lookTargets = evt.goods;
					Messages.Message(evt.msg);
				}
				
				if (evt.isDelayed) Messages.Message("deliveryHeldUpArriving".Translate(), evt.goods, MessageTypeDefOf.PositiveEvent);
			} 
			catch
			{
				Log.ErrorOnce("MakeDeliveryLetterAndMessage failed to attach targets to the message", 908347458);
			}
		}

		private static void SendShuttle(FCEvent evt)
		{
			Map playerHomeMap = Find.World.GetComponent<FactionFC>().TaxMap;
			List<ShipLandingArea> landingZones = ShipLandingBeaconUtility.GetLandingZones(playerHomeMap);

			IntVec3 landingCell = DropCellFinder.GetBestShuttleLandingSpot(playerHomeMap, Faction.OfPlayer);

			if (!landingZones.Any() || landingZones.Any(zone => zone.Clear))
			{
				MakeDeliveryLetterAndMessage(evt);
				Thing shuttle = ThingMaker.MakeThing(ThingDefOf.Shuttle);
				TransportShip transportShip = TransportShipMaker.MakeTransportShip(TransportShipDefOf.Ship_Shuttle, evt.goods, shuttle);

				transportShip.ArriveAt(landingCell, playerHomeMap.Parent);
				transportShip.AddJobs(new ShipJobDef[]
				{
								ShipJobDefOf.Unload,
								ShipJobDefOf.FlyAway
				});
			}
			else
			{
				if (!evt.isDelayed)
				{
					Messages.Message(((string)"shuttleLandingBlockedWithItems".Translate(evt.goods.ToLetterString())).Replace("\n", " "), MessageTypeDefOf.RejectInput);
					evt.isDelayed = true;
				}

				if (evt.source == -1) evt.source = playerHomeMap.Tile;

				evt.timeTillTrigger = Find.TickManager.TicksGame + 1000;
				CreateDeliveryEvent(evt);
			}
		}

		private static void SendDropPod(FCEvent evt)
		{
			Map playerHomeMap = Find.World.GetComponent<FactionFC>().TaxMap;
			MakeDeliveryLetterAndMessage(evt);
			DropPodUtility.DropThingsNear(DropCellFinder.TradeDropSpot(playerHomeMap), playerHomeMap, evt.goods, 110, false, false, false, false);
		}

		private static bool DoDelayCaravanDueToDanger(FCEvent evt)
		{
			Map playerHomeMap = Find.World.GetComponent<FactionFC>().TaxMap;
			if (playerHomeMap.dangerWatcher.DangerRating != StoryDanger.None)
			{

				if (!evt.isDelayed)
				{
					Messages.Message(((string)"caravanDangerTooHighWithItems".Translate(evt.goods.ToLetterString())).Replace("\n", " "), MessageTypeDefOf.RejectInput);
					evt.isDelayed = true;
				}

				if (evt.source == -1) evt.source = playerHomeMap.Tile;

				evt.timeTillTrigger = Find.TickManager.TicksGame + 1000;
				CreateDeliveryEvent(evt);
				return true;
			}

			return false;
		}

		private static void SendCaravan(FCEvent evt)
		{
			Map playerHomeMap = Find.World.GetComponent<FactionFC>().TaxMap;
			if (DoDelayCaravanDueToDanger(evt)) return;

			MakeDeliveryLetterAndMessage(evt);
			List<Pawn> pawns = new List<Pawn>();
			while (evt.goods.Count() > 0)
			{
				Pawn pawn = PawnGenerator.GeneratePawn(FCPawnGenerator.WorkerOrMilitaryRequest());
				Thing next = evt.goods.First();

				if (pawn.carryTracker.innerContainer.TryAdd(next))
				{
					evt.goods.Remove(next);
				}

				pawns.Add(pawn);
			}

			PawnsArrivalModeWorker_EdgeWalkIn pawnsArrivalModeWorker = new PawnsArrivalModeWorker_EdgeWalkIn();
			IncidentParms parms = StorytellerUtility.DefaultParmsNow(IncidentCategoryDefOf.FactionArrival, playerHomeMap);
			parms.spawnRotation = Rot4.FromAngleFlat((((Map)parms.target).Center - parms.spawnCenter).AngleFlat);

			RCellFinder.TryFindRandomPawnEntryCell(out parms.spawnCenter, playerHomeMap, CellFinder.EdgeRoadChance_Friendly);

			pawnsArrivalModeWorker.Arrive(pawns, parms);
			LordMaker.MakeNewLord(FCPawnGenerator.WorkerOrMilitaryRequest().Faction, new LordJob_DeliverSupplies(parms.spawnCenter), playerHomeMap, pawns);

		}

		private static void SpawnOnTaxSpot(FCEvent evt)
		{
			MakeDeliveryLetterAndMessage(evt);
			evt.goods.ForEach(thing => PaymentUtil.placeThing(thing));
		}

		public static TaxDeliveryMode TaxDeliveryModeForSettlement(bool canUseShuttle)
		{ 
			if (FactionColonies.Settings().forcedTaxDeliveryMode != default)
			{
				return FactionColonies.Settings().forcedTaxDeliveryMode;
			}

			if (DefDatabase<ResearchProjectDef>.GetNamed("TransportPod").IsFinished)
			{
				if (ModsConfig.RoyaltyActive && canUseShuttle)
				{
					return TaxDeliveryMode.Shuttle;
				}
				return TaxDeliveryMode.DropPod;
			}
			return TaxDeliveryMode.Caravan;
		}

		public static void Action(FCEvent evt, bool canUseShuttle = false)
		{
			try
			{
				TaxDeliveryMode taxDeliveryMode = TaxDeliveryModeForSettlement(canUseShuttle);

				switch (taxDeliveryMode)
				{
					case TaxDeliveryMode.Caravan:
						SendCaravan(evt);
						break;
					case TaxDeliveryMode.DropPod:
						SendDropPod(evt);
						break;
					case TaxDeliveryMode.Shuttle:
						SendShuttle(evt);
						break;
					default:
						SpawnOnTaxSpot(evt);
						break;
				}
			} 
			catch(Exception e)
			{
				Log.ErrorOnce("Critical delivery failure, spawning things on tax spot instead! Message: " + e.Message + " StackTrace: " + e.StackTrace + " Source: " + e.Source, 77239232);
				evt.goods.ForEach(thing => PaymentUtil.placeThing(thing));
			}
		}

		public static void CreateDeliveryEvent(FCEvent evtParams)
		{
			FCEvent evt = FCEventMaker.MakeEvent(FCEventDefOf.deliveryArrival);
			evt.source = evtParams.source;
			evt.goods = evtParams.goods;
			evt.classToRun = "FactionColonies.util.DeliveryEvent";
			evt.classMethodToRun = "Action";
			evt.passEventToClassMethodToRun = true;
			evt.customDescription = evtParams.customDescription;
			evt.hasCustomDescription = true;
			evt.timeTillTrigger = evtParams.timeTillTrigger;
			evt.let = evtParams.let;
			evt.msg = evtParams.msg;
			evt.isDelayed = evtParams.isDelayed;

			Find.World.GetComponent<FactionFC>().addEvent(evt);
		}

		public static string ShuttleEventInjuredString
		{
			get
			{
				if (DefDatabase<ResearchProjectDef>.GetNamed("TransportPod", false).IsFinished)
				{
					if (ModsConfig.RoyaltyActive)
					{
						return "transportingInjuredShuttle".Translate();
					}
					return "transportingInjuredDropPod".Translate();
				}
				return "transportingInjuredCaravan".Translate();
			}
		}
		
		public static IntVec3 GetDeliveryCell(TraverseParms traverseParms, Map map)
		{
			if (!PaymentUtil.checkForTaxSpot(map, out IntVec3 intVec3))
			{
				intVec3 = ValidLandingCell(new IntVec2(1, 1), map, true);
			}

			IntVec3 oldVec = intVec3;
			for (int i = 0; i < 10; i++)
			{
				if (CellFinder.TryFindRandomReachableCellNear(intVec3, map, i, traverseParms, cell => map.thingGrid.ThingsAt(cell) == null, null, out intVec3))
				{
					break;
				}

				if (i == 9)
				{
					intVec3 = oldVec;
				}

			}

			return intVec3;
		}

		private static IntVec3 ValidLandingCell(IntVec2 requiredSpace, Map map, bool canLandRoofed = false)
		{
			IEnumerable<IntVec3> validCells = map.areaManager.Home.ActiveCells.Where(cell => (!map.roofGrid.Roofed(cell) || canLandRoofed) && cell.CellFulfilsSpaceRequirementForSkyFaller(requiredSpace, map));

			if (validCells.Count() == 0)
			{
				validCells = map.areaManager.Home.ActiveCells.Where(cell => cell.CellFulfilsSpaceRequirementForSkyFaller(requiredSpace, map));
			}

			if (validCells.Count() == 0)
			{
				validCells = map.AllCells.Where(cell => !map.areaManager.Home.ActiveCells.Contains(cell));
			}

			if (validCells.Count() == 0)
			{
				validCells = map.AllCells;
			}

			return validCells.RandomElement();
		}
	}

	public enum TaxDeliveryMode
	{
		None,
		TaxSpot,
		Caravan,
		DropPod,
		Shuttle
	}
}
