using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using RimWorld;
using Verse;
using RimWorld.Planet;

namespace FactionColonies
{
	public class FCWindow_Confirm_TaxBreak : FCWindow_Confirm
	{



		public FCWindow_Confirm_TaxBreak(SettlementFC settlement) : base(settlement)
		{
			this.forcePause = false;
			this.draggable = true;
			this.doCloseX = true;
			this.preventCameraMotion = false;
			this.settlement = settlement;
			this.stringConfirm = "FCConfirmTaxBreak".Translate();
		}



		public override void confirm()
		{
			settlement.trait_Egalitarian_TaxBreak_Tick = Find.TickManager.TicksGame;
			settlement.trait_Egalitarian_TaxBreak_Enabled = true;
			Messages.Message(TranslatorFormattedStringExtensions.Translate("FCGivingTaxBreak", this.settlement.name), MessageTypeDefOf.NeutralEvent);
		}



	}
}

