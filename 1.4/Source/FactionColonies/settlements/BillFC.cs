using System.Collections.Generic;
using FactionColonies.util;
using RimWorld;
using Verse;

namespace FactionColonies
{

    public class TaxesFC : ILoadReferenceable, IExposable
    {
        //internal variables
        public int loadID;
        public List<Thing> itemTithes;
        public float silverAmount;
        public float electricityAllotted;
        public float researchCompleted;

        //ref
        public SettlementFC settlement;
        public BillFC bill;

        public void ExposeData()
        {
            Scribe_Collections.Look(ref itemTithes, "itemTithes", LookMode.Deep);
            Scribe_Values.Look(ref silverAmount, "silverAmount");
            Scribe_Values.Look(ref electricityAllotted, "electricityAllotted");
            Scribe_Values.Look(ref researchCompleted, "researchCompleted");

            Scribe_Values.Look(ref loadID, "loadID", -1);
            Scribe_References.Look(ref settlement, "settlement");
            Scribe_References.Look(ref bill, "bill");
        }

        public string GetUniqueLoadID()
        {
            return "Taxes_" + loadID;
        }

        public TaxesFC()
        {

        }

        public TaxesFC(BillFC bill)
        {
            SetUniqueLoadID();
            this.bill = bill;
            settlement = bill.settlement;
            silverAmount = 0;
            itemTithes = new List<Thing>();
            electricityAllotted = 0;
            researchCompleted = 0;

        }

        public void SetUniqueLoadID()
        {
            loadID = Find.World.GetComponent<FactionFC>().GetNextTaxID();
        }
    }

    public class BillFC : ILoadReferenceable, IExposable
    {
        //internal variables
        public int loadID;
        public int dueTick;
        

        //ref
        public SettlementFC settlement;
        public TaxesFC taxes;



        public void ExposeData()
        {
            Scribe_Values.Look(ref loadID, "loadID", -1);
            Scribe_Values.Look(ref dueTick, "dueTick", -1);
            

            Scribe_References.Look(ref settlement, "settlement");
            Scribe_Deep.Look(ref taxes, "taxes");
            
        }

        public string GetUniqueLoadID()
        {
            return "Bill_" + loadID;
        }

        public BillFC()
        {

        }

        public BillFC(SettlementFC settlement)
        {
            SetUniqueLoadID();
            this.settlement = settlement;
            dueTick = Find.TickManager.TicksGame + 300000;
            taxes = new TaxesFC(this);
        }

        public void SetUniqueLoadID()
        {
            loadID = Find.World.GetComponent<FactionFC>().GetNextBillID();
        }

        public bool resolve()
        {
            FactionFC factionfc = Find.World.GetComponent<FactionFC>();
            if (PaymentUtil.getSilver() >= -1 * taxes.silverAmount || taxes.silverAmount >= 0)
            { //if have enough silver on the current map to pay  & map belongs to player

                FCEventMaker.createTaxEvent(this);
                if (taxes.researchCompleted != 0)
                {
                    factionfc.researchPointPool += taxes.researchCompleted;
                    Messages.Message("PointsAddedToResearchPool".Translate(taxes.researchCompleted), MessageTypeDefOf.PositiveEvent);
                }

                if (taxes.electricityAllotted != 0)
                {
                    factionfc.powerPool += taxes.electricityAllotted;
                }

                return true;

            }

            string messageString = "NotEnoughSilverForBill".Translate() + " " + settlement.name + ". " + "ConfiscatedTithes".Translate() + "." + " " + "UnpaidTitheEffect".Translate();
            settlement.GainUnrestWithReason(new Message(messageString, MessageTypeDefOf.NegativeEvent), 10d);
            settlement.GainHappiness(-10d);
            factionfc.Bills.Remove(this);
            return false;
        }

        public bool attemptResolve()
        {
            FactionFC factionfc = Find.World.GetComponent<FactionFC>();
            if (PaymentUtil.getSilver() >= -1 * taxes.silverAmount || taxes.silverAmount >= 0)
            { //if have enough silver on the current map to pay  & map belongs to player

                FCEventMaker.createTaxEvent(this);
                if (taxes.researchCompleted != 0)
                {
                    factionfc.researchPointPool += taxes.researchCompleted;
                    Messages.Message("PointsAddedToResearchPool".Translate(taxes.researchCompleted), MessageTypeDefOf.PositiveEvent);
                }

                if (taxes.electricityAllotted != 0)
                {
                    factionfc.powerPool += taxes.electricityAllotted;
                }

                return true;

            }

            return false;
        }
    }



    public class billUtility
    {
        public static void processBills()
        {
            FactionFC factionfc = Find.World.GetComponent<FactionFC>();
            Reset:
            foreach(BillFC bill in factionfc.Bills)
            {
                if (bill.dueTick < Find.TickManager.TicksGame)
                { //if bill is overdue
                    bill.resolve();
                    goto Reset;
                }
            }
        }
    }
}
