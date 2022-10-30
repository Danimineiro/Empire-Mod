using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace FactionColonies
{
	class Searchable_FloatMenu : FloatMenu
	{
		private readonly List<FloatMenuOption> filteredOptions;
		private readonly char ignoreBeforeChar;
		private readonly bool useIgnoreBeforeChar;
		private readonly bool canBeForcedOpen;

		private string searchTerm = "";
		private Color baseColor = Color.white;
		private int noRemovalOnTick = 0;

		private readonly Vector2 SearchBarOffset = new Vector2(25f, -27f);
		private bool canClose = true;
		private bool stayOpenOptionClicked = false;

		/// <summary>
		/// Creates a floatmenuoption that allows the player to lock the Floatmenu open
		/// </summary>
        private FloatMenuOption ForceOpenOption
        {
            get
            {
				FloatMenuOption option = new FloatMenuOption("FCSMFForceOpen".Translate(), delegate
				{
					canClose = !canClose;
					stayOpenOptionClicked = true;
					SoundDefOf.Click.PlayOneShotOnCamera();
				}, canClose ? Widgets.CheckboxOffTex : Widgets.CheckboxOnTex, Color.white);

				option.SetSizeMode(SizeMode);

				return option;
            }
        }

		/// <summary>
		/// Decides if the window should close
		/// </summary>
        public bool CanBeClosed
        {
            get
            {
				if (noRemovalOnTick == Find.TickManager.TicksGame) return false;

				if (stayOpenOptionClicked)
                {
					stayOpenOptionClicked = false;
					filteredOptions[0] = ForceOpenOption;
					noRemovalOnTick = Find.TickManager.TicksGame;
					return false;
				}

                return canClose;
            }
        }

		/// <summary>
		/// Calculates the required Width dynamically
		/// </summary>
        private float ColumnWidth
		{
			get
			{
				float num = 70f;
				for (int i = 0; i < options.Count; i++)
				{
					float requiredWidth = options[i].RequiredWidth;
					if (requiredWidth >= 300f)
					{
						return 300f;
					}
					if (requiredWidth > num)
					{
						num = requiredWidth;
					}
				}
				return Mathf.Round(num);
			}
		}

		/// <summary>
		///	Like a normal <c>FloatMenu</c>, but searchable!
		/// </summary>
		/// <param name="options"></param>
		/// <param name="ignoreBeforeChar"></param>
		/// <param name="useIgnoreBeforeChar"></param>
		public Searchable_FloatMenu(List<FloatMenuOption> options, bool canBeForcedOpen = false, bool useIgnoreBeforeChar = true, char ignoreBeforeChar = '-') : base(FakeAddStayOpenOption(options, canBeForcedOpen))
		{
			this.useIgnoreBeforeChar = useIgnoreBeforeChar;
			this.ignoreBeforeChar = ignoreBeforeChar;
			this.canBeForcedOpen = canBeForcedOpen;

			//options = AddForceOpenOption(options);
			filteredOptions = options.ListFullCopy();

			options.Clear();
		}

		/// <summary>
		/// Adds the ForceOpenOption to the FloatMenu <paramref name="options"/>
		/// </summary>
		/// <param name="options"></param>
		/// <returns></returns>
		private List<FloatMenuOption> AddForceOpenOption(List<FloatMenuOption> options)
        {
			List<FloatMenuOption> returnOptions = new List<FloatMenuOption>();

			if (canBeForcedOpen) returnOptions.Add(ForceOpenOption);

			returnOptions.AddRange(options);
			return returnOptions;
        }

		/// <summary>
		/// Adds a fake option to the <paramref name="options"/>, only need this for the base constructor
		/// </summary>
		/// <param name="options"></param>
		/// <returns></returns>
		private static List<FloatMenuOption> FakeAddStayOpenOption(List<FloatMenuOption> options, bool canBeForcedOpen)
		{
			List<FloatMenuOption> returnOptions = new List<FloatMenuOption>();
			if (canBeForcedOpen) returnOptions.Add(new FloatMenuOption("", null));

			returnOptions.AddRange(options);
			return returnOptions;
		}

		/// <summary>
		/// Adds the search bar and search bar functionality
		/// </summary>
		public override void ExtraOnGUI()
		{
			base.ExtraOnGUI();
			Vector2 vector = new Vector2(windowRect.x, windowRect.y);
			Text.Font = GameFont.Small;
			Rect searchRect = new Rect(vector.x - SearchBarOffset.x, vector.y + SearchBarOffset.y, ColumnWidth + SearchBarOffset.x, 27f);

			if (stayOpenOptionClicked)
            {
				stayOpenOptionClicked = false;
				options[0] = ForceOpenOption;
            }

			Find.WindowStack.ImmediateWindow(6830963, searchRect, WindowLayer.Super, delegate
			{
				GUI.color = baseColor;
				Text.Font = GameFont.Small;
				Text.Anchor = TextAnchor.MiddleLeft;

				//Rect position = searchRect.AtZero();
				Rect bgRect = searchRect.AtZero();

				bgRect.width -= SearchBarOffset.x;
				bgRect.x += SearchBarOffset.x;

				Rect searchBarRect = bgRect.ContractedBy(2f);
				Rect labelRect = bgRect.ContractedBy(2f);

				//position.width = 150f;
				//GUI.DrawTexture(position, TexUI.TextBGBlack);

				Widgets.DrawWindowBackground(bgRect);

				searchTerm = Widgets.TextField(searchBarRect, searchTerm);

				options.Clear();
				if (searchTerm != "")
				{
					vanishIfMouseDistant = false;
					if (useIgnoreBeforeChar)
					{
						options.AddRange(filteredOptions.Where(option => option.Label.ToLower().Substring(0, (option.Label.IndexOf(ignoreBeforeChar) < 0) ? option.Label.Length : option.Label.IndexOf(ignoreBeforeChar) - 1).Contains(searchTerm.ToLower())));
					}
                    else
                    {
						options.AddRange(filteredOptions.Where(option => option.Label.ToLower().Contains(searchTerm.ToLower())));
					}
				}
				else
				{
					Widgets.Label(labelRect, $" {"FloatMenuSearchable".Translate()}");
					options = filteredOptions.ToList();
					vanishIfMouseDistant = true && CanBeClosed;
				}

				Text.Anchor = TextAnchor.UpperLeft;
			}, false, false, 0f);
		}
    }
}
