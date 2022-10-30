using System;
using Verse;
using RimWorld;
using UnityEngine;


namespace FactionColonies
{
    public class SettlementUpgradeWindowFc : Window
    {
        public override Vector2 InitialSize => new Vector2(380f, 300f);

        private readonly int yspacing = 30;
        private readonly int yoffset = 90;

        private readonly int length = 335;
        private readonly int xoffset = 0;
        private readonly int height = 200;
        private readonly int settlementUpgradeCost;
        private readonly int maxSettlementLevel;

        private readonly SettlementFC settlement;
        private readonly FactionFC factionfc;

        public string desc;
        public string header;

        public SettlementUpgradeWindowFc(SettlementFC settlement)
        {
            forcePause = false;
            draggable = true;
            doCloseX = true;
            preventCameraMotion = false;
            header = "UpgradeSettlement".Translate();
            this.settlement = settlement;
            settlementUpgradeCost = Convert.ToInt32(LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().settlementBaseUpgradeCost) + (settlement.settlementLevel * 1000);
            desc = settlement.name + " " + "CanBeUpgraded".Translate() + " " + settlementUpgradeCost + " " + "Silver".Translate().ToLower() + ". " + "UpgradeColonyDesc".Translate();
            factionfc = Find.World.GetComponent<FactionFC>();
            maxSettlementLevel = LoadedModManager.GetMod<FactionColoniesMod>().GetSettings<FactionColonies>().settlementMaxLevel;
        }

        /// <summary>
        /// Attempts to create an upgrading <c>FCEvent</c> for a <c>SettlementFC</c>.
        /// </summary>
        /// <returns>A message describing if the process was successful or not, including a reason in case it was not</returns>
        private Message UpgradeSettlement()
        {
            //failure reasons
            if (settlement.IsBeingUpgraded) return new Message("AlreadyUpgradeSettlement".Translate(), MessageTypeDefOf.RejectInput);
            if (settlement.isUnderAttack) return new Message("SettlementUnderAttack".Translate(), MessageTypeDefOf.RejectInput);
            if (PaymentUtil.getSilver() < settlementUpgradeCost) return new Message("NotEnoughSilverUpgrade".Translate(), MessageTypeDefOf.RejectInput);

            //on success
            PaymentUtil.paySilver(settlementUpgradeCost);
            FCEvent tmp = new FCEvent(true)
            {
                def = FCEventDefOf.upgradeSettlement,
                location = settlement.mapLocation,
                planetName = settlement.planetName,
                timeTillTrigger = Find.TickManager.TicksGame + (settlement.settlementLevel + 1) * 60000 * (factionfc.hasPolicy(FCPolicyDefOf.isolationist) ? 1 : 2)
            };
                
            Find.World.GetComponent<FactionFC>().addEvent(tmp);

            //Close this window and update the SettlementWindowFc
            Find.WindowStack.TryRemove(this);
            Find.WindowStack.WindowOfType<SettlementWindowFc>().windowUpdateFc();

            return new Message("StartUpgradeSettlement".Translate(), MessageTypeDefOf.NeutralEvent);
        }

        public override void DoWindowContents(Rect inRect)
        {
            //grab before anchor/font
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;

            //Settlement Tax Collection Header
            Text.Anchor = TextAnchor.MiddleLeft;
            Text.Font = GameFont.Medium;

            Widgets.Label(new Rect(2, 0, 300, 60), header);

            Text.Anchor = TextAnchor.UpperLeft;
            Text.Font = GameFont.Tiny;

            if (settlement.settlementLevel < maxSettlementLevel) //if settlement is not max level
            {
                if (Widgets.ButtonText(new Rect(xoffset + ((335 - 150) / 2), height + 10, 150, 40), "UpgradeSettlement".Translate() + ": " + settlementUpgradeCost)) Messages.Message(UpgradeSettlement());
            }
            else //if settlement is max level
            {
                desc = "CannotBeUpgradedPastMax".Translate() + ": " + maxSettlementLevel;
            }

            Widgets.Label(new Rect(xoffset + 2, yoffset - yspacing + 2, length - 4, height - 4 + yspacing * 2), desc);
            Widgets.DrawBox(new Rect(xoffset, yoffset - yspacing, length, height - yspacing * 2));

            //reset anchor/font
            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }
    }
}