using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI.Group;
using FactionColonies.util;
using System.Linq;
using Verse.AI;

namespace FactionColonies
{
	class LordJob_DeliverSupplies : LordJob
	{
		private IntVec3 fallbackLocation = IntVec3.Invalid;
		private IntVec3 defendPosition = IntVec3.Invalid;

		public LordJob_DeliverSupplies()
		{
		}

		public LordJob_DeliverSupplies(IntVec3 fallbackLocation)
		{
			this.fallbackLocation = fallbackLocation;
		}

		public override void ExposeData()
		{
			base.ExposeData();
			Scribe_Values.Look(ref fallbackLocation, "fallbackLocation", default, false);
			Scribe_Values.Look(ref defendPosition, "defendPosition", default, false);
		}

		private bool CanNotReach() => !lord.ownedPawns.NullOrEmpty() && lord.ownedPawns.All(pawn => pawn.carryTracker.CarriedThing == null) && lord.CurLordToil is LordToil_DeliverSupplies toil && !toil.LeavingModeEngaged && lord.ownedPawns[0].CanReach(lord.ownedPawns[0].CurJob.targetA, PathEndMode.OnCell, PawnUtility.ResolveMaxDanger(lord.ownedPawns[0], Danger.Some), false, false, TraverseMode.ByPawn);

		/// <summary>
		/// This <c>Transition</c> switches the delivery <c>Pawns</c> from delivery mode to fighting mode. It drops their items if they carry any and notifies the player of what's about to happen 
		/// </summary>
		/// <param name="stateGraph"></param>
		/// <returns>the Transition</returns>
		private Transition DeliveryToFightTransition(StateGraph stateGraph) => new Transition(stateGraph.StartingToil, stateGraph.lordToils[1])
		{
			triggers = new List<Trigger>(2)
			{
				new Trigger_PawnHarmed(),
				new Trigger_Custom((TriggerSignal s) => Map.dangerWatcher.DangerRating == StoryDanger.High)
			},
			preActions = new List<TransitionAction>(2)
			{
				new TransitionAction_Custom(() => lord.ownedPawns.ForEach(pawn => pawn.DropItem(pawn.Position, ThingPlaceMode.Direct, out _))),
				new TransitionAction_Custom(() => Messages.Message("deliveryPawnsHiding".Translate(), lord.ownedPawns, MessageTypeDefOf.NeutralEvent))
			}
		};

		/// <summary>
		/// This <c>Transition</c> fixes a bug, where sometimes some <c>Pawn</c>s don't get transitioned into this phase even though they should
		/// tbh, I (dani) don't know if this is still needed, but it doesn't seem to hurt being there either
		/// </summary>
		/// <param name="stateGraph"></param>
		/// <returns>the Transition</returns>
		private Transition RefreshFightTransition(StateGraph stateGraph) => new Transition(stateGraph.lordToils[1], stateGraph.lordToils[1], true)
		{
			triggers = new List<Trigger>(2)
			{
				new Trigger_PawnHarmed(),
				new Trigger_Custom((TriggerSignal _) => Map.dangerWatcher.DangerRating == StoryDanger.High)
			},
			preActions = new List<TransitionAction>
			{
				new TransitionAction_Custom(delegate()
				{
					var traverseParms = DeliveryEvent.DeliveryTraverseParms;
					traverseParms.pawn = lord.ownedPawns[0];
					((LordToil_DefendPoint)stateGraph.lordToils[1]).SetDefendPoint(defendPosition.IsValid ? defendPosition : (defendPosition = DeliveryEvent.GetDeliveryCell(traverseParms, lord.Map)));
				})
            }
		};

		/// <summary>
		/// Transitions from fighting to leaving, when the map has been peaceful for 1000 ticks
		/// </summary>
		/// <param name="stateGraph"></param>
		/// <returns>the Transition</returns>
		private Transition FightingToLeavingTransition(StateGraph stateGraph) => new Transition(stateGraph.lordToils[1], stateGraph.lordToils[2])
		{
			triggers = new List<Trigger>(1)
			{
				new Trigger_TicksPassedAndNoRecentHarm(1000)
			},
			preActions = new List<TransitionAction>(1)
			{
				new TransitionAction_Custom(() => Messages.Message(Map.HasDownedForFaction(lord.faction) ? "deliveryPawnsLeavingMapWithDowned".Translate() : "deliveryPawnsLeavingMap".Translate(), lord.ownedPawns, MessageTypeDefOf.NeutralEvent))
			}
		};

		/// <summary>
		/// This <c>Transition</c> switches the delivery <c>Pawns</c> from leaving mode to fighting mode. It notifies the player of what's about to happen 
		/// </summary>
		/// <param name="stateGraph"></param>
		/// <returns>the Transition</returns>
		private Transition LeavingToFightTransition(StateGraph stateGraph) => new Transition(stateGraph.lordToils[2], stateGraph.lordToils[1])
		{
			triggers = new List<Trigger>(2)
			{
				new Trigger_PawnHarmed(),
				new Trigger_Custom((TriggerSignal s) => Map.dangerWatcher.DangerRating == StoryDanger.High)
			},
			preActions = new List<TransitionAction>(2)
			{
				new TransitionAction_Custom(() => lord.ownedPawns.ForEach(pawn => pawn.DropItem(pawn.Position, ThingPlaceMode.Direct, out _))),
				new TransitionAction_Custom(() => Messages.Message("deliveryPawnsHiding".Translate(), lord.ownedPawns, MessageTypeDefOf.NeutralEvent))
			}
		};

		/// <summary>
		/// This <c>Transition</c> covers the case in which the delivering pawns can't find a path to the delivery tax spot, for example if the locks mod is used.
		/// Without this transition, pawns end up dropping their delivery and starving to death
		/// </summary>
		/// <param name="stateGraph"></param>
		/// <returns>the Transition</returns>
		private Transition CanNotDeliverToLeavingTransition(StateGraph stateGraph) => new Transition(stateGraph.lordToils[0], stateGraph.lordToils[2])
		{
			triggers = new List<Trigger>(1) { new Trigger_Custom((TriggerSignal _) => CanNotReach()) },
			preActions = new List<TransitionAction>(1) { new TransitionAction_Custom(() => Messages.Message("deliveryPawnsLeavingMapNoPath".Translate(), lord.ownedPawns, MessageTypeDefOf.NeutralEvent)) }
		};

		public override StateGraph CreateGraph()
		{
			StateGraph stateGraph = new StateGraph { StartingToil = new LordToil_DeliverSupplies() };

			stateGraph.AddToil(new LordToil_DefendPoint(fallbackLocation));
			stateGraph.AddToil(new LordToil_RecoverWoundedAndLeave(new LordToilData_ExitMap() { canDig = false, locomotion = LocomotionUrgency.Sprint, interruptCurrentJob = true }));

			stateGraph.AddTransition(DeliveryToFightTransition(stateGraph));
			stateGraph.AddTransition(RefreshFightTransition(stateGraph));
			stateGraph.AddTransition(FightingToLeavingTransition(stateGraph));
			stateGraph.AddTransition(LeavingToFightTransition(stateGraph));
			stateGraph.AddTransition(CanNotDeliverToLeavingTransition(stateGraph));

			return stateGraph;
		}
	}
}
