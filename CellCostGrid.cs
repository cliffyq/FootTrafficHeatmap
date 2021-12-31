using System;
using Verse;

namespace TrafficHeatmap
{
    public class CellCostGrid : IExposable
    {
        private Map map;
        private byte[] tmpArrayForScribe;
        public float[] Grid { get; set; }
        public CellCostGrid()
        {
            Log.Message("CellCostGrid paramless ctor called");
        }

        public CellCostGrid(Map map)
        {
            this.map = map;
            this.Grid = new float[map.cellIndices.NumGridCells];
        }

        public void ExposeData()
        {
            Log.Message($"CellCostGrid.ExposeData Scribe.mode={Scribe.mode}");
            Scribe_References.Look(ref this.map, "map");
            Log.Message($"CellCostGrid.ExposeData Scribe.mode={Scribe.mode}");

            if (this.map == null)
            {
                Log.Message($"CellCostGrid.ExposeData map==null");
            }

            if (Scribe.mode == LoadSaveMode.Saving)
            {
                this.tmpArrayForScribe = MapSerializeUtility.SerializeUshort(this.map, (IntVec3 c) => CellCostGrid.CellCostFloatToShort(this.GetCellCost(c)));
            }

            DataExposeUtility.ByteArray(ref this.tmpArrayForScribe, "grid");

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                if (this.Grid == null)
                {
                    Log.Message($"CellCostGrid.ExposeData this.Grid==null, initing");
                    this.Grid = new float[this.map.cellIndices.NumGridCells];
                }

                MapSerializeUtility.LoadUshort(this.tmpArrayForScribe, this.map, delegate (IntVec3 c, ushort val)
                {
                    this.Grid[this.map.cellIndices.CellToIndex(c)] = CellCostGrid.CellCostShortToFloat(val);
                });
                this.tmpArrayForScribe = null;
                Log.Message($"CellCostGrid.ExposeData finalize {this.ToString()}");
            }
        }

        private static float CellCostShortToFloat(ushort val)
        {
            return val / 65535f;
        }

        private static ushort CellCostFloatToShort(float val)
        {
            return (ushort)Math.Round(val *= 65535f);
        }

        private float GetCellCost(IntVec3 c)
        {
            if (!c.InBounds(this.map))
            {
                return 0f;
            }
            float val = this.Grid[this.map.cellIndices.CellToIndex(c)];
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

        public override string ToString()
        {
            string gridString, mapString;
            if (this.Grid == null)
            {
                gridString = "null";
            }
            else
            {
                gridString = this.Grid.Length.ToString();
            }
            if (this.map == null)
            {
                mapString = "null";
            }
            else
            {
                mapString = this.map.ToString();
            }
            return $"Map: {mapString}, Grid size: {gridString}";
        }
    }
}
