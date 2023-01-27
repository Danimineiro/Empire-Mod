using RimWorld.BaseGen;
using Verse;

namespace FactionColonies
{
    public class SymbolResolver_ColonyPart_Indoors : SymbolResolver
    {
        public override void Resolve(ResolveParams rp)
        {
            if (rp.rect.Width > 13 || rp.rect.Height > 13 || (rp.rect.Width >= 9 || rp.rect.Height >= 9) && Rand.Chance(0.3f))
                RimWorld.BaseGen.BaseGen.symbolStack.Push("colonyPart_indoors_division", rp);
            else
                RimWorld.BaseGen.BaseGen.symbolStack.Push("colonyPart_indoors_leaf", rp);
        }
    }
}