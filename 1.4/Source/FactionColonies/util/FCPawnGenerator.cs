using RimWorld;
using Verse;

namespace FactionColonies.util
{
    class FCPawnGenerator
	{
		private static readonly PawnGenerationRequest baseRequest = new PawnGenerationRequest
		{
			Context = PawnGenerationContext.NonPlayer,
			Tile = -1,
			ForceGenerateNewPawn = false,
			Newborn = false,
			AllowDead = false,
			AllowDowned = false,
			CanGeneratePawnRelations = true,
			MustBeCapableOfViolence = false,
			ColonistRelationChanceFactor = 0,
			Inhabitant = false,
			CertainlyBeenInCryptosleep = false,
			ForceRedressWorldPawnIfFormerColonist = false,
			WorldPawnFactionDoesntMatter = false,
			BiocodeApparelChance = 0,
			ExtraPawnForExtraRelationChance = null,
			RelationWithExtraPawnChanceFactor = 0,
			FixedBiologicalAge = Rand.Range(21, 50)
		};

		public static PawnGenerationRequest WorkerOrMilitaryRequest(PawnKindDef pawnKindDef = null)
		{
			PawnGenerationRequest request = baseRequest;

			request.KindDef = pawnKindDef ?? FactionColonies.getPlayerColonyFaction()?.RandomPawnKind() ?? DefDatabase<FactionDef>.GetNamed("PColony").pawnGroupMakers.RandomElement().options.RandomElement().kind;
			request.Faction = FactionColonies.getPlayerColonyFaction();
			request.FixedBiologicalAge = request.KindDef.GetReasonableMercenaryAge();
			request.MustBeCapableOfViolence = true;

			return request;
		}

		public static PawnGenerationRequest CivilianRequest(PawnKindDef pawnKindDef = null)
		{
			PawnGenerationRequest request = baseRequest;

			request.KindDef = pawnKindDef ?? (FactionColonies.getPlayerColonyFaction()?.RandomPawnKind());
			request.Faction = FactionColonies.getPlayerColonyFaction();

			return request;
		}

		public static PawnGenerationRequest AnimalRequest(PawnKindDef race)
		{
			PawnGenerationRequest request = baseRequest;

			request.KindDef = race;
			request.Faction = FactionColonies.getPlayerColonyFaction();
			request.FixedBiologicalAge = request.KindDef.GetReasonableMercenaryAge();

			return request;
		}
	}
}
