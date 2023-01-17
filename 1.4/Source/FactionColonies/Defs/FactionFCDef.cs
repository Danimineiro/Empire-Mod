using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace FactionColonies
{
    public class FactionFCDef : Def, IExposable
    {

        public FactionFCDef()
        {
        }

        public void ExposeData()
        {

            Scribe_Values.Look<TechLevel>(ref techLevel, "techLevel");
            Scribe_Deep.Look<ThingFilter>(ref apparelStuffFilter, "apparelStuffFilter");

        }

        public TechLevel techLevel = TechLevel.Undefined;
        public ThingFilter apparelStuffFilter = new ThingFilter();

        //public required research
    }



}
