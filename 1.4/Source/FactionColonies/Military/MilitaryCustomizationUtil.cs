using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FactionColonies
{
    //Mil customization class
    public class MilitaryCustomizationUtil : IExposable
    {
        public List<MilUnitFC> units = new List<MilUnitFC>();
        public List<MilSquadFC> squads = new List<MilSquadFC>();

        public List<MercenarySquadFC> mercenarySquads = new List<MercenarySquadFC>();
        public List<MilitaryFireSupport> fireSupport = new List<MilitaryFireSupport>();
        public List<MilitaryFireSupport> fireSupportDefs = new List<MilitaryFireSupport>();
        public MilUnitFC blankUnit;
        public List<Mercenary> deadPawns = new List<Mercenary>();
        public int tickChanged;

        public MilitaryCustomizationUtil()
        {
            //set load stuff here
            if (units == null)
            {
                units = new List<MilUnitFC>();
            }

            if (squads == null)
            {
                squads = new List<MilSquadFC>();
            }

            if (blankUnit == null)
            {
                //blankUnit = new MilUnitFC(true);
            }

            if (mercenarySquads == null)
            {
                mercenarySquads = new List<MercenarySquadFC>();
            }

            if (deadPawns == null)
            {
                deadPawns = new List<Mercenary>();
            }

            if (fireSupportDefs == null)
            {
                fireSupportDefs = new List<MilitaryFireSupport>();
            }
        }

        public void checkMilitaryUtilForErrors()
        {
            if (blankUnit == null)
                blankUnit = new MilUnitFC(true);
            //Log.Message("checking for errors" + Find.TickManager.TicksGame);
            foreach (MilSquadFC squad in squads)
            {
                bool changed = false;
                for (int count = 0; count < 30; count++)
                {
                    if (squad.units[count] != null &&
                        (units.Contains(squad.units[count]) || squad.units[count] == blankUnit)) continue;
                    squad.units[count] = blankUnit;
                    changed = true;
                }

                if (!changed) continue;
                foreach (var squadMerc in mercenarySquads.Where(squadMerc =>
                    squadMerc.outfit != null && squadMerc.outfit == squad))
                {
                    squadMerc.OutfitSquad(squad);
                }
            }

            foreach (MercenarySquadFC squad in mercenarySquads)
            {
                if (squad.outfit == null || squads.Contains(squad.outfit) == false)
                {
                    squad.StripSquad();
                    squad.outfit = null;
                }
                else
                {
                    int settlementMilLevel = 0;
                    if (squad.settlement != null)
                        settlementMilLevel = squad.settlement.settlementMilitaryLevel;
                    if (squad.outfit == null || !(squad.outfit.equipmentTotalCost >
                                                  FactionColonies.calculateMilitaryLevelPoints(settlementMilLevel)))
                        continue;
                    if (squad.settlement != null)
                    {
                        Messages.Message(
                            "The max allowed equipment cost for the squad assigned to " + squad.settlement.name +
                            " has been exceeded. Thus, the settlement's squad has been unassigned.",
                            MessageTypeDefOf.RejectInput);
                    }

                    squad.outfit = null;
                    squad.StripSquad();
                }
            }

            if (tickChanged >= GETLatestChange) return;
            foreach (var merc in mercenarySquads.Where(merc => merc.outfit != null))
            {
                merc.OutfitSquad(merc.outfit);
            }
        }

        public int GETLatestChange
        {
            get { return squads.Select(squadFC => squadFC.getLatestChanged).Prepend(0).Max(); }
        }

        public MercenarySquadFC returnSquadFromUnit(Pawn unit)
        {
            foreach (var squad in mercenarySquads.Where(squad => squad.AllDeployedMercenaryPawns.Contains(unit)))
            {
                return squad;
            }

            Log.Message("Empire - MercenarySquadFC - returnSquadFromUnit - Did not find squad.");
            return null;
        }

        public Mercenary returnMercenaryFromUnit(Pawn unit, MercenarySquadFC squad)
        {
            return squad.mercenaries.FirstOrDefault(merc => merc.pawn == unit);
        }

        public List<Mercenary> AllMercenaries
        {
            get
            {
                List<Mercenary> list = new List<Mercenary>();
                foreach (MercenarySquadFC squad in mercenarySquads)
                {
                    list.AddRange(squad.mercenaries);
                    if (squad.animals != null && squad.animals.Count > 0)
                    {
                        list.AddRange(squad.animals);
                    }
                }

                return list;
            }
        }

        public IEnumerable<MercenarySquadFC> DeployedSquads
        {
            get { return mercenarySquads.Where(squad => squad.isDeployed).ToList(); }
        }

        public List<Pawn> AllMercenaryPawns
        {
            get { return AllMercenaries.Select(merc => merc.pawn).ToList(); }
        }

        public void resetSquads()
        {
            squads = new List<MilSquadFC>();
        }

        public void updateUnits()
        {
            foreach (MilUnitFC unit in units)
            {
                unit.updateEquipmentTotalCost();
            }
        }

        public void attemptToAssignSquad(SettlementFC settlement, MilSquadFC squad)
        {
            if (FactionColonies.calculateMilitaryLevelPoints(settlement.settlementMilitaryLevel) >=
                squad.equipmentTotalCost)
            {
                if (squadExists(settlement))
                {
                    settlement.militarySquad.OutfitSquad(squad);
                }
                else
                {
                    //create new squad
                    createMercenarySquad(settlement);
                    settlement.militarySquad.OutfitSquad(squad);
                }

                Messages.Message(squad.name + "'s loadout has been assigned to " + settlement.name,
                    MessageTypeDefOf.TaskCompletion);
            }
            else
            {
                Messages.Message("That squad exceeds the settlement's max allotted cost", MessageTypeDefOf.RejectInput);
            }
        }

        public MercenarySquadFC createMercenarySquad(SettlementFC settlement, bool isExtra = false)
        {
            MercenarySquadFC squad = new MercenarySquadFC();
            squad.initiateSquad();
            mercenarySquads.Add(squad);
            if (!isExtra)
                settlement.militarySquad = findSquad(squad);
            squad.settlement = settlement;
            squad.isExtraSquad = isExtra;

            if (settlement.militarySquad == null)
            {
                Log.Message("Empire - createMercenarySquad fail. Found squad is Null");
            }

            return findSquad(squad);
        }

        public MercenarySquadFC findSquad(MercenarySquadFC squad)
        {
            return mercenarySquads.FirstOrDefault(mercSquad => squad == mercSquad);
        }

        public bool squadExists(SettlementFC settlement)
        {
            return settlement.militarySquad != null;
        }

        public void changeTick()
        {
            tickChanged = Find.TickManager.TicksGame;
        }

        public void ExposeData()
        {
            Scribe_Collections.Look(ref units, "units", LookMode.Deep);
            Scribe_Collections.Look(ref squads, "squads", LookMode.Deep);
            Scribe_Collections.Look(ref mercenarySquads, "mercenarySquads", LookMode.Deep);
            Scribe_Collections.Look(ref fireSupport, "fireSupport", LookMode.Deep);
            Scribe_Collections.Look(ref fireSupportDefs, "fireSupportDefs", LookMode.Deep);
            Scribe_Collections.Look(ref deadPawns, "deadPawns", LookMode.Deep);

            Scribe_Deep.Look(ref blankUnit, "blankUnit");
            Scribe_Values.Look(ref tickChanged, "tickChanged");
        }
    }
}