using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using FactionColonies.util;

namespace FactionColonies
{
    public class CreateColonyWindowFc : Window
    {
        public sealed override Vector2 InitialSize => new Vector2(300f, 600f);

        public int currentTileSelected = -1;
        public BiomeResourceDef currentBiomeSelected; //DefDatabase<BiomeResourceDef>.GetNamed(this.biome)
        public BiomeResourceDef currentHillinessSelected;
        public bool traitExpansionistReducedFee;
        public int timeToTravel = -1;

        private int settlementCreationCost = 0;
        private readonly FactionFC faction = null;

        private int SettlementCreationBaseCost => (int)(TraitUtilsFC.cycleTraits("createSettlementMultiplier", faction.traits, Operation.Multiplication) * (FactionColonies.silverToCreateSettlement + (500 * (faction.settlements.Count() + faction.settlementCaravansList.Count())) + (TraitUtilsFC.cycleTraits("createSettlementBaseCost", faction.traits, Operation.Addition))));

        public CreateColonyWindowFc()
        {
            forcePause = false;
            draggable = false;
            preventCameraMotion = false;
            doCloseX = true;
            windowRect = new Rect(UI.screenWidth - InitialSize.x, (UI.screenHeight - InitialSize.y) / 2f - (UI.screenHeight/8f), InitialSize.x, InitialSize.y);
            faction = Find.World.GetComponent<FactionFC>();
        }



        //Pre-Opening
        public override void PreOpen()
        {

        }

        //Drawing
        public override void DoWindowContents(Rect inRect)
        {
            faction.roadBuilder.DrawPaths();

            GetTileData();

            //grab before anchor/font
            GameFont fontBefore = Text.Font;
            TextAnchor anchorBefore = Text.Anchor;

            CalculateSettlementCreationCost();
            
            //Draw Label
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(0, 0, 268, 40), "SettleANewColony".Translate());

            //hori line
            Widgets.DrawLineHorizontal(0, 40, 300);


            //Upper menu
            Widgets.DrawMenuSection(new Rect(5, 45, 258, 220));

            DrawLabelBox(new Rect(10, 50, 100, 100), "TravelTime".Translate(), timeToTravel.ToTimeString());
            DrawLabelBox(new Rect(153, 50, 100, 100), "InitialCost".Translate(), settlementCreationCost + " " + "Silver".Translate());


            //Lower Menu label
            Text.Font = GameFont.Medium;
            Text.Anchor = TextAnchor.MiddleCenter;
            Widgets.Label(new Rect(0, 270, 268, 40), "BaseProductionStats".Translate());


            //Lower menu
            Widgets.DrawMenuSection(new Rect(5, 310, 258, 220));


            //Draw production
            DrawProduction();
            DrawCreateSettlementButton();

            //reset anchor/font
            Text.Font = fontBefore;
            Text.Anchor = anchorBefore;
        }

        private void GetTileData()
        {
            if (Find.WorldSelector.selectedTile != -1 && Find.WorldSelector.selectedTile != currentTileSelected)
            {
                currentTileSelected = Find.WorldSelector.selectedTile;
                //Log.Message("Current: " + currentTileSelected + ", Selected: " + Find.WorldSelector.selectedTile);
                currentBiomeSelected = DefDatabase<BiomeResourceDef>.GetNamed(Find.WorldGrid.tiles[currentTileSelected].biome.ToString(), false);
                //default biome
                if (currentBiomeSelected == default(BiomeResourceDef))
                {
                    //Log Modded Biome
                    currentBiomeSelected = BiomeResourceDefOf.defaultBiome;
                }

                currentHillinessSelected = DefDatabase<BiomeResourceDef>.GetNamed(Find.WorldGrid.tiles[currentTileSelected].hilliness.ToString());
                if (currentBiomeSelected.canSettle && currentHillinessSelected.canSettle && currentTileSelected != 1)
                {
                    timeToTravel = FactionColonies.ReturnTicksToArrive(faction.capitalLocation, currentTileSelected);
                }
                else
                {
                    timeToTravel = 0;
                }
            }
        }

        private void CalculateSettlementCreationCost()
        {
            settlementCreationCost = SettlementCreationBaseCost;

            if (faction.hasPolicy(FCPolicyDefOf.isolationist)) settlementCreationCost *= 2;

            if (!faction.hasPolicy(FCPolicyDefOf.expansionist)) return;

            if (!faction.settlements.Any() && !faction.settlementCaravansList.Any())
            {
                traitExpansionistReducedFee = false;
                settlementCreationCost = 0;
                return;
            }

            if (faction.traitExpansionistTickLastUsedSettlementFeeReduction == -1 || (faction.traitExpansionistBoolCanUseSettlementFeeReduction))
            {
                traitExpansionistReducedFee = true;
                settlementCreationCost /= 2;
                return;
            }

            traitExpansionistReducedFee = false;
        }
        
        private void DrawProduction()
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;

            //Production headers
            Widgets.Label(new Rect(40, 310, 60, 25), "Base".Translate());
            Widgets.Label(new Rect(110, 310, 60, 25), "Modifier".Translate());
            Widgets.Label(new Rect(180, 310, 60, 25), "Final".Translate());

            if (currentTileSelected != -1)
            {
                foreach (ResourceType titheType in ResourceUtils.resourceTypes)
                {
                    int titheTypeInt = (int)titheType;
                    int baseHeight = 15;
                    if (Widgets.ButtonImage(new Rect(20, 335 + titheTypeInt * (5 + baseHeight), baseHeight, baseHeight), faction.returnResource(titheType).getIcon()))
                    {
                        string label = faction.returnResource(titheType).label;

                        Find.WindowStack.Add(new DescWindowFc("SettlementProductionOf".Translate() + ": " + label, label.CapitalizeFirst()));
                    }

                    float xMod = 70f;
                    Rect baseRect = new Rect(40, 335 + titheTypeInt * (5 + baseHeight), 60, baseHeight + 2);

                    double titheAddBaseProductionCurBiome = currentBiomeSelected.BaseProductionAdditive[titheTypeInt];
                    double titheAddBaseProductionCurHilli = currentHillinessSelected.BaseProductionAdditive[titheTypeInt];

                    double titheMultBaseProductionCurBiome = currentBiomeSelected.BaseProductionMultiplicative[titheTypeInt];
                    double titheMultBaseProductionCurHilli = currentHillinessSelected.BaseProductionMultiplicative[titheTypeInt];

                    Widgets.Label(baseRect, (titheAddBaseProductionCurBiome + titheAddBaseProductionCurHilli).ToString());
                    Widgets.Label(baseRect.CopyAndShift(xMod, 0f), (titheMultBaseProductionCurBiome * titheMultBaseProductionCurHilli).ToString());
                    Widgets.Label(baseRect.CopyAndShift(xMod * 2f, 0f), ((titheAddBaseProductionCurBiome + titheAddBaseProductionCurHilli) * (titheMultBaseProductionCurBiome * titheMultBaseProductionCurHilli)).ToString());
                }
            }
        }

        private void DrawCreateSettlementButton()
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            int buttonLength = 130;
            if (Widgets.ButtonText(new Rect((InitialSize.x - 32 - buttonLength) / 2f, 535, buttonLength, 32), "Settle".Translate() + ": (" + settlementCreationCost + ")")) //add inital cost
            {
                if (!CanCreateSettlementHere()) return;

                PaymentUtil.paySilver(settlementCreationCost);

                //create settle event
                FCEvent evt = FCEventMaker.MakeEvent(FCEventDefOf.settleNewColony);
                evt.location = currentTileSelected;
                evt.planetName = Find.World.info.name;
                evt.timeTillTrigger = Find.TickManager.TicksGame + timeToTravel;
                evt.source = faction.capitalLocation;
                faction.addEvent(evt);

                faction.settlementCaravansList.Add(evt.location.ToString());
                Messages.Message("CaravanSentToLocation".Translate() + " " + (evt.timeTillTrigger - Find.TickManager.TicksGame).ToTimeString() + "!", MessageTypeDefOf.PositiveEvent);

                DoPostEventCreationTraitThings();
            }
        }

        private bool CanCreateSettlementHere()
        {
            StringBuilder reason = new StringBuilder();
            if (!WorldTileChecker.IsValidTileForNewSettlement(currentTileSelected, reason) || faction.checkSettlementCaravansList(currentTileSelected.ToString()) || !PlayerHasEnoughSilver(reason))
            {
                Messages.Message(reason.ToString(), MessageTypeDefOf.RejectInput);
                return false;
            }

            return true;
        }

        private void DoPostEventCreationTraitThings()
        {
            if (traitExpansionistReducedFee)
            {
                faction.traitExpansionistTickLastUsedSettlementFeeReduction = Find.TickManager.TicksGame;
                faction.traitExpansionistBoolCanUseSettlementFeeReduction = false;
            }
        }

        private bool PlayerHasEnoughSilver(StringBuilder reason)
        {
            if (PaymentUtil.getSilver() >= settlementCreationCost) return true;

            reason?.Append("NotEnoughSilverToSettle".Translate() + "!");
            return false;
        }

        public void DrawLabelBox(Rect rect, string text1, string text2)
        {
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.MiddleCenter;
            //Draw highlight
            Widgets.DrawHighlight(new Rect(rect.x, rect.y + rect.height /8, rect.width, rect.height / 4f));
            Widgets.Label(new Rect(rect.x, rect.y, rect.width, rect.height / 2f), text1);

            //divider
            Widgets.DrawLineHorizontal(rect.x + 5, rect.y + rect.height / 2, rect.width - 10);

            //Bottom Text - Gamers Rise Up
            Widgets.Label(new Rect(rect.x, rect.y + rect.height / 2, rect.width, rect.height / 2f), text2);
        }
    }
}
