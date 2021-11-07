using System;
using System.Linq;
using FactionColonies.util;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace FactionColonies
{
    class SimulateBattleFc
    {
        public static int FightBattle(militaryForce MFA, militaryForce MFB)
        {
            //Log.Message("Starting battle");
            while (MFA.forceRemaining > 0 && MFB.forceRemaining > 0)
            {
                FightRound(MFA, MFB);
            }

            if (MFA.forceRemaining <= 0)
            {
                //Log.Message("Defending Force has won.");
                //b is winner
                return 1;
            }

            if (MFB.forceRemaining <= 0)
            {
                //Log.Message("Attacking Force has won.");
                //a is winner
                return 0;
            }

            Log.Message("FightBattle catch statement - Empire");
            //catch
            return -1;
        }

        public static void FightRound(militaryForce MFA, militaryForce MFB)
        {
            int randA = Rand.Range(0, 20);
            int randB = Rand.Range(0, 20);
            //Log.Message("A Begin: " + MFA.forceRemaining + " : " + MFB.forceRemaining + " B begin");
            //Log.Message("A Rolled: " + randA.ToString() + " : " + randB.ToString() + " B rolled");
            if (randA == randB)
            {
            }
            else if (randA > randB)
            {
                MFB.forceRemaining -= 1;
            }
            else if (randB > randA)
            {
                MFA.forceRemaining -= 1;
            }
            //Log.Message("A Remain: " + MFA.forceRemaining + " : " + MFB.forceRemaining + " B remain");
        }
    }


    public class militaryForce : IExposable
    {
        public double militaryLevel;
        public double militaryEfficiency;
        public double forceRemaining;
        public int random = Rand.Range(0, 0);
        public SettlementFC homeSettlement;
        public Faction homeFaction;

        public void ExposeData()
        {
            Scribe_Values.Look(ref militaryLevel, "militaryLevel");
            Scribe_Values.Look(ref militaryEfficiency, "militaryEfficiency");
            Scribe_Values.Look(ref forceRemaining, "forceRemaining");
            Scribe_Values.Look(ref random, "random");
            Scribe_References.Look(ref homeSettlement, "homeSettlement");
            Scribe_References.Look(ref homeFaction, "homeFaction");
        }

        public militaryForce()
        {
        }

        public militaryForce(double militaryLevel, double militaryEfficiency, SettlementFC homeSettlement,
            Faction homeFaction)
        {
            this.militaryLevel = militaryLevel;
            this.militaryEfficiency = militaryEfficiency;
            this.homeSettlement = homeSettlement;
            this.homeFaction = homeFaction;
            forceRemaining = Math.Round(militaryLevel * militaryEfficiency);
        }

        public static militaryForce createMilitaryForceFromSettlement(SettlementFC settlement, bool isAttacking = false,
            militaryForce homeDefendingForce = null)
        {
            FactionFC faction = Find.World.GetComponent<FactionFC>();
            int militaryLevelBonus = 0;
            if (faction.hasTrait(FCPolicyDefOf.defenseInDepth) && isAttacking == false)
                militaryLevelBonus += 2;
            double homeForceLevel = 0;
            if (homeDefendingForce != null)
            {
                homeForceLevel = homeDefendingForce.militaryLevel;
            }

            double militaryLevel = settlement.settlementMilitaryLevel + militaryLevelBonus + homeForceLevel;
            double efficiency =
                TraitUtilsFC.cycleTraits(new double(), "militaryMultiplierCombatEfficiency", faction.traits,
                    Operation.Multiplikation) * TraitUtilsFC.cycleTraits(new double(), "militaryMultiplierCombatEfficiency",
                    settlement.traits, Operation.Multiplikation);
            if (isAttacking && faction.hasPolicy(FCPolicyDefOf.militaristic)) 
                efficiency *= 1.2;
            militaryForce returnForce = new militaryForce(militaryLevel, efficiency, settlement,
                FactionColonies.getPlayerColonyFaction());
            return returnForce;
            //create and return force.
        }

        public static militaryForce createMilitaryForceFromEnemySettlement(Settlement settlement)
        {
            double militaryLevel;
            double efficiency;

            switch (settlement.Faction.def.techLevel)
            {
                case TechLevel.Undefined:
                    militaryLevel = 1;
                    efficiency = .5;
                    break;
                case TechLevel.Animal:
                    militaryLevel = 2;
                    efficiency = .5;
                    break;
                case TechLevel.Neolithic:
                    militaryLevel = 3;
                    efficiency = 1;
                    break;
                case TechLevel.Medieval:
                    militaryLevel = 4;
                    efficiency = 1.2;
                    break;

                case TechLevel.Industrial:
                    militaryLevel = 4;
                    efficiency = 1.2;
                    break;
                case TechLevel.Spacer:
                    militaryLevel = 5;
                    efficiency = 1.3;
                    break;
                case TechLevel.Ultra:
                    militaryLevel = 6;
                    efficiency = 1.3;
                    break;
                case TechLevel.Archotech:
                    militaryLevel = 6;
                    efficiency = 1.5;
                    break;
                default:
                    militaryLevel = 1;
                    efficiency = 1;
                    Log.Message("Defaulted createMilitaryForceFromEnemyFaction switch case - Empire Mod");
                    break;
            }

            militaryForce returnForce = new militaryForce(militaryLevel, efficiency, null, settlement.Faction);
            return returnForce;
        }

        public static militaryForce createMilitaryForceFromFaction(Faction faction, bool handicap)
        {
            double militaryLevel = 1;
            double efficiency = 1;
            if (faction != null && faction.def != null && faction.def.techLevel != null)
            {
                switch (faction.def.techLevel)
                {
                    case TechLevel.Undefined:
                        militaryLevel = 1;
                        efficiency = .5;
                        break;
                    case TechLevel.Animal:
                        militaryLevel = 2;
                        efficiency = .5;
                        break;
                    case TechLevel.Neolithic:
                        militaryLevel = 3;
                        efficiency = 1;
                        break;
                    case TechLevel.Medieval:
                        militaryLevel = 4;
                        efficiency = 1.2;
                        break;

                    case TechLevel.Industrial:
                        militaryLevel = 4;
                        efficiency = 1.2;
                        break;
                    case TechLevel.Spacer:
                        militaryLevel = 5;
                        efficiency = 1.3;
                        break;
                    case TechLevel.Ultra:
                        militaryLevel = 6;
                        efficiency = 1.3;
                        break;
                    case TechLevel.Archotech:
                        militaryLevel = 6;
                        efficiency = 1.5;
                        break;
                    default:
                        militaryLevel = 1;
                        efficiency = 1;
                        Log.Message("Defaulted createMilitaryForceFromEnemyFaction switch case - Empire Mod");
                        break;
                }

                if (faction.def.defName == "VFEI_Insect")
                {
                    militaryLevel = 4;
                    efficiency = 1.2;
                }
            }

            double value = militaryLevel + FactionColonies.randomAttackModifier();
            if (handicap)
            {
                value = Math.Min(value,
                    (2 + Math.Round((double) (Find.TickManager.TicksGame -
                                              Find.World.GetComponent<FactionFC>().timeStart - GenDate.TicksPerSeason) /
                                    GenDate.TicksPerSeason)));
                //Log.Message(value.ToString());
            }

            militaryForce returnForce = new militaryForce(value, efficiency, null, faction);
            return returnForce;
        }
    }

    class MilitaryUtilFC
    {
        public static void attackPlayerSettlement(militaryForce attackingForce, SettlementFC settlement,
            Faction enemyFaction)
        {
            FactionFC factionfc = Find.World.GetComponent<FactionFC>();

            FCEvent tmp = FCEventMaker.MakeEvent(FCEventDefOf.settlementBeingAttacked);
            tmp.hasCustomDescription = true;
            tmp.timeTillTrigger = Find.TickManager.TicksGame + 60000;
            tmp.location = settlement.mapLocation;
            tmp.planetName = settlement.planetName;
            tmp.hasDestination = true;
            tmp.customDescription = "settlementAboutToBeAttacked"
                .Translate(settlement.name, enemyFaction.Name);
            tmp.militaryForceDefending = militaryForce.createMilitaryForceFromSettlement(settlement);
            tmp.militaryForceDefendingFaction = FactionColonies.getPlayerColonyFaction();
            tmp.militaryForceAttacking = attackingForce;
            tmp.militaryForceAttackingFaction = enemyFaction;
            tmp.settlementFCDefending = settlement;

            SettlementFC highest = null;

            foreach (SettlementFC settlementCompare in factionfc.settlements)
            {
                if (settlementCompare.autoDefend && settlementCompare.militaryBusy == false &&
                    settlementCompare.settlementMilitaryLevel > settlement.settlementMilitaryLevel &&
                    (highest == null || settlementCompare.settlementMilitaryLevel > highest.settlementMilitaryLevel))
                    highest = settlementCompare;
            }

            if (highest != null)
            {
                changeDefendingMilitaryForce(tmp, highest);
            }

            settlement.worldSettlement.defenderForce = tmp.militaryForceDefending;
            settlement.worldSettlement.attackerForce = tmp.militaryForceAttacking;

            Find.World.GetComponent<FactionFC>().addEvent(tmp);

            tmp.customDescription += "\n\nThe estimated attacking force's power is: " +
                                     tmp.militaryForceAttacking.forceRemaining;
            settlement.isUnderAttack = true;

            Find.LetterStack.ReceiveLetter("settlementInDanger".Translate(), tmp.customDescription,
                LetterDefOf.ThreatBig, new LookTargets(Find.WorldObjects.WorldObjectAt<WorldSettlementFC>(settlement.mapLocation)));
        }

        public static void changeDefendingMilitaryForce(FCEvent evt, SettlementFC settlementOfMilitaryForce)
        {
            FactionFC factionfc = Find.World.GetComponent<FactionFC>();
            militaryForce tmpMilitaryForce = null;
            SettlementFC homeSettlement = factionfc.returnSettlementByLocation(evt.location, evt.planetName);
            if (settlementOfMilitaryForce == evt.militaryForceDefending.homeSettlement)
            {
                Messages.Message("militaryAlreadyDefendingSettlement".Translate(), MessageTypeDefOf.RejectInput);
                return;
            }

            WorldSettlementFC target = Find.World.worldObjects.WorldObjectAt<WorldSettlementFC>(evt.location);

            if (evt.militaryForceDefending.homeSettlement !=
                factionfc.returnSettlementByLocation(evt.location, evt.planetName))
            {
                //if the forces defending aren't the forces belonging to the settlement
                evt.militaryForceDefending.homeSettlement.returnMilitary(false);
            }

            if (settlementOfMilitaryForce != homeSettlement)
            {
                tmpMilitaryForce =
                    militaryForce.createMilitaryForceFromSettlement(
                        factionfc.returnSettlementByLocation(evt.location, evt.planetName), true);
            }

            factionfc.militaryTargets.Remove(evt.location);
            evt.militaryForceDefending =
                militaryForce.createMilitaryForceFromSettlement(settlementOfMilitaryForce,
                    homeDefendingForce: tmpMilitaryForce);

            target.defenderForce = evt.militaryForceDefending;
            
            if (settlementOfMilitaryForce == homeSettlement)
            {
                //if home settlement is reseting to defense
                Messages.Message("defendingMilitaryReset".Translate(), MessageTypeDefOf.NeutralEvent);
            }
            else
            {
                //if settlement is foreign
                settlementOfMilitaryForce.sendMilitary(evt.settlementFCDefending.mapLocation, evt.planetName,
                    util.MilitaryJob.DefendFriendlySettlement, -1, evt.militaryForceAttackingFaction);
                Find.LetterStack.ReceiveLetter("Military Action", "ForeignMilitarySwitch"
                    .Translate(settlementOfMilitaryForce.name,
                        factionfc.returnSettlementByLocation(evt.location, evt.planetName).name,
                        evt.militaryForceDefending.militaryLevel), LetterDefOf.NeutralEvent);
            }
        }

        public static militaryForce returnDefendingMilitaryForce(FCEvent evt)
        {
            return evt.militaryForceDefending;
        }

        public static FCEvent returnMilitaryEventByLocation(int location)
        {
            return Find.World.GetComponent<FactionFC>().events
                .FirstOrDefault(evt => evt.def.isMilitaryEvent && evt.location == location);
        }
    }

    class RelationsUtilFC
    {
        public static void attackFaction(Faction faction)
        {
            //Log.Message(Find.FactionManager.OfPlayer.RelationWith(faction).goodwill + " player:colony ");
            Find.FactionManager.OfPlayer.TryAffectGoodwillWith(faction, -50);
            // FIXME Workaround, since method TrySetRelationKind is gone
            TrySetRelationKind(Find.FactionManager.OfPlayer, faction, FactionRelationKind.Hostile);
            resetPlayerColonyRelations();
            //Log.Message(Find.FactionManager.OfPlayer.RelationWith(faction).goodwill + " player:colony ");
            //FactionColonies.getPlayerColonyFaction().TryAffectGoodwillWith(faction, -50)
        }

        public static void resetPlayerColonyRelations()
        {
            Faction PCFaction = FactionColonies.getPlayerColonyFaction();
            foreach (Faction faction in Find.FactionManager.AllFactionsInViewOrder)
            {
                if (faction != Find.FactionManager.OfPlayer && faction != PCFaction)
                {
                    //if not player faction or player colony faction
                    PCFaction.TryAffectGoodwillWith(faction,
                        (Find.FactionManager.OfPlayer.RelationWith(faction).baseGoodwill -
                         PCFaction.RelationWith(faction).baseGoodwill));
                    // FIXME Workaround, since method TrySetRelationKind is gone
                    TrySetRelationKind(PCFaction, faction, Find.FactionManager.OfPlayer.RelationKindWith(faction));
                    //Log.Message(Find.FactionManager.OfPlayer.RelationWith(faction).goodwill + " player:colony " + PCFaction.RelationWith(faction).goodwill);
                }
            }
        }

        private static bool TrySetRelationKind(Faction self, Faction other, FactionRelationKind kind, bool canSendLetter = true)
        {
            FactionRelation factionRelation = self.RelationWith(other);
            if (factionRelation.kind == kind)
            {
                return true;
            }
            if (!self.HasGoodwill)
            {
                self.SetRelationDirect(other, kind, canSendLetter);
                return true;
            }
            switch (kind)
            {
                case FactionRelationKind.Hostile:
                    self.TryAffectGoodwillWith(other, -75 - factionRelation.baseGoodwill, canSendMessage: false, canSendLetter);
                    return factionRelation.kind == FactionRelationKind.Hostile;
                case FactionRelationKind.Neutral:
                    self.TryAffectGoodwillWith(other, -factionRelation.baseGoodwill, canSendMessage: false, canSendLetter);
                    return factionRelation.kind == FactionRelationKind.Neutral;
                case FactionRelationKind.Ally:
                    self.TryAffectGoodwillWith(other, 75 - factionRelation.baseGoodwill, canSendMessage: false, canSendLetter);
                    return factionRelation.kind == FactionRelationKind.Ally;
                default:
                    throw new NotSupportedException(kind.ToString());
            }
        }
    }
}