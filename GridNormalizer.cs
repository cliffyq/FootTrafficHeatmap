namespace TrafficHeatmap
{
    public abstract class GridNormalizer
    {
        // Normalizes value to a float between 0 and 1
        public abstract float Normalize(float value);
        public abstract void ClearStats();
        public abstract void RecalculateStats(float[] grid);

        public virtual void OnUpdateSingleValue(float cost)
        { }

        public virtual void OnMultiplyAll(float coefficient)
        { }
    }
}