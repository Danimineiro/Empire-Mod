using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace FactionColonies.util
{
	class ShuttleSender
	{
		protected readonly int Tile = -1;
		public static readonly int ShuttleRange = 70;
		public WorldSettlementFC settlementFC = null;
		public static readonly int cost = 1;

		public ShuttleSender(int Tile, WorldSettlementFC settlementFC)
		{
			this.Tile = Tile;
			this.settlementFC = settlementFC;
		}

		protected virtual bool TargetHasValidWorldObject(GlobalTargetInfo target) => target.HasWorldObject && target.WorldObject is MapParent mapParent && (mapParent.Map?.mapPawns?.AnyFreeColonistSpawned ?? false);

		public virtual bool ChoseWorldTarget(GlobalTargetInfo target) => target.Tile > -1 && Find.WorldGrid.TraversalDistanceBetween(target.Tile, Tile) <= ShuttleRange && TargetHasValidWorldObject(target);

		protected virtual TransportShip SendWaitingShuttle(MapParent target)
		{
			Thing shuttle = ThingMaker.MakeThing(ThingDefOf.Shuttle);
			shuttle.TryGetComp<CompShuttle>().permitShuttle = true;
			TransportShip transportShip = TransportShipMaker.MakeTransportShip(TransportShipDefOf.Ship_Shuttle, null, shuttle);

			IntVec3 landingCell = DropCellFinder.GetBestShuttleLandingSpot(target.Map, Faction.OfPlayer);
			transportShip.ArriveAt(landingCell, target.Map.Parent);
			transportShip.AddJobs(new ShipJobDef[]
			{
				ShipJobDefOf.WaitForever,
				ShipJobDefOf.Unload,
				ShipJobDefOf.FlyAway
			});

			settlementFC.shuttleUsesRemaining -= cost;
			CameraJumper.TryJump(landingCell, target.Map);
			return transportShip;
		}

		public virtual bool PerformActionWithTarget(GlobalTargetInfo target)
		{
			if (ChoseWorldTarget(target))
			{
				SendWaitingShuttle(target.WorldObject as MapParent);
				return true;
			}
			return false;
		}

		public string TargetingLabelGetter(GlobalTargetInfo target, int tile, int ShuttleRange, IEnumerable<IThingHolder> pods, Action<int, TransportPodsArrivalAction> launchAction) 
		{
			if (!target.IsValid)
            {
				return null;
			}
			if (!target.IsValid)
			{
				return null;
			}

			if (ShuttleRange > 0 && Find.WorldGrid.TraversalDistanceBetween(tile, target.Tile, true, int.MaxValue) > ShuttleRange)
			{
				GUI.color = ColorLibrary.RedReadable;
				return "TransportPodDestinationBeyondMaximumRange".Translate();
			}
			List<FloatMenuOption> source = null;

			try
			{
				source = CompLaunchable.GetOptionsForTile(target.Tile, pods, launchAction).ToList();
            }
            catch
			{
				//There is a bug in base game RimWorld where a shuttle containing Animals and Humans crashes the UI here
				List<IThingHolder> podsAnimalsRemoved = new List<IThingHolder>();
				foreach (IThingHolder thingHolder in pods)
                {
					thingHolder.GetDirectlyHeldThings().RemoveAll(thing => thing.def.race?.Animal ?? false);
				}
				source = CompLaunchable.GetOptionsForTile(target.Tile, pods, launchAction).ToList();
            }

			if (!source.Any())
			{
				return string.Empty;
			}

			if (source.Count() == 1)
			{
				if (source.First().Disabled)
				{
					GUI.color = ColorLibrary.RedReadable;
				}
				return source.First().Label;
			}
			if (target.WorldObject is MapParent mapParent)
            {
                return "ClickToSeeAvailableOrders_WorldObject".Translate(mapParent.LabelCap);
            }
            return "ClickToSeeAvailableOrders_Empty".Translate();
		}

		public virtual string DisplayTargetInformation(GlobalTargetInfo target)
		{
			if (!ChoseWorldTarget(target))
			{
				return "targetAnythingWithColonists".Translate();
			}

			if (target.WorldObject is Caravan)
			{
				return "requestShuttleToCaravan".Translate();
			}

			if (target.WorldObject is Settlement)
			{
				return "requestShuttleToColony".Translate();
			}

			if (target.WorldObject is MapParent)
			{
				return "requestShuttleToMap".Translate();
			}

			return "targetAnythingWithColonists".Translate();
		}

		public void DrawWorldRadiusRing() => GenDraw.DrawWorldRadiusRing(Tile, ShuttleRange);
	}
}
