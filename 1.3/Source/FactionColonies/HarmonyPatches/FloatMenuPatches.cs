//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using HarmonyLib;
//using Verse;

//namespace FactionColonies
//{
//    [HarmonyPatch(typeof(WindowStack), "TryRemove", typeof(Window), typeof(bool))]
//    class FloatMenuPatches
//    {
//        public static bool Prefix(Window window)
//        {
//            if (window is Searchable_FloatMenu searchable && !searchable.CanBeClosed) return false;

//            return true;
//        }
//    }
//}
