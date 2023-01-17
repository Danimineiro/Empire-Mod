using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public class FCWindow_Overview : Window
    {
        public override Vector2 InitialSize
        {
            get { return new Vector2(838f, 538f); }
        }


        public FactionFC faction;
        Texture2D foreground;
        Texture2D background;
        public int maxScroll;
        public int scroll;


        //Header
        Rect menuSectionHeader = new Rect(0, 0, 400, 80);
        Rect headerFactionIcon = new Rect(3, 13, 60, 60);
        Rect headerFactionName = new Rect(65, 20, 335, 30);
        Rect headerFactionTitle = new Rect(80, 45, 320, 25);
        Rect headerSettings = new Rect(375, 5, 20, 20);
        Rect progressBarLevelUp = new Rect(0, 80, 400, 20); //use for label as well

        Rect factionLevel = new Rect(3, 80, 200, 20);

        //Policies & Traits
        Rect policy_1 = new Rect(10, 110, 40, 40);
        Rect policy_2 = new Rect(55, 110, 40, 40);

        Rect trait_1 = new Rect(3, 160, 200, 50);
        Rect trait_2 = new Rect(3, 215, 200, 50);
        Rect trait_3 = new Rect(3, 270, 200, 50);
        Rect trait_4 = new Rect(3, 325, 200, 50);
        Rect trait_5 = new Rect(3, 380, 200, 50);

        // Road Building
        Rect roadBuilding = new Rect(3, 380 + 55, 100, 25);

        //Settlements Box
        Rect settlementsBoxLabels = new Rect(235, 110, 555, 30);
        Rect settlementsBox = new Rect(235, 130, 555, 320);


        //Settlements info
        public int settlementTabHeight = 25;
        Rect settlement_1 = new Rect(0, 0, 120, 25); // Name //520
        Rect settlement_2 = new Rect(120, 0, 60, 25); // Level  400 left
        Rect settlement_3 = new Rect(180, 0, 60, 25); // military level   380 left
        Rect settlement_4 = new Rect(240, 0, 60, 25); // profit   340 left
        Rect settlement_5 = new Rect(300, 0, 75, 25); // Happiness
        Rect settlement_6 = new Rect(375, 0, 60, 25); // Loyalty
        Rect settlement_7 = new Rect(435, 0, 60, 25); // Unrest
        Rect settlement_8 = new Rect(495, 0, 60, 25); // Rest


        public FCWindow_Overview()
        {
            forcePause = false;
            draggable = true;
            doCloseX = true;
            preventCameraMotion = false;
            faction = Find.World.GetComponent<FactionFC>();


            foreground = new Texture2D(1, 1);
            Color green = new Color(25 / 255f, 120 / 255f, 25 / 255f);
            foreground.SetPixel(0, 0, green);
            foreground.Apply();
            background = new Texture2D(1, 1);
            background.SetPixel(0, 0, Color.black);
            background.Apply();

            maxScroll = (int) ((faction.settlements.Count() * 25) - settlementsBox.height);
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

            Widgets.DrawMenuSection(menuSectionHeader);
            Widgets.DrawBox(menuSectionHeader);
            //Icon button
            if (Widgets.ButtonImage(headerFactionIcon, faction.factionIcon))
            {
            }

            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Medium;
            Widgets.Label(headerFactionName, faction.name);

            Text.Font = GameFont.Small;
            Widgets.Label(headerFactionTitle, faction.title);

            //Settings button
            if (Widgets.ButtonImage(headerSettings, TexLoad.iconCustomize))
            {
                Faction fact = FactionColonies.getPlayerColonyFaction();
                if (fact != null)
                    Find.WindowStack.Add(new FactionCustomizeWindowFc(faction));
            }


            //Progress bar

            Widgets.FillableBar(progressBarLevelUp, faction.factionXPCurrent / faction.factionXPGoal, foreground,
                background, true);
            Widgets.DrawShadowAround(progressBarLevelUp);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(progressBarLevelUp, Math.Round(faction.factionXPCurrent) + "/" + faction.factionXPGoal);
            Text.Anchor = TextAnchor.MiddleLeft;
            Widgets.DrawBox(progressBarLevelUp);

            Text.Font = GameFont.Small;
            Widgets.Label(factionLevel, "FCLevel".Translate(faction.factionLevel));


            //Policies
            if (faction.policies.Count() == 2)
            {
                Widgets.ButtonImage(policy_1, faction.policies[0].def.IconLight);
                if (policy_1.Contains(Event.current.mousePosition))
                {
                    TooltipHandler.TipRegion(policy_1, returnPolicyText(faction.policies[0].def));
                }

                //Widgets.Label(policy_2, new GUIContent("test", "test test test"));
                Widgets.ButtonImage(policy_2, faction.policies[1].def.IconLight);
                if (policy_2.Contains(Event.current.mousePosition))
                {
                    TooltipHandler.TipRegion(policy_2, returnPolicyText(faction.policies[1].def));
                }
            }
            else
            {
                Widgets.Label(new Rect(policy_1.x, policy_1.y, 200, 50), "FCSelectYourTraits".Translate());
            }


            List<FloatMenuOption> list = new List<FloatMenuOption>();
            List<FCPolicyDef> available = availableTraitsList();
            //Trait)

            //TraitSlot
            if (Widgets.ButtonTextSubtle(trait_1, returnTraitAvailibility(1, faction.factionTraits[0]))) //CHANGE THIS
            {
                if (canChangeTrait(1, faction.factionTraits[0]))
                {
                    foreach (FCPolicyDef trait in available)
                    {
                        list.Add(new FloatMenuOption(trait.label, delegate
                            {
                                List<FloatMenuOption> confirm = new List<FloatMenuOption>();
                                confirm.Add(new FloatMenuOption("FCConfirmTrait".Translate(trait.label),
                                    delegate { faction.factionTraits[0] = new FCPolicy(trait); }));
                                Find.WindowStack.Add(new FloatMenu(confirm));
                            },
                            mouseoverGuiAction: delegate
                            {
                                TooltipHandler.TipRegion(new Rect(Event.current.mousePosition, new Vector2(100, 200)),
                                    returnPolicyText(trait));
                            }));
                    }

                    Find.WindowStack.Add(new FloatMenu(list));
                }
            }

            if (Mouse.IsOver(trait_1) && faction.factionTraits[0].def != FCPolicyDefOf.empty)
            {
                TooltipHandler.TipRegion(new Rect(Event.current.mousePosition, new Vector2(100, 200)),
                    returnPolicyText(faction.factionTraits[0].def));
            }
            //End Trait Slot

            //TraitSlot
            if (Widgets.ButtonTextSubtle(trait_2, returnTraitAvailibility(2, faction.factionTraits[1]))) //CHANGE THIS
            {
                if (canChangeTrait(2, faction.factionTraits[1]))
                {
                    foreach (FCPolicyDef trait in available)
                    {
                        list.Add(new FloatMenuOption(trait.label, delegate
                            {
                                List<FloatMenuOption> confirm = new List<FloatMenuOption>();
                                confirm.Add(new FloatMenuOption("FCConfirmTrait".Translate(trait.label),
                                    delegate { faction.factionTraits[1] = new FCPolicy(trait); }));
                                Find.WindowStack.Add(new FloatMenu(confirm));
                            },
                            mouseoverGuiAction: delegate
                            {
                                TooltipHandler.TipRegion(new Rect(Event.current.mousePosition, new Vector2(200, 200)),
                                    returnPolicyText(trait));
                            }));
                    }

                    Find.WindowStack.Add(new FloatMenu(list));
                }
            }

            if (Mouse.IsOver(trait_2) && faction.factionTraits[1].def != FCPolicyDefOf.empty)
            {
                TooltipHandler.TipRegion(new Rect(Event.current.mousePosition, new Vector2(200, 200)),
                    returnPolicyText(faction.factionTraits[1].def));
            }
            //End Trait Slot

            //TraitSlot
            if (Widgets.ButtonTextSubtle(trait_3, returnTraitAvailibility(3, faction.factionTraits[2]))) //CHANGE THIS
            {
                if (canChangeTrait(3, faction.factionTraits[2]))
                {
                    foreach (FCPolicyDef trait in available)
                    {
                        list.Add(new FloatMenuOption(trait.label, delegate
                            {
                                List<FloatMenuOption> confirm = new List<FloatMenuOption>();
                                confirm.Add(new FloatMenuOption("FCConfirmTrait".Translate(trait.label),
                                    delegate { faction.factionTraits[2] = new FCPolicy(trait); }));
                                Find.WindowStack.Add(new FloatMenu(confirm));
                            },
                            mouseoverGuiAction: delegate
                            {
                                TooltipHandler.TipRegion(new Rect(Event.current.mousePosition, new Vector2(300, 200)),
                                    returnPolicyText(trait));
                            }));
                    }

                    Find.WindowStack.Add(new FloatMenu(list));
                }
            }

            if (Mouse.IsOver(trait_3) && faction.factionTraits[2].def != FCPolicyDefOf.empty)
            {
                TooltipHandler.TipRegion(new Rect(Event.current.mousePosition, new Vector2(300, 200)),
                    returnPolicyText(faction.factionTraits[2].def));
            }
            //End Trait Slot

            //TraitSlot
            if (Widgets.ButtonTextSubtle(trait_4, returnTraitAvailibility(4, faction.factionTraits[3]))) //CHANGE THIS
            {
                if (canChangeTrait(4, faction.factionTraits[3]))
                {
                    foreach (FCPolicyDef trait in available)
                    {
                        list.Add(new FloatMenuOption(trait.label, delegate
                            {
                                List<FloatMenuOption> confirm = new List<FloatMenuOption>();
                                confirm.Add(new FloatMenuOption("FCConfirmTrait".Translate(trait.label),
                                    delegate { faction.factionTraits[3] = new FCPolicy(trait); }));
                                Find.WindowStack.Add(new FloatMenu(confirm));
                            },
                            mouseoverGuiAction: delegate
                            {
                                TooltipHandler.TipRegion(new Rect(Event.current.mousePosition, new Vector2(400, 200)),
                                    returnPolicyText(trait));
                            }));
                    }

                    Find.WindowStack.Add(new FloatMenu(list));
                }
            }

            if (Mouse.IsOver(trait_4) && faction.factionTraits[3].def != FCPolicyDefOf.empty)
            {
                TooltipHandler.TipRegion(new Rect(Event.current.mousePosition, new Vector2(400, 200)),
                    returnPolicyText(faction.factionTraits[3].def));
            }
            //End Trait Slot

            //TraitSlot
            if (Widgets.ButtonTextSubtle(trait_5, returnTraitAvailibility(5, faction.factionTraits[4]))) //CHANGE THIS
            {
                if (canChangeTrait(5, faction.factionTraits[4]))
                {
                    foreach (FCPolicyDef trait in available)
                    {
                        list.Add(new FloatMenuOption(trait.label, delegate
                            {
                                List<FloatMenuOption> confirm = new List<FloatMenuOption>();
                                confirm.Add(new FloatMenuOption("FCConfirmTrait".Translate(trait.label),
                                    delegate { faction.factionTraits[4] = new FCPolicy(trait); }));
                                Find.WindowStack.Add(new FloatMenu(confirm));
                            },
                            mouseoverGuiAction: delegate
                            {
                                TooltipHandler.TipRegion(new Rect(Event.current.mousePosition, new Vector2(500, 200)),
                                    returnPolicyText(trait));
                            }));
                    }

                    Find.WindowStack.Add(new FloatMenu(list));
                }
            }

            if (Mouse.IsOver(trait_5) && faction.factionTraits[4].def != FCPolicyDefOf.empty)
            {
                TooltipHandler.TipRegion(new Rect(Event.current.mousePosition, new Vector2(500, 200)),
                    returnPolicyText(faction.factionTraits[4].def));
            }
            //End Trait Slot

            FactionFC fc = Find.World.GetComponent<FactionFC>();
            Widgets.Label(roadBuilding, "FCBuildRoads".Translate());
            Widgets.Checkbox(roadBuilding.x + roadBuilding.width + 5, roadBuilding.y,
                ref fc.roadBuilder.roadBuildingEnabled);

            //SettlementBox


            Widgets.DrawMenuSection(settlementsBox);
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.BeginGroup(settlementsBoxLabels);
            Widgets.DrawMenuSection(new Rect(0, 0, settlementsBox.width, 20));
            Widgets.DrawLightHighlight(new Rect(0, 0, settlementsBox.width, 20));
            Widgets.Label(settlement_1, "Name");
            if (Widgets.ButtonInvisible(settlement_1))
            {
                faction.settlements.Sort(FactionColonies.CompareSettlementName);
            }

            Widgets.Label(settlement_2, "Level");
            if (Widgets.ButtonInvisible(settlement_2))
            {
                faction.settlements.Sort(FactionColonies.CompareSettlementLevel);
            }

            Widgets.Label(settlement_3, "Mil Level");
            if (Widgets.ButtonInvisible(settlement_3))
            {
                faction.settlements.Sort(FactionColonies.CompareSettlementMilitaryLevel);
            }

            Widgets.Label(settlement_4, "Profit");
            if (Widgets.ButtonInvisible(settlement_4))
            {
                faction.settlements.Sort(FactionColonies.CompareSettlementProfit);
            }

            Widgets.Label(settlement_5, "Free Workers");
            if (Widgets.ButtonInvisible(settlement_5))
            {
                faction.settlements.Sort(FactionColonies.CompareSettlementFreeWorkers);
            }

            Widgets.Label(settlement_6, "Happiness");
            if (Widgets.ButtonInvisible(settlement_6))
            {
                faction.settlements.Sort(FactionColonies.CompareSettlementHappiness);
            }

            Widgets.Label(settlement_7, "Loyalty");
            if (Widgets.ButtonInvisible(settlement_7))
            {
                faction.settlements.Sort(FactionColonies.CompareSettlementLoyalty);
            }

            Widgets.Label(settlement_8, "Unrest");
            if (Widgets.ButtonInvisible(settlement_8))
            {
                faction.settlements.Sort(FactionColonies.CompareSettlementUnrest);
            }


            GUI.EndGroup();


            GUI.BeginGroup(settlementsBox);

            for (int i = 0; i < faction.settlements.Count(); i++)
            {
                SettlementFC settlement = faction.settlements[i];

                //settlement name
                if (Widgets.ButtonTextSubtle(AdjustRect(settlement_1, i), settlement.name))
                {
                    Find.WindowStack.Add(new SettlementWindowFc(settlement));
                }

                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.MiddleCenter;
                Widgets.Label(AdjustRect(settlement_2, i), settlement.settlementLevel.ToString());
                Widgets.Label(AdjustRect(settlement_3, i), settlement.settlementMilitaryLevel.ToString());
                Widgets.Label(AdjustRect(settlement_4, i), ((int)settlement.getTotalProfit()).ToString());
                Widgets.Label(AdjustRect(settlement_5, i), (settlement.workersUltraMax - settlement.getTotalWorkers()).ToString());
                Widgets.Label(AdjustRect(settlement_6, i), ((int)settlement.Happiness).ToString());
                Widgets.Label(AdjustRect(settlement_7, i), ((int)settlement.Loyalty).ToString());
                Widgets.Label(AdjustRect(settlement_8, i), ((int)settlement.Unrest).ToString());
            }

            GUI.EndGroup();


            //reset anchor/font
            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;

            if (Event.current.type == EventType.ScrollWheel)
            {
                scrollWindow(Event.current.delta.y);
            }
        }

        public Rect AdjustRect(Rect rect, int i)
        {
            Rect tmp = new Rect();
            tmp = rect;
            tmp.y = 25 * i + scroll;
            return tmp;
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

        string returnPolicyText(FCPolicyDef def)
        {
            string str = "";

            str += def.LabelCap + "\n";

            foreach (string positive in def.positiveEffects)
            {
                str += "\n" + positive;
            }

            str += "\n==========";
            foreach (string negative in def.negativeEffects)
            {
                str += "\n" + negative;
            }

            return str;
        }

        string returnTraitAvailibility(int slot, FCPolicy current)
        {
            int requiredLevel = slot;

            if (current.def != FCPolicyDefOf.empty)
            {
                return current.def.label;
            }

            if (faction.factionLevel >= requiredLevel)
            {
                return "FCSelectANewTrait".Translate();
            }

            return "FCTraitLockedUntilLevel".Translate(slot);
        }

        bool canChangeTrait(int slot, FCPolicy current)
        {
            int requiredLevel = slot;

            if (current.def != FCPolicyDefOf.empty)
            {
                return false;
            }

            if (faction.factionLevel >= requiredLevel)
            {
                return true;
            }

            return false;
        }

        List<FCPolicyDef> availableTraitsList()
        {
            List<FCPolicyDef> list = new List<FCPolicyDef>();
            if (!faction.hasTrait(FCPolicyDefOf.resilient))
                list.Add(FCPolicyDefOf.resilient);
            if (!faction.hasTrait(FCPolicyDefOf.raiders))
                list.Add(FCPolicyDefOf.raiders);
            if (!faction.hasTrait(FCPolicyDefOf.defenseInDepth))
                list.Add(FCPolicyDefOf.defenseInDepth);
            if (!faction.hasTrait(FCPolicyDefOf.industrious))
                list.Add(FCPolicyDefOf.industrious);
            if (!faction.hasTrait(FCPolicyDefOf.roadBuilders))
                list.Add(FCPolicyDefOf.roadBuilders);
            if (!faction.hasTrait(FCPolicyDefOf.mercantile))
                list.Add(FCPolicyDefOf.mercantile);
            if (!faction.hasTrait(FCPolicyDefOf.innovative))
                list.Add(FCPolicyDefOf.innovative);
            //if (!faction.hasTrait(FCPolicyDefOf.lucky))
            //list.Add(FCPolicyDefOf.lucky);

            return list;
        }
    }
}