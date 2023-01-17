using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public class FactionCustomizeWindowFc : Window
    {
        public override Vector2 InitialSize => new Vector2(838f, 538f);

        //declare variables


        private FactionFC faction;

        public string desc;
        public string header;

        private string name;
        private string title;
        private Texture2D tempFactionIcon;
        private string tempFactionIconPath;

        Rect labelFaction = new Rect(0, 0, 200, 40);

        Rect labelFactionName = new Rect(0, 70, 100, 40);
        Rect textfieldName = new Rect(105, 70, 250, 40);

        Rect labelFactionTitle = new Rect(0, 110, 100, 40);
        Rect textfieldTitle = new Rect(105, 110, 250, 40);

        Rect labelFactionIcon = new Rect(0, 150, 100, 40);
        Rect buttonIcon = new Rect(105, 150, 40, 40);

        Rect buttonAllowedRaces = new Rect(25, 195, 200, 40);

        Rect labelTraits = new Rect(0, 235, 200, 40);
        Rect buttonTrait1 = new Rect(25, 260, 200, 40);
        Rect buttonTrait2 = new Rect(25, 300, 200, 40);


        Rect labelPickTrait = new Rect(400, 0, 400, 60);

        float circleX = 500;
        float circleY = 20;
        float circleR = 80;

        Rect menusectionTrait = new Rect(400, 200, 400, 300);

        Rect buttonConfirm = new Rect(130, 450, 200, 30);

        string alertText = "";

        bool traitsChosen;

        Rect buttonMilitaristic = new Rect((float) (600 + 60 * Math.Cos(0 * Math.PI)),
            (float) (100 + 60 * Math.Sin(0 * Math.PI)), 30, 30);

        Rect buttonAuthoritarian = new Rect((float) (600 + 60 * Math.Cos(0.25 * Math.PI)),
            (float) (100 + 60 * Math.Sin(0.25 * Math.PI)), 30, 30);

        Rect buttonIsolationist = new Rect((float) (600 + 60 * Math.Cos(0.5 * Math.PI)),
            (float) (100 + 60 * Math.Sin(0.5 * Math.PI)), 30, 30);

        Rect buttonFeudal = new Rect((float) (600 + 60 * Math.Cos(0.75 * Math.PI)),
            (float) (100 + 60 * Math.Sin(0.75 * Math.PI)), 30, 30);

        Rect buttonPacifist = new Rect((float) (600 + 60 * Math.Cos(1 * Math.PI)),
            (float) (100 + 60 * Math.Sin(1 * Math.PI)), 30, 30);

        Rect buttonEgalitarian = new Rect((float) (600 + 60 * Math.Cos(1.25 * Math.PI)),
            (float) (100 + 60 * Math.Sin(1.25 * Math.PI)), 30, 30);

        Rect buttonExpansionist = new Rect((float) (600 + 60 * Math.Cos(1.5 * Math.PI)),
            (float) (100 + 60 * Math.Sin(1.5 * Math.PI)), 30, 30);

        Rect buttonTechnocrat = new Rect((float) (600 + 60 * Math.Cos(1.75 * Math.PI)),
            (float) (100 + 60 * Math.Sin(1.75 * Math.PI)), 30, 30);

        int numberTraitsSelected;
        bool boolMilitaristic;
        bool boolPacifist;
        bool boolAuthoritarian;
        bool boolEgalitarian;
        bool boolIsolationist;
        bool boolExpansionist;
        bool boolTechnocrat;
        bool boolFeudal;

        string policyText = "";


        public FactionCustomizeWindowFc(FactionFC faction)
        {
            forcePause = false;
            draggable = true;
            doCloseX = true;
            preventCameraMotion = false;
            this.faction = faction;
            header = "CustomizeFaction".Translate();
            name = faction.name;
            title = faction.title;

            tempFactionIcon = faction.factionIcon;
            tempFactionIconPath = faction.factionIconPath;

            numberTraitsSelected = faction.policies.Count();

            if (numberTraitsSelected != 0)
            {
                foreach (FCPolicy policy in faction.policies)
                {
                    switch (policy.def.defName)
                    {
                        case "militaristic":
                            boolMilitaristic = true;
                            break;
                        case "pacifist":
                            boolPacifist = true;
                            break;
                        case "authoritarian":
                            boolAuthoritarian = true;
                            break;
                        case "egalitarian":
                            boolEgalitarian = true;
                            break;
                        case "isolationist":
                            boolIsolationist = true;
                            break;
                        case "expansionist":
                            boolExpansionist = true;
                            break;
                        case "technocratic":
                            boolTechnocrat = true;
                            break;
                        case "feudal":
                            boolFeudal = true;
                            break;
                    }
                }
            }

            if (numberTraitsSelected == 2)
            {
                traitsChosen = true;
            }
            else
            {
                traitsChosen = false;
                faction.policies = new List<FCPolicy>();
            }
        }

        public override void OnAcceptKeyPressed()
        {
            base.OnAcceptKeyPressed();
            faction.title = title;
            faction.name = name;
            FactionColonies.getPlayerColonyFaction().Name = name;
            //Find.World.GetComponent<FactionFC>().name = name;
        }

        public override void DoWindowContents(Rect inRect)
        {
            //grab before anchor/font
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;


            //Settlement Tax Collection Header
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Medium;


            Widgets.Label(labelFaction, header);

            Text.Font = GameFont.Small;
            Widgets.Label(labelFactionName, "FactionName".Translate() + ":");
            name = Widgets.TextField(textfieldName, name);

            Widgets.Label(labelFactionTitle, "FactionTitle".Translate() + ":");
            title = Widgets.TextField(textfieldTitle, title);

            Widgets.Label(labelFactionIcon, "FactionIcon".Translate());
            if (Widgets.ButtonImage(buttonIcon, tempFactionIcon))
            {
                List<FloatMenuOption> list = TexLoad.factionIcons.Select(texture => new FloatMenuOption(texture.name, delegate
                    {
                        tempFactionIcon = texture;
                        tempFactionIconPath = texture.name;
                    }, texture, Color.white)).ToList();

                FloatMenu menu = new FloatMenu(list);
                Find.WindowStack.Add(menu);
            }

            if (Widgets.ButtonTextSubtle(buttonAllowedRaces, "AllowedRaces".Translate()))
            {
                List<FloatMenuOption> list = new List<FloatMenuOption>
                {
                    new FloatMenuOption("Enable All", delegate { faction.resetRaceFilter(); })
                };
                List<string> races = new List<string>();
                foreach (PawnKindDef def in DefDatabase<PawnKindDef>.AllDefsListForReading.Where(def => def.IsHumanlikeWithLabelRace() && !races.Contains(def.race.label)))
                {
                    if (def.race.label == "Human" && def.LabelCap != "Colonist") continue;
                    races.Add(def.race.label);
                    list.Add(new FloatMenuOption(
                        def.race.label.CapitalizeFirst() + " - Allowed: " + faction.raceFilter.Allows(def.race),
                        delegate
                        {
                            if (faction.raceFilter.AllowedThingDefs.Count() == 1 && faction.raceFilter.Allows(def.race))
                            {
                                Messages.Message("CannotHaveLessThanOneRace".Translate(), MessageTypeDefOf.RejectInput);
                            }
                            else if (faction.raceFilter.AllowedThingDefs.Count() > 0)
                            {
                                if (!faction.raceFilter.SetAllow(def.race, !faction.raceFilter.Allows(def.race)))
                                {
                                    Messages.Message(new Message("InvalidFaction".Translate(), MessageTypeDefOf.RejectInput));
                                }
                            }
                            else
                            {
                                Log.Message("Empire Error - Zero races available for faction - Report this");
                                Log.Message("Resetting race filter");
                                faction.resetRaceFilter();
                            }
                        }));
                }

                FloatMenu menu = new FloatMenu(list);
                Find.WindowStack.Add(menu);
            }

            if (Widgets.ButtonText(buttonConfirm, "ConfirmChanges".Translate()))
            {
                Faction fact = FactionColonies.getPlayerColonyFaction();
                faction.title = title;
                faction.name = name;
                fact.Name = name;
                faction.name = name;
                faction.factionIconPath = tempFactionIconPath;
                faction.factionIcon = tempFactionIcon;
                faction.updateFactionRaces();
                faction.factionBackup = fact;

                faction.updateFactionIcon(ref fact, "FactionIcons/" + tempFactionIconPath);


                if (!traitsChosen)
                {
                    //check each trait bool. If true and does not exist already, add to factionfc
                    if (boolMilitaristic)
                        faction.policies.Add(new FCPolicy(FCPolicyDefOf.militaristic));
                    if (boolPacifist)
                        faction.policies.Add(new FCPolicy(FCPolicyDefOf.pacifist));
                    if (boolAuthoritarian)
                        faction.policies.Add(new FCPolicy(FCPolicyDefOf.authoritarian));
                    if (boolEgalitarian)
                        faction.policies.Add(new FCPolicy(FCPolicyDefOf.egalitarian));
                    if (boolIsolationist)
                        faction.policies.Add(new FCPolicy(FCPolicyDefOf.isolationist));
                    if (boolExpansionist)
                        faction.policies.Add(new FCPolicy(FCPolicyDefOf.expansionist));
                    if (boolTechnocrat)
                        faction.policies.Add(new FCPolicy(FCPolicyDefOf.technocratic));
                    if (boolFeudal)
                        faction.policies.Add(new FCPolicy(FCPolicyDefOf.feudal));
                }

                Find.WindowStack.TryRemove(this);
            }


            if (!traitsChosen)
                switch (faction.policies.Count())
                {
                    case 0:
                    case 1:
                        alertText = "FCSelectTraits0".Translate();
                        break;
                    case 2:
                        alertText = "FCSelectTraits2".Translate();
                        break;
                }
            else
            {
                alertText = "FCTraitsChosen".Translate();
            }


            Widgets.Label(labelPickTrait, alertText);


            Texture2D icon = TexLoad.iconLoyalty;
            if (boolMilitaristic)
                icon = FCPolicyDefOf.militaristic.IconLight;
            else
                icon = FCPolicyDefOf.militaristic.IconDark;
            if (buttonMilitaristic.Contains(Event.current.mousePosition))
            {
                TooltipHandler.TipRegion(buttonMilitaristic, returnPolicyText(FCPolicyDefOf.militaristic));
            }

            if (Widgets.ButtonImage(buttonMilitaristic, icon))
            {
                if (numberTraitsSelected <= 1 || boolMilitaristic)
                {
                    //Continue
                    if (boolPacifist == false)
                    {
                        boolMilitaristic = !boolMilitaristic;
                        if (boolMilitaristic)
                        {
                            numberTraitsSelected += 1;
                        }
                        else
                        {
                            numberTraitsSelected -= 1;
                        }

                        policyText = returnPolicyText(FCPolicyDefOf.militaristic);
                    }
                    else
                    {
                        Messages.Message("FCConflictingTraits".Translate(), MessageTypeDefOf.RejectInput);
                    }
                }
                else
                {
                    Messages.Message("FCUnselectTrait".Translate(), MessageTypeDefOf.RejectInput);
                }
            }


            if (boolAuthoritarian)
                icon = FCPolicyDefOf.authoritarian.IconLight;
            else
                icon = FCPolicyDefOf.authoritarian.IconDark;
            if (buttonAuthoritarian.Contains(Event.current.mousePosition))
            {
                TooltipHandler.TipRegion(buttonAuthoritarian, returnPolicyText(FCPolicyDefOf.authoritarian));
            }

            if (Widgets.ButtonImage(buttonAuthoritarian, icon))
            {
                if (numberTraitsSelected <= 1 || boolAuthoritarian)
                {
                    //Continue
                    if (boolEgalitarian == false)
                    {
                        boolAuthoritarian = !boolAuthoritarian;
                        if (boolAuthoritarian)
                        {
                            numberTraitsSelected += 1;
                        }
                        else
                        {
                            numberTraitsSelected -= 1;
                        }

                        policyText = returnPolicyText(FCPolicyDefOf.authoritarian);
                    }
                    else
                    {
                        Messages.Message("FCConflictingTraits".Translate(), MessageTypeDefOf.RejectInput);
                    }
                }
                else
                {
                    Messages.Message("FCUnselectTrait".Translate(), MessageTypeDefOf.RejectInput);
                }
            }


            if (boolIsolationist)
                icon = FCPolicyDefOf.isolationist.IconLight;
            else
                icon = FCPolicyDefOf.isolationist.IconDark;
            if (buttonIsolationist.Contains(Event.current.mousePosition))
            {
                TooltipHandler.TipRegion(buttonIsolationist, returnPolicyText(FCPolicyDefOf.isolationist));
            }

            if (Widgets.ButtonImage(buttonIsolationist, icon))
            {
                if (numberTraitsSelected <= 1 || boolIsolationist)
                {
                    //Continue
                    if (boolExpansionist == false)
                    {
                        boolIsolationist = !boolIsolationist;
                        if (boolIsolationist)
                        {
                            numberTraitsSelected += 1;
                        }
                        else
                        {
                            numberTraitsSelected -= 1;
                        }

                        policyText = returnPolicyText(FCPolicyDefOf.isolationist);
                    }
                    else
                    {
                        Messages.Message("FCConflictingTraits".Translate(), MessageTypeDefOf.RejectInput);
                    }
                }
                else
                {
                    Messages.Message("FCUnselectTrait".Translate(), MessageTypeDefOf.RejectInput);
                }
            }


            if (boolFeudal)
                icon = FCPolicyDefOf.feudal.IconLight;
            else
                icon = FCPolicyDefOf.feudal.IconDark;
            if (buttonFeudal.Contains(Event.current.mousePosition))
            {
                TooltipHandler.TipRegion(buttonFeudal, returnPolicyText(FCPolicyDefOf.feudal));
            }

            if (Widgets.ButtonImage(buttonFeudal, icon))
            {
                if (numberTraitsSelected <= 1 || boolFeudal)
                {
                    //Continue
                    if (boolTechnocrat == false)
                    {
                        boolFeudal = !boolFeudal;
                        if (boolFeudal)
                        {
                            numberTraitsSelected += 1;
                        }
                        else
                        {
                            numberTraitsSelected -= 1;
                        }

                        policyText = returnPolicyText(FCPolicyDefOf.feudal);
                    }
                    else
                    {
                        Messages.Message("FCConflictingTraits".Translate(), MessageTypeDefOf.RejectInput);
                    }
                }
                else
                {
                    Messages.Message("FCUnselectTrait".Translate(), MessageTypeDefOf.RejectInput);
                }
            }


            if (boolPacifist)
                icon = FCPolicyDefOf.pacifist.IconLight;
            else
                icon = FCPolicyDefOf.pacifist.IconDark;
            if (buttonPacifist.Contains(Event.current.mousePosition))
            {
                TooltipHandler.TipRegion(buttonPacifist, returnPolicyText(FCPolicyDefOf.pacifist));
            }

            if (Widgets.ButtonImage(buttonPacifist, icon))
            {
                if (numberTraitsSelected <= 1 || boolPacifist)
                {
                    //Continue
                    if (boolMilitaristic == false)
                    {
                        boolPacifist = !boolPacifist;
                        if (boolPacifist)
                        {
                            numberTraitsSelected += 1;
                        }
                        else
                        {
                            numberTraitsSelected -= 1;
                        }

                        policyText = returnPolicyText(FCPolicyDefOf.pacifist);
                    }
                    else
                    {
                        Messages.Message("FCConflictingTraits".Translate(), MessageTypeDefOf.RejectInput);
                    }
                }
                else
                {
                    Messages.Message("FCUnselectTrait".Translate(), MessageTypeDefOf.RejectInput);
                }
            }


            if (boolEgalitarian)
                icon = FCPolicyDefOf.egalitarian.IconLight;
            else
                icon = FCPolicyDefOf.egalitarian.IconDark;
            if (buttonEgalitarian.Contains(Event.current.mousePosition))
            {
                TooltipHandler.TipRegion(buttonEgalitarian, returnPolicyText(FCPolicyDefOf.egalitarian));
            }

            if (Widgets.ButtonImage(buttonEgalitarian, icon))
            {
                if (numberTraitsSelected <= 1 || boolEgalitarian)
                {
                    //Continue
                    if (boolAuthoritarian == false)
                    {
                        boolEgalitarian = !boolEgalitarian;
                        if (boolEgalitarian)
                        {
                            numberTraitsSelected += 1;
                        }
                        else
                        {
                            numberTraitsSelected -= 1;
                        }

                        policyText = returnPolicyText(FCPolicyDefOf.egalitarian);
                    }
                    else
                    {
                        Messages.Message("FCConflictingTraits".Translate(), MessageTypeDefOf.RejectInput);
                    }
                }
                else
                {
                    Messages.Message("FCUnselectTrait".Translate(), MessageTypeDefOf.RejectInput);
                }
            }


            if (boolExpansionist)
                icon = FCPolicyDefOf.expansionist.IconLight;
            else
                icon = FCPolicyDefOf.expansionist.IconDark;
            if (buttonExpansionist.Contains(Event.current.mousePosition))
            {
                TooltipHandler.TipRegion(buttonExpansionist, returnPolicyText(FCPolicyDefOf.expansionist));
            }

            if (Widgets.ButtonImage(buttonExpansionist, icon))
            {
                if (numberTraitsSelected <= 1 || boolExpansionist)
                {
                    //Continue
                    if (boolIsolationist == false)
                    {
                        boolExpansionist = !boolExpansionist;
                        if (boolExpansionist)
                        {
                            numberTraitsSelected += 1;
                        }
                        else
                        {
                            numberTraitsSelected -= 1;
                        }

                        policyText = returnPolicyText(FCPolicyDefOf.expansionist);
                    }
                    else
                    {
                        Messages.Message("FCConflictingTraits".Translate(), MessageTypeDefOf.RejectInput);
                    }
                }
                else
                {
                    Messages.Message("FCUnselectTrait".Translate(), MessageTypeDefOf.RejectInput);
                }
            }


            if (boolTechnocrat)
                icon = FCPolicyDefOf.technocratic.IconLight;
            else
                icon = FCPolicyDefOf.technocratic.IconDark;
            if (buttonTechnocrat.Contains(Event.current.mousePosition))
            {
                TooltipHandler.TipRegion(buttonTechnocrat, returnPolicyText(FCPolicyDefOf.technocratic));
            }

            if (Widgets.ButtonImage(buttonTechnocrat, icon))
            {
                if (numberTraitsSelected <= 1 || boolTechnocrat)
                {
                    //Continue
                    if (boolFeudal == false)
                    {
                        boolTechnocrat = !boolTechnocrat;
                        if (boolTechnocrat)
                        {
                            numberTraitsSelected += 1;
                        }
                        else
                        {
                            numberTraitsSelected -= 1;
                        }

                        policyText = returnPolicyText(FCPolicyDefOf.technocratic);
                    }
                    else
                    {
                        Messages.Message("FCConflictingTraits".Translate(), MessageTypeDefOf.RejectInput);
                    }
                }
                else
                {
                    Messages.Message("FCUnselectTrait".Translate(), MessageTypeDefOf.RejectInput);
                }
            }


            //Widgets.DrawMenuSection(menusectionTrait);
            //Widgets.Label(menusectionTrait, policyText);


            //reset anchor/font
            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
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
    }
}