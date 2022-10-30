using RimWorld;

namespace FactionColonies.util
{
    static class TicksExtensions
    {
        public static string ToTimeString(this int ticks)
        {
            string hours = "" + ticks / 2500;
            string fractionHours = "" + ticks % 2500 / 2500f;

            if (fractionHours.Length > 3)
            { 
                fractionHours = fractionHours.Substring(2, 2); 
            } 
            else if (fractionHours.Length == 3)
            {
                fractionHours = fractionHours.Substring(2, 1) + "0";
            }

            while (hours.Length < 2) hours = "0" + hours;

            if (ticks < 60000) return hours + "." + fractionHours + " hours";
            return GenDate.ToStringTicksToDays(ticks);
        }
    }
}
