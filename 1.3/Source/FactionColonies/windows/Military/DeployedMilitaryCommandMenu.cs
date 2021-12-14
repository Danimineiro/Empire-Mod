using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FactionColonies.util;
using Verse;
using RimWorld;
using UnityEngine;


namespace FactionColonies
{
    public class DeployedMilitaryCommandMenu : Window
    {
        readonly FactionFC faction;
        public MercenarySquadFC selectedSquad;
        public string squadText;

        public Dictionary<MercenarySquadFC, IntVec3> currentOrderPositionDic = new Dictionary<MercenarySquadFC, IntVec3>();
        public Dictionary<MercenarySquadFC, MilitaryOrder> squadMilitaryOrderDic = new Dictionary<MercenarySquadFC, MilitaryOrder>();

        public DeployedMilitaryCommandMenu()
        {
            layer = WindowLayer.Super;
            closeOnClickedOutside = false;
            closeOnAccept = false;
            closeOnCancel = false;
            doCloseX = false;
            draggable = true;
            drawShadow = false;
            doWindowBackground = false;
            preventCameraMotion = false;
            faction = Find.World.GetComponent<FactionFC>();

            selectedSquad = faction.militaryCustomizationUtil.DeployedSquads.Where(squad => squad.getSettlement != null).RandomElementWithFallback();
        }

        public override Vector2 InitialSize => new Vector2(200f, 240f);

        protected override void SetInitialSizeAndPosition()
        {
            windowRect = new Rect(UI.screenWidth - InitialSize.x, 0f, InitialSize.x, InitialSize.y);
        }
        
        /// <summary>
        /// Lets the user select a squad from the squads active on the map
        /// </summary>
        private void DoSelectSquadCommand()
        {
            List<FloatMenuOption> list = new List<FloatMenuOption>();
            foreach (MercenarySquadFC squad in faction.militaryCustomizationUtil.DeployedSquads)
            {
                if (squad.getSettlement != null)
                {
                    list.Add(new FloatMenuOption("selectedDeployedSquad".Translate(squad.getSettlement.name, squad.outfit.name), () => selectedSquad = squad));
                }
            }
            if (!list.Any())
            {
                //This should never happen
                Log.Error("No deployed squad, but window is still open? Closing..");
                Close();
                list.Add(new FloatMenuOption("noSquadsAvailable".Translate(), null));
            }

            Find.WindowStack.Add(new FloatMenu(list));
        }

        /// <summary>
        /// Makes the currently active squad execute the attack command
        /// </summary>
        private void DoAttackCommand()
        {
            if (selectedSquad != null)
            {
                squadMilitaryOrderDic.SetOrAdd(selectedSquad, MilitaryOrder.Hunt);
                Messages.Message("attackSuccess".Translate(selectedSquad.outfit.name), MessageTypeDefOf.NeutralEvent);
            }
        }

        /// <summary>
        /// Makes the currently active squad execute the move command. The move command is actually a point defence command.
        /// </summary>
        private void DoMoveCommand()
        {
            if (selectedSquad != null)
            {
                DebugTool tool;
                IntVec3 Position;
                tool = new DebugTool("selectMilitaryMovePosition".Translate(), delegate ()
                {
                    Position = UI.MouseCell();

                    squadMilitaryOrderDic.SetOrAdd(selectedSquad, MilitaryOrder.DefendPoint);
                    currentOrderPositionDic.SetOrAdd(selectedSquad, Position);
                    Messages.Message("moveSuccess".Translate(selectedSquad.outfit.name), MessageTypeDefOf.NeutralEvent);

                    DebugTools.curTool = null;
                });
                DebugTools.curTool = tool;
            }
        }

        /// <summary>
        /// Makes the currently selected squad immediately inactive and leave the map
        /// </summary>
        private void DoLeaveCommand()
        {
            if (selectedSquad != null)
            {
                squadMilitaryOrderDic.SetOrAdd(selectedSquad, MilitaryOrder.RecoverWoundedAndLeave);
                selectedSquad.isDeployed = false;
                Messages.Message("commandLeave".Translate(selectedSquad.outfit.name, selectedSquad.dead), MessageTypeDefOf.NeutralEvent);
            }
        }

        /// <summary>
        /// If the dev mode is enabled, despawnes all squads and disables their deployment
        /// </summary>
        private void DoDebugCommand()
        {
            foreach (MercenarySquadFC squad in faction.militaryCustomizationUtil.DeployedSquads)
            {
                foreach (Mercenary merc in squad.mercenaries.Concat(squad.animals))
                {
                    if (merc?.pawn?.Map != null)
                    {
                        merc?.animal?.pawn?.Destroy();
                        merc.pawn.Destroy();
                    }
                }

                try
                {
                    foreach (Pawn pawn in Find.CurrentMap.mapPawns.SpawnedPawnsInFaction(FactionColonies.getPlayerColonyFaction()))
                    {
                        pawn.Destroy();
                    }
                }
                catch { }

                squad.isDeployed = false;
                squad.InitiateCooldownEvent();
            }
        }

        public override void DoWindowContents(Rect rect) 
        {
            if (faction.militaryCustomizationUtil.DeployedSquads.Count() == 0) Close();

            GameFont prevFont = Text.Font;
            TextAnchor prevAnchor = Text.Anchor;

            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;

            float rectBaseHeight = 40f;
            float rectWidth = 160f;

            Rect selectSquad = new Rect(0, 0, rectWidth, rectBaseHeight);
            Rect commandAttack = new Rect(0, rectBaseHeight, rectWidth, rectBaseHeight);
            Rect commandMove = new Rect(0, rectBaseHeight * 2, rectWidth, rectBaseHeight);
            Rect commandHeal = new Rect(0, rectBaseHeight * 3, rectWidth, rectBaseHeight);
            Rect commandKillWindow = new Rect(0, rectBaseHeight * 4, rectWidth, rectBaseHeight);

            squadText = (selectedSquad == null) ? "selectDeployedSquad".Translate() : "selectedDeployedSquad".Translate(selectedSquad.getSettlement.name, selectedSquad.outfit.name);

            if (Widgets.ButtonText(selectSquad, squadText)) DoSelectSquadCommand();
            if (Widgets.ButtonTextSubtle(commandAttack, "commandAttack".Translate())) DoAttackCommand();
            if (Widgets.ButtonTextSubtle(commandMove, "commandMove".Translate())) DoMoveCommand();
            if (Widgets.ButtonTextSubtle(commandHeal, "commandLeave".Translate())) DoLeaveCommand();

            if (Prefs.DevMode) if (Widgets.ButtonTextSubtle(commandKillWindow, "debugRemoveAllCommand".Translate())) DoDebugCommand();

            Text.Font = prevFont;
            Text.Anchor = prevAnchor;
        }
    }
}
