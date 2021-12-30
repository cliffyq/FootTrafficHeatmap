using System;
using Verse;

namespace TrafficHeatmap
{
    public class CellCostGrid : IExposable
    {
        private readonly Map map;
        public float[] Grid { get; set; }

        public CellCostGrid(Map map)
        {
            this.map = map;
            this.Grid = new float[map.cellIndices.NumGridCells];
        }

        public void ExposeData()
        {
            MapExposeUtility.ExposeUshort(this.map, (IntVec3 c) => CellCostGrid.CellCostFloatToShort(this.GetCellCost(c)), delegate (IntVec3 c, ushort val)
            {
                this.Grid[this.map.cellIndices.CellToIndex(c)] = CellCostGrid.CellCostShortToFloat(val);
            }, "grid");
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
    }
}
