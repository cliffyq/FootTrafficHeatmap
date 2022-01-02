using System;
using Verse;

namespace TrafficHeatmap
{
    public enum ScalingMethod
    {
        Linear, SquareRoot
    }

    public class MinMaxScalingNormalizer : GridNormalizer, ISettingsObserver
    {
        protected float max = 0f;
        protected float min = 1f;
        protected float minThreshold;
        protected ScalingMethod scalingMethod;

        public MinMaxScalingNormalizer()
        {
            var mod = LoadedModManager.GetMod<FootTrafficHeatmapMod>();
            mod.Subscribe(this);
            this.UpdateFromSettings(mod.GetSettings<TrafficHeatmapModSettings>());
        }

        public override void ClearStats()
        {
            this.min = 1f;
            this.max = 0f;
        }

        public override float Normalize(float value)
        {
            if (this.min > this.max)
            {
                Log.Error($"Min {this.min} is greater than max {this.max}!");
            }

            float normalized;
            if (value <= this.min)
            {
                normalized = 0;
            }
            else if (value >= this.max)
            {
                normalized = 1;
            }
            else
            {
                normalized = (value - this.min) / (this.max - this.min);
                if (this.scalingMethod == ScalingMethod.SquareRoot)
                {
                    normalized = (float)Math.Sqrt(normalized);
                }
            }
            return normalized;
        }

        public override void OnMultiplyAll(float coefficient)
        {
            this.min = Math.Max(this.min * coefficient, this.minThreshold);
            this.max = Math.Max(this.max * coefficient, this.minThreshold);
        }

        public void OnSettingsChanged(TrafficHeatmapModSettings settings)
        {
            this.UpdateFromSettings(settings);
        }

        public override void OnUpdateSingleValue(float value)
        {
            if (value < this.min)
            {
                this.min = Math.Max(this.minThreshold, value);
            }
            if (value > this.max)
            {
                this.max = Math.Max(this.minThreshold, value);
            }
        }

        public override void RecalculateStats(float[] grid)
        {
            this.ClearStats();
            foreach (float value in grid)
            {
                this.OnUpdateSingleValue(value);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    LoadedModManager.GetMod<FootTrafficHeatmapMod>().Unsubscribe(this);
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                this.disposedValue = true;
            }
        }

        private void UpdateFromSettings(TrafficHeatmapModSettings settings)
        {
            this.minThreshold = settings.minThreshold;
            this.scalingMethod = settings.enhanceLessVisitedAreas ? ScalingMethod.SquareRoot : ScalingMethod.Linear;
        }
    }
}