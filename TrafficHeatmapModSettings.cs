using System;
using RimWorld;
using Verse;

namespace TrafficHeatmap
{
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
        public int globalDecayMode;
        
        public TrafficHeatmapModSettings()
        {
            Init();
        }

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