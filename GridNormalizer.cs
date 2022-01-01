using System;

namespace TrafficHeatmap
{
    public abstract class GridNormalizer : IDisposable
    {
        protected bool disposedValue;

        // Normalizes value to a float between 0 and 1
        public abstract float Normalize(float value);
        public abstract void ClearStats();
        public abstract void RecalculateStats(float[] grid);

        public virtual void OnUpdateSingleValue(float cost)
        { }

        public virtual void OnMultiplyAll(float coefficient)
        { }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                this.disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        // ~GridNormalizer()
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
    }
}