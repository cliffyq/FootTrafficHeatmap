using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TrafficHeatmap
{
    public class TrafficHeatmapModSettings : ModSettings
    {
        /// <summary>
        /// The three settings our mod has.
        /// </summary>
        public const int DefaultMovingWindowSize = 3;
        public const int DefaultSampleInterval = 180;
        public const bool DefaultEnhanceInfrequentlyVisitedAreas = true;
        public bool enhanceInfrequentlyVisitedAreas = DefaultEnhanceInfrequentlyVisitedAreas;
        public int movingWindowSizeInDays = DefaultMovingWindowSize;
        public int sampleInterval = DefaultSampleInterval;
        public float minThreshold;
        public float coefficient;

        /// <summary>
        /// The part that writes our settings to file. Note that saving is by ref.
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.movingWindowSizeInDays, "movingWindowSizeInDays", DefaultMovingWindowSize);
            Scribe_Values.Look(ref this.sampleInterval, "sampleInterval", DefaultSampleInterval);
            Scribe_Values.Look(ref this.enhanceInfrequentlyVisitedAreas, "enhanceInfrequentlyVisitedAreas", DefaultEnhanceInfrequentlyVisitedAreas);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                this.Init();
            }
        }

        public void Init()
        {
            double windowSizeInTicks = this.movingWindowSizeInDays * GenDate.TicksPerDay;
            this.coefficient = 1f - (float)Math.Exp(-this.sampleInterval / windowSizeInTicks);
            this.minThreshold = (float)(20f / this.sampleInterval * this.coefficient * Math.Pow(1 - this.coefficient, windowSizeInTicks / this.sampleInterval));
        }
    }

    public class TrafficHeatmapMod : Mod
    {
        private string editBufferMovingWindowSizeInDays;
        HashSet<ISettingsObserver> observers = new HashSet<ISettingsObserver>();
        /// <summary>
        /// A reference to our settings.
        /// </summary>
        TrafficHeatmapModSettings settings;

        /// <summary>
        /// A mandatory constructor which resolves the reference to our settings.
        /// </summary>
        /// <param name="content"></param>
        public TrafficHeatmapMod(ModContentPack content) : base(content)
        {
            this.settings = this.GetSettings<TrafficHeatmapModSettings>();
        }

        /// <summary>
        /// The (optional) GUI part to set your settings.
        /// </summary>
        /// <param name="inRect">A Unity Rect with the size of the settings window.</param>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.Label($"Update interval: frequency of update in ticks. Smaller number will cause heatmap to update more frequently but can have negative impact on performance. Current: {this.settings.sampleInterval} (Default: {TrafficHeatmapModSettings.DefaultSampleInterval})");
            this.settings.sampleInterval = (int)listingStandard.Slider(this.settings.sampleInterval, 60f, 1000f);
            listingStandard.Label($"Look back window size: how many days to look back. Heatmap is generated based on (roughly) the average traffic in the past n days. (Default: {TrafficHeatmapModSettings.DefaultMovingWindowSize})");
            if (this.settings.movingWindowSizeInDays < 1)
            {
                this.settings.movingWindowSizeInDays = 1;
            }
            else if (this.settings.movingWindowSizeInDays > 60)
            {
                this.settings.movingWindowSizeInDays = 60;
            }
            this.editBufferMovingWindowSizeInDays = this.settings.movingWindowSizeInDays.ToString();
            listingStandard.IntEntry(ref this.settings.movingWindowSizeInDays, ref this.editBufferMovingWindowSizeInDays);
            listingStandard.CheckboxLabeled($"Enhance infrequently visited areas: (Default: {TrafficHeatmapModSettings.DefaultEnhanceInfrequentlyVisitedAreas})", ref this.settings.enhanceInfrequentlyVisitedAreas, "When turned on, will enhance infrequetly visited areas.");
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
            return "Foot Traffic Heatmap";
        }

        public void Subscribe(ISettingsObserver observer)
        {
            this.observers.Add(observer);
        }

        public void Unsubscribe(ISettingsObserver observer)
        {
            this.observers.Remove(observer);
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            // TODO send event to listener
            this.settings.Init();
            foreach (var observer in this.observers)
            {
                observer.OnSettingsChanged(this.settings);
            }
        }
    }
}
