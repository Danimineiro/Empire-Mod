using System;
using System.Collections.Generic;
using System.Linq;
using FactionColonies.util;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace FactionColonies
{
    class militaryCustomizationWindowFC : Window
    {
        int tab = 1;
        string selectedText = "";
        MilUnitFC selectedUnit;
        MilSquadFC selectedSquad;
        MilitaryFireSupport selectedSupport;
        SettlementFC settlementPointReference;
        FactionFC faction;
        MilitaryCustomizationUtil util;

        public float scroll;

        //public int maxScroll = 0;
        public float settlementMaxScroll;
        public float fireSupportMaxScroll;
        public int settlementHeight;
        public int settlementYSpacing;
        public int settlementWindowHeight = 500;

        private List<FloatMenuOption> DeploymentOptions(SettlementFC settlement) => new List<FloatMenuOption>
        {
            new FloatMenuOption("walkIntoMapDeploymentOption".Translate(), delegate
            {
                FactionColonies.CallinAlliedForces(settlement, false);
            }),
            DropPodDeploymentOption(settlement)
        };

        private FloatMenuOption DropPodDeploymentOption(SettlementFC settlement)
        {
            bool medievalOnly = LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().medievalTechOnly;
            if (!medievalOnly && (DefDatabase<ResearchProjectDef>.GetNamed("TransportPod", false)?.IsFinished ?? false))
            {
                return new FloatMenuOption("dropPodDeploymentOption".Translate(), delegate
                {
                    FactionColonies.CallinAlliedForces(settlement, true);
                });
            }

            return new FloatMenuOption("dropPodDeploymentOption".Translate() + (medievalOnly ? "dropPodDeploymentOptionUnavailableReasonMedieval".Translate() : "dropPodDeploymentOptionUnavailableReasonTech".Translate(DefDatabase<ResearchProjectDef>.GetNamed("TransportPod", false)?.label ?? "errorDropPodResearchCouldNotBeFound".Translate())), null);
        }

        public override Vector2 InitialSize
        {
            get { return new Vector2(838f, 600); }
        }


        public militaryCustomizationWindowFC()
        {
            forcePause = false;
            draggable = true;
            doCloseX = true;
            preventCameraMotion = false;
            tab = 1;
            selectedText = "";

            settlementHeight = 120;
            settlementYSpacing = 5;
            util = Find.World.GetComponent<FactionFC>().militaryCustomizationUtil;
            faction = Find.World.GetComponent<FactionFC>();
        }

        public override void PreOpen()
        {
            base.PreOpen();
            scroll = 0;
            settlementMaxScroll =
                (Find.World.GetComponent<FactionFC>().settlements.Count * (settlementYSpacing + settlementHeight) -
                 settlementWindowHeight);
        }

        public override void PostClose()
        {
            base.PostClose();
            util.checkMilitaryUtilForErrors();
        }

        public override void DoWindowContents(Rect inRect)
        {
            switch (tab)
            {
                case 0:
                    DrawTabDesignate(inRect);
                    break;
                case 1:
                    DrawTabAssign(inRect);
                    break;
                case 2:
                    DrawTabSquad(inRect);
                    break;
                case 3:
                    DrawTabUnit(inRect);
                    break;
                case 4:
                    DrawTabFireSupport(inRect);
                    break;
            }

            DrawHeaderTabs(inRect);

            //Widgets.ThingIcon(new Rect(50, 50, 60, 60), util.defaultPawn);
        }

        public void DrawHeaderTabs(Rect inRect)
        {
            Rect milDesigination = new Rect(0, 0, 0, 35);
            Rect milSetSquad = new Rect(milDesigination.x + milDesigination.width, milDesigination.y, 187,
                milDesigination.height);
            Rect milCreateSquad = new Rect(milSetSquad.x + milSetSquad.width, milDesigination.y, 187,
                milDesigination.height);
            Rect milCreateUnit = new Rect(milCreateSquad.x + milCreateSquad.width, milDesigination.y, 187,
                milDesigination.height);
            Rect milCreateFiresupport = new Rect(milCreateUnit.x + milCreateUnit.width, milDesigination.y, 187,
                milDesigination.height);
            Rect helpButton = new Rect(760, 0, 30, 30);


            if (Widgets.ButtonImage(helpButton, TexLoad.questionmark))
            {
                string header = "Help! What is this for?";
                string description = "Need Help with this menu? Go to this youtube video: https://youtu.be/lvWb1rMMsq8";
                Find.WindowStack.Add(new DescWindowFc(description, header));
            }

            if (Widgets.ButtonTextSubtle(milDesigination, "Military Designations"))
            {
                tab = 0;
                util.checkMilitaryUtilForErrors();
            }

            if (Widgets.ButtonTextSubtle(milSetSquad, "Designate Squads"))
            {
                tab = 1;
                scroll = 0;
                util.checkMilitaryUtilForErrors();
            }

            if (Widgets.ButtonTextSubtle(milCreateSquad, "Create Squads"))
            {
                tab = 2;
                selectedText = "Select A Squad";
                if (selectedSquad != null)
                {
                    selectedText = selectedSquad.name;
                    selectedSquad.updateEquipmentTotalCost();
                }

                if (util.blankUnit == null)
                {
                    util.blankUnit = new MilUnitFC(true);
                }

                util.checkMilitaryUtilForErrors();
            }

            if (Widgets.ButtonTextSubtle(milCreateUnit, "Create Units"))
            {
                tab = 3;
                selectedText = "Select A Unit";
                if (selectedUnit != null)
                {
                    selectedText = selectedUnit.name;
                }

                util.checkMilitaryUtilForErrors();
            }

            if (Widgets.ButtonTextSubtle(milCreateFiresupport, "Create Fire Support"))
            {
                tab = 4;
                selectedText = "Select a fire support";

                util.checkMilitaryUtilForErrors();
            }
        }

        public void DrawTabDesignate(Rect inRect)
        {
        }

        public void DrawTabAssign(Rect inRect)
        {
            Rect SettlementBox = new Rect(5, 45, 535, settlementHeight);
            Rect SettlementName = new Rect(SettlementBox.x + 5, SettlementBox.y + 5, 250, 25);
            Rect MilitaryLevel = new Rect(SettlementName.x, SettlementName.y + 30, 250, 25);
            Rect AssignedSquad = new Rect(MilitaryLevel.x, MilitaryLevel.y + 30, 250, 25);
            Rect isBusy = new Rect(AssignedSquad.x, AssignedSquad.y + 30, 250, 25);

            Rect buttonSetSquad = new Rect(SettlementBox.x + SettlementBox.width - 265, SettlementBox.y + 5, 100, 25);
            Rect buttonViewSquad = new Rect(buttonSetSquad.x, buttonSetSquad.y + 3 + buttonSetSquad.height,
                buttonSetSquad.width, buttonSetSquad.height);
            Rect buttonDeploySquad = new Rect(buttonViewSquad.x, buttonViewSquad.y + 3 + buttonViewSquad.height,
                buttonSetSquad.width, buttonSetSquad.height);
            Rect buttonResetPawns = new Rect(buttonDeploySquad.x, buttonDeploySquad.y + 3 + buttonDeploySquad.height,
                buttonSetSquad.width, buttonSetSquad.height);
            Rect buttonOrderFireSupport = new Rect(buttonSetSquad.x + 125 + 5, SettlementBox.y + 5, 125, 25);


            //set text anchor and font
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;


            int count = 0;
            foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
            {
                Text.Font = GameFont.Small;

                Widgets.DrawMenuSection(new Rect(SettlementBox.x,
                    SettlementBox.y + (SettlementBox.height + settlementYSpacing) * count + scroll,
                    SettlementBox.width, SettlementBox.height));

                //click on settlement name
                if (Widgets.ButtonTextSubtle(
                    new Rect(SettlementName.x,
                        SettlementName.y + (SettlementBox.height + settlementYSpacing) * count + scroll,
                        SettlementName.width, SettlementName.height), settlement.name))
                {
                    Find.WindowStack.Add(new SettlementWindowFc(settlement));
                }

                Widgets.Label(
                    new Rect(MilitaryLevel.x,
                        MilitaryLevel.y + (SettlementBox.height + settlementYSpacing) * count + scroll,
                        MilitaryLevel.width, MilitaryLevel.height * 2),
                    "Mil Level: " + settlement.settlementMilitaryLevel + " - Max Squad Cost: " +
                    FactionColonies.calculateMilitaryLevelPoints(settlement.settlementMilitaryLevel));
                if (settlement.militarySquad != null)
                {
                    if (settlement.militarySquad.outfit != null)
                    {
                        Widgets.Label(
                            new Rect(AssignedSquad.x,
                                AssignedSquad.y + (SettlementBox.height + settlementYSpacing) * count + scroll,
                                AssignedSquad.width, AssignedSquad.height),
                            "Assigned Squad: " +
                            settlement.militarySquad.outfit.name); //settlement.militarySquad.name);
                    }
                    else
                    {
                        Widgets.Label(
                            new Rect(AssignedSquad.x,
                                AssignedSquad.y + (SettlementBox.height + settlementYSpacing) * count + scroll,
                                AssignedSquad.width, AssignedSquad.height),
                            "No assigned Squad"); //settlement.militarySquad.name);
                    }
                }
                else
                {
                    Widgets.Label(
                        new Rect(AssignedSquad.x,
                            AssignedSquad.y + (SettlementBox.height + settlementYSpacing) * count + scroll,
                            AssignedSquad.width, AssignedSquad.height), "No assigned Squad");
                }


                Widgets.Label(
                    new Rect(isBusy.x, isBusy.y + (SettlementBox.height + settlementYSpacing) * count + scroll,
                        isBusy.width, isBusy.height), "Available: " + (!settlement.isMilitaryBusySilent()));

                Text.Font = GameFont.Tiny;

                //Set Squad Button
                if (Widgets.ButtonText(
                    new Rect(buttonSetSquad.x,
                        buttonSetSquad.y + (SettlementBox.height + settlementYSpacing) * count + scroll,
                        buttonSetSquad.width, buttonSetSquad.height), "Set Squad"))
                {
                    //check null
                    if (util.squads == null)
                    {
                        util.resetSquads();
                    }

                    List<FloatMenuOption> squads = new List<FloatMenuOption>();

                    squads.AddRange(util.squads
                        .Select(squad => new FloatMenuOption(squad.name + " - Total Equipment Cost: " +
                                                             squad.equipmentTotalCost, delegate
                        {
                            //Unit is selected
                            util.attemptToAssignSquad(settlement, squad);
                        })));

                    if (!squads.Any())
                    {
                        squads.Add(new FloatMenuOption("No Available Squads", null));
                    }

                    FloatMenu selection = new FloatMenuSearchable(squads);
                    Find.WindowStack.Add(selection);
                }

                //View Squad
                if (Widgets.ButtonText(
                    new Rect(buttonViewSquad.x,
                        buttonViewSquad.y + (SettlementBox.height + settlementYSpacing) * count + scroll,
                        buttonViewSquad.width, buttonViewSquad.height), "View Squad"))
                {
                    Messages.Message("This is currently not implemented.", MessageTypeDefOf.RejectInput);
                }


                //Deploy Squad
                if (Widgets.ButtonText(
                    new Rect(buttonDeploySquad.x,
                        buttonDeploySquad.y + (SettlementBox.height + settlementYSpacing) * count + scroll,
                        buttonDeploySquad.width, buttonDeploySquad.height), "Deploy Squad"))
                {
                    if (!(settlement.isMilitaryBusy()) && settlement.isMilitarySquadValid())
                    {

                        Find.WindowStack.Add(new FloatMenu(DeploymentOptions(settlement)));
                    }
                    else if (settlement.isMilitaryBusy() && settlement.isMilitarySquadValid() && faction.hasPolicy(FCPolicyDefOf.militaristic))
                    {
                        if ((faction.traitMilitaristicTickLastUsedExtraSquad + GenDate.TicksPerDay * 5) <=
                            Find.TickManager.TicksGame)
                        {
                            int cost = (int) Math.Round(settlement.militarySquad.outfit.updateEquipmentTotalCost() *
                                                        .2);
                            List<FloatMenuOption> options = new List<FloatMenuOption>();

                            options.Add(new FloatMenuOption("Deploy Secondary Squad - $" + cost + " silver",
                                delegate
                                {
                                    if (PaymentUtil.getSilver() >= cost)
                                    {
                                        List<FloatMenuOption> deploymentOptions = new List<FloatMenuOption>();

                                        deploymentOptions.Add(new FloatMenuOption("Walk into map", delegate
                                        {
                                            FactionColonies.CallinAlliedForces(settlement, false, cost);
                                            Find.WindowStack.currentlyDrawnWindow.Close();
                                        }));
                                        //check if medieval only
                                        bool medievalOnly = LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().medievalTechOnly;
                                        if (!medievalOnly && (DefDatabase<ResearchProjectDef>.GetNamed("TransportPod", false)?.IsFinished ?? false))
                                        {
                                            deploymentOptions.Add(new FloatMenuOption("Drop-Pod", delegate
                                            {
                                                FactionColonies.CallinAlliedForces(settlement, true, cost);
                                                Find.WindowStack.currentlyDrawnWindow.Close();
                                            }));
                                        }

                                        Find.WindowStack.Add(new FloatMenu(deploymentOptions));
                                    }
                                    else
                                    {
                                        Messages.Message("NotEnoughSilverToDeploySquad".Translate(), MessageTypeDefOf.RejectInput);
                                    }
                                }));

                            Find.WindowStack.Add(new FloatMenu(options));
                        }
                        else
                        {
                            Messages.Message(
                                "XDaysToRedeploy".Translate(Math.Round(
                                    ((faction.traitMilitaristicTickLastUsedExtraSquad + GenDate.TicksPerDay * 5) -
                                     Find.TickManager.TicksGame).TicksToDays(), 1)), MessageTypeDefOf.RejectInput);
                        }
                    }
                }

                //Reset Squad
                if (Widgets.ButtonText(
                    new Rect(buttonResetPawns.x,
                        buttonResetPawns.y + (SettlementBox.height + settlementYSpacing) * count + scroll,
                        buttonResetPawns.width, buttonResetPawns.height), "Reset Pawns"))
                {
                    FloatMenuOption confirm = new FloatMenuOption("Are you sure? Click to confirm", delegate
                    {
                        if (settlement.militarySquad != null)
                        {
                            Messages.Message("Pawns have been regenerated for the squad",
                                MessageTypeDefOf.NeutralEvent);
                            settlement.militarySquad.initiateSquad();
                        }
                        else
                        {
                            Messages.Message("There is no pawns to reset. Assign a squad first.",
                                MessageTypeDefOf.RejectInput);
                        }
                    });

                    List<FloatMenuOption> list = new List<FloatMenuOption>();
                    list.Add(confirm);
                    Find.WindowStack.Add(new FloatMenu(list));
                }

                //Order Fire Support
                if (Widgets.ButtonText(
                    new Rect(buttonOrderFireSupport.x,
                        buttonOrderFireSupport.y + (SettlementBox.height + settlementYSpacing) * count + scroll,
                        buttonOrderFireSupport.width, buttonOrderFireSupport.height), "Order Fire Support"))
                {
                    List<FloatMenuOption> list = new List<FloatMenuOption>();


                    foreach (MilitaryFireSupport support in util.fireSupportDefs)
                    {
                        float cost = support.returnTotalCost();
                        FloatMenuOption option = new FloatMenuOption(support.name + " - $" + cost, delegate
                        {
                            if (support.returnTotalCost() <=
                                FactionColonies.calculateMilitaryLevelPoints(settlement.settlementMilitaryLevel))
                            {
                                if (settlement.buildings.Contains(BuildingFCDefOf.artilleryOutpost))
                                {
                                    if (settlement.artilleryTimer <= Find.TickManager.TicksGame)
                                    {
                                        if (PaymentUtil.getSilver() >= cost)
                                        {
                                            FactionColonies.FireSupport(settlement, support);
                                            Find.WindowStack.TryRemove(typeof(militaryCustomizationWindowFC));
                                        }
                                        else
                                        {
                                            Messages.Message(
                                                "You lack the required amount of silver to use that firesupport option!",
                                                MessageTypeDefOf.RejectInput);
                                        }
                                    }
                                    else
                                    {
                                        Messages.Message(
                                            "That firesupport option is on cooldown for another " +
                                            (settlement.artilleryTimer - Find.TickManager.TicksGame)
                                            .ToStringTicksToDays(), MessageTypeDefOf.RejectInput);
                                    }
                                }
                                else
                                {
                                    Messages.Message(
                                        "The settlement requires an artillery outpost to be built to use that firesupport option",
                                        MessageTypeDefOf.RejectInput);
                                }
                            }
                            else
                            {
                                Messages.Message(
                                    "The settlement requires a higher military level to use that fire support!",
                                    MessageTypeDefOf.RejectInput);
                            }
                        });
                        list.Add(option);
                    }

                    if (!list.Any())
                    {
                        list.Add(new FloatMenuOption("No fire supports currently made. Make one", delegate { }));
                    }

                    FloatMenu menu = new FloatMenuSearchable(list);
                    Find.WindowStack.Add(menu);
                }

                count++;
            }

            //Reset Text anchor and font
            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;


            if (Event.current.type == EventType.ScrollWheel)
            {
                scrollWindow(Event.current.delta.y, settlementMaxScroll);
            }
        }

        public void DrawTabSquad(Rect inRect)
        {
            //set text anchor and font
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;

            Rect SelectionBar = new Rect(5, 45, 200, 30);
            Rect importButton = new Rect(5, SelectionBar.y + SelectionBar.height + 10, 200, 30);
            Rect nameTextField = new Rect(5, importButton.y + importButton.height + 10, 250, 30);
            Rect isTrader = new Rect(5, nameTextField.y + nameTextField.height + 10, 130, 30);

            Rect UnitStandBase = new Rect(170, 220, 50, 30);
            Rect EquipmentTotalCost = new Rect(350, 50, 450, 40);
            Rect ResetButton = new Rect(700, 100, 100, 30);
            Rect DeleteButton = new Rect(ResetButton.x, ResetButton.y + ResetButton.height + 5, ResetButton.width,
                ResetButton.height);
            Rect PointRefButton = new Rect(DeleteButton.x, DeleteButton.y + DeleteButton.height + 5, DeleteButton.width,
                DeleteButton.height);
            Rect SaveSquadButton = new Rect(DeleteButton.x, PointRefButton.y + DeleteButton.height + 5,
                DeleteButton.width, DeleteButton.height);

            //If squad is not selected
            if (Widgets.CustomButtonText(ref SelectionBar, selectedText, Color.gray, Color.white, Color.black))
            {
                //check null
                if (util.squads == null)
                {
                    util.resetSquads();
                }

                List<FloatMenuOption> squads = new List<FloatMenuOption>
                {
                    new FloatMenuOption("Create New Squad", delegate
                    {
                        MilSquadFC newSquad = new MilSquadFC(true)
                        {
                            name = $"New Squad {(util.squads.Count + 1).ToString()}"
                        };
                        selectedText = newSquad.name;
                        selectedSquad = newSquad;
                        selectedSquad.newSquad();
                        util.squads.Add(newSquad);
                    })
                };

                //Create list of selectable units
                squads.AddRange(util.squads.Select(squad => new FloatMenuOption(squad.name, delegate
                {
                    //Unit is selected
                    selectedText = squad.name;
                    selectedSquad = squad;
                    selectedSquad.updateEquipmentTotalCost();
                })));
                FloatMenu selection = new FloatMenuSearchable(squads);
                Find.WindowStack.Add(selection);
            }

            if (Widgets.ButtonText(importButton, "Import Squad"))
            {
                Find.WindowStack.Add(new Dialog_ManageSquadExportsFC(
                    FactionColoniesMilitary.SavedSquads.ToList()));
            }


            //if squad is selected
            if (selectedSquad != null)
            {
                Text.Anchor = TextAnchor.MiddleLeft;
                Text.Font = GameFont.Small;

                if (settlementPointReference != null)
                {
                    Widgets.Label(EquipmentTotalCost, "Total Squad Equipment Cost: " +
                                                      selectedSquad.equipmentTotalCost +
                                                      " / " + FactionColonies
                                                          .calculateMilitaryLevelPoints(settlementPointReference
                                                              .settlementMilitaryLevel) +
                                                      " (Max Cost)");
                }
                else
                {
                    Widgets.Label(EquipmentTotalCost, "Total Squad Equipment Cost: " +
                                                      selectedSquad.equipmentTotalCost +
                                                      " / " + "No Reference");
                }

                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.UpperCenter;


                Widgets.CheckboxLabeled(isTrader, "is Trader Caravan", ref selectedSquad.isTraderCaravan);
                selectedSquad.setTraderCaravan(selectedSquad.isTraderCaravan);
                
                //Unit Name
                selectedSquad.name = Widgets.TextField(nameTextField, selectedSquad.name);

                if (Widgets.ButtonText(ResetButton, "Reset to Default"))
                {
                    selectedSquad.newSquad();
                }

                if (Widgets.ButtonText(DeleteButton, "Delete Squad"))
                {
                    selectedSquad.deleteSquad();
                    util.checkMilitaryUtilForErrors();
                    selectedSquad = null;
                    selectedText = "Select A Squad";

                    //Reset Text anchor and font
                    Text.Font = fontBefore;
                    Text.Anchor = anchorBefore;
                    return;
                }

                if (Widgets.ButtonText(PointRefButton, "Set Point Ref"))
                {
                    List<FloatMenuOption> settlementList = Find.World.GetComponent<FactionFC>()
                        .settlements.Select(settlement => new FloatMenuOption(settlement.name + " - Military Level : " +
                                                                              settlement.settlementMilitaryLevel,
                            delegate
                            {
                                //set points
                                settlementPointReference = settlement;
                            }))
                        .ToList();

                    if (!settlementList.Any())
                    {
                        settlementList.Add(new FloatMenuOption("No Valid Settlements", null));
                    }

                    FloatMenu floatMenu = new FloatMenu(settlementList) {vanishIfMouseDistant = true};
                    Find.WindowStack.Add(floatMenu);
                }

                if (Widgets.ButtonText(SaveSquadButton, "Export Squad"))
                {
                    // TODO: Confirm if squad with name already exists
                    FactionColoniesMilitary.SaveSquad(new SavedSquadFC(selectedSquad));
                    Messages.Message("ExportSquad".Translate(), MessageTypeDefOf.TaskCompletion);
                }

                //for (int k = 0; k < 30; k++)
                //{
                //	Widgets.ButtonImage(new Rect(UnitStandBase.x + (k * 15), UnitStandBase.y + ((k % 5) * 70), 50, 20), texLoad.unitCircle);
                //}


                for (int k = 0; k < 30; k++)
                {
                    if (Widgets.ButtonImage(new Rect(UnitStandBase.x + k % 6 * 80,
                        UnitStandBase.y + (k - k % 6) / 5 * 70,
                        50, 20), TexLoad.unitCircle))
                    {
                        int click = k;
                        //Option to clear unit slot
                        List<FloatMenuOption> units = new List<FloatMenuOption>
                        {
                            new FloatMenuOption("clearUnitSlot".Translate(), delegate
                            {
                                //Log.Message(selectedSquad.units.Count().ToString());
                                //Log.Message(click.ToString());
                                selectedSquad.units[click] = new MilUnitFC(true);
                                selectedSquad.updateEquipmentTotalCost();
                                selectedSquad.ChangeTick();
                            })
                        };

                        //Create list of selectable units
                        units.AddRange(util.units.Select(unit => new FloatMenuOption(unit.name +
                            " - Cost: " + unit.equipmentTotalCost, delegate
                            {
                                //Unit is selected
                                selectedSquad.units[click] = unit;
                                selectedSquad.updateEquipmentTotalCost();
                                selectedSquad.ChangeTick();
                            })));

                        FloatMenu selection = new FloatMenuSearchable(units);
                        Find.WindowStack.Add(selection);
                    }

                    if (selectedSquad.units[k].isBlank) continue;
                    if (selectedSquad.units.ElementAt(k).animal != null)
                    {
                        Widgets.ButtonImage(
                            new Rect(UnitStandBase.x + 15 + ((k % 6) * 80), UnitStandBase.y - 45 + (k - k % 6) / 5 * 70,
                                60, 60), selectedSquad.units.ElementAt(k).animal.race.uiIcon);
                    }

                    Widgets.ThingIcon(
                        new Rect(UnitStandBase.x - 5 + ((k % 6) * 80), UnitStandBase.y - 45 + (k - k % 6) / 5 * 70, 60,
                            60), selectedSquad.units.ElementAt(k).defaultPawn);
                    if (selectedSquad.units.ElementAt(k).defaultPawn.equipment.AllEquipmentListForReading.Count > 0)
                    {
                        Widgets.ThingIcon(
                            new Rect(UnitStandBase.x - 5 + ((k % 6) * 80), UnitStandBase.y - 15 + (k - k % 6) / 5 * 70,
                                40, 40),
                            selectedSquad.units.ElementAt(k).defaultPawn.equipment.AllEquipmentListForReading[0]);
                    }

                    Widgets.Label(
                        new Rect(UnitStandBase.x - 15 + ((k % 6) * 80), UnitStandBase.y - 65 + (k - k % 6) / 5 * 70, 80,
                            60), selectedSquad.units.ElementAt(k).name);
                }

                //Reset Text anchor and font
                Text.Font = fontBefore;
                Text.Anchor = anchorBefore;
            }

            //Reset Text anchor and font
            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }

        public void DrawTabUnit(Rect inRect)
        {
            Rect SelectionBar = new Rect(5, 45, 200, 30);
            Rect importButton = new Rect(5, SelectionBar.y + SelectionBar.height + 10, 200, 30);
            Rect nameTextField = new Rect(5, importButton.y + importButton.height + 10, 250, 30);
            Rect isCivilian = new Rect(5, nameTextField.y + nameTextField.height + 10, 100, 30);
            Rect isTrader = new Rect(isCivilian.x, isCivilian.y + isCivilian.height + 5, isCivilian.width,
                isCivilian.height);

            Rect unitIcon = new Rect(560, 235, 120, 120);
            Rect animalIcon = new Rect(560, 335, 120, 120);

            Rect ApparelHead = new Rect(600, 140, 50, 50);
            Rect ApparelTorsoSkin = new Rect(700, 170, 50, 50);
            Rect ApparelBelt = new Rect(700, 240, 50, 50);
            Rect ApparelLegs = new Rect(700, 310, 50, 50);

            Rect AnimalCompanion = new Rect(500, 160, 50, 50);
            Rect ApparelTorsoShell = new Rect(500, 230, 50, 50);
            Rect ApparelTorsoMiddle = new Rect(500, 310, 50, 50);
            Rect EquipmentWeapon = new Rect(440, 230, 50, 50);

            Rect ApparelWornItems = new Rect(440, 385, 330, 175);
            Rect EquipmentTotalCost = new Rect(450, 50, 350, 40);

            Rect ResetButton = new Rect(700, 50, 100, 30);
            Rect DeleteButton = new Rect(ResetButton.x, ResetButton.y + ResetButton.height + 5,
                ResetButton.width,
                ResetButton.height);
            Rect SavePawn = new Rect(DeleteButton.x, DeleteButton.y + DeleteButton.height + 5,
                DeleteButton.width,
                DeleteButton.height);
            Rect ChangeRace = new Rect(325, ResetButton.y, SavePawn.width, SavePawn.height);
            Rect RollNewPawn = new Rect(325, ResetButton.y + SavePawn.height + 5, SavePawn.width,
                SavePawn.height);

            if (Widgets.CustomButtonText(ref SelectionBar, selectedText, Color.gray, Color.white, Color.black))
            {
                List<FloatMenuOption> Units = new List<FloatMenuOption>
                {
                    new FloatMenuOption("Create New Unit", delegate
                    {
                        MilUnitFC newUnit = new MilUnitFC(false)
                        {
                            name = $"New Unit {util.units.Count() + 1}"
                        };
                        selectedText = newUnit.name;
                        selectedUnit = newUnit;
                        util.units.Add(newUnit);
                        newUnit.unequipAllEquipment();
                    })
                };

                //Option to create new unit

                //Create list of selectable units
                foreach (MilUnitFC unit in util.units)
                {
                    void action()
                    {
                        selectedText = unit.name;
                        selectedUnit = unit;
                    }

                    //Prevent units being modified when their squads are deployed
                    FactionFC factionFC = Find.World.GetComponent<FactionFC>();
                    List<MilSquadFC> squadsContainingUnit = factionFC?.militaryCustomizationUtil?.squads.Where(squad => squad?.units != null && squad.units.Contains(unit)).ToList();
                    List<SettlementFC> settlementsContainingSquad = factionFC?.settlements?.Where(settlement => settlement?.militarySquad?.outfit != null && squadsContainingUnit.Any(squad => settlement.militarySquad.outfit == squad)).ToList();

                    if ((settlementsContainingSquad?.Count ?? 0) > 0)
                    {
                        if (settlementsContainingSquad.Any(settlement => settlement.militarySquad.isDeployed))
                        {
                            Units.Add(new FloatMenuOption(unit.name, delegate { Messages.Message("CantBeModified".Translate(unit.name, "ReasonDeployed".Translate()), MessageTypeDefOf.NeutralEvent, false); }));
                            continue;
                        }
                        else if (settlementsContainingSquad.Any(settlement => settlement.isUnderAttack && settlementsContainingSquad.Contains(settlement.worldSettlement.defenderForce.homeSettlement)))
                        {
                            Units.Add(new FloatMenuOption(unit.name, delegate { Messages.Message("CantBeModified".Translate(unit.name, "ReasonDefending".Translate()), MessageTypeDefOf.NeutralEvent, false); }));
                            continue;
                        }
                    } 
                    
                    if (unit.defaultPawn.equipment.Primary != null)
                    {
                        Units.Add(new FloatMenuOption(unit.name, action, unit.defaultPawn.equipment.Primary.def));
                    }
                    else
                    {
                        Units.Add(new FloatMenuOption(unit.name, action));
                    }
                }

                FloatMenu selection = new FloatMenuSearchable(Units);
                Find.WindowStack.Add(selection);
            }

            if (Widgets.ButtonText(importButton, "importUnit".Translate()))
            {
                Find.WindowStack.Add(new Dialog_ManageUnitExportsFC(
                    FactionColoniesMilitary.SavedUnits.ToList()));
            }

            //Worn Items
            Widgets.DrawMenuSection(ApparelWornItems);

            //set text anchor and font
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;

            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperCenter;

            //if unit is not selected
            Widgets.Label(new Rect(new Vector2(ApparelHead.x, ApparelHead.y - 15), ApparelHead.size), "fcLabelHead".Translate());
            Widgets.DrawMenuSection(ApparelHead);
            Widgets.Label(
                new Rect(new Vector2(ApparelTorsoSkin.x, ApparelTorsoSkin.y - 15), ApparelTorsoSkin.size),
                "fcLabelShirt".Translate());
            Widgets.DrawMenuSection(ApparelTorsoSkin);
            Widgets.Label(
                new Rect(new Vector2(ApparelTorsoMiddle.x, ApparelTorsoMiddle.y - 15), ApparelTorsoMiddle.size),
                "fcLabelChest".Translate());
            Widgets.DrawMenuSection(ApparelTorsoMiddle);
            Widgets.Label(
                new Rect(new Vector2(ApparelTorsoShell.x, ApparelTorsoShell.y - 15), ApparelTorsoShell.size),
                "fcLabelOver".Translate());
            Widgets.DrawMenuSection(ApparelTorsoShell);
            Widgets.Label(new Rect(new Vector2(ApparelBelt.x, ApparelBelt.y - 15), ApparelBelt.size), "fcLabelBelt".Translate());
            Widgets.DrawMenuSection(ApparelBelt);
            Widgets.Label(new Rect(new Vector2(ApparelLegs.x, ApparelLegs.y - 15), ApparelLegs.size), "fcLabelPants".Translate());
            Widgets.DrawMenuSection(ApparelLegs);
            Widgets.Label(
                new Rect(new Vector2(EquipmentWeapon.x, EquipmentWeapon.y - 15), EquipmentWeapon.size),
                "fcLabelWeapon".Translate());
            Widgets.DrawMenuSection(EquipmentWeapon);
            Widgets.Label(
                new Rect(new Vector2(AnimalCompanion.x, AnimalCompanion.y - 15), AnimalCompanion.size),
                "fcLabelAnimal".Translate());
            Widgets.DrawMenuSection(AnimalCompanion);

            //Reset Text anchor and font
            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;

            //if unit is selected
            if (selectedUnit == null) return;
            Text.Font = GameFont.Tiny;
            Text.Anchor = TextAnchor.UpperCenter;

            if (Widgets.ButtonText(ResetButton, "resetUnitToDefaultButton".Translate()))
            {
                selectedUnit.unequipAllEquipment();
            }

            if (Widgets.ButtonText(DeleteButton, "deleteUnitButton".Translate()))
            {
                selectedUnit.removeUnit();
                util.checkMilitaryUtilForErrors();
                selectedUnit = null;
                selectedText = "selectAUnitButton".Translate();

                //Reset Text anchor and font
                Text.Font = fontBefore;
                Text.Anchor = anchorBefore;
                return;
            }

            if (Widgets.ButtonText(RollNewPawn, "rollANewUnitButton".Translate()))
            {
                selectedUnit.generateDefaultPawn();
            }

            if (Widgets.ButtonText(ChangeRace, "changeUnitRaceButton".Translate()))
            {
                List<string> races = new List<string>();
                List<FloatMenuOption> options = new List<FloatMenuOption>();

                foreach (PawnKindDef def in DefDatabase<PawnKindDef>.AllDefsListForReading.Where(def => def.IsHumanLikeRace() && !races.Contains(def.race.label) && faction.raceFilter.Allows(def.race)))
                {
                    if (def.race == ThingDefOf.Human && def.LabelCap != "Colonist") continue;
                    races.Add(def.race.label);

                    string optionStr = def.race.label.CapitalizeFirst() + " - Cost: " + Math.Floor(def.race.BaseMarketValue * FactionColonies.militaryRaceCostMultiplier);
                    options.Add(new FloatMenuOption(optionStr, delegate
                    {
                        selectedUnit.pawnKind = def;
                        selectedUnit.generateDefaultPawn();
                        selectedUnit.changeTick();
                    }));
                }

                if (!options.Any())
                {
                    options.Add(new FloatMenuOption("changeUnitRaceNoRaces".Translate(), null));
                }

                options.Sort(FactionColonies.CompareFloatMenuOption);
                FloatMenu menu = new FloatMenuSearchable(options);
                Find.WindowStack.Add(menu);
            }

            if (Widgets.ButtonText(SavePawn, "exportUnitButton".Translate()))
            {
                // TODO: confirm
                FactionColoniesMilitary.SaveUnit(new SavedUnitFC(selectedUnit));
                Messages.Message("ExportUnit".Translate(), MessageTypeDefOf.TaskCompletion);
            }

            //Unit Name
            selectedUnit.name = Widgets.TextField(nameTextField, selectedUnit.name);

            Widgets.CheckboxLabeled(isCivilian, "unitIsCivilianLabel".Translate(), ref selectedUnit.isCivilian);
            Widgets.CheckboxLabeled(isTrader, "unitIsTraderLabel".Translate(), ref selectedUnit.isTrader);
            selectedUnit.setTrader(selectedUnit.isTrader);
            selectedUnit.setCivilian(selectedUnit.isCivilian);

            //Reset Text anchor and font
            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
            //Draw Pawn
            if (selectedUnit.defaultPawn != null)
            {
                if (selectedUnit.animal != null)
                {
                    //Widgets.DrawTextureFitted(animalIcon, selectedUnit.animal.race.graphicData.Graphic.MatNorth.mainTexture, 1);
                }

                Widgets.ThingIcon(unitIcon, selectedUnit.defaultPawn);
            }

            //Animal Companion
            if (Widgets.ButtonInvisible(AnimalCompanion))
            {
                List<FloatMenuOption> list = (from animal in DefDatabase<PawnKindDef>.AllDefs
                    where animal.IsAnimalAndAllowed()
                    select new FloatMenuOption(animal.LabelCap + " - Cost: " +
                                               Math.Floor(animal.race.BaseMarketValue *
                                                          FactionColonies.militaryAnimalCostMultiplier),
                        delegate
                        {
                            //Do add animal code here
                            selectedUnit.animal = animal;
                        }, animal.race.uiIcon, Color.white)).ToList();

                list.Sort(FactionColonies.CompareFloatMenuOption);

                list.Insert(0, new FloatMenuOption("unitActionUnequipThing".Translate(), delegate
                {
                    //unequip here
                    selectedUnit.animal = null;
                }));
                FloatMenu menu = new FloatMenuSearchable(list);
                Find.WindowStack.Add(menu);
            }

            //Weapon Equipment
            if (Widgets.ButtonInvisible(EquipmentWeapon))
            {
                List<FloatMenuOption> list = (from thing in DefDatabase<ThingDef>.AllDefs
                    where thing.IsWeapon && thing.BaseMarketValue != 0 && FactionColonies.canCraftItem(thing)
                    where true
                    select new FloatMenuOption(thing.LabelCap + " - Cost: " + thing.BaseMarketValue, delegate
                    {
                        if (thing.MadeFromStuff)
                        {
                            //If made from stuff
                            List<FloatMenuOption> stuffList = (from stuff in DefDatabase<ThingDef>.AllDefs
                                where stuff.IsStuff &&
                                      thing.stuffCategories.SharesElementWith(stuff.stuffProps.categories)
                                select new FloatMenuOption(stuff.LabelCap + " - Total Value: " +
                                                           StatWorker_MarketValue.CalculatedBaseMarketValue(
                                                               thing,
                                                               stuff),
                                    delegate
                                    {
                                        selectedUnit.equipWeapon(
                                            ThingMaker.MakeThing(thing, stuff) as ThingWithComps);
                                    })).ToList();

                            stuffList.Sort(FactionColonies.CompareFloatMenuOption);
                            FloatMenu stuffWindow = new FloatMenuSearchable(stuffList);
                            Find.WindowStack.Add(stuffWindow);
                        }
                        else
                        {
                            //If not made from stuff

                            selectedUnit.equipWeapon(ThingMaker.MakeThing(thing) as ThingWithComps);
                        }
                    }, thing)).ToList();


                list.Sort(FactionColonies.CompareFloatMenuOption);

                list.Insert(0, new FloatMenuOption("unitActionUnequipThing".Translate(), delegate { selectedUnit.unequipWeapon(); }));

                FloatMenu menu = new FloatMenuSearchable(list);

                Find.WindowStack.Add(menu);
            }

            //headgear Slot
            if (Widgets.ButtonInvisible(ApparelHead))
            {
                List<FloatMenuOption> headgearList = new List<FloatMenuOption>();


                foreach (ThingDef thing in DefDatabase<ThingDef>.AllDefs)
                {
                    if (thing.IsApparel)
                    {
                        if (thing.apparel.layers.Contains(ApparelLayerDefOf.Overhead) &&
                            FactionColonies.canCraftItem(thing))
                        {
                            headgearList.Add(new FloatMenuOption(
                                thing.LabelCap + " - Cost: " + thing.BaseMarketValue,
                                delegate
                                {
                                    if (thing.MadeFromStuff)
                                    {
                                        //If made from stuff
                                        List<FloatMenuOption> stuffList = new List<FloatMenuOption>();
                                        foreach (ThingDef stuff in DefDatabase<ThingDef>.AllDefs)
                                        {
                                            if (stuff.IsStuff &&
                                                thing.stuffCategories.SharesElementWith(stuff.stuffProps
                                                    .categories))
                                            {
                                                stuffList.Add(new FloatMenuOption(
                                                    stuff.LabelCap + " - Total Value: " +
                                                    (StatWorker_MarketValue.CalculatedBaseMarketValue(thing,
                                                        stuff)),
                                                    delegate
                                                    {
                                                        selectedUnit.wearEquipment(
                                                            ThingMaker.MakeThing(thing, stuff) as Apparel,
                                                            true);
                                                    }));
                                            }
                                        }

                                        stuffList.Sort(FactionColonies.CompareFloatMenuOption);
                                        FloatMenu stuffWindow = new FloatMenuSearchable(stuffList);
                                        Find.WindowStack.Add(stuffWindow);
                                    }
                                    else
                                    {
                                        //If not made from stuff

                                        selectedUnit.wearEquipment(ThingMaker.MakeThing(thing) as Apparel,
                                            true);
                                    }
                                }, thing));
                        }
                    }
                }

                headgearList.Sort(FactionColonies.CompareFloatMenuOption);

                headgearList.Insert(0, new FloatMenuOption("unitActionUnequipThing".Translate(), delegate
                {
                    //Remove old
                    foreach (Apparel apparel in selectedUnit.defaultPawn.apparel.WornApparel
                        .Where(apparel => apparel.def.apparel.layers.Contains(ApparelLayerDefOf.Overhead)))
                    {
                        selectedUnit.defaultPawn.apparel.Remove(apparel);
                        break;
                    }
                }));

                FloatMenu menu = new FloatMenuSearchable(headgearList);

                Find.WindowStack.Add(menu);
            }


            //Torso Shell Slot
            if (Widgets.ButtonInvisible(ApparelTorsoShell))
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();


                foreach (ThingDef thing in DefDatabase<ThingDef>.AllDefs)
                {
                    if (thing.IsApparel)
                    {
                        if (thing.apparel.layers.Contains(ApparelLayerDefOf.Shell) &&
                            thing.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso) &&
                            FactionColonies.canCraftItem(thing)) //CHANGE THIS
                        {
                            list.Add(new FloatMenuOption(thing.LabelCap + " - Cost: " + thing.BaseMarketValue,
                                delegate
                                {
                                    if (thing.MadeFromStuff)
                                    {
                                        //If made from stuff
                                        List<FloatMenuOption> stuffList = new List<FloatMenuOption>();
                                        foreach (ThingDef stuff in DefDatabase<ThingDef>.AllDefs)
                                        {
                                            if (stuff.IsStuff &&
                                                thing.stuffCategories.SharesElementWith(stuff.stuffProps
                                                    .categories))
                                            {
                                                stuffList.Add(new FloatMenuOption(
                                                    stuff.LabelCap + " - Total Value: " +
                                                    (StatWorker_MarketValue.CalculatedBaseMarketValue(thing,
                                                        stuff)),
                                                    delegate
                                                    {
                                                        selectedUnit.wearEquipment(
                                                            ThingMaker.MakeThing(thing, stuff) as Apparel,
                                                            true);
                                                    }));
                                            }
                                        }

                                        stuffList.Sort(FactionColonies.CompareFloatMenuOption);
                                        FloatMenu stuffWindow = new FloatMenuSearchable(stuffList);
                                        Find.WindowStack.Add(stuffWindow);
                                    }
                                    else
                                    {
                                        //If not made from stuff

                                        selectedUnit.wearEquipment(ThingMaker.MakeThing(thing) as Apparel,
                                            true);
                                    }
                                }, thing));
                        }
                    }
                }

                list.Sort(FactionColonies.CompareFloatMenuOption);

                list.Insert(0, new FloatMenuOption("unitActionUnequipThing".Translate(), delegate
                {
                    //Remove old
                    foreach (Apparel apparel in selectedUnit.defaultPawn.apparel.WornApparel)
                    {
                        if (apparel.def.apparel.layers.Contains(ApparelLayerDefOf.Shell) &&
                            apparel.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso)) //CHANGE THIS
                        {
                            selectedUnit.defaultPawn.apparel.Remove(apparel);
                            break;
                        }
                    }
                }));
                FloatMenu menu = new FloatMenuSearchable(list);

                Find.WindowStack.Add(menu);
            }


            //Torso Middle Slot
            if (Widgets.ButtonInvisible(ApparelTorsoMiddle))
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();


                foreach (ThingDef thing in DefDatabase<ThingDef>.AllDefs)
                {
                    if (thing.IsApparel)
                    {
                        if (thing.apparel.layers.Contains(ApparelLayerDefOf.Middle) &&
                            thing.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso) &&
                            FactionColonies.canCraftItem(thing)) //CHANGE THIS
                        {
                            list.Add(new FloatMenuOption(thing.LabelCap + " - Cost: " + thing.BaseMarketValue,
                                delegate
                                {
                                    if (thing.MadeFromStuff)
                                    {
                                        //If made from stuff
                                        List<FloatMenuOption> stuffList = new List<FloatMenuOption>();
                                        foreach (ThingDef stuff in DefDatabase<ThingDef>.AllDefs)
                                        {
                                            if (stuff.IsStuff &&
                                                thing.stuffCategories.SharesElementWith(stuff.stuffProps
                                                    .categories))
                                            {
                                                stuffList.Add(new FloatMenuOption(
                                                    stuff.LabelCap + " - Total Value: " +
                                                    (StatWorker_MarketValue.CalculatedBaseMarketValue(thing,
                                                        stuff)),
                                                    delegate
                                                    {
                                                        selectedUnit.wearEquipment(
                                                            ThingMaker.MakeThing(thing, stuff) as Apparel,
                                                            true);
                                                    }));
                                            }
                                        }

                                        stuffList.Sort(FactionColonies.CompareFloatMenuOption);
                                        FloatMenu stuffWindow = new FloatMenuSearchable(stuffList);
                                        Find.WindowStack.Add(stuffWindow);
                                    }
                                    else
                                    {
                                        //If not made from stuff

                                        selectedUnit.wearEquipment(ThingMaker.MakeThing(thing) as Apparel,
                                            true);
                                    }
                                }, thing));
                        }
                    }
                }

                list.Sort(FactionColonies.CompareFloatMenuOption);

                list.Insert(0, new FloatMenuOption("unitActionUnequipThing".Translate(), delegate
                {
                    //Remove old
                    foreach (Apparel apparel in selectedUnit.defaultPawn.apparel.WornApparel)
                    {
                        if (apparel.def.apparel.layers.Contains(ApparelLayerDefOf.Middle) &&
                            apparel.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso)) //CHANGE THIS
                        {
                            selectedUnit.defaultPawn.apparel.Remove(apparel);
                            break;
                        }
                    }
                }));
                FloatMenu menu = new FloatMenuSearchable(list);

                Find.WindowStack.Add(menu);
            }


            //Torso Skin Slot
            if (Widgets.ButtonInvisible(ApparelTorsoSkin))
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();


                foreach (ThingDef thing in DefDatabase<ThingDef>.AllDefs)
                {
                    if (thing.IsApparel)
                    {
                        if (thing.apparel.layers.Contains(ApparelLayerDefOf.OnSkin) &&
                            thing.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso) &&
                            FactionColonies.canCraftItem(thing)) //CHANGE THIS
                        {
                            list.Add(new FloatMenuOption(thing.LabelCap + " - Cost: " + thing.BaseMarketValue,
                                delegate
                                {
                                    if (thing.MadeFromStuff)
                                    {
                                        //If made from stuff
                                        List<FloatMenuOption> stuffList = new List<FloatMenuOption>();
                                        foreach (ThingDef stuff in DefDatabase<ThingDef>.AllDefs)
                                        {
                                            if (stuff.IsStuff &&
                                                thing.stuffCategories.SharesElementWith(stuff.stuffProps
                                                    .categories))
                                            {
                                                stuffList.Add(new FloatMenuOption(
                                                    stuff.LabelCap + " - Total Value: " +
                                                    (StatWorker_MarketValue.CalculatedBaseMarketValue(thing,
                                                        stuff)),
                                                    delegate
                                                    {
                                                        selectedUnit.wearEquipment(
                                                            ThingMaker.MakeThing(thing, stuff) as Apparel,
                                                            true);
                                                    }));
                                            }
                                        }

                                        stuffList.Sort(FactionColonies.CompareFloatMenuOption);
                                        FloatMenu stuffWindow = new FloatMenuSearchable(stuffList);
                                        Find.WindowStack.Add(stuffWindow);
                                    }
                                    else
                                    {
                                        //If not made from stuff

                                        selectedUnit.wearEquipment(ThingMaker.MakeThing(thing) as Apparel,
                                            true);
                                    }
                                }, thing));
                        }
                    }
                }

                list.Sort(FactionColonies.CompareFloatMenuOption);


                list.Insert(0, new FloatMenuOption("unitActionUnequipThing".Translate(), delegate
                {
                    //Remove old
                    foreach (Apparel apparel in selectedUnit.defaultPawn.apparel.WornApparel)
                    {
                        if (apparel.def.apparel.layers.Contains(ApparelLayerDefOf.OnSkin) &&
                            apparel.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso)) //CHANGE THIS
                        {
                            selectedUnit.defaultPawn.apparel.Remove(apparel);
                            break;
                        }
                    }
                }));
                FloatMenu menu = new FloatMenuSearchable(list);

                Find.WindowStack.Add(menu);
            }


            //Pants Slot
            if (Widgets.ButtonInvisible(ApparelLegs))
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();

                foreach (ThingDef thing in DefDatabase<ThingDef>.AllDefs)
                {
                    if (thing.IsApparel)
                    {
                        if (thing.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Legs) &&
                            thing.apparel.layers.Contains(ApparelLayerDefOf.OnSkin) &&
                            FactionColonies.canCraftItem(thing)) //CHANGE THIS
                        {
                            list.Add(new FloatMenuOption(thing.LabelCap + " - Cost: " + thing.BaseMarketValue,
                                delegate
                                {
                                    if (thing.MadeFromStuff)
                                    {
                                        //If made from stuff
                                        List<FloatMenuOption> stuffList = new List<FloatMenuOption>();
                                        foreach (ThingDef stuff in DefDatabase<ThingDef>.AllDefs)
                                        {
                                            if (stuff.IsStuff &&
                                                thing.stuffCategories.SharesElementWith(stuff.stuffProps
                                                    .categories))
                                            {
                                                stuffList.Add(new FloatMenuOption(
                                                    stuff.LabelCap + " - Total Value: " +
                                                    (StatWorker_MarketValue.CalculatedBaseMarketValue(thing,
                                                        stuff)),
                                                    delegate
                                                    {
                                                        selectedUnit.wearEquipment(
                                                            ThingMaker.MakeThing(thing, stuff) as Apparel,
                                                            true);
                                                    }));
                                            }
                                        }

                                        stuffList.Sort(FactionColonies.CompareFloatMenuOption);
                                        FloatMenu stuffWindow = new FloatMenuSearchable(stuffList);
                                        Find.WindowStack.Add(stuffWindow);
                                    }
                                    else
                                    {
                                        //If not made from stuff

                                        selectedUnit.wearEquipment(ThingMaker.MakeThing(thing) as Apparel,
                                            true);
                                    }
                                }, thing));
                        }
                    }
                }

                list.Sort(FactionColonies.CompareFloatMenuOption);

                list.Insert(0, new FloatMenuOption("unitActionUnequipThing".Translate(), delegate
                {
                    //Remove old
                    foreach (Apparel apparel in selectedUnit.defaultPawn.apparel.WornApparel)
                    {
                        if (apparel.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Legs) &&
                            apparel.def.apparel.layers.Contains(ApparelLayerDefOf.OnSkin)) //CHANGE THIS
                        {
                            selectedUnit.defaultPawn.apparel.Remove(apparel);
                            break;
                        }
                    }
                }));
                FloatMenu menu = new FloatMenuSearchable(list);

                Find.WindowStack.Add(menu);
            }


            //Apparel Belt Slot
            if (Widgets.ButtonInvisible(ApparelBelt))
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>();

                foreach (ThingDef thing in DefDatabase<ThingDef>.AllDefs)
                {
                    if (thing.IsApparel)
                    {
                        if (thing.apparel.layers.Contains(ApparelLayerDefOf.Belt) &&
                            FactionColonies.canCraftItem(thing))
                        {
                            list.Add(new FloatMenuOption(thing.LabelCap + " - Cost: " + thing.BaseMarketValue,
                                delegate
                                {
                                    if (thing.MadeFromStuff)
                                    {
                                        //If made from stuff
                                        List<FloatMenuOption> stuffList = new List<FloatMenuOption>();
                                        foreach (ThingDef stuff in DefDatabase<ThingDef>.AllDefs)
                                        {
                                            if (stuff.IsStuff &&
                                                thing.stuffCategories.SharesElementWith(stuff.stuffProps
                                                    .categories))
                                            {
                                                stuffList.Add(new FloatMenuOption(
                                                    stuff.LabelCap + " - Total Value: " +
                                                    (StatWorker_MarketValue.CalculatedBaseMarketValue(thing,
                                                        stuff)),
                                                    delegate
                                                    {
                                                        selectedUnit.wearEquipment(
                                                            ThingMaker.MakeThing(thing, stuff) as Apparel,
                                                            true);
                                                    }));
                                            }
                                        }

                                        stuffList.Sort(FactionColonies.CompareFloatMenuOption);
                                        FloatMenu stuffWindow = new FloatMenuSearchable(stuffList);
                                        Find.WindowStack.Add(stuffWindow);
                                    }
                                    else
                                    {
                                        //If not made from stuff
                                        //Remove old equipment
                                        foreach (Apparel apparel in selectedUnit.defaultPawn.apparel
                                            .WornApparel)
                                        {
                                            if (apparel.def.apparel.layers.Contains(ApparelLayerDefOf.Belt))
                                            {
                                                selectedUnit.defaultPawn.apparel.Remove(apparel);
                                                break;
                                            }
                                        }

                                        selectedUnit.wearEquipment(ThingMaker.MakeThing(thing) as Apparel,
                                            true);
                                    }
                                }, thing));
                        }
                    }
                }

                list.Sort(FactionColonies.CompareFloatMenuOption);

                list.Insert(0, new FloatMenuOption("unitActionUnequipThing".Translate(), delegate
                {
                    //Remove old
                    foreach (Apparel apparel in selectedUnit.defaultPawn.apparel.WornApparel)
                    {
                        if (apparel.def.apparel.layers.Contains(ApparelLayerDefOf.Belt))
                        {
                            selectedUnit.defaultPawn.apparel.Remove(apparel);
                            break;
                        }
                    }
                }));
                FloatMenu menu = new FloatMenuSearchable(list);

                Find.WindowStack.Add(menu);
            }


            //worn items
            float totalCost = 0;
            int i = 0;

            totalCost += (float) Math.Floor(selectedUnit.defaultPawn.def.BaseMarketValue *
                                            FactionColonies.militaryRaceCostMultiplier);

            foreach (Thing thing in selectedUnit.defaultPawn.apparel.WornApparel.Concat(selectedUnit.defaultPawn
                .equipment.AllEquipmentListForReading))
            {
                Rect tmp = new Rect(ApparelWornItems.x, ApparelWornItems.y + i * 25, ApparelWornItems.width,
                    25);
                i++;

                totalCost += thing.MarketValue;

                if (Widgets.CustomButtonText(ref tmp, thing.LabelCap + " Cost: " + thing.MarketValue,
                    Color.white,
                    Color.black, Color.black))
                {
                    Find.WindowStack.Add(new Dialog_InfoCard(thing));
                }
            }

            if (selectedUnit.animal != null)
            {
                Widgets.ButtonImage(AnimalCompanion, selectedUnit.animal.race.uiIcon);
                totalCost += (float) Math.Floor(selectedUnit.animal.race.BaseMarketValue *
                                                FactionColonies.militaryAnimalCostMultiplier);
            }

            foreach (Thing thing in selectedUnit.defaultPawn.apparel.WornApparel)
            {
                //Log.Message(thing.Label);


                if (thing.def.apparel.layers.Contains(ApparelLayerDefOf.Overhead))
                {
                    Widgets.ButtonImage(ApparelHead, thing.def.uiIcon);
                }

                if (thing.def.apparel.layers.Contains(ApparelLayerDefOf.Belt))
                {
                    Widgets.ButtonImage(ApparelBelt, thing.def.uiIcon);
                }

                if (thing.def.apparel.layers.Contains(ApparelLayerDefOf.Shell) &&
                    thing.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso))
                {
                    Widgets.ButtonImage(ApparelTorsoShell, thing.def.uiIcon);
                }

                if (thing.def.apparel.layers.Contains(ApparelLayerDefOf.Middle) &&
                    thing.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso))
                {
                    Widgets.ButtonImage(ApparelTorsoMiddle, thing.def.uiIcon);
                }

                if (thing.def.apparel.layers.Contains(ApparelLayerDefOf.OnSkin) &&
                    thing.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Torso))
                {
                    Widgets.ButtonImage(ApparelTorsoSkin, thing.def.uiIcon);
                }

                if (thing.def.apparel.bodyPartGroups.Contains(BodyPartGroupDefOf.Legs) &&
                    thing.def.apparel.layers.Contains(ApparelLayerDefOf.OnSkin))
                {
                    Widgets.ButtonImage(ApparelLegs, thing.def.uiIcon);
                }
            }

            foreach (Thing thing in selectedUnit.defaultPawn.equipment.AllEquipmentListForReading)
            {
                Widgets.ButtonImage(EquipmentWeapon, thing.def.uiIcon);
            }

            totalCost = (float) Math.Ceiling(totalCost);
            Widgets.Label(EquipmentTotalCost, "totalEquipmentCostLabel".Translate() + totalCost);
        }


        public void DrawTabFireSupport(Rect inRect)
        {
            //set text anchor and font
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;


            float projectileBoxHeight = 30;
            Rect SelectionBar = new Rect(5, 45, 200, 30);
            Rect nameTextField = new Rect(5, 90, 250, 30);
            Rect floatRangeAccuracyLabel = new Rect(nameTextField.x, nameTextField.y + nameTextField.height + 5,
                nameTextField.width, (float) (nameTextField.height * 1.5));
            Rect floatRangeAccuracy = new Rect(floatRangeAccuracyLabel.x,
                floatRangeAccuracyLabel.y + floatRangeAccuracyLabel.height + 5, floatRangeAccuracyLabel.width,
                floatRangeAccuracyLabel.height);


            Rect UnitStandBase = new Rect(140, 200, 50, 30);
            Rect TotalCost = new Rect(325, 50, 450, 20);
            Rect numberProjectiles = new Rect(TotalCost.x, TotalCost.y + TotalCost.height + 5, TotalCost.width,
                TotalCost.height);
            Rect duration = new Rect(numberProjectiles.x, numberProjectiles.y + numberProjectiles.height + 5,
                numberProjectiles.width, numberProjectiles.height);

            Rect ResetButton = new Rect(700 - 2, 100, 100, 30);
            Rect DeleteButton = new Rect(ResetButton.x, ResetButton.y + ResetButton.height + 5,
                ResetButton.width,
                ResetButton.height);
            Rect PointRefButton = new Rect(DeleteButton.x, DeleteButton.y + DeleteButton.height + 5,
                DeleteButton.width,
                DeleteButton.height);


            //Up here to make sure it goes behind other layers
            if (selectedSupport != null)
            {
                DrawFireSupportBox(10, 230, 30);
            }

            Widgets.DrawMenuSection(new Rect(0, 0, 800, 225));

            //If firesupport is not selected
            if (Widgets.CustomButtonText(ref SelectionBar, selectedText, Color.gray, Color.white, Color.black))
            {
                List<FloatMenuOption> supports = new List<FloatMenuOption>();

                //Option to create new firesupport
                supports.Add(new FloatMenuOption("Create New Fire Support", delegate
                {
                    MilitaryFireSupport newFireSupport = new MilitaryFireSupport();
                    newFireSupport.name = "New Fire Support " + (util.fireSupportDefs.Count() + 1);
                    newFireSupport.setLoadID();
                    newFireSupport.projectiles = new List<ThingDef>();
                    selectedText = newFireSupport.name;
                    selectedSupport = newFireSupport;
                    util.fireSupportDefs.Add(newFireSupport);
                }));

                //Create list of selectable firesupports
                foreach (MilitaryFireSupport support in util.fireSupportDefs)
                {
                    supports.Add(new FloatMenuOption(support.name, delegate
                    {
                        //Unit is selected
                        selectedText = support.name;
                        selectedSupport = support;
                    }));
                }

                FloatMenu selection = new FloatMenuSearchable(supports);
                Find.WindowStack.Add(selection);
            }


            //if firesupport is selected
            if (selectedSupport != null)
            {
                //Need to adjust
                fireSupportMaxScroll =
                    selectedSupport.projectiles.Count() * projectileBoxHeight - 10 * projectileBoxHeight;

                Text.Anchor = TextAnchor.MiddleLeft;
                Text.Font = GameFont.Small;


                if (settlementPointReference != null)
                {
                    Widgets.Label(TotalCost,
                        "Total Fire Support Silver Cost: " + selectedSupport.returnTotalCost() + " / " +
                        FactionColonies.calculateMilitaryLevelPoints(settlementPointReference
                            .settlementMilitaryLevel) +
                        " (Max Cost)");
                }
                else
                {
                    Widgets.Label(TotalCost,
                        "Total Fire Support Silver Cost: " + selectedSupport.returnTotalCost() + " / " +
                        "No Reference");
                }

                Widgets.Label(numberProjectiles,
                    "Number of Projectiles: " + selectedSupport.projectiles.Count());
                Widgets.Label(duration,
                    "Duration of fire support: " + Math.Round(selectedSupport.projectiles.Count() * .25, 2) +
                    " seconds");
                Widgets.Label(floatRangeAccuracyLabel,
                    selectedSupport.accuracy +
                    " = Accuracy of fire support (In tiles radius): Affecting cost by : " +
                    selectedSupport.returnAccuracyCostPercentage() + "%");
                selectedSupport.accuracy = Widgets.HorizontalSlider(floatRangeAccuracy,
                    selectedSupport.accuracy,
                    Math.Max(3, (15 - Find.World.GetComponent<FactionFC>().returnHighestMilitaryLevel())), 30,
                    roundTo: 1);
                Text.Font = GameFont.Tiny;
                Text.Anchor = TextAnchor.UpperCenter;


                //Unit Name
                selectedSupport.name = Widgets.TextField(nameTextField, selectedSupport.name);

                if (Widgets.ButtonText(ResetButton, "Reset to Default"))
                {
                    selectedSupport.projectiles = new List<ThingDef>();
                }

                if (Widgets.ButtonText(DeleteButton, "Delete Support"))
                {
                    selectedSupport.delete();
                    util.checkMilitaryUtilForErrors();
                    selectedSupport = null;
                    selectedText = "Select A Fire Support";

                    //Reset Text anchor and font
                    Text.Font = fontBefore;
                    Text.Anchor = anchorBefore;
                    return;
                }

                if (Widgets.ButtonText(PointRefButton, "Set Point Ref"))
                {
                    List<FloatMenuOption> settlementList = new List<FloatMenuOption>();

                    foreach (SettlementFC settlement in Find.World.GetComponent<FactionFC>().settlements)
                    {
                        settlementList.Add(new FloatMenuOption(
                            settlement.name + " - Military Level : " + settlement.settlementMilitaryLevel,
                            delegate
                            {
                                //set points
                                settlementPointReference = settlement;
                            }));
                    }

                    if (!settlementList.Any())
                    {
                        settlementList.Add(new FloatMenuOption("No Valid Settlements", null));
                    }

                    FloatMenu floatMenu = new FloatMenu(settlementList);
                    Find.WindowStack.Add(floatMenu);
                }

                //Reset Text anchor and font
                Text.Font = fontBefore;
                Text.Anchor = anchorBefore;
            }

            if (Event.current.type == EventType.ScrollWheel)
            {
                scrollWindow(Event.current.delta.y, fireSupportMaxScroll);
            }

            //Reset Text anchor and font
            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }

        private void scrollWindow(float num, float maxScroll)
        {
            if (scroll - num * 5 < -1 * maxScroll)
            {
                scroll = -1f * maxScroll;
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

            //Log.Message(scroll.ToString());
        }

        public Rect deriveRectRow(Rect rect, float x, float y = 0, float width = 0, float height = 0)
        {
            float inputWidth;
            float inputHeight;
            if (width == 0)
            {
                inputWidth = rect.width;
            }
            else
            {
                inputWidth = width;
            }

            if (height == 0)
            {
                inputHeight = rect.height;
            }
            else
            {
                inputHeight = height;
            }

            Rect newRect = new Rect(rect.x + rect.width + x, rect.y + y, inputWidth, inputHeight);
            //Log.Message(newRect.width.ToString());
            return newRect;
        }

        public void DrawFireSupportBox(float x, float y, float rowHeight)
        {
            //Set Text anchor and font
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;
            Text.Anchor = TextAnchor.MiddleCenter;
            Text.Font = GameFont.Small;


            for (int i = 0; i <= selectedSupport.projectiles.Count(); i++)
            {
                //Declare Rects
                Rect text = new Rect(x + 2, y + 2 + i * rowHeight + scroll, rowHeight - 4, rowHeight - 4);
                Rect cost = deriveRectRow(text, 2, 0, 150);
                Rect icon = deriveRectRow(cost, 2, 0, 250);
                //Rect name = deriveRectRow(icon, 2, 0, 150);
                Rect options = deriveRectRow(icon, 2, 0, 74);
                Rect upArrow = deriveRectRow(options, 12, 0, rowHeight - 4, rowHeight - 4);
                Rect downArrow = deriveRectRow(upArrow, 4);
                Rect delete = deriveRectRow(downArrow, 12);
                //Create outside box last to encapsulate entirety
                Rect box = new Rect(x, y + i * rowHeight + scroll, delete.x + delete.width + 4 - x, rowHeight);


                Widgets.DrawHighlight(box);
                Widgets.DrawMenuSection(box);

                if (i == selectedSupport.projectiles.Count())
                {
                    //If on last row
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(text, i.ToString());
                    if (Widgets.ButtonTextSubtle(icon, "Add new projectile"))
                    {
                        //if creating new projectile
                        List<FloatMenuOption> thingOptions = new List<FloatMenuOption>();
                        foreach (ThingDef def in selectedSupport.returnFireSupportOptions())
                        {
                            thingOptions.Add(new FloatMenuOption(
                                def.LabelCap + " - " + Math.Round(def.BaseMarketValue * 1.5, 2).ToString(),
                                delegate { selectedSupport.projectiles.Add(def); }, def));
                        }

                        if (thingOptions.Count() == 0)
                        {
                            thingOptions.Add(
                                new FloatMenuOption("No available projectiles found", delegate { }));
                        }

                        Find.WindowStack.Add(new FloatMenu(thingOptions));
                    }
                }
                else
                {
                    //if on row with projectile
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(text, i.ToString());
                    if (Widgets.ButtonTextSubtle(icon, ""))
                    {
                        List<FloatMenuOption> thingOptions = new List<FloatMenuOption>();
                        foreach (ThingDef def in selectedSupport.returnFireSupportOptions())
                        {
                            int k = i;
                            thingOptions.Add(new FloatMenuOption(
                                def.LabelCap + " - " + Math.Round(def.BaseMarketValue * 1.5, 2).ToString(),
                                delegate { selectedSupport.projectiles[k] = def; }, def));
                        }

                        if (thingOptions.Count() == 0)
                        {
                            thingOptions.Add(
                                new FloatMenuOption("No available projectiles found", delegate { }));
                        }

                        Find.WindowStack.Add(new FloatMenu(thingOptions));
                    }

                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(cost,
                        "$ " + (Math.Round(selectedSupport.projectiles[i].BaseMarketValue * 1.5,
                            2))); //ADD in future mod setting for firesupport cost

                    Widgets.DefLabelWithIcon(icon, selectedSupport.projectiles[i]);
                    if (Widgets.ButtonTextSubtle(options, "Options"))
                    {
                        //If clicked options button
                        int k = i;
                        List<FloatMenuOption> listOptions = new List<FloatMenuOption>();
                        listOptions.Add(new FloatMenuOption("Insert Projectile Above Slot", delegate
                        {
                            List<FloatMenuOption> thingOptions = new List<FloatMenuOption>();
                            foreach (ThingDef def in selectedSupport.returnFireSupportOptions())
                            {
                                thingOptions.Add(new FloatMenuOption(
                                    def.LabelCap + " - " + Math.Round(def.BaseMarketValue * 1.5, 2).ToString(),
                                    delegate
                                    {
                                        Log.Message("insert at " + k);
                                        selectedSupport.projectiles.Insert(k, def);
                                    }, def));
                            }

                            if (thingOptions.Count() == 0)
                            {
                                thingOptions.Add(new FloatMenuOption("No available projectiles found",
                                    delegate { }));
                            }

                            Find.WindowStack.Add(new FloatMenu(thingOptions));
                        }));
                        listOptions.Add(new FloatMenuOption("Duplicate", delegate
                        {
                            ThingDef tempDef = selectedSupport.projectiles[k];
                            List<FloatMenuOption> thingOptions = new List<FloatMenuOption>();

                            thingOptions.Add(new FloatMenuOption("1x", delegate
                            {
                                for (int l = 0; l < 1; l++)
                                {
                                    if (k == selectedSupport.projectiles.Count() - 1)
                                    {
                                        selectedSupport.projectiles.Add(tempDef);
                                    }
                                    else
                                    {
                                        selectedSupport.projectiles.Insert(k + 1, tempDef);
                                    }
                                }
                            }));
                            thingOptions.Add(new FloatMenuOption("5x", delegate
                            {
                                for (int l = 0; l < 5; l++)
                                {
                                    if (k == selectedSupport.projectiles.Count() - 1)
                                    {
                                        selectedSupport.projectiles.Add(tempDef);
                                    }
                                    else
                                    {
                                        selectedSupport.projectiles.Insert(k + 1, tempDef);
                                    }
                                }
                            }));
                            thingOptions.Add(new FloatMenuOption("10x", delegate
                            {
                                for (int l = 0; l < 10; l++)
                                {
                                    if (k == selectedSupport.projectiles.Count() - 1)
                                    {
                                        selectedSupport.projectiles.Add(tempDef);
                                    }
                                    else
                                    {
                                        selectedSupport.projectiles.Insert(k + 1, tempDef);
                                    }
                                }
                            }));
                            thingOptions.Add(new FloatMenuOption("20x", delegate
                            {
                                for (int l = 0; l < 20; l++)
                                {
                                    if (k == selectedSupport.projectiles.Count() - 1)
                                    {
                                        selectedSupport.projectiles.Add(tempDef);
                                    }
                                    else
                                    {
                                        selectedSupport.projectiles.Insert(k + 1, tempDef);
                                    }
                                }
                            }));
                            thingOptions.Add(new FloatMenuOption("50x", delegate
                            {
                                for (int l = 0; l < 50; l++)
                                {
                                    if (k == selectedSupport.projectiles.Count() - 1)
                                    {
                                        selectedSupport.projectiles.Add(tempDef);
                                    }
                                    else
                                    {
                                        selectedSupport.projectiles.Insert(k + 1, tempDef);
                                    }
                                }
                            }));
                            Find.WindowStack.Add(new FloatMenu(thingOptions));
                        }));
                        Find.WindowStack.Add(new FloatMenu(listOptions));
                    }

                    if (Widgets.ButtonTextSubtle(upArrow, ""))
                    {
                        //if click up arrow button
                        if (i != 0)
                        {
                            ThingDef temp = selectedSupport.projectiles[i];
                            selectedSupport.projectiles[i] = selectedSupport.projectiles[i - 1];
                            selectedSupport.projectiles[i - 1] = temp;
                        }
                    }

                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(upArrow, "^");
                    if (Widgets.ButtonTextSubtle(downArrow, ""))
                    {
                        //if click down arrow button
                        if (i != selectedSupport.projectiles.Count() - 1)
                        {
                            ThingDef temp = selectedSupport.projectiles[i];
                            selectedSupport.projectiles[i] = selectedSupport.projectiles[i + 1];
                            selectedSupport.projectiles[i + 1] = temp;
                        }
                    }

                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(downArrow, "v");
                    if (Widgets.ButtonTextSubtle(delete, ""))
                    {
                        //if click delete  button
                        selectedSupport.projectiles.RemoveAt(i);
                    }

                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(delete, "X");
                }
            }


            //Reset Text anchor and font
            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }

        public void SetActive(MilSquadFC squad)
        {
            selectedSquad = squad;
            selectedText = squad.name;
        }

        public void SetActive(MilUnitFC unit)
        {
            selectedUnit = unit;
            selectedText = unit.name;
        }
    }
}