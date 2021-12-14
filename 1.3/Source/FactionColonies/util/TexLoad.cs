using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    [StaticConstructorOnStartup]
    public static class TexLoad
    {
        static TexLoad()
        {
            var icons = ContentFinder<Texture2D>.GetAllInFolder("FactionIcons");
            factionIcons = icons.ToList();
            if (factionIcons.NullOrEmpty())
            {
                Log.Error("Empire - No faction icons found, will probably result in Empire not working properly.");
            }
        }

        public static readonly Texture2D iconTest100 = ContentFinder<Texture2D>.Get("GUI/100x");
        public static readonly Texture2D questionmark = ContentFinder<Texture2D>.Get("GUI/questionmark");
        public static readonly Texture2D buildingLocked = ContentFinder<Texture2D>.Get("GUI/LockedBuildingSlot");
        public static readonly Texture2D refreshIcon = ContentFinder<Texture2D>.Get("GUI/Buttons/Refresh");

        // Tynan made the definitions in Verse internal so we gotta get them here
        public static readonly Texture2D deleteX = ContentFinder<Texture2D>.Get("UI/Buttons/Delete");
        
        //test icons
        public static readonly Texture2D iconHappiness = ContentFinder<Texture2D>.Get("GUI/Happiness");
        public static readonly Texture2D iconLoyalty = ContentFinder<Texture2D>.Get("GUI/Loyalty");
        public static readonly Texture2D iconUnrest = ContentFinder<Texture2D>.Get("GUI/Unrest");
        public static readonly Texture2D iconProsperity = ContentFinder<Texture2D>.Get("GUI/Prosperity");
        public static readonly Texture2D iconMilitary = ContentFinder<Texture2D>.Get("GUI/MilitaryLevel");
        public static readonly Texture2D iconCustomize = ContentFinder<Texture2D>.Get("GUI/customizebutton");
        
        public static readonly Texture2D iconTrade = ContentFinder<Texture2D>.Get("UI/Commands/Trade");


        //Trait Icons
        public static readonly Texture2D traitAuthoritarianLight = ContentFinder<Texture2D>.Get("GUI/MainTraits/Authoritarian");
        public static readonly Texture2D traitAuthoritarianDark = ContentFinder<Texture2D>.Get("GUI/MainTraits/AuthoritarianDark");
        public static readonly Texture2D traitEgalitarianLight = ContentFinder<Texture2D>.Get("GUI/MainTraits/Egalitarian");
        public static readonly Texture2D traitEgalitarianDark = ContentFinder<Texture2D>.Get("GUI/MainTraits/EgalitarianDark");
        public static readonly Texture2D traitExpansionistLight = ContentFinder<Texture2D>.Get("GUI/MainTraits/Expansionist");
        public static readonly Texture2D traitExpansionistDark = ContentFinder<Texture2D>.Get("GUI/MainTraits/ExpansionistDark");
        public static readonly Texture2D traitFeudalLight = ContentFinder<Texture2D>.Get("GUI/MainTraits/Feudal");
        public static readonly Texture2D traitFeudalDark = ContentFinder<Texture2D>.Get("GUI/MainTraits/FeudalDark");
        public static readonly Texture2D traitIsolationistLight = ContentFinder<Texture2D>.Get("GUI/MainTraits/Isolationist");
        public static readonly Texture2D traitIsolationistDark = ContentFinder<Texture2D>.Get("GUI/MainTraits/IsolationistDark");
        public static readonly Texture2D traitMilitaristicLight = ContentFinder<Texture2D>.Get("GUI/MainTraits/Militaristic");
        public static readonly Texture2D traitMilitaristicDark = ContentFinder<Texture2D>.Get("GUI/MainTraits/MilitaristicDark");
        public static readonly Texture2D traitPacifistLight = ContentFinder<Texture2D>.Get("GUI/MainTraits/Pacifist");
        public static readonly Texture2D traitPacifistDark = ContentFinder<Texture2D>.Get("GUI/MainTraits/PacifistDark");
        public static readonly Texture2D traitTechnocraticLight = ContentFinder<Texture2D>.Get("GUI/MainTraits/Technocratic");
        public static readonly Texture2D traitTechnocraticDark = ContentFinder<Texture2D>.Get("GUI/MainTraits/TechnocraticDark");

        //UnitCustomization
        public static readonly Texture2D unitCircle = ContentFinder<Texture2D>.Get("GUI/unitCircle");

        //test production icons
        //public static readonly Texture2D iconProdFood = ContentFinder<Texture2D>.Get("GUI/productionfood");
        //public static readonly Texture2D iconProdWeapons = ContentFinder<Texture2D>.Get("GUI/productionweapons");
        //public static readonly Texture2D iconProdApparel = ContentFinder<Texture2D>.Get("GUI/productionapparel");
        //public static readonly Texture2D iconProdAnimals = ContentFinder<Texture2D>.Get("GUI/productionanimals");
        //public static readonly Texture2D iconProdLogging = ContentFinder<Texture2D>.Get("GUI/productionlogging");
        //public static readonly Texture2D iconProdMining = ContentFinder<Texture2D>.Get("GUI/productionmining");



        public static readonly List<KeyValuePair<string, Texture2D>> textures = new List<KeyValuePair<string, Texture2D>>() 
        { 
            new KeyValuePair<string, Texture2D>("food", ContentFinder<Texture2D>.Get("GUI/ProductionFood")), 
            new KeyValuePair<string, Texture2D>("weapons", ContentFinder<Texture2D>.Get("GUI/ProductionWeapons")), 
            new KeyValuePair<string, Texture2D>("apparel", ContentFinder<Texture2D>.Get("GUI/ProductionApparel")), 
            new KeyValuePair<string, Texture2D>("animals", ContentFinder<Texture2D>.Get("GUI/ProductionAnimals")), 
            new KeyValuePair<string, Texture2D>("logging", ContentFinder<Texture2D>.Get("GUI/ProductionLogging")), 
            new KeyValuePair<string, Texture2D>("mining", ContentFinder<Texture2D>.Get("GUI/ProductionMining")),
             new KeyValuePair<string, Texture2D>("medicine", ContentFinder<Texture2D>.Get("GUI/ProductionMedicine")),
              new KeyValuePair<string, Texture2D>("power", ContentFinder<Texture2D>.Get("GUI/ProductionPower")),
               new KeyValuePair<string, Texture2D>("research", ContentFinder<Texture2D>.Get("GUI/ProductionResearch"))
        };

        public static List<Texture2D> factionIcons = new List<Texture2D>();
    }
}
