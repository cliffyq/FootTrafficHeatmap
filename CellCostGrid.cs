using System;
using Verse;

namespace TrafficHeatmap
{
    public class CellCostGrid : IExposable, ISettingsObserver
    {
        private bool disposedValue;
        private float[] grid;
        private Map map;
        private float threshold;
        private byte[] tmpByteArrayForScribe;

        public CellCostGrid()
        {
            this.Normalizer = new MinMaxScalingNormalizer();
        }

        public CellCostGrid(Map map)
        {
            this.map = map;
            this.grid = new float[map.cellIndices.NumGridCells];
            this.Normalizer = new MinMaxScalingNormalizer();
            var mod = LoadedModManager.GetMod<FootTrafficHeatmapMod>();
            mod.Subscribe(this);
            this.UpdateFromSettings(mod.GetSettings<TrafficHeatmapModSettings>());
        }

        public GridNormalizer Normalizer { get; set; }

        public void AddRawCost(int index, float cost)
        {
            this.grid[index] += cost;
            this.Normalizer.OnUpdateSingleValue(this.grid[index]);
        }

        public void Clear()
        {
            Array.Clear(this.grid, 0, this.grid.Length);
            this.Normalizer.ClearStats();
        }

        public void Decay(float decayCoefficient)
        {
            for (int i = 0; i < this.grid.Length; i++)
            {
                grid[i] *= decayCoefficient;
            }
            this.Normalizer.OnMultiplyAll(decayCoefficient);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public void ExposeData()
        {
            Scribe_References.Look(ref this.map, "map");

            this.ExposeGrid();
        }

        // Returns a normalized cost between 0 and 1, or -1 if the raw cost is below threshold
        public float GetNormalizedCost(int index)
        {
            if (this.Normalizer != null)
            {
                return this.Normalizer.Normalize(this.grid[index]);
            }
            else
            {
                Log.Error("Normalizer not found.");
                return index;
            }
        }

        public float GetRawCost(int index)
        {
            return this.grid[index];
        }

        public void OnSettingsChanged(TrafficHeatmapModSettings settings)
        {
            this.UpdateFromSettings(settings);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                    LoadedModManager.GetMod<FootTrafficHeatmapMod>().Unsubscribe(this);
                    if (this.Normalizer is ISettingsObserver observer)
                    {
                        observer.Dispose();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                this.disposedValue = true;
            }
        }

        private static ushort CellCostFloatToShort(float val)
        {
            return (ushort)Math.Round(val * 65535f);
        }

        private static float CellCostShortToFloat(ushort val)
        {
            return val / 65535f;
        }

        private void ExposeGrid()
        {
            if (Scribe.mode == LoadSaveMode.Saving)
            {
                this.tmpByteArrayForScribe = MapSerializeUtility.SerializeUshort(this.map, (IntVec3 c) => CellCostGrid.CellCostFloatToShort(this.GetCellCost(c)));
            }

            DataExposeUtility.ByteArray(ref this.tmpByteArrayForScribe, "grid");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (this.grid == null)
                {
                    if (this.map == null)
                    {
                        Log.Error($"CellCostGrid.ExposeGrid error: map==null");
                        return;
                    }
                    this.grid = new float[this.map.cellIndices.NumGridCells];
                }

                MapSerializeUtility.LoadUshort(this.tmpByteArrayForScribe, this.map, delegate (IntVec3 c, ushort val)
                {
                    this.grid[this.map.cellIndices.CellToIndex(c)] = CellCostGrid.CellCostShortToFloat(val);
                });
                this.tmpByteArrayForScribe = null;
                this.Normalizer.RecalculateStats(this.grid);
            }
        }

        private float GetCellCost(IntVec3 c)
        {
            if (!c.InBounds(this.map))
            {
                return 0f;
            }
            float val = this.grid[this.map.cellIndices.CellToIndex(c)];
            if (val < 0f)
            {
                Log.Error($"Invalid cell cost {val} at {c}!");
                val = 0f;
            }
            if (val > 1f)
            {
                Log.Error($"Invalid cell cost {val} at {c}!");
                val = 1f;
            }
            return val;
        }

        private void UpdateFromSettings(TrafficHeatmapModSettings settings)
        {
            this.threshold = settings.minThreshold;
        }
    }
}