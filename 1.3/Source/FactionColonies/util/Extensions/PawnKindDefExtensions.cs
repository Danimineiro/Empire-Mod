using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace FactionColonies
{
	static class PawnKindDefExtensions
	{
		public static bool IsHumanLikeRace(this PawnKindDef pawnKindDef)
		{
			return pawnKindDef.race.race?.intelligence == Intelligence.Humanlike && pawnKindDef.race.BaseMarketValue != 0;
		}

		public static bool IsHumanlikeWithLabelRace(this PawnKindDef pawnKindDef)
		{
			return pawnKindDef?.race?.label != null && pawnKindDef.IsHumanLikeRace();
		}

		private static List<string> BlackListedTradeTags
		{
			get
			{
				return new List<string>() 
				{
					"AnimalDryad",
					"AnimalMonster",
					"AnimalGenetic",
					"AnimalAlpha"
				};
			}
		}

		/// <summary>
		///		Checks if a given <c>PawnKindDef</c> <paramref name="pawnKindDef"/> is an Animal and if it is not blacklisted by tradeTag 
		/// </summary>
		/// <param name="pawnKindDef"></param>
		/// <returns></returns>
		public static bool IsAnimalAndAllowed(this PawnKindDef pawnKindDef)
		{
			return pawnKindDef.race.race.Animal && pawnKindDef.RaceProps.IsFlesh &&
									pawnKindDef.race.race.animalType != AnimalType.Dryad &&
									pawnKindDef.race.tradeTags != null &&
									!pawnKindDef.race.tradeTags.Any(tag => BlackListedTradeTags.Contains(tag));
		}

		public static int GetReasonableMercenaryAge(this PawnKindDef pawnKindDef) => Rand.Range((int)Math.Ceiling(pawnKindDef.race.race.lifeExpectancy * 0.2625d), (int)Math.Floor(pawnKindDef.race.race.lifeExpectancy * 0.625d));
	}
}
