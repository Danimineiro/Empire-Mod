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
	public class FCWindow_Confirm : Window
	{

		public override Vector2 InitialSize
		{
			get
			{
				return new Vector2(438f, 188f);
			}
		}


		public SettlementFC settlement;

		public string stringConfirm;

		Rect label_Title = new Rect(0, 20, 400, 30);
		Rect label_Upper = new Rect(0, 50, 400, 70);

		Rect button_Confirm = new Rect(155, 120, 90, 30);



		public FCWindow_Confirm(SettlementFC settlement)
		{
			this.forcePause = false;
			this.draggable = true;
			this.doCloseX = true;
			this.preventCameraMotion = false;
			this.settlement = settlement;
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


		public virtual void confirm()
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

			Widgets.Label(label_Title, "FCConfirmDecision".Translate());

			Widgets.Label(label_Upper, stringConfirm);

			if(Widgets.ButtonText(button_Confirm, "FCConfirm".Translate()))
			{
				this.confirm();
				this.Close();

			}

			//reset anchor/font
			Text.Font = fontBefore;
			Text.Anchor = anchorBefore;

		}



	}
}

