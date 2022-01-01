using System;
using Verse;

namespace TrafficHeatmap
{
    public class MinMaxScalingNormalizer : GridNormalizer
    {
        private float min = 1f;
        private float max = 0f;
        private float minThreshold;

        public MinMaxScalingNormalizer(float minThreshold)
        {
            this.minThreshold = minThreshold;
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
            }
            return normalized;
        }

        public override void OnUpdateSingleValue(float value)
        {
            if (value > this.minThreshold)
            {
                if (value < this.min)
                {
                    this.min = value;
                }
                else if (value > this.max)
                {
                    this.max = value;
                }
            }
        }

        public override void OnMultiplyAll(float coefficient)
        {
            this.min = Math.Max(this.min * coefficient, this.minThreshold);
            this.max = Math.Max(this.max * coefficient, this.minThreshold);
        }

        public override void RecalculateStats(float[] grid)
        {
            this.ClearStats();
            foreach (float value in grid)
            {
                this.OnUpdateSingleValue(value);
            }
        }

        public override void ClearStats()
        {
            this.min = 1f;
            this.max = 0f;
        }
    }
}