using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FactionColonies.util;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace FactionColonies
{
	public static class DebugActionsMisc
	{
		[DebugAction("Mods", "Display Empire patch notes", false, false, allowedGameStates = AllowedGameStates.Entry)]
		public static void PatchNotesDisplayWindow() => Find.WindowStack.Add(new PatchNotesDisplayWindow());
	}

	class PatchNotesDisplayWindow : Window
	{
		public override Vector2 InitialSize => new Vector2(1200f + (StandardMargin * 2), 595f + (StandardMargin * 2));

		private static readonly Rect PatchNotesWindowTitleRect = new Rect(5f, 0f, 1190f, 30f);
		private static readonly Rect HorizontalLineRect = new Rect(5f, 30f, 1190f, 15f);
		private static readonly Rect PatchNotesImageArea = new Rect(675f, 45f, 520f, 545f);
		private static readonly Rect PatchNotesScrollArea = new Rect(5f, 45f, 655f, 545f);
		private static readonly Rect PatchNotesImageRect = new Rect(685f, 55f, 500f, 280f);
		private static readonly Rect PatchNotesImageToolTipRect = new Rect(735f, 55f, 400f, 280f);
		private static readonly Rect LastImageButtonRect = new Rect(685f, 55f, 50f, 280f);
		private static readonly Rect NextImageButtonRect = new Rect(1135f, 55f, 50f, 280f);
		private static readonly Rect ImageDescRect = new Rect(685f, 345f, 500f, 161f);
		private static readonly Rect LinkButtonsRect = new Rect(685f, 516f, 500f, 64f);
		private static readonly Rect baseLinkButtonRect = new Rect(0f, 0f, 64f, 64f);
		private static readonly Rect VerticalDeviderRect = new Rect(660f, 30f, 15f, 545f);

		private static readonly float commonMargin = 5f;
		private static readonly GameFont prevFont = Text.Font;
		private static readonly TextAnchor prevAnchor = Text.Anchor;
		private static readonly Color prevColor = GUI.color;
		private static readonly List<PatchNoteDef> patchNoteDefs = DefDatabase<PatchNoteDef>.AllDefsListForReading.ListFullCopy();

		private readonly string title = "FCPatchNotesWindowTitle".Translate();

		//For the patch note area
		private bool firstRun = true;
		private bool fixDone = false;
		private bool shouldRefreshHeight = true;
		private int openDef = 0;
		private float scrollViewHeight = 0f;
		private float extraHeightRequired = 0f;
		private Vector2 patchNoteScrollPos = new Vector2();
		private Rect patchNotesScrollViewRect;
		private Rect basePatchNoteRect;

		//For the image area
		private int displayedImage = -1;
		private Vector2 imageDescScrollPos = new Vector2(); 
		Vector2 baseLinkButtonOffset = new Vector2(baseLinkButtonRect.width + (LinkButtonsRect.width + baseLinkButtonRect.width) / baseLinkButtonRect.width, 0f);
		private Color orange = Color.Lerp(Color.yellow, Color.red, 0.5f);
		private Rect toolTipRect;

		/// <summary>
		/// Constructs a PatchNotesDisplayWindow class and saves the current Text.Font, Text.Anchor and GUI.color
		/// </summary>
		public PatchNotesDisplayWindow()
		{
			patchNotesScrollViewRect = PatchNotesScrollArea.LeftPartPixels(PatchNotesScrollArea.width - 17f);
			basePatchNoteRect = patchNotesScrollViewRect.TopPartPixels(45f);
			CalculateScrollViewSize();

			patchNoteDefs.SortBy((def) => def.ReleaseDate, (def) => def.ToOldEmpireVersion);
			patchNoteDefs.Reverse();
		}

		/// <summary>
		/// Constructs a PatchNotesDisplayWindow class and saves the current Text.Font, Text.Anchor and GUI.color and gives it the given <paramref name="title"/>
		/// </summary>
		/// <param name="title"></param>
		public PatchNotesDisplayWindow(string title) : this() => this.title = title;

		/// <summary>
		/// This function draws the contents of this window class. The Rects were laid out using NesGui
		/// </summary>
		/// <param name="inRect"></param>
		public override void DoWindowContents(Rect inRect)
		{
			FixScrollingBug();
			CalculateScrollViewSize();
			DrawTitle();
			DrawDividers();
			DrawPatchNotes();
			DrawImageContent();
		}

		/// <summary>
		/// Fixes a bug where a note being opened in the first frame causes the scrolling bars to not appear as long as it isn't closed
		/// </summary>
		private void FixScrollingBug()
        {
			if (fixDone) return;

			if (!firstRun)
			{
				shouldRefreshHeight = true;
				fixDone = true;
			}
			else
			{
				firstRun = false;
			}
		}

		/// <summary>
		/// Draws the Image related content of any selected Def or a text instructing the user to select a def
		/// </summary>
		private void DrawImageContent()
		{
			Widgets.DrawBox(PatchNotesImageArea);
			Widgets.DrawLightHighlight(ImageDescRect);

			if (openDef == -1)
			{
				DrawImageContentMissing("FCSelectPatchNotes".Translate(), orange);
			} 
			else
            {
				DrawImageContentOfDef();
				DrawLinkButtons();
			}
		}

		/// <summary>
		/// Draws buttons that open links defined in the defs
		/// </summary>
		public void DrawLinkButtons()
        {
			PatchNoteDef def = patchNoteDefs[openDef];

			GUI.BeginGroup(LinkButtonsRect);

			for (int i = 0; i < def.Links.Count; i++)
            {
				Rect tempButtonRect = baseLinkButtonRect.CopyAndShift(baseLinkButtonOffset * i);
				TooltipHandler.TipRegion(tempButtonRect, def.LinkButtonToolTips[i]);
				if (Widgets.ButtonImage(tempButtonRect, def.LinkButtonImages[i]))
                {
					SteamUtility.OpenUrl(def.Links[i]);
					SoundDefOf.Click.PlayOneShotOnCamera();
				}
            }

			GUI.EndGroup();
        }

		/// <summary>
		/// Displays a tooltip instructing the user to click to enlargen an image.
		/// Automatically scales the area in which the tool tip is displayed based on buttons being displayed.
		/// </summary>
		private void MakeToolTip()
		{
			toolTipRect = new Rect(PatchNotesImageToolTipRect);

			if (displayedImage == 0)
            {
				toolTipRect.x -= LastImageButtonRect.width;
				toolTipRect.width += LastImageButtonRect.width;
            }

			if (displayedImage == patchNoteDefs[openDef].PatchNoteImages.Count - 1)
			{
				toolTipRect.width += NextImageButtonRect.width;
			}

			TooltipHandler.TipRegion(toolTipRect, "FCPatchNotesImageZoomTooltip".Translate());
		}

		/// <summary>
		/// retrieves the list of Images from the openDef and displays that image
		/// Also draws the controls
		/// </summary>
		private void DrawImageContentOfDef()
        {
			PatchNoteDef def = patchNoteDefs[openDef];
			List<Texture2D> patchNoteImages = def.PatchNoteImages;

			if (patchNoteImages.NullOrEmpty())
			{
				DrawImageContentMissing("FCPatchNotesImagesMissing".Translate(), orange);
			}
			else
			{
				displayedImage = displayedImage == -1 ? 0 : displayedImage;
				Texture2D tex = patchNoteImages[displayedImage];
				GUI.DrawTexture(PatchNotesImageRect, tex, ScaleMode.ScaleToFit);

				DrawImageSelectors(patchNoteImages.Count - 1);

				Text.Font = GameFont.Small;
				Widgets.LabelScrollable(ImageDescRect.ContractedBy(commonMargin), def.PatchNoteImageDescriptions[displayedImage], ref imageDescScrollPos);
				MakeToolTip();
				if (Widgets.ButtonInvisible(toolTipRect)) Find.WindowStack.Add(new ImageViewerForPatchNoteDefs(patchNoteDefs[openDef], displayedImage));
			}

			ResetTextAndColor();
        }

		/// <summary>
		/// Draws two buttons labeled that change which image is displayed. <paramref name="max"/> is the last index of images displayable
		/// </summary>
		/// <param name="max"></param>
		private void DrawImageSelectors(int max)
		{
			Text.Anchor = TextAnchor.MiddleCenter;
			Text.Font = GameFont.Medium;

			DrawImageSelector(NextImageButtonRect, ">", () => displayedImage < max, () => displayedImage++);
			DrawImageSelector(LastImageButtonRect, "<", () => displayedImage  >  0, () => displayedImage--);

			ResetTextAndColor();
		}

		/// <summary>
		/// Draws a grey button into a given <paramref name="buttonRect"/> with the <paramref name="buttonLabel"/> 
		/// when <paramref name="predicate"/> is true that executes <paramref name="action"/> 
		/// when pressed
		/// </summary>
		/// <param name="buttonRect"></param>
		/// <param name="buttonLabel"></param>
		/// <param name="predicate"></param>
		/// <param name="action"></param>
		private void DrawImageSelector(Rect buttonRect, string buttonLabel, Func<bool> predicate, Action action)
		{
			GUI.color = prevColor;

			if (predicate())
			{
				Color guiColor = Color.black;
				guiColor.a = 0.8f;

				if (!Mouse.IsOver(buttonRect)) guiColor.a = 0.3f;
				Widgets.DrawBoxSolid(buttonRect, guiColor);

				guiColor = prevColor;
				if (!Mouse.IsOver(buttonRect)) guiColor.a = 0.3f;

				GUI.color = guiColor;

				if (Widgets.ButtonInvisible(buttonRect))
				{
					action();
					SoundDefOf.Click.PlayOneShotOnCamera();
					imageDescScrollPos = new Vector2();
				}

				Widgets.Label(buttonRect, buttonLabel);
			}
		}

		/// <summary>
		/// Displays a warning in place of the image notifying the user of the <paramref name="reason"/> why no image can be displayed
		/// </summary>
		/// <param name="reason"></param>
		private void DrawImageContentMissing(string reason, Color reasonColor)
        {
			Text.Anchor = TextAnchor.MiddleCenter;
			Text.Font = GameFont.Medium;
			GUI.color = reasonColor;

			Widgets.DrawBoxSolid(PatchNotesImageRect, Color.black);
			Widgets.Label(PatchNotesImageRect, reason);

			displayedImage = -1;
			ResetTextAndColor();
        }

		/// <summary>
		/// Draws the buttons used to select which patch notes to display and the patch notes
		/// </summary>
		private void DrawPatchNotes()
		{
			Widgets.BeginScrollView(PatchNotesScrollArea, ref patchNoteScrollPos, patchNotesScrollViewRect);

			for (int i = 0; i < patchNoteDefs.Count; i++)
			{
				Rect curPatchNoteRect = basePatchNoteRect.CopyAndShift(0f, i * (basePatchNoteRect.height + commonMargin) + (i > openDef ? extraHeightRequired : 0f));
				Rect expandCollapseIconRect = curPatchNoteRect.LeftPartPixels(curPatchNoteRect.height);

				if (i % 2 == 0)
					Widgets.DrawHighlight(curPatchNoteRect);
				else
					Widgets.DrawLightHighlight(curPatchNoteRect);

				Text.Font = GameFont.Medium;
				Text.Anchor = TextAnchor.MiddleLeft;

				Widgets.DrawBox(curPatchNoteRect);
				Widgets.Label(curPatchNoteRect.RightPartPixels(curPatchNoteRect.width - expandCollapseIconRect.width - commonMargin), patchNoteDefs[i].Title);
				Widgets.DrawTextureFitted(expandCollapseIconRect.ContractedBy(11f), i == openDef ? TexButton.Collapse : TexButton.Reveal, 1f);
				if (Widgets.ButtonInvisible(curPatchNoteRect))
				{
					if (i == openDef)
					{
						openDef = -1;
						SoundDefOf.TabClose.PlayOneShotOnCamera();
                    }
                    else
                    {
						openDef = i;
						SoundDefOf.TabOpen.PlayOneShotOnCamera();
                    }
					shouldRefreshHeight = true;
				}
			}

			if (openDef != -1 && patchNoteDefs.Count > openDef) 
			{
				Text.Font = GameFont.Small;

				PatchNoteDef def = patchNoteDefs[openDef];
				string patchNotesString = def.CompletePatchNotesString;
			
				Rect temp = new Rect(basePatchNoteRect.x + commonMargin * 2f, basePatchNoteRect.y + basePatchNoteRect.height + openDef * (basePatchNoteRect.height + commonMargin), basePatchNoteRect.width - commonMargin * 4f, 100f);

				Widgets.LabelCacheHeight(ref temp, patchNotesString);
				extraHeightRequired = temp.height;
            }
            else
            {
				extraHeightRequired = 0;
			}

			patchNotesScrollViewRect.height = scrollViewHeight;
			ResetTextAndColor();
			Widgets.EndScrollView();
		}

		/// <summary>
		/// Draws the lines that separate the title, patch notes and patch note images
		/// </summary>
		private void DrawDividers()
		{
			GUI.color = Color.gray;
			Widgets.DrawLineHorizontal(HorizontalLineRect.x, HorizontalLineRect.center.y - 1f, HorizontalLineRect.width);
			Widgets.DrawLineVertical(VerticalDeviderRect.center.x, HorizontalLineRect.center.y - 1f, VerticalDeviderRect.height + 45f - HorizontalLineRect.center.y);

			ResetTextAndColor();
		}

		/// <summary>
		/// Draws this windows title and the close button
		/// </summary>
		private void DrawTitle()
		{
			Text.Font = GameFont.Medium;
			Text.Anchor = TextAnchor.UpperLeft;

			Widgets.Label(PatchNotesWindowTitleRect, title);
			if (Widgets.ButtonImage(PatchNotesWindowTitleRect.RightPartPixels(PatchNotesWindowTitleRect.height).ContractedBy(6f), TexButton.CloseXSmall)) Close();

			ResetTextAndColor();
		}

		/// <summary>
		/// Resets the Text.Font, Text.Anchor and GUI.color setting
		/// </summary>
		private void ResetTextAndColor()
		{
			Text.Font = prevFont;
			Text.Anchor = prevAnchor;
			GUI.color = prevColor;
		}

		/// <summary>
		/// Calculates the size of the PatchNotesScrollArea Rect
		/// </summary>
		private void CalculateScrollViewSize()
		{
			if (!shouldRefreshHeight) return;
			shouldRefreshHeight = false;

			scrollViewHeight = (basePatchNoteRect.height + commonMargin) * patchNoteDefs.Count + extraHeightRequired - commonMargin;

			//for some reason you have to make the scroll rect extend higher than the height you want before setting it to the height you want
			//I don't really know why but this fixes all the issues I have
			patchNotesScrollViewRect.height = float.MaxValue;
			basePatchNoteRect = patchNotesScrollViewRect.TopPartPixels(45f);
		}
	}
}
