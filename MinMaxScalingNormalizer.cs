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
        private bool disposedValue;

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
            if (value >= this.minThreshold)
            {
                this.min = Math.Min(this.min, value);
                this.max = Math.Max(this.max, value);
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

        protected virtual void Dispose(bool disposing)
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

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~MinMaxScalingNormalizer()
        // {
        //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        //     Dispose(disposing: false);
        // }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public override string GetDebugString()
        {
            return $"Min:{min}, Max: {max}, MinThreshold: {minThreshold}, Hash:{this.GetHashCode()}";
        }
    }
}