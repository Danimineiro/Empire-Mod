using UnityEngine;

namespace FactionColonies.util
{
	static class RectExtensions
	{
		public static Rect CopyAndShift(this Rect rect, Vector2 vector2)
		{
			return CopyAndShift(rect, vector2.x, vector2.y);
		}

		public static Rect CopyAndShift(this Rect rect, float x, float y)
		{
			Rect newRect = new Rect(rect);
			newRect.x += x;
			newRect.y += y;

			return newRect;
		}
	}
}
