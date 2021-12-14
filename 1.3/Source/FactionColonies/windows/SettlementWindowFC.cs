using System;
using System.Collections.Generic;
using System.Linq;
using FactionColonies.util;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public sealed class SettlementWindowFc : Window
    {
        public override Vector2 InitialSize
        {
            get { return new Vector2(1055f, 545f); }
        }


        //UI STUFF
        public const int ScrollSpacing = 45;
        public const int ScrollHeight = 315;

        //time variables
        private int uiUpdateTimer;
        private int scroll;
        private int maxScroll;
        private FactionFC factionfc;

        public void windowUpdateFc()
        {
            settlement.updateProfitAndProduction();
            settlement.updateDescription();
        }

        public override void PreOpen()
        {
            base.PreOpen();
            settlement.updateDescription();
            settlement.updateProfitAndProduction();
            maxScroll = (ResourceUtils.resourceTypes.Length * ScrollSpacing) - ScrollHeight;
            //settlement.update description
            factionfc = Find.World.GetComponent<FactionFC>();
        }

        public void UiUpdate()
        {
            if (uiUpdateTimer == 0)
            {
                uiUpdateTimer = FactionColonies.updateUiTimer;
                windowUpdateFc();
            }
            else
            {
                uiUpdateTimer -= 1;
            }
        }

        public override void WindowUpdate()
        {
            base.WindowUpdate();
            UiUpdate();
        }

        private readonly List<string> stats = new List<string>(5) 
        {
            "FCMilitaryLevel".Translate(),
            "FCHappiness".Translate(),
            "FCLoyality".Translate(),
            "FCUnrest".Translate(),
            "FCProsperity".Translate()
        };

        private readonly List<string> buttons = new List<string>(5)
        {
            "DeleteSettlement".Translate(), 
            "UpgradeTown".Translate(), 
            "FCSpecialActions".Translate(),
            "PrisonersMenu".Translate(), 
            "Military".Translate()
        };

        public SettlementFC settlement; //Don't expose

        public SettlementWindowFc(SettlementFC settlement)
        {
            if (settlement == null)
            {
                Close();
            }

            this.settlement = settlement;
            forcePause = false;
            draggable = true;
            doCloseX = true;
            preventCameraMotion = false;
        }


        public override void DoWindowContents(Rect inRect)
        {
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;

            DrawHeader();
            DrawSettlementStats(0, 80);
            //set 1 = settlement, set 2 = production
            DrawButtons(370, 336, 145, 25, 1);

            if (settlement != null)
            {
                //Upgrades
                DrawFacilities(0, 295);
                DrawDescription(150, 80, 370, 220);

                //Divider
                Widgets.DrawLineVertical(530, 0, 564);

                //ProDuctTion
                DrawProductionHeader(550, 0);
                DrawButtons(560, 40, 100, 24, 2);
                DrawEconomicStats(687, 0, 139, 15);
                //lowerProDucTion
                Widgets.DrawLineHorizontal(601, 80, 422);
                DrawProductionHeaderLower(550, 80, 5);
            }

            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }

        /// <summary>
        /// Transforms the given bool <paramref name="var"/> into it's keyed translation
        /// </summary>
        /// <param name="var"></param>
        /// <returns></returns>
        private string IsAllowedTranslation(bool var)
        {
            if (var) return "FCIsAllowed".Translate();
            return "FCIsNotAllowed".Translate();
        }

        /// <summary>
        /// Handles the tithe cutomization FloatMenuOptions for any given <paramref name="resource"/> with <paramref name="resourceType"/>
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="resourceType"></param>
        private void TitheCustomizationClicked(ResourceFC resource, ResourceType resourceType)
        {
            //if click faction customize button
            if (resource.filter == null)
            {
                resource.filter = new ThingFilter();
                PaymentUtil.resetThingFilter(settlement, resourceType);
            }

            List<FloatMenuOption> options = new List<FloatMenuOption>
            {
                new FloatMenuOption("FCTitheEnableAll".Translate(),
                delegate
                {
                    PaymentUtil.resetThingFilter(settlement, resourceType);
                    resource.returnLowestCost();
                }),
                new FloatMenuOption("FCTitheDisableAll".Translate(),
                delegate
                {
                    resource.filter.SetDisallowAll();
                    resource.returnLowestCost();
                })
            };

            List<ThingDef> things = PaymentUtil.debugGenerateTithe(resourceType);

            foreach (ThingDef thing in things.Where(thing => thing.race?.animalType != AnimalType.Dryad))
            {
                if (!FactionColonies.canCraftItem(thing))
                {
                    resource.filter.SetAllow(thing, false);
                    continue;
                }

                FloatMenuOption option = new FloatMenuOption("FCTitheSingleOption".Translate(thing.LabelCap, thing.BaseMarketValue, IsAllowedTranslation(resource.filter.Allows(thing))), delegate
                {
                    resource.filter.SetAllow(thing, !resource.filter.Allows(thing));
                    resource.returnLowestCost();
                }, thing);
                options.Add(option);
            }

            Find.WindowStack.Add(new FloatMenuSearchable(options));
        }

        /// <summary>
        /// If <paramref name="isClicked"/>, changes the <paramref name="resource"/>'s tithe status and updates some necessary things
        /// </summary>
        /// <param name="isClicked"></param>
        /// <param name="resource"></param>
        private void DoTitheCheckboxAction(bool isClicked, ResourceFC resource)
        {
            if (isClicked)
            {
                resource.isTitheBool = resource.isTithe;
                settlement.updateProfitAndProduction();
                windowUpdateFc();
            }
        }

        /// <summary>
        /// Draws a <paramref name="resource"/>'s description window
        /// </summary>
        /// <param name="resource"></param>
        /// <param name="resourceType"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="spacing"></param>
        private void DoResourceDescriptionButton(ResourceFC resource, ResourceType resourceType, int x, int y, int spacing)
        {
            if (Widgets.ButtonImage(new Rect(x + 45, scroll + y + 75 + (int)resourceType * (45 + spacing), 30, 30), resource.getIcon()))
            {
                Find.WindowStack.Add(new DescWindowFc("SettlementProductionOf".Translate() + ": "
                    + resource.label,
                    char.ToUpper(resource.label[0])
                    + resource.label.Substring(1)));
            }
        }

        /// <summary>
        /// Increases the amount of workers in a settlement. Decreases if <paramref name="negative"/> is true. Modifies the amount based on if shift/ctrl are held
        /// </summary>
        /// <param name="resourceType"></param>
        /// <param name="negative"></param>
        private void IncreaseWorkers(ResourceType resourceType, bool negative = false)
        {
            if (settlement.isUnderAttack)
            {
                Messages.Message("SettlementUnderAttack".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }
            //if clicked to lower amount of workers
            settlement.increaseWorkers(resourceType, (negative ? -1 : 1) * Modifiers.GetModifier);
            windowUpdateFc();
        }

        private bool ShouldTitheBeLockedForResouceType(ResourceType t) => t == ResourceType.Research || t == ResourceType.Power;

        private void DrawResources(int x, int y, int spacing)
        {
            foreach (ResourceType resourceType in ResourceUtils.resourceTypes)
            {
                ResourceFC resource = settlement.getResource(resourceType);
                float rectY = scroll + y + 70 + (int)resourceType * (45 + spacing);

                //Don't draw if outside view
                if ((int)resourceType * ScrollSpacing + scroll < 0) continue;

                bool titheDisabled = false;
                if (ShouldTitheBeLockedForResouceType(resourceType))
                    titheDisabled = true;
                else if (Widgets.ButtonImage(new Rect(x - 15,scroll + y + 65 + (int)resourceType * (45 + spacing) + 8, 20, 20), TexLoad.iconCustomize)) 
                    TitheCustomizationClicked(resource, resourceType);

                Widgets.Checkbox(new Vector2(x + 8, scroll + y + 65 + (int)resourceType * (45 + spacing) + 8), ref resource.isTithe, 24, titheDisabled);
                DoTitheCheckboxAction(resource.isTithe != resource.isTitheBool, resource);
                DoResourceDescriptionButton(resource, resourceType, x, y, spacing);

                //Production Efficiency
                Widgets.DrawBox(new Rect(x + 80, rectY, 100, 20));
                Widgets.FillableBar(new Rect(x + 80, rectY, 100, 20), (float)Math.Min(resource.baseProductionMultiplier, 1.0));
                Widgets.Label(new Rect(x + 80, scroll + y + 90 + (int)resourceType * (45 + spacing), 100, 20), "Workers".Translate() + ": " + resource.assignedWorkers);
                if (Widgets.ButtonText(new Rect(x + 80, scroll + y + 90 + (int)resourceType * (45 + spacing), 20, 20), "<")) IncreaseWorkers(resourceType, true);
                if (Widgets.ButtonText(new Rect(x + 160, scroll + y + 90 + (int)resourceType * (45 + spacing), 20, 20), ">")) IncreaseWorkers(resourceType);

                //Base Production
                Widgets.Label(new Rect(x + 195, rectY, 45, 40),
                    FactionColonies.FloorStat(resource.baseProduction));

                //Final Modifier
                Widgets.Label(new Rect(x + 250, rectY, 50, 40),
                    FactionColonies.FloorStat(resource.endProductionMultiplier));

                //Final Base
                Widgets.Label(new Rect(x + 310, rectY, 45, 40),
                    (FactionColonies.FloorStat(resource.endProduction)));

                //Est Income
                Widgets.Label(new Rect(x + 365, rectY, 45, 40),
                    (FactionColonies.FloorStat(resource.endProduction * LoadedModManager
                        .GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().silverPerResource)));

                //Tithe Percentage
                resource.returnTaxPercentage();
                string taxPercentage = FactionColonies.FloorStat(resource.taxPercentage) + "%";
                Widgets.Label(new Rect(x + 420, rectY, 45, 40), taxPercentage);
            }

        }

        public void DrawProductionHeaderLower(int x, int y, int spacing)
        {
            Text.Anchor = TextAnchor.MiddleRight;
            Text.Font = GameFont.Small;

            //Assigned workers
            Widgets.Label(new Rect(x, y, 410, 30), string.Format("{0}: {1}/{2}/{3}", "AssignedWorkers".Translate(), settlement.getTotalWorkers(), settlement.workersMax, settlement.workersUltraMax));

            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;

            //Item Headers
            Widgets.DrawHighlight(new Rect(x, y + 30, 40, 40));
            Widgets.Label(new Rect(x, y + 30, 40, 40), "IsTithe".Translate() + "?");

            Widgets.DrawHighlight(new Rect(x + 80, y + 30, 100, 40));
            Widgets.Label(new Rect(x + 80, y + 30, 100, 40), "ProductionEfficiency".Translate());

            Widgets.DrawHighlight(new Rect(x + 195, y + 30, 45, 40));
            Widgets.Label(new Rect(x + 195, y + 30, 45, 40), "Base".Translate());

            Widgets.DrawHighlight(new Rect(x + 250, y + 30, 50, 40));
            Widgets.Label(new Rect(x + 250, y + 30, 50, 40), "Modifier".Translate());

            Widgets.DrawHighlight(new Rect(x + 310, y + 30, 45, 40));
            Widgets.Label(new Rect(x + 310, y + 30, 45, 40), "Final".Translate());

            Widgets.DrawHighlight(new Rect(x + 365, y + 30, 45, 40));
            Widgets.Label(new Rect(x + 365, y + 30, 45, 40), "EstimatedProfit".Translate());

            Widgets.DrawHighlight(new Rect(x + 420, y + 30, 45, 40));
            Widgets.Label(new Rect(x + 420, y + 30, 45, 40), "TaxPercentage".Translate());

            DrawResources(x, y, spacing);

            //Scroll window for resources
            if (Event.current.type == EventType.ScrollWheel)
            {
                scrollWindow(Event.current.delta.y);
            }
        }

        public void DrawHeader()
        {
            //Draw Settlement Header Highlight
            Widgets.DrawHighlight(new Rect(0, 0, 520, 60));
            Widgets.DrawBox(new Rect(0, 0, 520, 60));

            //Draw town level and shadow backing
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.DrawShadowAround(new Rect(5, 15, 30, 30));
            Widgets.Label(new Rect(5, 15, 30, 30), settlement.settlementLevel.ToString());

            //Draw town name
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.Label(new Rect(40, 0, 520, 30), settlement.name);

            //Draw town title
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(50, 25, 150, 20), settlement.title); //returnSettlement().title);

            //Draw town location flabor text
            Text.Font = GameFont.Tiny;
            Widgets.Label(new Rect(55, 40, 470, 20),
                "Located".Translate() + " " +
                Find.WorldGrid.tiles[settlement.mapLocation].hilliness.GetLabel() + " " +
                "LandOf".Translate() + " " +
                Find.WorldGrid.tiles[settlement.mapLocation].biome.LabelCap.ToLower()); //returnSettlement().title);

            //Draw header Settings button
            if (Widgets.ButtonImage(new Rect(495, 5, 20, 20), TexLoad.iconCustomize))
            {
                //if click faction customize button
                Find.WindowStack.Add(new SettlementCustomizeWindowFc(settlement));
                //Log.Message("Settlement customize clicked");
            }
        }

        public void DrawSettlementStats(int x, int y)
        {
            int statSize = 30;
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Medium;
            for (int i = 0; i < stats.Count(); i++)
            {
                Widgets.DrawMenuSection(new Rect(x, y + (statSize + 15) * i, 125, statSize + 10));
                //Widgets.DrawHighlight(new Rect(x, y + ((statSize + 15) * i), 125, statSize + 10));
                if (stats[i] == "militaryLevel")
                {
                    if (Widgets.ButtonImage(
                        new Rect(x + 5 - 2, y + 5 + (statSize + 15) * i - 2, statSize + 4, statSize + 4),
                        TexLoad.iconMilitary))
                    {
                        Find.WindowStack.Add(new DescWindowFc("SettlementMilitaryLevelDesc".Translate(),
                            "SettlementMilitaryLevel".Translate()));
                    }

                    Widgets.Label(new Rect(x + 50, y + (statSize + 15) * i, 80, statSize + 10),
                        settlement.settlementMilitaryLevel.ToString());
                }

                if (stats[i] == "happiness")
                {
                    if (Widgets.ButtonImage(
                        new Rect(x + 5 - 2, y + 5 + (statSize + 15) * i - 2, statSize + 4, statSize + 4),
                        TexLoad.iconHappiness))
                    {
                        Find.WindowStack.Add(new DescWindowFc("SettlementHappinessDesc".Translate(),
                            "SettlementHappiness".Translate()));
                    }

                    Widgets.Label(new Rect(x + 50, y + (statSize + 15) * i, 80, statSize + 10),
                        settlement.happiness + "%");
                }

                if (stats[i] == "loyalty")
                {
                    if (Widgets.ButtonImage(
                        new Rect(x + 5 - 2, y + 5 + (statSize + 15) * i - 2, statSize + 4, statSize + 4),
                        TexLoad.iconLoyalty))
                    {
                        Find.WindowStack.Add(new DescWindowFc("SettlementLoyaltyDesc".Translate(),
                            "SettlementLoyalty".Translate()));
                    }

                    Widgets.Label(new Rect(x + 50, y + (statSize + 15) * i, 80, statSize + 10),
                        settlement.loyalty + "%");
                }

                if (stats[i] == "unrest")
                {
                    if (Widgets.ButtonImage(
                        new Rect(x + 5 - 2, y + 5 + (statSize + 15) * i - 2, statSize + 4, statSize + 4),
                        TexLoad.iconUnrest))
                    {
                        Find.WindowStack.Add(new DescWindowFc("SettlementUnrestDesc".Translate(),
                            "SettlementUnrest".Translate()));
                    }

                    Widgets.Label(new Rect(x + 50, y + (statSize + 15) * i, 80, statSize + 10),
                        settlement.unrest + "%");
                }

                if (stats[i] != "prosperity") continue;
                if (Widgets.ButtonImage(
                    new Rect(x + 5 - 2, y + 5 + (statSize + 15) * i - 2, statSize + 4, statSize + 4),
                    TexLoad.iconProsperity))
                {
                    Find.WindowStack.Add(new DescWindowFc("SettlementProsperityDesc".Translate(),
                        "SettlementProsperity".Translate()));
                }

                Widgets.Label(new Rect(x + 50, y + (statSize + 15) * i, 80, statSize + 10),
                    settlement.prosperity + "%");
            }
        }

        public void DrawButtons(int x, int y, int length, int size, int set)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Tiny;

            if (set == 1)
            {
                for (int i = 0; i < buttons.Count(); i++)
                {
                    if (Widgets.ButtonText(new Rect(x, y + ((size + 10) * i), length, size), buttons[i]))
                    {
                        //If click a button button
                        if (buttons[i] == "UpgradeTown".Translate())
                        {
                            //if click upgrade town button
                            Find.WindowStack.Add(new SettlementUpgradeWindowFc(settlement));
                            //Log.Message(buttons[i]);
                        }

                        if (buttons[i] == "AreYouSureRemove".Translate())
                        {
                            //if click to delete colony
                            Find.WindowStack.TryRemove(this);
                            FactionColonies.removePlayerSettlement(settlement);
                        }

                        if (buttons[i] == "DeleteSettlement".Translate())
                        {
                            //if click town log button
                            //Log.Message(buttons[i]);
                            buttons[i] = "AreYouSureRemove".Translate();
                        }

                        if (buttons[i] == "FCSpecialActions".Translate())
                        {
                            List<FloatMenuOption> list = new List<FloatMenuOption>
                            {
                                //Add to all
                                new FloatMenuOption("GoToLocation".Translate(), delegate
                                {
                                    Find.WindowStack.TryRemove(this);
                                    settlement.goTo();
                                })
                            };


                            if (factionfc.hasPolicy(FCPolicyDefOf.authoritarian))
                                list.Add(new FloatMenuOption("FCBuyLoyalty".Translate(),
                                    delegate { Find.WindowStack.Add(new FCWindow_Pay_Silver(settlement)); }));

                            if (factionfc.hasPolicy(FCPolicyDefOf.egalitarian))
                                list.Add(new FloatMenuOption("FCGiveTaxBreak".Translate(), delegate
                                {
                                    if (settlement.trait_Egalitarian_TaxBreak_Enabled == false)
                                    {
                                        Find.WindowStack.Add(new FCWindow_Confirm_TaxBreak(settlement));
                                    }
                                    else
                                        Messages.Message(
                                            "FCAlreadyGivingTaxBreak".Translate(Math.Round(
                                                (settlement.trait_Egalitarian_TaxBreak_Tick +
                                                 GenDate.TicksPerDay * 10 -
                                                 Find.TickManager.TicksGame) / (double) GenDate.TicksPerDay, 1)),
                                            MessageTypeDefOf.RejectInput);
                                }));

                            if (list.Count() == 0)
                                list.Add(new FloatMenuOption("No special actions to take", delegate { }));
                            Find.WindowStack.Add(new FloatMenu(list));
                        }

                        if (buttons[i] == "PrisonersMenu".Translate())
                        {
                            Find.WindowStack.Add(new FCPrisonerMenu(settlement));
                        }

                        if (buttons[i] == "Military".Translate())
                        {
                            List<FloatMenuOption> list = new List<FloatMenuOption>
                            {
                                new FloatMenuOption(
                                "ToggleAutoDefend".Translate(settlement.autoDefend.ToString()),
                                delegate
                                {
                                    settlement.autoDefend = !settlement.autoDefend;
                                    Messages.Message("autoDefendWarning".Translate(), MessageTypeDefOf.CautionInput);
                                })
                            };

                            if (settlement.isUnderAttack)
                            {
                                FCEvent evt = MilitaryUtilFC.returnMilitaryEventByLocation(settlement.mapLocation);

                                list.Add(new FloatMenuOption(
                                    "SettlementDefendingInformation".Translate(
                                        evt.militaryForceDefending.homeSettlement.name,
                                        evt.militaryForceDefending.militaryLevel), null, MenuOptionPriority.High));
                                list.Add(new FloatMenuOption("ChangeDefendingForce".Translate(), delegate
                                {
                                    List<FloatMenuOption> settlementList = new List<FloatMenuOption>();
                                    SettlementFC homeSettlement = settlement;

                                    settlementList.Add(new FloatMenuOption(
                                        "ResetToHomeSettlement".Translate(homeSettlement.settlementMilitaryLevel),
                                        delegate { MilitaryUtilFC.changeDefendingMilitaryForce(evt, homeSettlement); },
                                        MenuOptionPriority.High));

                                    foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements
                                    )
                                    {
                                        if (settlement.isMilitaryValid() && settlement != homeSettlement)
                                        {
                                            //if military is valid to use.

                                            settlementList.Add(new FloatMenuOption(
                                                settlement.name + " " + "ShortMilitary".Translate() + " " +
                                                settlement.settlementMilitaryLevel + " - " + "FCAvailable".Translate() +
                                                ": " + (!settlement.isMilitaryBusySilent()).ToString(), delegate
                                                {
                                                    if (settlement.isMilitaryBusy())
                                                    {
                                                        //military is busy
                                                    }
                                                    else
                                                    {
                                                        MilitaryUtilFC.changeDefendingMilitaryForce(evt, settlement);
                                                    }
                                                }
                                            ));
                                        }
                                    }

                                    if (settlementList.Count == 0)
                                    {
                                        settlementList.Add(new FloatMenuOption("NoValidMilitaries".Translate(), null));
                                    }

                                    Find.WindowStack.Add(new FloatMenuSearchable(settlementList) { vanishIfMouseDistant = true });


                                    //set to raid settlement here
                                }));

                                Find.WindowStack.Add(new FloatMenu(list));
                            }
                            else
                            {
                                list.Add(new FloatMenuOption("SettlementNotBeingAttacked".Translate(), null));
                                Find.WindowStack.Add(new FloatMenu(list));
                            }
                        }
                    }
                }
            }

            //set two buttons
        }

        public void DrawFacilities(int x, int y)
        {
            //Widgets.DrawHighlight(new Rect(x, y, 500, 209));
            //Widgets.DrawBox(new Rect(x, y, 500, 209));

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleLeft;

            Widgets.Label(new Rect(x + 5, y + 5, 450, 30), "BuildingUpgrades".Translate());

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.LowerCenter;


            //Establish variables to customize the UI

            int elementsPerRow = 4;
            int spacing = 15;
            int boxSide = 72;

            int spacingFromText = 45;
            int spacingFromSide = 15; // (494 - (spacing + boxSide) * elementsPerRow) / 2;

            int row;
            int column;

            Rect box = new Rect(0 + spacingFromSide, 0 + spacingFromText, boxSide, boxSide);
            Rect buildingIcon = new Rect(4 + box.x, 4 + box.y, boxSide - 8, boxSide - 8);

            Rect nBox;
            Rect nBuilding;


            int i = 0;

            foreach (BuildingFCDef building in settlement.buildings)
            {
                //Update Variables for List
                row = (int) Math.Floor(i / (double) elementsPerRow);
                column = i % elementsPerRow;

                nBox = new Rect(
                    new Vector2(box.x + x + ((box.width + spacing) * column), box.y + y + ((box.height + 10) * row)),
                    box.size);
                nBuilding = new Rect(
                    new Vector2(buildingIcon.x + x + ((box.width + spacing) * column),
                        buildingIcon.y + y + ((box.height + 10) * row)), buildingIcon.size);


                //Actual UI Code
                Widgets.DrawMenuSection(nBox);
                if (i < settlement.NumberBuildings)
                {
                    if (Widgets.ButtonImage(nBuilding, building.Icon))
                    {
                        //Find.WindowStack.Add(new listBuildingFC(building, i, settlement));
                        Find.WindowStack.Add(new FCBuildingWindow(settlement, i));
                    }
                }
                else
                {
                    if (Widgets.ButtonImage(nBuilding, TexLoad.buildingLocked))
                    {
                        Messages.Message("That Building is locked", MessageTypeDefOf.RejectInput);
                    }
                }
                //Optional Label
                //Widgets.Label(nBuilding, building.LabelCap);

                i++;
            }
        }

        public void DrawDescription(int x, int y, int length, int size)
        {
            //Widgets.Label(new Rect(x, y - 20, 100, 30), "Description".Translate());
            Widgets.DrawMenuSection(new Rect(x, y, length, size));

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(new Rect(x + 5, y + 5, length - 10, size - 10), settlement.description);
        }

        public void DrawProductionHeader(int x, int y)
        {
            FactionFC faction = Find.World.GetComponent<FactionFC>();
            double egalitarianTaxBoost = 0;
            if (faction.hasPolicy(FCPolicyDefOf.egalitarian))
            {
                egalitarianTaxBoost = Math.Floor(settlement.happiness / 10);
                if (settlement.trait_Egalitarian_TaxBreak_Enabled)
                {
                    egalitarianTaxBoost -= 30;
                }
            }

            double isolationistTaxBoost = 0;
            if (faction.hasPolicy(FCPolicyDefOf.isolationist))
                isolationistTaxBoost = 10;

            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(new Rect(x, y, 400, 30), "Production".Translate());
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperLeft;
            Widgets.Label(new Rect(x + 5, y + 60, 150, 20),
                "TaxBase".Translate() + ": " + (((100 + egalitarianTaxBoost + isolationistTaxBoost) +
                                                 TraitUtilsFC.cycleTraits("taxBasePercentage",
                                                     settlement.traits, Operation.Addition) + TraitUtilsFC.cycleTraits("taxBasePercentage", Find.World.GetComponent<FactionFC>().traits,
                                                     Operation.Addition))).ToString() + "%");
        }

        public void DrawEconomicStats(int x, int y, int length, int size)
        {
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;

            Widgets.Label(new Rect(x, 0, length, size), "Total".Translate() + " " + "CashSymbol".Translate() + " " + "Income".Translate());
            Widgets.Label(new Rect(x, size + 3, length, size), settlement.totalIncome.ToString());
            Widgets.Label(new Rect(x, size * 2 + 6, length, size), "FCUpkeep".Translate());
            Widgets.Label(new Rect(x, size * 3 + 9, length, size), settlement.totalUpkeep.ToString());
            Widgets.Label(new Rect(x + length + 10, 0, length, size), "Total".Translate() + " " + "CashSymbol".Translate() + " " + "Profit".Translate());
            Widgets.Label(new Rect(x + length + 10, size + 3, length, size), settlement.totalProfit.ToString());
            Widgets.Label(new Rect(x + length + 10, size * 2 + 6, length, size), "CostPerWorker".Translate());
            Widgets.Label(new Rect(x + length + 10, size * 3 + 9, length, size), settlement.workerCost.ToString());
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
                scroll -= (int) Event.current.delta.y * 5;
            }

            Event.current.Use();
        }
    }
}