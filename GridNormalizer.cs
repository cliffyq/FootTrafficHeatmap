using System;

namespace TrafficHeatmap
{
    public abstract class GridNormalizer : IDisposable
    {
        protected bool disposedValue;

        public abstract void ClearStats();

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        // Normalizes value to a float between 0 and 1
        public abstract float Normalize(float value);

        public abstract void OnMultiplyAll(float coefficient);

        public abstract void OnUpdateSingleValue(float cost);

        public abstract void RecalculateStats(float[] grid);

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
    }
}