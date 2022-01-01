using System;
using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace TrafficHeatmap
{
    public class TrafficHeatmapMod : Mod
    {
        private string editBufferMovingWindowSizeInDays;
        private HashSet<ISettingsObserver> observers = new HashSet<ISettingsObserver>();

        private TrafficHeatmapModSettings settings;

        public TrafficHeatmapMod(ModContentPack content) : base(content)
        {
            this.settings = this.GetSettings<TrafficHeatmapModSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.Label($"Update interval in ticks: Smaller number will cause heatmap to update more frequently but can have negative impact on performance. Current: {this.settings.sampleInterval} (Default: {TrafficHeatmapModSettings.DefaultSampleInterval})");
            this.settings.sampleInterval = (int)listingStandard.Slider(this.settings.sampleInterval, 60f, 1000f);
            listingStandard.Label($"Look back window size: How many days to look back. Heatmap is generated based on (roughly) the average traffic in the past n days. (Default: {TrafficHeatmapModSettings.DefaultMovingWindowSize})");
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
            listingStandard.CheckboxLabeled($"Enhance display for less visited areas: (Default: {TrafficHeatmapModSettings.DefaultEnhanceLessVisitedAreas})", ref this.settings.enhanceLessVisitedAreas, "When turned on, will increase the color difference between less visited areas, this will also make the heatmap appear hotter overall. When turned off, the colors represent the actual proportion between areas. ");
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }

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
            this.settings.Init();
            foreach (var observer in this.observers)
            {
                observer.OnSettingsChanged(this.settings);
            }
        }
    }

    public class TrafficHeatmapModSettings : ModSettings
    {
        public const bool DefaultEnhanceLessVisitedAreas = true;
        public const int DefaultMovingWindowSize = 3;
        public const int DefaultSampleInterval = 180;

        public float coefficient;
        public bool enhanceLessVisitedAreas = DefaultEnhanceLessVisitedAreas;
        public float minThreshold;
        public int movingWindowSizeInDays = DefaultMovingWindowSize;
        public int sampleInterval = DefaultSampleInterval;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref this.movingWindowSizeInDays, "movingWindowSizeInDays", DefaultMovingWindowSize);
            Scribe_Values.Look(ref this.sampleInterval, "sampleInterval", DefaultSampleInterval);
            Scribe_Values.Look(ref this.enhanceLessVisitedAreas, "enhanceLessVisitedAreas", DefaultEnhanceLessVisitedAreas);
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
}