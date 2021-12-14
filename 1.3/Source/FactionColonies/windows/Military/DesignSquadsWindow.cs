using System.Collections.Generic;
using System.Linq;
using FactionColonies.util;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public class DesignSquadsWindow : MilitaryWindow
    {
        private SettlementFC settlementPointReference;
        private MilitaryCustomizationUtil util;
        private MilSquadFC selectedSquad;
        
        public DesignSquadsWindow(MilitaryCustomizationUtil util)
        {
            this.util = util;
            selectedText = "Select A Squad";

            if (util.blankUnit == null)
            {
                util.blankUnit = new MilUnitFC(true);
            }

            util.checkMilitaryUtilForErrors();
        }

        public override void Select(IExposable selecting)
        {
            MilSquadFC squad = (MilSquadFC) selecting;
            selectedSquad = squad;
            selectedText = squad.name;
        }

        public override void DrawTab(Rect rect)
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
    }
}