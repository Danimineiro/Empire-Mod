using RimWorld.BaseGen;
using Verse;

namespace FactionColonies
{
    public class SymbolResolver_ColonyPart_Indoors_Division_Split: SymbolResolver
    {
        private const int MinLengthAfterSplit = 5;
        private const int MinWidthOrHeight = 9;

        public override bool CanResolve(ResolveParams rp)
        {
            if (!base.CanResolve(rp))
                return false;
            return rp.rect.Width >= 9 || rp.rect.Height >= 9;
        }

        public override void Resolve(ResolveParams rp)
        {
            if (rp.rect.Width < 9 && rp.rect.Height < 9)
                Log.Warning("Too small rect. params=" + rp);
            else if ((!Rand.Bool || rp.rect.Height < 9 ? (rp.rect.Width < 9 ? 1 : 0) : 1) != 0)
            {
                int num = Rand.RangeInclusive(4, rp.rect.Height - 5);
                ResolveParams resolveParams1 = rp;
                resolveParams1.rect = new CellRect(rp.rect.minX, rp.rect.minZ, rp.rect.Width, num + 1);
                BaseGen.symbolStack.Push("colonyPart_indoors", resolveParams1);
                ResolveParams resolveParams2 = rp;
                resolveParams2.rect = new CellRect(rp.rect.minX, rp.rect.minZ + num, rp.rect.Width, rp.rect.Height - num);
                BaseGen.symbolStack.Push("colonyPart_indoors", resolveParams2);
            }
            else
            {
                int num = Rand.RangeInclusive(4, rp.rect.Width - 5);
                ResolveParams resolveParams1 = rp;
                resolveParams1.rect = new CellRect(rp.rect.minX, rp.rect.minZ, num + 1, rp.rect.Height);
                BaseGen.symbolStack.Push("colonyPart_indoors", resolveParams1);
                ResolveParams resolveParams2 = rp;
                resolveParams2.rect = new CellRect(rp.rect.minX + num, rp.rect.minZ, rp.rect.Width - num, rp.rect.Height);
                BaseGen.symbolStack.Push("colonyPart_indoors", resolveParams2);
            }
        }
    }
}