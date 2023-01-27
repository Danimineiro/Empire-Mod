using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Unity;
using UnityEngine;
using System.Reflection;
using FactionColonies.util;

namespace FactionColonies
{
    class FCPrisonerMenu : Window
    {
        public List<FCPrisoner> prisoners;
        public SettlementFC settlement;
        public FactionFC faction;

        public Vector2 scrollPosition = Vector2.zero;
        public int scrollBoxHeight = 440;

        public int optionHeight = 90;

        public override Vector2 InitialSize => new Vector2(538f, 478f); //19




        public FCPrisonerMenu(SettlementFC settlement)
        {
            //Window Information
            this.faction = Find.World.GetComponent<FactionFC>();
            this.settlement = settlement;
            this.prisoners = settlement.prisonerList;

            //Window Properties
            this.forcePause = false;
            this.draggable = true;
            this.doCloseX = true;
            this.preventCameraMotion = false;
        }

        public override void DoWindowContents(Rect inRect)
        {
            //grab before anchor/font
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;

            //top label
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;



            Text.Anchor = TextAnchor.MiddleLeft;
            var outRect = new Rect(0, 0, inRect.width, inRect.height);
            var viewRect = new Rect(outRect.x, outRect.y, outRect.width - 16f, prisoners.Count * optionHeight);
            Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
            var ls = new Listing_Standard();
            ls.Begin(viewRect);
            int i = 0;
            foreach (FCPrisoner prisoner in prisoners)
            {
                Rect box = ls.GetRect(optionHeight);
                Rect pawnIcon = new Rect(box.x, box.y + 18, 50, 50);
                Rect pawnName = new Rect(box.x + 50, box.y + 4, 300, 20);
                Rect pawnHealth = new Rect(pawnName.x, pawnName.y + 20, 300, 20);
                Rect pawnUnrest = new Rect(pawnHealth.x, pawnHealth.y + 20, 300, 20);
                Rect pawnWorkload = new Rect(pawnUnrest.x, pawnUnrest.y + 20, 150, 20);
                Rect buttonInfo = new Rect(box.xMax - 150 - 10, pawnHealth.y + 20, 150, 20);
                Rect buttonAction = new Rect(box.xMax - 150 - 10, pawnUnrest.y + 20, 150, 20);


                //display stuff now
                Widgets.DrawMenuSection(box);
                //on every other box
                if (i % 2 == 0)
                {
                    Widgets.DrawHighlight(box);
                }

                //show pawn;
                Widgets.ThingIcon(pawnIcon, prisoner.prisoner);
                //Pawn Name
                Widgets.Label(pawnName, prisoner.prisoner.Name.ToString());
                //Pawn Health
                Widgets.Label(pawnHealth, "Health".Translate().CapitalizeFirst() + " " + prisoner.health);
                //Pawn Unrest
                    //Widgets.Label(PawnUnrest, "Unrest".Translate().CapitalizeFirst() + " " + prisoner.unrest);



                //Pawn Workload
                string workload;
                switch (prisoner.workload)
                {
                    case FCWorkLoad.Heavy:
                        workload = "FCHeavy".Translate().CapitalizeFirst();
                        break;
                    case FCWorkLoad.Medium:
                        workload = "FCMedium".Translate().CapitalizeFirst();
                        break;
                    case FCWorkLoad.Light:
                        workload = "FCLight".Translate().CapitalizeFirst();
                        break;
                    default:
                        workload = "null";
                        break;
                }
                if (Widgets.ButtonText(pawnWorkload, "FCWorkload".Translate().CapitalizeFirst() + ": " + workload))
                {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();
                    list.Add(new FloatMenuOption("FCHeavy".Translate().CapitalizeFirst() + " - " + "FCHeavyExplanation".Translate(), delegate
                    {
                        prisoner.workload = FCWorkLoad.Heavy;
                    }));
                    list.Add(new FloatMenuOption("FCMedium".Translate().CapitalizeFirst() + " - " + "FCMediumExplanation".Translate(), delegate
                    {
                        prisoner.workload = FCWorkLoad.Medium;
                    }));
                    list.Add(new FloatMenuOption("FCLight".Translate().CapitalizeFirst() + " - " + "FCLightExplanation".Translate(), delegate
                    {
                        prisoner.workload = FCWorkLoad.Light;
                    }));
                    FloatMenu menu = new FloatMenu(list);
                    Find.WindowStack.Add(menu);
                }

                //Info Button
                if (Widgets.ButtonTextSubtle(buttonInfo, "ViewInfo".Translate()))
                {
                    Pawn pawn = new Pawn();
                    pawn = prisoner.prisoner;

                    if (prisoner.healthTracker != null)
                    {
                        prisoner.prisoner.health = prisoner.healthTracker;
                    }
                    else
                    {
                        prisoner.prisoner.health = new Pawn_HealthTracker(prisoner.prisoner);
                        prisoner.healthTracker = new Pawn_HealthTracker(prisoner.prisoner);
                    }

                    pawn.health = prisoner.healthTracker;


                    Find.WindowStack.Add(new Dialog_InfoCard(pawn));
                }

                //Action button
                if (Widgets.ButtonTextSubtle(buttonAction, "Actions".Translate()))
                {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();

                    list.Add(new FloatMenuOption("SellPawn".Translate() + " $" + prisoner.prisoner.MarketValue + " " + "SellPawnInfo".Translate(), delegate
                    
                    {
                        settlement.addSilverIncome(prisoner.prisoner.MarketValue);

                        //reset window
                        prisoners.Remove(prisoner);
                        WindowUpdate();
                        return;

                    }));

                    list.Add(new FloatMenuOption("ReturnToPlayer".Translate(), delegate
                    {
                        if (prisoner.healthTracker != null)
                        {
                            prisoner.prisoner.health = prisoner.healthTracker;
                        } else
                        {
                            prisoner.prisoner.health = new Pawn_HealthTracker(prisoner.prisoner);
                            prisoner.healthTracker = new Pawn_HealthTracker(prisoner.prisoner);
                        }

                        if (!HealthUtility.TryAnesthetize(prisoner.prisoner)) HealthUtility.DamageUntilDowned(prisoner.prisoner, false);

                        if (prisoner.prisoner.guest == null)
                        {
                            prisoner.prisoner.guest = new Pawn_GuestTracker();
                        }
                        prisoner.prisoner.guest.guestStatusInt = GuestStatus.Prisoner;
                        FieldInfo hostFaction = typeof(Pawn_GuestTracker).GetField("hostFactionInt", BindingFlags.NonPublic | BindingFlags.Instance);
                        hostFaction.SetValue(prisoner.prisoner.guest, Find.FactionManager.OfPlayer);

                        DeliveryEvent.CreateDeliveryEvent(new FCEvent
                        {
                                location = Find.AnyPlayerHomeMap.Tile,
                                source = settlement.mapLocation,
                                planetName = settlement.planetName,
                                goods = new List<Thing> { prisoner.prisoner },
                                customDescription = "aPrisonerIsBeingDeliveredToYou".Translate(),
                                timeTillTrigger = Find.TickManager.TicksGame + FactionColonies.ReturnTicksToArrive(settlement.mapLocation, Find.AnyPlayerHomeMap.Tile)
                        });

                        //reset window
                        prisoners.Remove(prisoner);
                        WindowUpdate();
                        return;
                    }));


                    FloatMenu menu = new FloatMenu(list);
                    Find.WindowStack.Add(menu);
                }
                



                //increment i
                i++;
            }

            ls.End();
            Widgets.EndScrollView();
            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }
    }
}
