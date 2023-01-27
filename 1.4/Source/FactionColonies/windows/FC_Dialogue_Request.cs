using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;
using RimWorld.Planet;


namespace FactionColonies
{
    public class FC_Dialogue_Request : Window
    {
        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(250f, 100f);
            }
        }

        public FC_Dialogue_Request()
        {
            //init variables here I guess
            this.forcePause = true;

            this.preventCameraMotion = true;
            this.closeOnAccept = false;
            this.closeOnCancel = false;
            this.closeOnClickedOutside = false;

            
        }

        public FC_Dialogue_Request(string label, string reason)
        {
            this.label = label;
            this.reason = reason;
            this.forcePause = true;
            this.preventCameraMotion = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            //set text anchor and font
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;

            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Small;


            Widgets.Label(new Rect(0,0,250,25), label);
           text = Widgets.TextField(new Rect(0,35,150,25), text);
            if(Widgets.ButtonText(new Rect(155, 35, 60, 25), "Confirm"))
            {
                if (reason == "faction")
                {
                    Find.World.GetComponent<FactionFC>().name = text;
                    FactionColonies.getPlayerColonyFaction().Name = text;
                }
                Find.WindowStack.TryRemove(this);
            }
            

            //Reset Text anchor and font
            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }


        public string text;
        public string label;
        public string reason;


    }
}
