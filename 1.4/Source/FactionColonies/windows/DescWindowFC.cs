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
	public class DescWindowFc : Window
	{

		public override Vector2 InitialSize => new Vector2(400f, 320f);

		//declare variables

		//private int xspacing = 60;
		private int yspacing = 30;
		private int yoffset = 90;
		//private int headerSpacing = 30;
		private int length = 360;
		private int xoffset = 0;
		private int height = 220;



		public string desc;
		public string header;


		public DescWindowFc()
		{
			this.forcePause = false;
			this.draggable = true;
			this.doCloseX = true;
			this.preventCameraMotion = false;
		}

		public DescWindowFc(string desc, string header)
		{
			this.forcePause = false;
			this.draggable = true;
			this.doCloseX = true;
			this.preventCameraMotion = false;
			this.desc = desc;
			this.header = header;
		}

		public DescWindowFc(string desc)
		{
			this.forcePause = false;
			this.draggable = true;
			this.doCloseX = true;
			this.preventCameraMotion = false;
			this.desc = desc;
			this.header = "Description".Translate() + ":";
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
			Widgets.Label(new Rect(2, 0, 300, 60), header);




			//settlement buttons

			Text.Anchor = TextAnchor.UpperLeft;
			Text.Font = GameFont.Tiny;

			//0 tithe total string
			//1 source - -1
			//2 due/delivery date
			//3 Silver (- || +)
			//4 tithe


			Widgets.Label(new Rect(xoffset + 2, yoffset - yspacing + 2, length - 4, height - 4 + yspacing * 2), desc);
			Widgets.DrawBox(new Rect(xoffset, yoffset - yspacing, length, height));

			//reset anchor/font
			Text.Font = fontBefore;
			Text.Anchor = anchorBefore;

		}



	}
}
