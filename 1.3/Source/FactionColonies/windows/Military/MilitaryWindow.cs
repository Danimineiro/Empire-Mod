using System;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    public abstract class MilitaryWindow
    {
        public string selectedText;
        public float scroll;

        public abstract void DrawTab(Rect rect);

        public virtual void Select(IExposable selecting)
        {
            throw new ApplicationException("Trying to select " + selecting + " on window " + this);
        }
        
        public void scrollWindow(float num, float maxScroll)
        {
            if (scroll - num * 5 < -1 * maxScroll)
            {
                scroll = -1f * maxScroll;
            }
            else if (scroll - num * 5 > 0)
            {
                scroll = 0;
            }
            else
            {
                scroll -= (int) Event.current.delta.y * 5;
            }

            Event.current.Use();

            //Log.Message(scroll.ToString());
        }
    }
}