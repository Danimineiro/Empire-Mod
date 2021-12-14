using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FactionColonies.util;
using RimWorld;
using UnityEngine;
using Verse;

namespace FactionColonies
{
    class PatchNoteDef : Def
    {
        private readonly int major = 0;
        private readonly int minor = 0;
        private readonly int patch = 0;
        private readonly int releaseDay = 01;
        private readonly int releaseMonth = 01;
        private readonly int releaseYear = 2000;
        private readonly PatchNoteType patchNoteType = PatchNoteType.Undefined;
        private readonly List<string> patchNoteLines = new List<string>();
        private readonly List<string> additionalNotes = new List<string>();
        private readonly List<string> linkButtonToolTips = new List<string>();
        private readonly List<string> patchNoteImageDescriptions = new List<string>();

        [NoTranslate]
        public readonly string modId = "";
        
        [NoTranslate]
        private readonly string introStringBase = "";

        [NoTranslate]
        private readonly string authorStringBase = "";

        [NoTranslate]
        private readonly List<string> links = new List<string>();

        [NoTranslate]
        private readonly List<string> authors = new List<string>();

        [NoTranslate]
        private readonly List<string> patchNoteImagePaths = new List<string>();

        [NoTranslate]
        private readonly List<string> linkButtonImagePaths = new List<string>();

        private ModContentPack modContentPackCached = null;
        private List<Texture2D> imagesCached = new List<Texture2D>();
        private List<Texture2D> linkButtonImagesCached = new List<Texture2D>();

        /// <summary>
        /// The title of the update example: [Empire] Update 0.38.00
        /// </summary>
        public string Title => $"[{ModName}] {label} {VersionNumber}";

        /// <summary>
        /// Returns the ModContentPack assosiated with the given ModId
        /// </summary>
        public ModContentPack ModContentPack
        {
            get
            {
                modContentPackCached = modContentPackCached ?? (modContentPackCached = LoadedModManager.RunningModsListForReading.FirstOrFallback(pack => pack.PackageId == modId));

                if (modContentPackCached == null)
                {
                    Log.ErrorOnce($"Couldn't find mod with ModId: {modId} Please check the spelling in the PatchNoteDef!", releaseDay + releaseMonth + releaseYear);
                }

                return modContentPackCached;
            }
        }

        /// <summary>
        /// Returns the mod name or an error if the mod wasn't found
        /// </summary>
        public string ModName => ModContentPack?.ModMetaData.Name ?? "MissingModContentPack";

        /// <summary>
        /// The def description
        /// </summary>
        public string Description => description;

        /// <summary>
        /// The complete Version number formatted like: "1.02.03"
        /// </summary>
        public string VersionNumber => $"{major}.{ToVersion(minor)}.{ToVersion(patch)}";

        /// <summary>
        /// Converts the version to the old Empire version format
        /// </summary>
        public double ToOldEmpireVersion => double.Parse($"{major}.{ToVersion(minor)}{ToVersion(patch)}", System.Globalization.CultureInfo.InvariantCulture);

        /// <summary>
        /// The Major version number
        /// </summary>
        public int Major => major;

        /// <summary>
        /// The Minor version number
        /// </summary>
        public int Minor => minor;

        /// <summary>
        /// The Patch version number
        /// </summary>
        public int Patch => patch;

        /// <summary>
        /// Returns the PatchNoteType
        /// </summary>
        public PatchNoteType GetPatchNoteType => patchNoteType;

        /// <summary>
        /// Returns a link as provided by the def
        /// </summary>
        public List<string> Links => links;

        /// <summary>
        /// Returns the patch notes seperated by new lines
        /// </summary>
        public string PatchNotesFormatted => string.Join("\n", patchNoteLines);

        /// <summary>
        /// Returns additional notes as provided by the def
        /// </summary>
        public string AdditionalNotesFormatted => string.Join("\n", additionalNotes);

        /// <summary>
        /// Returns the opening sentence of the patch notes
        /// </summary>
        public string PatchNotesIntroString => string.Format(introStringBase, ModName, VersionNumber);

        /// <summary>
        /// Returns the list of authors in this format: "name0, name1, name2, ..., nameN-1 and nameN" where N is the amount of authors
        /// only returns the name of one author if there is only one
        /// </summary>
        public string AuthorsFormatted
        {
            get
            {
                List<string> workList = authors.ListFullCopy();
                string lastAuthor = workList.Pop();

                if (workList.NullOrEmpty()) return lastAuthor;

                return $"{string.Join(", ", workList)} and {lastAuthor}";
            }
        }

        /// <summary>
        /// Returns a string that contains the authors in a sentence
        /// </summary>
        public string AuthorLine => string.Format(authorStringBase, AuthorsFormatted);

        /// <summary>
        /// Returns the cached link button images, caches them if not yet cached
        /// </summary>
        public List<Texture2D> LinkButtonImages
        {
            get
            {
                if (linkButtonImagesCached.NullOrEmpty())
                {
                    foreach (string path in linkButtonImagePaths)
                    {
                        linkButtonImagesCached.Add(ContentFinder<Texture2D>.Get(path));
                    }
                }

                return linkButtonImagesCached;
            }
        }

        /// <summary>
        /// Returns the tool tips for each LinkButton. May include empty strings
        /// </summary>
        public List<string> LinkButtonToolTips => linkButtonToolTips;

        /// <summary>
        /// Returns the cached patchnote images, caches them if not yet cached
        /// </summary>
        public List<Texture2D> PatchNoteImages
        {
            get
            {
                if (imagesCached.NullOrEmpty())
                {
                    foreach (string path in patchNoteImagePaths)
                    {
                        imagesCached.Add(ContentFinder<Texture2D>.Get(path));
                    }
                }

                return imagesCached;
            }
        }

        public List<string> PatchNoteImageDescriptions => patchNoteImageDescriptions;

        public DateTime ReleaseDate => new DateTime(releaseYear, releaseMonth, releaseDay);

        public string CompletePatchNotesString => $"{description}\n\n{PatchNotesIntroString}\n{PatchNotesFormatted}\n\n{AuthorLine}{(additionalNotes.NullOrEmpty() ? "" : "\n" + AdditionalNotesFormatted)}";

        /// <summary>
        /// Clears the cached data of this def
        /// </summary>
        public override void ClearCachedData()
        {
            base.ClearCachedData();

            imagesCached = new List<Texture2D>();
            linkButtonImagesCached = new List<Texture2D>();
            modContentPackCached = null;
        }

        private static string ToVersion(int num) => (num > 10) ? num.ToString() : '0' + num.ToString();

        /// <summary>
        /// Sorts all patchNoteDefs to find the latest one for a mod using it's <paramref name="modId"/>
        /// </summary>
        /// <param name="modId"></param>
        /// <returns>the newest PatchNoteDef, null if no PatchNoteDefs with the <paramref name="modId"/> exist</returns>
        public static PatchNoteDef GetLatestForMod(string modId)
        {
            List<PatchNoteDef> patchNoteDefs = DefDatabase<PatchNoteDef>.AllDefsListForReading.Where(def => def.modId == modId).ToList();

            if (patchNoteDefs.NullOrEmpty())
            {
                Log.Error($"Could not find any PatchNoteDefs for {modId}!");
                return null;
            }

            patchNoteDefs.SortBy(def => def.ReleaseDate, def => def.ToOldEmpireVersion);
            return patchNoteDefs[0];
        }

        [DefOf]
        public class PatchNoteDefOf
        {
            static PatchNoteDefOf() => DefOfHelper.EnsureInitializedInCtor(typeof(PatchNoteDefOf));
        }
    }
}
