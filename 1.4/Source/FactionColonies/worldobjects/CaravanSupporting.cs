using System.Collections.Generic;
using Verse;

namespace FactionColonies
{
    public class CaravanSupporting : IExposable
    {
        public List<Pawn> pawns = new List<Pawn>();

        public void ExposeData()
        {
            Scribe_Collections.Look(ref pawns, "Pawns", LookMode.Reference);
        }
    }
}