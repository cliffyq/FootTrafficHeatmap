namespace TrafficHeatmap
{
    public abstract class GridNormalizer
    {
        public abstract void ClearStats();

        // Normalizes value to a float between 0 and 1
        public abstract float Normalize(float value);

        public abstract void OnMultiplyAll(float coefficient);

        public abstract void OnUpdateSingleValue(float cost);

        public abstract void RecalculateStats(float[] grid);

        public abstract string GetDebugString();
    }
}