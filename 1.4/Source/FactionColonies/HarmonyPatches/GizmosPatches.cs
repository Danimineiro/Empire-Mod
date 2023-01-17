using System.Collections.Generic;
using System.Linq;
using FactionColonies.util;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace FactionColonies
{
	[HarmonyPatch(typeof(Pawn), "GetGizmos")]
	class PawnDraftGizmos
	{
		public static void Postfix(ref Pawn __instance, ref IEnumerable<Gizmo> __result)
		{
			List<Gizmo> output = __result.ToList();
			if (__result == null || __instance?.Faction == null || !output.Any() ||
				!(__instance.Map.Parent is WorldSettlementFC))
			{
				return;
			}

			Pawn found = __instance;
			Pawn_DraftController pawnDraftController = __instance.drafter ?? new Pawn_DraftController(__instance);

			WorldSettlementFC settlementFc = (WorldSettlementFC)__instance.Map.Parent;
			if (__instance.Faction.Equals(FactionColonies.getPlayerColonyFaction()))
			{
				Command_Toggle draftColonists = new Command_Toggle
				{
					hotKey = KeyBindingDefOf.Command_ColonistDraft,
					isActive = () => false,
					toggleAction = () =>
					{
						if (pawnDraftController.pawn.Faction.Equals(Faction.OfPlayer)) return;
						pawnDraftController.pawn.SetFaction(Faction.OfPlayer);
						pawnDraftController.Drafted = true;
					},
					defaultDesc = "CommandToggleDraftDesc".Translate(),
					icon = TexCommand.Draft,
					turnOnSound = SoundDefOf.DraftOn,
					groupKey = 81729172,
					defaultLabel = "CommandDraftLabel".Translate()
				};
				if (pawnDraftController.pawn.Downed) draftColonists.Disable("IsIncapped".Translate(pawnDraftController.pawn.LabelShort, pawnDraftController.pawn));
				draftColonists.tutorTag = "Draft";
				output.Add(draftColonists);
			}
			else if (__instance.Faction.Equals(Faction.OfPlayer) && __instance.Drafted &&
					 !settlementFc.supporting.Any(caravan => caravan.pawns.Any(pawn => pawn.Equals(found))))
			{
				foreach (Command_Toggle action in output.Where(gizmo => gizmo is Command_Toggle))
				{
					if (action.hotKey != KeyBindingDefOf.Command_ColonistDraft)
					{
						continue;
					}

					int index = output.IndexOf(action);
					action.toggleAction = () =>
					{
						found.SetFaction(FactionColonies.getPlayerColonyFaction());
						//settlementFc.worldSettlement.defenderLord.AddPawn(__instance);
					};
					output[index] = action;
					break;
				}
			}

			__result = output;
		}
	}

	[HarmonyPatch(typeof(Pawn), "GetGizmos")]
	class PrisonerGizmosPatch
	{
		private static bool IsPrisonerAndCanBeSend(Pawn pawn) => pawn.guest == null || !pawn.guest.IsPrisoner || !pawn.guest.PrisonerIsSecure || !QuestUtility.GetQuestRelatedGizmos(pawn).EnumerableNullOrEmpty();

		/// <param name="prisoner"></param>
		/// <returns>A <c>Command_Action</c> that sends the selected <paramref name="prisoner"/> to an empire settlementFC. Only displays if the <paramref name="prisoner"/> can be send.</returns>
		private static Command_Action SendPrisonerAction(Pawn prisoner) => new Command_Action
		{
			defaultLabel = "SendToSettlement".Translate(),
			defaultDesc = "",
			icon = TexLoad.iconMilitary,
			action = delegate
			{
				if (prisoner.Map.dangerWatcher.DangerRating != StoryDanger.None)
				{
					Messages.Message("cantSendWithDangerLevel".Translate(prisoner.Map.dangerWatcher.DangerRating.ToString()), MessageTypeDefOf.RejectInput);
					return;
				}

				List<FloatMenuOption> settlementList = Find.World.GetComponent<FactionFC>().settlements.Select(settlement => new FloatMenuOption("floatMenuOptionSendPrisonerToSettlement".Translate(settlement.name, settlement.settlementLevel, settlement.prisonerList.Count()), delegate
				{
					//disappear prisoner
					FactionColonies.sendPrisoner(prisoner, settlement);

					foreach (var bed in Find.Maps.Where(map => map.IsPlayerHome).SelectMany(map => map.listerBuildings.allBuildingsColonist).OfType<Building_Bed>().Where(bed => bed.OwnersForReading.Any(bedPawn => bedPawn == prisoner)))
					{
						bed.ForPrisoners = false;
						bed.ForPrisoners = true;
					}
				})).ToList();

				Find.WindowStack.Add(new FloatMenu(settlementList));
			}
		};

		public static void Postfix(ref Pawn __instance, ref IEnumerable<Gizmo> __result)
		{
			Pawn pawn = __instance;
			if (IsPrisonerAndCanBeSend(pawn)) return;

			__result = __result.Append(SendPrisonerAction(pawn));
		}
	}

	[HarmonyPatch(typeof(WorldObject), "GetGizmos")]
	class AddButtonsToNonEmpireObjects
	{
		private static readonly Dictionary<MilitaryJob, (string, string)> MilJobOptionStringsDic = new Dictionary<MilitaryJob, (string, string)> 
		{ 
			{ MilitaryJob.CaptureEnemySettlement, ("CaptureSettlement", "FCCaptureFloatMenuOption") },
			{ MilitaryJob.RaidEnemySettlement, ("RaidSettlement", "FCRaidFloatMenuOption") },
			{ MilitaryJob.EnslaveEnemySettlement, ("EnslavePopulation", "FCEnslaveFloatMenuOption") },
		};

		/// <summary>
		/// Checks if a <paramref name="settlement"/> has a currently usable military squad
		/// </summary>
		/// <param name="settlement"></param>
		/// <returns>true if usable, false otherwise</returns>
		private static bool SettlementHasUsableMilitary(SettlementFC settlement) => settlement.isMilitaryValid() && !settlement.militaryBusy;

		/// <summary>
		/// Takes a <paramref name="job"/> and generates a FloatMenuOptions using the strings in AddButtonsToNonEmpireObjects.MilJobOptionStringsDic
		/// </summary>
		/// <param name="factionFC"></param>
		/// <param name="faction"></param>
		/// <param name="tile"></param>
		/// <param name="job"></param>
		/// <returns>the generated FloatMenuOption</returns>
		private static FloatMenuOption NewOption(FactionFC factionFC, Faction faction, int tile, MilitaryJob job) => new FloatMenuOption((MilJobOptionStringsDic[job].Item1 ?? "FCUnsupportedMilJobError").Translate(), delegate
		{
			List<FloatMenuOption> settlementList = new List<FloatMenuOption>();

			foreach (SettlementFC settlement in factionFC.settlements)
			{
				if (SettlementHasUsableMilitary(settlement))
				{
					//if military is valid to use.

					settlementList.Add(new FloatMenuOption((MilJobOptionStringsDic[job].Item2 ?? "FCUnsupportedMilJobError").Translate(settlement.name, settlement.settlementMilitaryLevel), delegate
					{
						RelationsUtilFC.attackFaction(faction);
						settlement.SendMilitary(tile, Find.World.info.name, job, 60000, faction);
					}));
				}
			}

			if (settlementList.Count == 0) settlementList.Add(new FloatMenuOption("NoValidMilitaries".Translate(), null));

			Find.WindowStack.Add(new FloatMenu(settlementList));
		});

		/// <param name="factionFC"></param>
		/// <param name="faction"></param>
		/// <param name="tile"></param>
		/// <returns>A <c>Command_Action</c> that creates a <c>FloatMenu</c> displaying hostile actions a player can take against a settlement</returns>
		private static Command_Action HostileAction(FactionFC factionFC, Faction faction, int tile) => new Command_Action
		{
			defaultLabel = "AttackSettlement".Translate(faction.HasName ? faction.Name : "FCUnsupportedSettlementFaction".Translate().ToString()),
			defaultDesc = "",
			icon = TexLoad.iconMilitary,
			action = delegate
			{
				List<FloatMenuOption> list = new List<FloatMenuOption>();

				if (!factionFC.hasPolicy(FCPolicyDefOf.isolationist)) list.Add(NewOption(factionFC, faction, tile, MilitaryJob.CaptureEnemySettlement));
				list.Add(NewOption(factionFC, faction, tile, MilitaryJob.RaidEnemySettlement));
				if (factionFC.hasPolicy(FCPolicyDefOf.authoritarian) && faction.def.defName != "VFEI_Insect") list.Add(NewOption(factionFC, faction, tile, MilitaryJob.EnslaveEnemySettlement));

				Find.WindowStack.Add(new FloatMenu(list));
			}
		};

		/// <param name="factionFC"></param>
		/// <param name="faction"></param>
		/// <returns>A <c>Command_Action</c> that sends a diplomatic envoy if used</returns>
		private static Command_Action PeacefulAction(FactionFC factionFC, Faction faction) => new Command_Action
		{
			defaultLabel = "FCIncreaseRelations".Translate(),
			defaultDesc = "",
			icon = TexLoad.iconProsperity,
			action = delegate { factionFC.sendDiplomaticEnvoy(faction); }
		};

		/// <summary>
		/// Checks if a worldObject is not part of the player or their empire faction
		/// </summary>
		/// <param name="worldObject"></param>
		/// <returns>true if the faction linked isn't from the player or their empire faction, false otherwise</returns>
		private static bool HasValidFaction(WorldObject worldObject) => worldObject.Faction != FactionColonies.getPlayerColonyFaction() && worldObject.Faction != Find.FactionManager.OfPlayer;

		/// <summary>
		/// This Postfix adds Gizmos on settlements not owned by the player or their empire
		/// </summary>
		/// <param name="__instance"></param>
		/// <param name="__result"></param>
		public static void Postfix(ref WorldObject __instance, ref IEnumerable<Gizmo> __result)
		{
			if (__instance.def.defName != "Settlement") return;
			if (!HasValidFaction(__instance)) return;
			
			int tile = __instance.Tile;
			Faction faction = __instance.Faction;
			FactionFC factionFC = Find.World.GetComponent<FactionFC>();

			if (factionFC.hasPolicy(FCPolicyDefOf.pacifist))
			{
				__result = __result.AddItem(PeacefulAction(factionFC, faction));
				return;
			}

			__result = __result.AddItem(HostileAction(factionFC, faction, tile));
		}
	}
}
