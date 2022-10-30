using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public class ListThingFc : Window
    {
        public override Vector2 InitialSize => new Vector2(345f, 380f);

        //declare variables
        public int scroll;
        public int maxScroll;

        private int xspacing = 60;
        private int yspacing = 30;
        private int yoffset = 90;
        private int headerSpacing = 30;
        private int length = 300;
        private int xoffset = 0;
        private int height = 200;

        public List<Thing> list = new List<Thing>();


        public ListThingFc()
        {
            forcePause = false;
            draggable = true;
            doCloseX = true;
            preventCameraMotion = false;
        }

        public ListThingFc(List<Thing> list)
        {
            this.list = list;
            forcePause = false;
            draggable = true;
            doCloseX = true;
            preventCameraMotion = false;
        }

        public override void PreOpen()
        {
            base.PreOpen();
            scroll = 0;
            maxScroll = list.Count * yspacing - height;
        }

        public override void DoWindowContents(Rect inRect)
        {
            //grab before anchor/font
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;

            //Settlement Tax Collection Header
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Medium;
            Widgets.Label(new Rect(2, 0, 300, 60), "TitheList".Translate());


            //settlement buttons

            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;

            //0 tithe total string
            //1 source - -1
            //2 due/delivery date
            //3 Silver (- || +)
            //4 tithe

            List<String> headerList = new List<String>
                {"Name".Translate(), "StackCount".Translate(), "MarketValue".Translate(), "FCTotalValue".Translate()};
            for (int i = 0; i < 4; i++) //-2 to exclude location and ID
            {
                if (i == 0)
                {
                    Widgets.Label(
                        new Rect(xoffset + 2 + i * xspacing, yoffset - yspacing, xspacing + headerSpacing, yspacing),
                        headerList[i]);
                }
                else
                {
                    Widgets.Label(
                        new Rect(xoffset + headerSpacing + 2 + i * xspacing, yoffset - yspacing, xspacing, yspacing),
                        headerList[i]);
                }
            }

            for (int i = 0; i < list.Count(); i++) //browse through tax list
            {
                if (i * yspacing + scroll >= 0 && i * yspacing + scroll <= height)
                {
                    if (i % 2 == 0)
                    {
                        Widgets.DrawHighlight(new Rect(xoffset, yoffset + i * yspacing + scroll, length, yspacing));
                    }

                    for (int k = 0; k < 4; k++) //Browse through thing information
                    {
                        if (k == 0) //name of thing
                        {
                            Widgets.Label(
                                new Rect(xoffset + 2 + k * xspacing, yoffset + i * yspacing + scroll,
                                    xspacing + headerSpacing, yspacing), list[i].def.label); //timedue is date made
                        }
                        else if (k == 1) //number of thing
                        {
                            Widgets.Label(
                                new Rect(xoffset + headerSpacing + 2 + k * xspacing, yoffset + i * yspacing + scroll,
                                    xspacing, yspacing), list[i].stackCount.ToString());
                        }
                        else if (k == 2) //Base market value
                        {
                            Widgets.Label(
                                new Rect(xoffset + headerSpacing + 2 + k * xspacing, yoffset + i * yspacing + scroll,
                                    xspacing, yspacing), list[i].MarketValue.ToString());
                        }
                        else if (k == 3) //Value of thing
                        {
                            Widgets.Label(
                                new Rect(xoffset + headerSpacing + 2 + k * xspacing, yoffset + i * yspacing + scroll,
                                    xspacing, yspacing),
                                "CashSymbol".Translate() + (list[i].stackCount * list[i].MarketValue).ToString());
                        }
                        else //Catch all
                        {
                            Widgets.Label(
                                new Rect(xoffset + headerSpacing + 2 + k * xspacing, yoffset + i * yspacing + scroll,
                                    xspacing, yspacing), "REPORT THIS - listThingFC");
                        }
                    }
                }
            }

            Widgets.DrawBox(new Rect(xoffset, yoffset - yspacing, length, height + yspacing * 2));

            //reset anchor/font
            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;

            if (Event.current.type == EventType.ScrollWheel)
            {
                scrollWindow(Event.current.delta.y);
            }
        }

        private void scrollWindow(float num)
        {
            if (scroll - num * 10 < -1 * maxScroll)
            {
                scroll = -1 * maxScroll;
            }
            else if (scroll - num * 10 > 0)
            {
                scroll = 0;
            }
            else
            {
                scroll -= (int) Event.current.delta.y * 10;
            }

            Event.current.Use();
        }
    }
}