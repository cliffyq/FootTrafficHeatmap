using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace TrafficHeatmap
{
    public class Settings : ModSettings
    {
        /// <summary>
        /// The three settings our mod has.
        /// </summary>
        public bool exampleBool;
        public float exampleFloat = 200f;
        public List<Pawn> exampleListOfPawns = new List<Pawn>();

        /// <summary>
        /// The part that writes our settings to file. Note that saving is by ref.
        /// </summary>
        public override void ExposeData()
        {
            Scribe_Values.Look(ref this.exampleBool, "exampleBool");
            Scribe_Values.Look(ref this.exampleFloat, "exampleFloat", 200f);
            Scribe_Collections.Look(ref this.exampleListOfPawns, "exampleListOfPawns", LookMode.Reference);
            base.ExposeData();
        }
    }

    public class ModBase : Mod
    {
        /// <summary>
        /// A reference to our settings.
        /// </summary>
        Settings settings;

        /// <summary>
        /// A mandatory constructor which resolves the reference to our settings.
        /// </summary>
        /// <param name="content"></param>
        public ModBase(ModContentPack content) : base(content)
        {
            this.settings = this.GetSettings<Settings>();
        }

        /// <summary>
        /// The (optional) GUI part to set your settings.
        /// </summary>
        /// <param name="inRect">A Unity Rect with the size of the settings window.</param>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled("exampleBoolExplanation", ref this.settings.exampleBool, "exampleBoolToolTip");
            listingStandard.Label("exampleFloatExplanation");
            this.settings.exampleFloat = listingStandard.Slider(this.settings.exampleFloat, 100f, 300f);
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

        /// <summary>
        /// Override SettingsCategory to show up in the list of settings.
        /// Using .Translate() is optional, but does allow for localisation.
        /// </summary>
        /// <returns>The (translated) mod name.</returns>
        public override string SettingsCategory()
        {
            return "MyExampleModName".Translate();
        }
    }
}
