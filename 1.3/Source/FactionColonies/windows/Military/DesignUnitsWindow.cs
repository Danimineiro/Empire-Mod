using System;
using System.Collections.Generic;
using System.Linq;
using FactionColonies.util;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public class DesignUnitsWindow : MilitaryWindow
    {
        private readonly MilitaryCustomizationUtil util;
        private readonly FactionFC faction;
        private MilUnitFC selectedUnit;

        public DesignUnitsWindow(MilitaryCustomizationUtil util, FactionFC faction)
        {
            this.util = util;
            this.faction = faction;
            
            selectedText = "Select A Unit";

            util.checkMilitaryUtilForErrors();
        }
        
        public override void Select(IExposable selecting)
        {
            MilUnitFC squad = (MilUnitFC) selecting;
            selectedUnit = squad;
            selectedText = squad.name;
        }
        
        public override void DrawTab(Rect rect)
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
                selectedUnit.unequipAllEquipment();
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
    }
}