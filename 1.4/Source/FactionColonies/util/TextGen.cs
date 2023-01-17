using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using System.Text;
using System.Threading.Tasks;

namespace FactionColonies.util
{
    /// <summary>
    /// Arguably only generates short names so far, but should contain everything "random text" in future
    /// </summary>
    static class TextGen
    {
        /// <summary>
        /// Converts the given string <paramref name="name"/> into a shorter version. The resulting string contains the first word and every uppercase char of the following words
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string ToShortName(string name)
        {
            IEnumerable<string> nameSplit = name.Split(' ').Where(str => !str.NullOrEmpty() && char.IsUpper(str[0]));

            if (nameSplit.EnumerableNullOrEmpty()) return name;

            string main = nameSplit.First();

            return nameSplit.Aggregate(main, (total, next) => total + ((main == next) ? ' ' : next[0]));
        }
    }
}
