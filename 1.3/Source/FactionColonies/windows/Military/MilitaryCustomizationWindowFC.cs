using System;
using JetBrains.Annotations;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public class MilitaryCustomizationWindowFc : Window
    {
        [CanBeNull] private MilitaryWindow window;
        
        FactionFC faction;
        MilitaryCustomizationUtil util;
        
        public override Vector2 InitialSize
        {
            get { return new Vector2(838f, 600); }
        }


        public MilitaryCustomizationWindowFc()
        {
            forcePause = false;
            draggable = true;
            doCloseX = true;
            preventCameraMotion = false;

            util = Find.World.GetComponent<FactionFC>().militaryCustomizationUtil;
            faction = Find.World.GetComponent<FactionFC>();
        }

        public override void PostClose()
        {
            base.PostClose();
            util.checkMilitaryUtilForErrors();
        }

        public override void DoWindowContents(Rect inRect)
        {
            window?.DrawTab(inRect);

            DrawHeaderTabs();

            //Widgets.ThingIcon(new Rect(50, 50, 60, 60), util.defaultPawn);
        }

        public void DrawHeaderTabs()
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
                window = null;
                util.checkMilitaryUtilForErrors();
            }

            if (Widgets.ButtonTextSubtle(milSetSquad, "Designate Squads"))
            {
                window = new AssignSquadsWindow(util, faction);
                util.checkMilitaryUtilForErrors();
            }

            if (Widgets.ButtonTextSubtle(milCreateSquad, "Create Squads"))
            {
                window = new DesignSquadsWindow(util);
            }

            if (Widgets.ButtonTextSubtle(milCreateUnit, "Create Units"))
            {
                window = new DesignUnitsWindow(util, faction);
            }

            if (Widgets.ButtonTextSubtle(milCreateFiresupport, "Create Fire Support"))
            {
                window = new FireSupportWindow(util);
            }
        }

        public void SetActive(IExposable selecting)
        {
            if (window == null)
            {
                throw new ApplicationException("Tried to set active on null");
            }
            window?.Select(selecting);
        }
    }
}