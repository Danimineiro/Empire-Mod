using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Media;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Object = System.Object;

namespace FactionColonies
{
    public abstract class Dialog_ManageExportsFC : Window
    {
        #region UIVars
        
        static float ElementPadding = 5.0f;
        static float ElementHeight = 35f;
        
        static float ElementNameWidth = 200f;
        static float ElementNameHeight = 35f;
        
        static float ElementImportWidth = 80f;
        static float ElementImportHeight = 35f;
        
        static float ElementDeleteWidth = 35f;
        static float ElementDeleteHeight = 35f;

        #endregion

        protected Vector2 scrollPos;
        public override Vector2 InitialSize => new Vector2(620f, 700f);

        public Dialog_ManageExportsFC()
        {
            this.doCloseX = true;
            this.draggable = true;
            this.resizeable = true;
            this.doCloseButton = true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Rect rect = inRect;
            Rect view = inRect.AtZero();
            view.height = this.GetAll().Count() * (ElementHeight + ElementPadding);

            Widgets.BeginScrollView(rect, ref scrollPos, view);
            DrawElements(rect);
            Widgets.EndScrollView();
        }

        protected virtual void DrawElements(Rect inRect)
        {
            Text.Anchor = TextAnchor.MiddleLeft;
            Rect elemRect = new Rect(inRect.x, inRect.y, inRect.width, ElementHeight);
            
            Rect nameRect = new Rect(inRect.x, inRect.y, ElementNameWidth, ElementNameHeight);
            
            Rect deleteRect = new Rect(inRect.width - inRect.x - ElementDeleteWidth,
                inRect.y, ElementDeleteWidth, ElementDeleteHeight);
            Rect importRect = new Rect(deleteRect.x - ElementImportWidth,
                inRect.y, ElementImportWidth, ElementImportHeight);
            
            bool alternate = false;
            foreach (string name in GetAll())
            {
                if(alternate)
                    Widgets.DrawAltRect(elemRect);

                Widgets.Label(nameRect, name);
                
                if (Widgets.ButtonText(importRect, "FCImport".Translate()))
                {
                    OnImport(name);
                }

                if (Widgets.ButtonImage(deleteRect, TexLoad.deleteX))
                {
                    OnDelete(name);
                }

                elemRect.y += ElementHeight + ElementPadding;
                nameRect.y += ElementHeight + ElementPadding;
                importRect.y += ElementHeight + ElementPadding;
                deleteRect.y += ElementHeight + ElementPadding;
                alternate ^= true;
            }
            Text.Anchor = TextAnchor.UpperLeft;
        }

        protected abstract void OnDelete(string name);
        protected abstract void OnImport(string name);
        protected abstract IEnumerable<string> GetAll();
    }

    public class Dialog_ManageSquadExportsFC : Dialog_ManageExportsFC
    {
        private List<SavedSquadFC> squads;
        public Dialog_ManageSquadExportsFC(List<SavedSquadFC> elements)
        {
            squads = elements;
        }

        protected override void OnDelete(string name)
        {
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                "ConfirmDelete".Translate((NamedArgument) name), () => 
            {
                FactionColoniesMilitary.RemoveSquad(name);
                this.squads.RemoveAll(squads => squads.name == name);
                Messages.Message("FCDeleted".Translate((NamedArgument) name), MessageTypeDefOf.PositiveEvent);
            }));
        }

        protected override void OnImport(string name)
        {
            FactionFC fc = Find.World.GetComponent<FactionFC>();
            MilSquadFC squad = FactionColoniesMilitary.GetSquad(name).Import();
            
            MilitaryCustomizationWindowFc mil = (MilitaryCustomizationWindowFc)Find.WindowStack.Windows.FirstOrFallback(
                window => window.GetType() == typeof(MilitaryCustomizationWindowFc));
            
            mil.SetActive(squad);
            
            MessageTypeDefOf.PositiveEvent.sound.PlayOneShotOnCamera();
            Messages.Message("FCImported".Translate((NamedArgument) name), MessageTypeDefOf.PositiveEvent);
            this.Close();
        }
        protected override IEnumerable<string> GetAll() => squads.Select(squad => squad.name);
    }
    public class Dialog_ManageUnitExportsFC : Dialog_ManageExportsFC
    {
        private List<SavedUnitFC> units;
        public Dialog_ManageUnitExportsFC(List<SavedUnitFC> elements)
        {
            units = elements;
        }

        protected override void OnDelete(string name)
        {
            Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
                "ConfirmDelete".Translate((NamedArgument) name), () => 
            {
                FactionColoniesMilitary.RemoveUnit(name);
                this.units.RemoveAll(unit => unit.name == name);
                Messages.Message("FCDeleted".Translate((NamedArgument) name), MessageTypeDefOf.PositiveEvent);
            }));
        }

        protected override void OnImport(string name)
        {
            FactionFC fc = Find.World.GetComponent<FactionFC>();
            MilUnitFC unit = FactionColoniesMilitary.GetUnit(name).Import();
            
            MilitaryCustomizationWindowFc mil = (MilitaryCustomizationWindowFc)Find.WindowStack.Windows.FirstOrFallback(
                window => window.GetType() == typeof(MilitaryCustomizationWindowFc));
            
            mil.SetActive(unit);
            
            MessageTypeDefOf.PositiveEvent.sound.PlayOneShotOnCamera();
            Messages.Message("FCImported".Translate((NamedArgument) name), MessageTypeDefOf.PositiveEvent);
            this.Close();
        }
        protected override IEnumerable<string> GetAll() => units.Select(unit => unit.name);
    }
}