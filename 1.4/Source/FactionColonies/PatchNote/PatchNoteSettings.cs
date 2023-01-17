using Verse;

namespace FactionColonies.PatchNote
{
    class PatchNoteSettings : ModSettings
    {
        public double lastVersion = 0d;
        public double curVersion = 0d;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref lastVersion, "lastVersion");
            Scribe_Values.Look(ref curVersion, "curVersion");
            base.ExposeData();
        }
    }
}
