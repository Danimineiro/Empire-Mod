using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using RimWorld;
using Verse;
using RimWorld.Planet;
using FactionColonies.util;
using HarmonyLib;

namespace FactionColonies
{
    class FCEventWindow : Window
    {

        public List<FCEvent> events;
        public FactionFC faction;

        public int scroll = 0;
        public int maxScroll;
        public int scrollBoxHeight = 210;

        public int eventHeight = 30;

        //Rect Placements
        Rect eventsBox;
        Rect eventNameBase;
        Rect eventDescBase;
        Rect eventLocationBase;
        Rect eventTimeRemaining;



        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(628f, 278f);
            }
        }


        public FCEventWindow()
        {
            //Window Information
            this.faction = Find.World.GetComponent<FactionFC>();
            this.events = faction.events;

            this.scroll = 0;
            this.maxScroll = (events.Count() * eventHeight) - scrollBoxHeight;


            //Window Properties
            this.forcePause = false;
            this.draggable = true;
            this.doCloseX = true;
            this.preventCameraMotion = false;




            //rect for title
            //Rect titleBox = new Rect(0, 0, 300, 60);
            //rect for box outline
            eventsBox = new Rect(0, 30, 590, 212);

            //rect for event name
            eventNameBase = new Rect(0, 0, 250, eventHeight);
            //rect for description
            eventDescBase = new Rect(eventNameBase.width, 0, 100, eventHeight);
            //rect for source button
            eventLocationBase = new Rect(eventDescBase.x + eventDescBase.width, 0, 100, eventHeight);
            //rect time time remaining
            eventTimeRemaining = new Rect(eventLocationBase.x + eventLocationBase.width, 0, 140, eventHeight);
        }

        public override void WindowUpdate()
        {
            base.WindowUpdate();
            this.maxScroll = (events.Count() * eventHeight) - scrollBoxHeight;
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
            Widgets.DrawMenuSection(eventsBox);


            //loop through each event
            //GoTo Here if change
            int i = 0;

            Text.Anchor = TextAnchor.MiddleCenter;
            foreach (FCEvent evt in events)
            {
                i++;
                Rect name = new Rect();
                Rect desc = new Rect();
                Rect location = new Rect();
                Rect time = new Rect();
                Rect highlight = new Rect();


                name = eventNameBase;
                desc = eventDescBase;
                location = eventLocationBase;
                time = eventTimeRemaining;

                name.y = scroll + eventHeight * i;
                desc.y = scroll + eventHeight * i;
                location.y = scroll + eventHeight * i;
                time.y = scroll + eventHeight * i;

                highlight = new Rect(name.x, name.y, time.x + time.width, eventHeight);


                
                if(i % 2 == 0)
                {
                    Widgets.DrawHighlight(highlight);
                }
                Widgets.Label(name, evt.def.label);
                //
                if (Widgets.ButtonText(desc, "Desc"))
                {
                    if (evt.hasCustomDescription == false)
                    {
                        //If desc button clicked
                        
                        string settlementString = evt.settlementTraitLocations.Join((settlement) => $" {settlement.name}", "\n");
                        if (!settlementString.NullOrEmpty())
                        {
                            Log.Message(evt.settlementTraitLocations.Count + "");
                            Find.WindowStack.Add(new DescWindowFc($"{evt.def.desc}\n{"EventAffectingSettlements".Translate()}\n{settlementString}"));
                        }
                        else
                        {
                            Find.WindowStack.Add(new DescWindowFc(evt.def.desc));
                        }
                    } else
                    {
                        //has custom description
                        Find.WindowStack.Add(new DescWindowFc(evt.customDescription));
                    }
                }
                //
                if(Widgets.ButtonText(location, "Location".Translate().CapitalizeFirst()))
                {
                    if(evt.hasDestination == true)
                    {
                        Find.WindowStack.Add(new SettlementWindowFc(faction.returnSettlementByLocation(evt.location, evt.planetName)));
                    } else
                    {
                        if (evt.settlementTraitLocations.Count() > 0)
                        {
                            //if event affecting colonies
                            List<FloatMenuOption> list = new List<FloatMenuOption>();
                            foreach (SettlementFC settlement in evt.settlementTraitLocations)
                            {
                                if (settlement != null)
                                {
                                    list.Add(new FloatMenuOption(settlement.name, delegate { Find.WindowStack.Add(new SettlementWindowFc(settlement)); }));
                                }
                            }
                            if (list.Count == 0) { list.Add(new FloatMenuOption("Null", null)); }
                            Find.WindowStack.Add(new FloatMenu(list));
                                
                        } else
                        {
                           if (evt.def == FCEventDefOf.taxColony && evt.source != -1)
                            {
                                Find.WindowStack.Add(new SettlementWindowFc(faction.returnSettlementByLocation(evt.source, evt.planetName)));
                            }
                        }
                    }
                }
                Widgets.Label(time, (evt.timeTillTrigger - Find.TickManager.TicksGame).ToTimeString());
            }



            //Top label
            Widgets.ButtonTextSubtle(eventNameBase, "Name".Translate());
            Widgets.ButtonTextSubtle(eventDescBase, "Description".Translate());
            Widgets.ButtonTextSubtle(eventLocationBase, "Source".Translate());
            Widgets.ButtonTextSubtle(eventTimeRemaining, "TimeRemaining".Translate());

            //Menu Outline
            Widgets.DrawBox(eventsBox);


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
