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
	public class FCWindow_Pay_Silver : Window
	{

		public override Vector2 InitialSize
		{
			get
			{
				return new Vector2(438f, 238f);
			}
		}

		public int silverCount;
		public int selectedSilver;
		public SettlementFC settlement;

		public string stringEffect;

		Rect label_Title = new Rect(0, 20, 400, 30);
		Rect label_Upper = new Rect(0, 50, 400, 30);

		Rect slider = new Rect(50, 70, 300, 30);

		Rect label_Lower = new Rect(0, 90, 400, 30);
		Rect button_Confirm = new Rect(155, 120, 90, 30);



		public FCWindow_Pay_Silver(SettlementFC settlement)
		{
			this.forcePause = false;
			this.draggable = true;
			this.doCloseX = true;
			this.preventCameraMotion = false;
			this.silverCount = PaymentUtil.getSilver();
			this.settlement = settlement;
			this.selectedSilver = 0;
		}

		public override void PreOpen()
		{
			base.PreOpen();
		}

		public override void WindowUpdate()
		{
			base.WindowUpdate();
		}

		public override void OnAcceptKeyPressed()
		{
			base.OnAcceptKeyPressed();
			//faction.title = title;
			

		}

		public virtual float returnValue(int silver)
		{
			float loyalty;
			loyalty = silver / 100;
			return loyalty;
		}

		public virtual void useValue(float value)
		{
			
		}

		public override void DoWindowContents(Rect inRect)
		{





			//grab before anchor/font
			GameFont fontBefore = Text.Font;
			TextAnchor anchorBefore = Text.Anchor;



			//Settlement Tax Collection Header
			Text.Anchor = TextAnchor.MiddleCenter;
			Text.Font = GameFont.Small;

			Widgets.Label(label_Title, "SendSilverToColony".Translate());
			Widgets.Label(label_Upper, TranslatorFormattedStringExtensions.Translate("SendingXSilver", selectedSilver));

			
			selectedSilver = (int)Widgets.HorizontalSlider(slider, selectedSilver, 0, silverCount, roundTo: 1);

			Widgets.Label(label_Lower, TranslatorFormattedStringExtensions.Translate(stringEffect, returnValue(selectedSilver)));

			if(Widgets.ButtonText(button_Confirm, "FCConfirm".Translate()))
			{
				PaymentUtil.paySilver(selectedSilver);
				this.useValue(selectedSilver);
				this.Close();

			}

			//reset anchor/font
			Text.Font = fontBefore;
			Text.Anchor = anchorBefore;

		}



	}
}

