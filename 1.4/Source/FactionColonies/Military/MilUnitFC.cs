using System;
using System.Collections.Generic;
using System.Linq;
using FactionColonies.util;
using RimWorld;
using Verse;

namespace FactionColonies
{
    public class MilUnitFC : IExposable, ILoadReferenceable
    {
        public int loadID;
        public string name;
        public Pawn defaultPawn;
        public bool isBlank;
        public double equipmentTotalCost;
        public bool isTrader;
        public bool isCivilian;
        public int tickChanged = -1;
        public PawnKindDef animal;
        public PawnKindDef pawnKind;

        public MilUnitFC()
        {
        }

        public MilUnitFC(bool blank)
        {
            loadID = Find.World.GetComponent<FactionFC>().NextUnitID;
            isBlank = blank;
            equipmentTotalCost = 0;

            pawnKind = FactionColonies.getPlayerColonyFaction()?.RandomPawnKind() ?? DefDatabase<FactionDef>.GetNamed("PColony").pawnGroupMakers.RandomElement().options.RandomElement().kind;
            generateDefaultPawn();
        }

        public string GetUniqueLoadID()
        {
            return $"MilUnitFC_{loadID}";
        }

        public void ExposeData()
        {
            Scribe_Values.Look(ref loadID, "loadID");
            Scribe_Deep.Look(ref defaultPawn, "defaultPawn");
            Scribe_Values.Look(ref name, "name");
            Scribe_Values.Look(ref isBlank, "blank");
            Scribe_Values.Look(ref equipmentTotalCost, "equipmentTotalCost", -1);
            Scribe_Values.Look(ref isTrader, "isTrader");
            Scribe_Values.Look(ref isCivilian, "isCivilian");
            Scribe_Values.Look(ref tickChanged, "tickChanged");
            Scribe_Defs.Look(ref pawnKind, "PawnKind");
            Scribe_Defs.Look(ref animal, "animal");
        }

        public void generateDefaultPawn()
        {
            List<Apparel> apparel = new List<Apparel>();
            List<ThingWithComps> equipment = new List<ThingWithComps>();

            if (defaultPawn != null)
            {
                apparel.AddRange(defaultPawn.apparel.WornApparel);
                equipment.AddRange(defaultPawn.equipment.AllEquipmentListForReading);

                Reset:
                foreach (Apparel cloth in defaultPawn.apparel.WornApparel)
                {
                    defaultPawn.apparel.Remove(cloth);
                    goto Reset;
                }

                foreach (ThingWithComps weapon in defaultPawn.equipment.AllEquipmentListForReading)
                {
                    defaultPawn.equipment.Remove(weapon);
                    goto Reset;
                }

                defaultPawn.Destroy();
            }

            defaultPawn = PawnGenerator.GeneratePawn(FCPawnGenerator.WorkerOrMilitaryRequest(pawnKind));
            defaultPawn.health.forceIncap = true;
            defaultPawn.mindState.canFleeIndividual = false;
            defaultPawn.apparel.DestroyAll();

            foreach (Apparel clothes in apparel)
            {
                //Log.Message(clothes.Label);
                defaultPawn.apparel.Wear(clothes);
            }

            foreach (ThingWithComps weapon in equipment)
            {
                //Log.Message(weapon.Label);
                equipWeapon(weapon);
            }
        }

        public void changeTick()
        {
            tickChanged = Find.TickManager.TicksGame;
        }

        public void equipWeapon(ThingWithComps weapon)
        {
            changeTick();
            if (isCivilian == false)
            {
                unequipWeapon();
                defaultPawn.equipment.AddEquipment(weapon);
            }
            else
            {
                Messages.Message("You cannot put a weapon on a civilian!", MessageTypeDefOf.RejectInput);
            }

            MilSquadFC.UpdateEquipmentTotalCostOfSquadsContaining(this);
        }

        public void unequipWeapon()
        {
            changeTick();
            defaultPawn.equipment.DestroyAllEquipment();

            MilSquadFC.UpdateEquipmentTotalCostOfSquadsContaining(this);
        }

        public void wearEquipment(Apparel Equipment, bool wear)
        {
            changeTick();
            Reset:
            foreach (ApparelLayerDef layer in Equipment.def.apparel.layers)
            {
                foreach (BodyPartGroupDef part in Equipment.def.apparel.bodyPartGroups)
                {
                    foreach (Apparel apparel in defaultPawn.apparel.WornApparel)
                    {
                        if ((apparel.def.apparel.layers.Contains(layer) &&
                             apparel.def.apparel.bodyPartGroups.Contains(part)) ||
                            (Equipment.def.apparel.layers.Contains(ApparelLayerDefOf.Overhead) &&
                             apparel.def.apparel.layers.Contains(ApparelLayerDefOf.Overhead)))
                        {
                            defaultPawn.apparel.Remove(apparel);
                            goto Reset;
                        }
                    }
                }
            }

            if (wear == false)
            {
                //NOTHING
            }
            else
            {
                defaultPawn.apparel.Wear(Equipment);
            }

            MilSquadFC.UpdateEquipmentTotalCostOfSquadsContaining(this);
        }

        public void removeUnit()
        {
            Find.World.GetComponent<FactionFC>().militaryCustomizationUtil.units.Remove(this);
        }

        public void unequipAllEquipment()
        {
            changeTick();
            defaultPawn.apparel.DestroyAll();
            defaultPawn.equipment.DestroyAllEquipment();

            MilSquadFC.UpdateEquipmentTotalCostOfSquadsContaining(this);
        }

        public double getTotalCost
        {
            get
            {
                updateEquipmentTotalCost();
                return equipmentTotalCost;
            }
        }

        public void updateEquipmentTotalCost()
        {
            if (isBlank)
            {
                equipmentTotalCost = 0;
            }
            else
            {
                double totalCost = 0;
                totalCost += Math.Floor(defaultPawn.def.BaseMarketValue * FactionColonies.militaryRaceCostMultiplier);

                totalCost = defaultPawn.apparel.WornApparel.Aggregate(totalCost,
                    (current, thing) => current + thing.MarketValue);

                totalCost = defaultPawn.equipment.AllEquipmentListForReading.Aggregate(totalCost,
                    (current, thing) => current + thing.MarketValue);

                if (animal != null)
                {
                    totalCost += Math.Floor(animal.race.BaseMarketValue * FactionColonies.militaryAnimalCostMultiplier);
                }

                equipmentTotalCost = Math.Ceiling(totalCost);
            }
        }

        public void setTrader(bool state)
        {
            changeTick();
            isTrader = state;
            if (state)
            {
                setCivilian(true);
            }
        }

        public void setCivilian(bool state)
        {
            changeTick();
            isCivilian = state;
            if (state)
            {
                unequipWeapon();
            }
            else
            {
                setTrader(false);
            }
        }
    }
}