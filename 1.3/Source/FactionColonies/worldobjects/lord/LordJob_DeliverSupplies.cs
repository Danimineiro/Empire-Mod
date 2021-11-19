﻿using System.Collections.Generic;
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
		private IntVec3 fallbackLocation;

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
		}

		private bool CanNotReach() => !lord.ownedPawns.NullOrEmpty() && lord.ownedPawns.All(pawn => pawn.carryTracker.CarriedThing == null) && lord.ownedPawns[0].mindState?.duty.def != DutyDefOf.ExitMapBestAndDefendSelf && lord.ownedPawns[0].CanReach(lord.ownedPawns[0].CurJob.targetA, PathEndMode.OnCell, PawnUtility.ResolveMaxDanger(lord.ownedPawns[0], Danger.Some), false, false, TraverseMode.ByPawn);

		private Transition DeliveryToFightTransition(StateGraph stateGraph) => new Transition(stateGraph.StartingToil, stateGraph.lordToils[1])
		{
			triggers = new List<Trigger>(2)
			{
				new Trigger_PawnHarmed(),
				new Trigger_Custom((TriggerSignal s) => Map.dangerWatcher.DangerRating == StoryDanger.High)
			},
			preActions = new List<TransitionAction>
			{
				new TransitionAction_Custom(() => Messages.Message("deliveryPawnsEngageEnemy".Translate(), lord.ownedPawns, MessageTypeDefOf.NeutralEvent))
			}
		};

		/// <summary>
		/// This <c>Transition</c> fixes a bug, where sometimes some <c>Pawn</c>s don't get transitioned into this phase even though they should
		/// </summary>
		/// <param name="stateGraph"></param>
		/// <returns></returns>
		private Transition RefreshFightTransition(StateGraph stateGraph) => new Transition(stateGraph.lordToils[1], stateGraph.lordToils[1], true)
		{
			triggers = new List<Trigger>(2)
			{
				new Trigger_PawnHarmed(),
				new Trigger_Custom((TriggerSignal _) => Map.dangerWatcher.DangerRating == StoryDanger.High)
			}
		};

		private Transition FightingToLeavingTransition(StateGraph stateGraph) => new Transition(stateGraph.lordToils[1], stateGraph.lordToils[2])
		{
			triggers = new List<Trigger>
			{
				new Trigger_TicksPassedAndNoRecentHarm(1000)
			},
			preActions = new List<TransitionAction>
			{
				new TransitionAction_Custom(() => Messages.Message(Map.HasWoundedForFaction(lord.faction) ? "pawnsLeavingMapWithDowned".Translate() : "pawnsLeavingMap".Translate(), lord.ownedPawns, MessageTypeDefOf.NeutralEvent))
			}
		};

		/// <summary>
		/// This <c>Transition</c> covers the case in which the delivering pawns can't find a path to the delivery tax spot, for example if the locks mod is used.
		/// Without this transition, pawns end up dropping their delivery and starving to death
		/// </summary>
		/// <param name="stateGraph"></param>
		/// <returns></returns>
		private Transition CanNotDeliverToLeavingTransition(StateGraph stateGraph) => new Transition(stateGraph.lordToils[0], stateGraph.lordToils[2])
		{
			triggers = new List<Trigger>(1) { new Trigger_Custom((TriggerSignal _) => CanNotReach()) },
			preActions = new List<TransitionAction>(1) { new TransitionAction_Custom(() => Messages.Message("pawnsLeavingMapNoPath".Translate(), lord.ownedPawns, MessageTypeDefOf.NeutralEvent)) }
		};

		public override StateGraph CreateGraph()
		{
			StateGraph stateGraph = new StateGraph { StartingToil = new LordToil_DeliverSupplies() };

			stateGraph.AddToil(new LordToil_HuntEnemies(fallbackLocation));
			stateGraph.AddToil(new LordToil_TakeWoundedAndLeave());

			stateGraph.AddTransition(DeliveryToFightTransition(stateGraph));
			stateGraph.AddTransition(RefreshFightTransition(stateGraph));
			stateGraph.AddTransition(FightingToLeavingTransition(stateGraph));
			stateGraph.AddTransition(CanNotDeliverToLeavingTransition(stateGraph));

			return stateGraph;
		}
	}
}
