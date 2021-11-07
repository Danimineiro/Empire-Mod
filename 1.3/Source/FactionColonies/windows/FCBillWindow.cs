using System;
using System.Collections.Generic;
using System.Linq;
using FactionColonies.util;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    class FCBillWindow : Window
    {

        public List<BillFC> bills;
        public FactionFC faction;

        public int scroll;
        public int maxScroll;
        public int scrollBoxHeight = 210;

        public int billHeight = 30;

        //Rect Placements
        Rect billsBox;
        Rect billNameBase;
        Rect billDescBase;
        Rect billLocationBase;
        Rect billTimeRemaining;
        Rect billResolveBase;



        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(628f, 278f);
            }
        }


        public FCBillWindow()
        {
            //Window Information
            faction = Find.World.GetComponent<FactionFC>();
            bills = faction.Bills;

            scroll = 0;
            maxScroll = (bills.Count() * billHeight) - scrollBoxHeight;


            //Window Properties
            forcePause = false;
            draggable = true;
            doCloseX = true;
            preventCameraMotion = false;




            //rect for title
            //Rect titleBox = new Rect(0, 0, 300, 60);
            //rect for box outline
            billsBox = new Rect(0, 30, 590, 212);

            //rect for bill name
            billNameBase = new Rect(0, 0, 200, billHeight);
            //rect for description
            billDescBase = new Rect(billNameBase.width, 0, 80, billHeight);
            //rect for source button
            billLocationBase = new Rect(billDescBase.x + billDescBase.width, 0, 80, billHeight);
            //rect time time remaining
            billTimeRemaining = new Rect(billLocationBase.x + billLocationBase.width, 0, 90, billHeight);
            //rect for bill resolve button
            billResolveBase = new Rect(billTimeRemaining.x + billTimeRemaining.width, 0, 140, billHeight);
        }   
            

        public override void WindowUpdate()
        {
            base.WindowUpdate();
            maxScroll = (bills.Count() * billHeight) - scrollBoxHeight;
        }

        public override void DoWindowContents(Rect inRect)
        {
            //grab before anchor/font
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;

            //top label
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;

            //build outline
            Widgets.DrawMenuSection(billsBox);


            //loop through each bill
            //GoTo Here if change
            Reset:

            int i = 0;

            Text.Anchor = TextAnchor.MiddleCenter;
            foreach (BillFC bill in bills)
            {
                i++;
                Rect settlement = new Rect();
                Rect date = new Rect();
                Rect amount = new Rect();
                Rect tithe = new Rect();
                Rect resolve = new Rect();
                Rect highlight = new Rect();


                settlement = billNameBase;
                date = billDescBase;
                amount = billLocationBase;
                tithe = billTimeRemaining;
                resolve = billResolveBase;

                settlement.y = scroll + billHeight * i;
                date.y = scroll + billHeight * i;
                amount.y = scroll + billHeight * i;
                tithe.y = scroll + billHeight * i;
                resolve.y = scroll + billHeight * i;

                highlight = new Rect(settlement.x, settlement.y, resolve.x + resolve.width, billHeight);

                

                if (i % 2 == 0)
                {
                    Widgets.DrawHighlight(highlight);
                }
                String settlementName;
                if( bill.settlement != null) { settlementName = bill.settlement.name; } else { settlementName = "Null"; }
                if(Widgets.ButtonText(settlement, settlementName))
                {
                    if (bill.settlement != null)
                    {
                        Find.WindowStack.Add(new SettlementWindowFc(bill.settlement));
                    }
                }
                //
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(date, (bill.dueTick - Find.TickManager.TicksGame).ToTimeString());

                //
                Widgets.Label(amount, bill.taxes.silverAmount.ToString());
                //
                bool bul;
                bul = (bill.taxes.itemTithes.Count > 0);
                Widgets.Checkbox(new Vector2(tithe.x + tithe.width/2 - 12, tithe.y), ref bul);

                if (Widgets.ButtonText(resolve, "ResolveBill".Translate()))
                {
                    if (PaymentUtil.getSilver() >= -1 * (bill.taxes.silverAmount) || bill.taxes.silverAmount >= 0)
                    { //if have enough silver on the current map to pay  & map belongs to player
                        FCEventMaker.createTaxEvent(bill);
                        if (bill.taxes.researchCompleted != 0)
                        {
                            faction.researchPointPool += bill.taxes.researchCompleted;
                            Messages.Message("PointsAddedToResearchPool".Translate(bill.taxes.researchCompleted), MessageTypeDefOf.PositiveEvent);
                        }
                        if (bill.taxes.electricityAllotted != 0)
                        {
                            faction.powerPool += bill.taxes.electricityAllotted;
                        }
                        goto Reset;
                    }

                    Messages.Message("NotEnoughSilverOnMapToPayBill".Translate() + "!", MessageTypeDefOf.RejectInput);
                }
            }



            //Top label
            Widgets.ButtonTextSubtle(billNameBase, "Settlement".Translate());
            Widgets.ButtonTextSubtle(billDescBase, "DueFC".Translate());
            Widgets.ButtonTextSubtle(billLocationBase, "Amount".Translate());
            Widgets.ButtonTextSubtle(billTimeRemaining, "HasTithe".Translate());
            if(Widgets.ButtonTextSubtle(billResolveBase, "Auto-Resolve"))
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();

                FloatMenuOption option = new FloatMenuOption("Auto-Resolving : " + faction.autoResolveBills, delegate 
                {
                    faction.autoResolveBills = !faction.autoResolveBills;
                    switch (faction.autoResolveBills)
                    {
                        case true:
                            Messages.Message("Bills are now autoresolving!", MessageTypeDefOf.NeutralEvent);
                            PaymentUtil.autoresolveBills(bills);
                            break;
                        case false:
                            Messages.Message("Bills are now not autoresolving.", MessageTypeDefOf.NeutralEvent);
                            break;
                    }
                    
                });
                list.Add(option);


                FloatMenu menu = new FloatMenu(list);
                Find.WindowStack.Add(menu);
            }
            Widgets.Checkbox(new Vector2(billResolveBase.x + billResolveBase.width - 30, billResolveBase.y + 3), ref faction.autoResolveBills, 24, true);

            //Menu Outline
            Widgets.DrawBox(billsBox);


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
            if (scroll - num * 5 < -1 * maxScroll)
            {
                scroll = -1 * maxScroll;
            }
            else if (scroll - num * 5 > 0)
            {
                scroll = 0;
            }
            else
            {
                scroll -= (int)Event.current.delta.y * 5;
            }
            Event.current.Use();
        }

    }
}
