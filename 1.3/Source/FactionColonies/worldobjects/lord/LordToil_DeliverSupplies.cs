﻿using System.Linq;
using FactionColonies.util;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace FactionColonies
{
	class LordToil_DeliverSupplies : LordToil
	{
		public override bool AllowSatisfyLongNeeds => false;

		private bool NoPawnCarries => lord.ownedPawns.All(pawn => pawn.carryTracker.CarriedThing == null);

		private bool sendMessage = false;
		private bool cellIsSet = false;
		public IntVec3 deliveryCell;


		public void SetCell()
		{
			if (!cellIsSet)
			{
				TraverseParms traverseParms = DeliveryEvent.DeliveryTraverseParms;
				traverseParms.pawn = lord.ownedPawns[0];
				deliveryCell = DeliveryEvent.GetDeliveryCell(traverseParms, lord.Map);
				cellIsSet = true;
			}
		}

		public override void UpdateAllDuties()
		{
			for (int i = 0; i < lord.ownedPawns.Count(); i++)
			{
				SetCell();
				Pawn pawn = lord.ownedPawns[i];
				pawn.mindState.canFleeIndividual = true;
				if (!NoPawnCarries)
				{
					if (i == 0)
					{
						pawn.mindState.duty = new PawnDuty(DefDatabase<DutyDef>.GetNamed("FCDeliverItem"))
						{
							focus = deliveryCell
						};
					}
					else
					{
						TraverseParms traverseParms = DeliveryEvent.DeliveryTraverseParms;
						traverseParms.pawn = pawn;
						pawn.mindState.duty = new PawnDuty(DefDatabase<DutyDef>.GetNamed("FCFollowAndDeliverItem"))
						{
							focus = (lord.ownedPawns[0].carryTracker.CarriedThing == null) ? (LocalTargetInfo) deliveryCell : lord.ownedPawns[0]
						};
					}
				}
				else
				{
					if (!sendMessage) 
					{
						Messages.Message("pawnsLeavingMap".Translate(), MessageTypeDefOf.NeutralEvent);
						sendMessage = true;
					}
					pawn.mindState.duty = new PawnDuty(DutyDefOf.ExitMapBestAndDefendSelf);
				}
			};
		}

		public override void Notify_ReachedDutyLocation(Pawn pawn)
		{
			UpdateAllDuties();
			base.Notify_ReachedDutyLocation(pawn);
		}
	}
}

