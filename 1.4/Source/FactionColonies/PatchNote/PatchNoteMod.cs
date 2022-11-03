using UnityEngine;
using Verse;
using System.Reflection;
using System.Linq;
using Verse;
using UnityEngine;
using HarmonyLib;
using RimWorld;

namespace FactionColonies.PatchNote
{
    class PatchNoteMod : Verse.Mod
    {
        public PatchNoteMod(ModContentPack content) : base(content) { }

        [StaticConstructorOnStartup]
        public static class ModStartup
        {
            static ModStartup()
            {
                //Hugslibs-added defs will be queued up before this Static Constructor
                //So queue replace frame generation after that
                //LongEventHandler.QueueLongEvent(() => ThingDefGenerator_ReplaceFrame.AddReplaceFrames(), null, true, null);
                //LongEventHandler.QueueLongEvent(() => CoolersOverWalls.DesignatorBuildDropdownStuffFix.SanityCheck(), null, true, null);
            }
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            base.DoSettingsWindowContents(inRect);
           // settings.DoWindowContents(inRect);
        }
    }
}
