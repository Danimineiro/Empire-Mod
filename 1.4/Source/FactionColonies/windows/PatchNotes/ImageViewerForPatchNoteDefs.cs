using System;
using FactionColonies.util;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace FactionColonies
{
	class ImageViewerForPatchNoteDefs : Window
	{
		private readonly Rect descriptionRect;
		private readonly PatchNoteDef patchNoteDef;
		private readonly float commonMargin = 5f;

		private Rect imageRect = new Rect(0, 0, UI.screenWidth, UI.screenHeight);
		private Vector2 scrollPos = new Vector2();
		private int displayedImage;

		public new const float StandardMargin = 0f;

		public override Vector2 InitialSize => new Vector2(UI.screenWidth, UI.screenHeight);

		public ImageViewerForPatchNoteDefs(PatchNoteDef patchNoteDef, int displayedImage)
		{
			this.patchNoteDef = patchNoteDef;
			this.displayedImage = displayedImage;
			doWindowBackground = false;
			closeOnClickedOutside = true;

			Vector2 screenCenter = imageRect.center;
			imageRect = new Rect(0f, 0f, 1374f, 776f)
			{
				center = screenCenter
			};

			imageRect.position -= new Vector2(0f, 50f);
			descriptionRect = new Rect(imageRect.x, imageRect.y + imageRect.height, imageRect.width, UI.screenHeight - (imageRect.y + imageRect.height) - 100f);
		}

		public override void DoWindowContents(Rect inRect)
		{
			Color prevColor = GUI.color;
			GameFont prevFont = Text.Font;
			TextAnchor prevAnchor = Text.Anchor;

			GUI.color = Color.black;
			Widgets.DrawBoxSolid(imageRect, GUI.color);
			Widgets.DrawBox(descriptionRect, 4);
			GUI.color = prevColor;

			Widgets.DrawWindowBackground(descriptionRect.ContractedBy(4f));
			Widgets.DrawLightHighlight(descriptionRect.ContractedBy(4f + commonMargin));

			Text.Font = GameFont.Small;
			Rect contracted = descriptionRect.ContractedBy(4f + commonMargin * 2f);
			Rect nextButtonRect = contracted.RightPartPixels(100f).TopPartPixels((contracted.height - commonMargin) / 2f);
			Rect lastButtonRect = nextButtonRect.CopyAndShift(0f, nextButtonRect.height + commonMargin);

			Widgets.LabelScrollable(contracted.LeftPartPixels(contracted.width - 100f - commonMargin), patchNoteDef.PatchNoteImageDescriptions[displayedImage], ref scrollPos);

			Text.Font = GameFont.Medium;
			Text.Anchor = TextAnchor.MiddleCenter;
			if (displayedImage < patchNoteDef.PatchNoteImages.Count - 1)
			{
				Widgets.DrawBoxSolid(nextButtonRect, Color.black);
				Widgets.Label(nextButtonRect, ">");

				if (Widgets.ButtonInvisible(nextButtonRect))
				{
					displayedImage++;
					SoundDefOf.Click.PlayOneShotOnCamera();
				}
			}

			if (displayedImage > 0)
			{
				Widgets.DrawBoxSolid(lastButtonRect, Color.black);
				Widgets.Label(lastButtonRect, "<");

				if (Widgets.ButtonInvisible(lastButtonRect))
				{
					displayedImage--;
					SoundDefOf.Click.PlayOneShotOnCamera();
				}
			}

			//Make images look better if they are 16/9
			Texture2D tex = patchNoteDef.PatchNoteImages[displayedImage];
			if (GenMath.RoundedHundredth((float) tex.width / tex.height / (16f / 9f)) == 1f)
			{
				GUI.DrawTexture(imageRect.ContractedBy(4f), tex);
			}
            else
            {
				GUI.DrawTexture(imageRect.ContractedBy(4f), tex, ScaleMode.ScaleToFit);
			}


			Text.Anchor = prevAnchor;
			Text.Font = prevFont;
			if (Widgets.ButtonInvisible(windowRect) && !Mouse.IsOver(imageRect) && !Mouse.IsOver(descriptionRect)) Close();
		}
	}
}
