using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace FactionColonies
{
    
    //Squad Class
    public class MilSquadFC : IExposable, ILoadReferenceable
    {
        public int loadID = -1;
        public string name;
        public List<MilUnitFC> units = new List<MilUnitFC>();
        public double equipmentTotalCost;
        public bool isTraderCaravan;
        public bool isCivilian;
        public int tickChanged;

        public static void UpdateEquipmentTotalCostOfSquadsContaining(MilUnitFC unit)
        {
            Find.World.GetComponent<FactionFC>().militaryCustomizationUtil.squads.ForEach(delegate(MilSquadFC squad)
            {
                if (squad.units.Contains(unit))
                {
                    squad.updateEquipmentTotalCost();
                }
            });
        }

        public MilSquadFC()
        {
        }

        public MilSquadFC(bool newSquad)
        {
            if (newSquad)
            {
                setLoadID();
            }
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref loadID, "loadID", -1);
            Scribe_Values.Look(ref name, "name");
            Scribe_Collections.Look(ref units, "units", LookMode.Reference);
            Scribe_Values.Look(ref equipmentTotalCost, "equipmentTotalCost", -1);
            Scribe_Values.Look(ref isTraderCaravan, "isTraderCaravan");
            Scribe_Values.Look(ref isCivilian, "isCivilian");
            Scribe_Values.Look(ref tickChanged, "tickChanged");

            updateEquipmentTotalCost();
        }

        public void setLoadID()
        {
            loadID = Find.World.GetComponent<FactionFC>().NextSquadID;
        }

        public int updateEquipmentTotalCost()
        {
            double totalCost = 0;
            foreach (MilUnitFC unit in units)
            {
                totalCost += unit.getTotalCost;
            }

            equipmentTotalCost = totalCost;
            return (int) equipmentTotalCost;
        }

        public void newSquad()
        {
            units = new List<MilUnitFC>();
            for (int sq = 0; sq < 30; sq++)
            {
                units.Add(Find.World.GetComponent<FactionFC>().militaryCustomizationUtil.blankUnit);
            }

            isTraderCaravan = false;
            isCivilian = false;

            updateEquipmentTotalCost();
        }

        public void ChangeTick()
        {
            tickChanged = Find.TickManager.TicksGame;
        }

        public int getLatestChanged
        {
            get
            {
                int latestChange;
                latestChange = tickChanged;
                foreach (MilUnitFC unit in units)
                {
                    latestChange = Math.Max(unit.tickChanged, latestChange);
                }

                return latestChange;
            }
        }

        public void deleteSquad()
        {
            Find.World.GetComponent<FactionFC>().militaryCustomizationUtil.squads.Remove(this);
        }

        public string GetUniqueLoadID()
        {
            return $"MilSquadFC_{loadID}";
        }

        public void setTraderCaravan(bool state)
        {
            ChangeTick();
            isTraderCaravan = state;
            if (state)
            {
                int hasTraderCount = units.Count(unit => unit.isTrader);

                if (hasTraderCount == 0)
                {
                    Messages.Message("There must be a trader in the squad to be a trader caravan!",
                        MessageTypeDefOf.RejectInput);
                    return;
                }

                if (hasTraderCount > 1)
                {
                    Messages.Message("There cannot be more than one trader in the caravan!",
                        MessageTypeDefOf.RejectInput);
                    return;
                }

                setCivilian(true);
            }
            else
            {
                setCivilian(false);
            }

            isTraderCaravan = state;
        }

        public void setCivilian(bool state)
        {
            ChangeTick();
            isCivilian = state;
            if (state)
            {
            }
        }
    }
}