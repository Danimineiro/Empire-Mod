using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using UnityEngine;

namespace FactionColonies
{
    public class FCOptionDef : Def
    {

        public FCOptionDef()
        {
            //Constructor
        }

        //Option Variables
        //Defname
        //label
        public float baseChanceOfSuccess;
        public string affectingVariable = null;
        public int silverCost = 0;
		public FCEventDef parentEvent;
        public FCEventDef successEvent = null;
        public FCEventDef failEvent = null;
    }

	[DefOf]
	public class FCOptionDefOf
	{
		static FCOptionDefOf()
		{
			DefOfHelper.EnsureInitializedInCtor(typeof(FCOptionDefOf));
		}
	}
	
	//==========
	
	public class FCOptionWindow : Window
	{

		public override Vector2 InitialSize
		{
			get
			{
				return new Vector2(580f, 580f);
			}
		}

		//declare variables

		//private int xspacing = 60;
		private int yspacing = 10;
		private int yoffset = 165;
		//private int headerSpacing = 30;
		private int length = 350;
		private int xoffset = 0;
		private int height = 70;
		public List<FCOptionDef> options = new List<FCOptionDef>();
		public string header;
		public string desc;
		public FCEvent parentEvent;

		public FCOptionWindow(FCEventDef evt, FCEvent parentEvent)
		{
			this.forcePause = !FactionColonies.Settings().disableForcedPausingDuringEvents;
			this.draggable = true;
			this.doCloseX = false;
			this.preventCameraMotion = false;
			this.header = evt.label;
			this.options = evt.options;
			if (evt.optionDescription == "")
			{
				this.desc = evt.desc;
			}
			else
			{
				this.desc = evt.optionDescription;
			}
			this.closeOnAccept = false;
			this.closeOnCancel = false;
			this.closeOnClickedOutside = false;
			this.parentEvent = parentEvent;
		}

		public override void PreOpen()
		{
			base.PreOpen();
		}

		public override void WindowUpdate()
		{
			base.WindowUpdate();
		}

		public override void DoWindowContents(Rect inRect)
		{





			//grab before anchor/font
			GameFont fontBefore = Text.Font;
			TextAnchor anchorBefore = Text.Anchor;



			//Settlement Tax Collection Header
			Text.Anchor = TextAnchor.MiddleLeft;
			Text.Font = GameFont.Medium;
			Widgets.DrawMenuSection(new Rect(0, 0, 544, 150));
			Widgets.Label(new Rect(10, 0, 500, 60), header);
			Widgets.DrawLineHorizontal(0, 155, 544);



			Text.Anchor = TextAnchor.UpperLeft;
			Text.Font = GameFont.Tiny;
			Widgets.Label(new Rect(20, 60, 470, 90), desc);


			Text.Anchor = TextAnchor.MiddleLeft;
			Text.Font = GameFont.Tiny;


			

			for(int i = 0; i < options.Count(); i++)
			{
				if(Widgets.ButtonTextSubtle(new Rect(xoffset, yoffset + (i*(height+yspacing)), length, height), ""))
				{
					if (PaymentUtil.getSilver() >= options[i].silverCost)
					{
						PaymentUtil.paySilver(options[i].silverCost);

						FCEventMaker.calculateSuccess(options[i], parentEvent);
						//Log.Message(options[i].label);


						Find.WindowStack.TryRemove(this);
					}
					else
					{
						Messages.Message("You do not have enough silver on the map to pay for that option", MessageTypeDefOf.RejectInput);
					}
				}

				//label
				Widgets.Label(new Rect(xoffset + 5, yoffset + 5 + (i * (height + yspacing)), length-10, height-10), options[i].label);
			}
			//Widgets.DrawBox(new Rect(xoffset, yoffset - yspacing, length, height - yspacing * 2));

			//reset anchor/font
			Text.Font = fontBefore;
			Text.Anchor = anchorBefore;

		}



	}
}
