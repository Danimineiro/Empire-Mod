using System.Collections.Generic;
using System.Linq;
using Verse;

namespace FactionColonies.util
{
    /// <summary>
    /// This class provides extensions for some kinds of lists
    /// </summary>
    static class ListExtensions
    {
        /// <summary>
        /// Transforms a <paramref name="list"/> into a string<para />
        /// Example (The linebreaks are twice as big as ingame):<para />
        /// Steel 75x<para />
        /// Wood 150x
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static string ToLetterString(this IEnumerable<Thing> list)
        {
            Dictionary<string, int> thingCountDic = new Dictionary<string, int>();
            foreach (Thing thing in list)
            {
                if (thingCountDic.ContainsKey(thing.LabelCapNoCount))
                {
                    thingCountDic[thing.LabelCapNoCount] += thing.stackCount;
                }
                else
                {
                    thingCountDic.Add(thing.LabelCapNoCount, thing.stackCount);
                }
            }

            return thingCountDic.Aggregate("", (current, next) => current + $"{next.Key} {next.Value}x\n").TrimEnd('\n');
        }
    }
}
