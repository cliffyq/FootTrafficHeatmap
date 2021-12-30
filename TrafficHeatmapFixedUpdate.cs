//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using RimWorld;
//using UnityEngine;
//using Verse;

//namespace TrafficHeatmap
//{
//    public class TrafficHeatmapFixedUpdate : MapComponent, ICellBoolGiver
//    {
//        public static bool ShowHeatMap, ShowHeatMapCost;
//        readonly Dictionary<Pawn, CostMap> pawnToCostMap = new Dictionary<Pawn, CostMap>();
//        float[] filteredTotalCostMap;
//        readonly Predicate<Pawn> shouldShowFor = (p) => true;
//        readonly CellBoolDrawer cellBoolDrawer;
//        Gradient gradient;
//        float maxCost = 0.01f;
//        float precision;
//        float coefficient;
//        int windowSizeTicks;
//        int lastGlobalFalloffAt;
//        int fallOffInterval = 100;
//        int numGridCells;
//        Stopwatch sw = new Stopwatch();
//        double cellBoolDrawerUpdateAvgTicks, updateAvgTicks, globalFalloffAvgTicks;
//        int cellBoolDrawerUpdateCount, updateCount, globalFalloffCount;

//        public TrafficHeatmapFixedUpdate(Map map) : base(map)
//        {
//            this.cellBoolDrawer = new CellBoolDrawer(this, map.Size.x, map.Size.z);
//            this.gradient = this.GetGradient();
//            this.windowSizeTicks = GenDate.TicksPerDay;
//            this.precision = 1f / this.windowSizeTicks;
//            this.numGridCells = this.map.cellIndices.NumGridCells;
//            this.filteredTotalCostMap = new float[this.numGridCells];
//            this.coefficient = 1f / this.windowSizeTicks;
//            for (int i = 0; i < this.numGridCells; i++)
//            {
//                this.filteredTotalCostMap[i] = 0;
//            }
//        }

//        public Color Color => Color.white;

//        public bool GetCellBool(int index)
//        {
//            return this.filteredTotalCostMap[index] > this.precision;
//        }

//        public Color GetCellExtraColor(int index)
//        {
//            return this.GetColorForCost(this.filteredTotalCostMap[index]);
//        }

//        public override void MapComponentUpdate()
//        {
//            base.MapComponentUpdate();
//            int curTick = Find.TickManager.TicksGame;
//            if (curTick > this.lastGlobalFalloffAt + this.fallOffInterval)
//            {
//                this.sw.Restart();
//                this.FalloffForAll(this.coefficient, this.precision, curTick);

//                for (int i = 0; i < this.filteredTotalCostMap.Length; i++)
//                {
//                    if (this.filteredTotalCostMap[i] > this.precision)
//                    {
//                        this.filteredTotalCostMap[i] *= (float)Math.Pow(1 - this.coefficient, curTick - this.lastGlobalFalloffAt);
//                    }
//                }
//                this.lastGlobalFalloffAt = curTick;

//                this.sw.Stop();
//                var ticks = this.sw.ElapsedTicks;
//                double coefficient = (double)1 / (++this.globalFalloffCount);
//                this.globalFalloffAvgTicks = this.globalFalloffAvgTicks * (1 - coefficient) + coefficient * ticks;
//            }
//            if (ShowHeatMap)
//            {
//                this.cellBoolDrawer.MarkForDraw();
//                this.sw.Restart();
//                this.cellBoolDrawer.CellBoolDrawerUpdate();
//                this.sw.Stop();
//                var ticks = this.sw.ElapsedTicks;
//                double coefficient = (double)1 / (++this.cellBoolDrawerUpdateCount);
//                this.cellBoolDrawerUpdateAvgTicks = this.cellBoolDrawerUpdateAvgTicks * (1 - coefficient) + coefficient * ticks;
//            }
//        }

//        public override void MapComponentOnGUI()
//        {
//            base.MapComponentOnGUI();
//            if (ShowHeatMapCost)
//            {
//                for (int i = 0; i < this.numGridCells; i++)
//                {
//                    float totalFilteredCost = this.filteredTotalCostMap[i];
//                    if (totalFilteredCost > this.precision)
//                    {// TODO: center text when camera is close, see DebugDrawerOnGUI()
//                        var cell = this.map.cellIndices.IndexToCell(i);
//                        var drawTopLeft = GenMapUI.LabelDrawPosFor(cell);
//                        var labelRect = new Rect(drawTopLeft.x - 20f, drawTopLeft.y, 40f, 20f);
//                        Widgets.Label(labelRect, (totalFilteredCost / this.maxCost).ToString());
//                    }
//                }
//            }

//            Widgets.Label(new Rect(10, Screen.height * 3 / 5f, 300, 300),
//                       $"{this.GetType().Name} CellBoolDrawerUpdate avg ticks: {this.cellBoolDrawerUpdateAvgTicks:N0}\n" +
//                       $"{this.GetType().Name} Update avg ticks: {this.updateAvgTicks:N0}\n" +
//                       $"{this.GetType().Name} GlobalFalloff avg ticks: {this.globalFalloffAvgTicks:N0}\n");
//        }

//        private Color GetColorForCost(float cost)
//        {
//            return this.gradient.Evaluate(Math.Min(cost / this.maxCost, 1f));
//        }

//        //private IEnumerable<KeyValuePair<Pawn, float[]>> GetFilteredCostMaps()
//        //{
//        //    return this.pawnToCostMap.Where(kv => this.shouldShowFor(kv.Key));
//        //}

//        //private float GetFilteredTotalCost(int index)
//        //{
//        //    return this.GetFilteredCostMaps().Sum(kv => kv.Value[index]);
//        //}

//        Gradient GetGradient()
//        {
//            var gradient = new Gradient();

//            var colorKey = new GradientColorKey[3];
//            colorKey[0].color = Color.blue;
//            colorKey[0].time = 0f;
//            colorKey[1].color = Color.yellow;
//            colorKey[1].time = 0.5f;
//            colorKey[2].color = Color.red;
//            colorKey[2].time = 1f;

//            var alphaKey = new GradientAlphaKey[2];
//            alphaKey[0].alpha = 1f;
//            alphaKey[0].time = 0f;
//            alphaKey[1].alpha = 1f;
//            alphaKey[1].time = 1f;

//            gradient.SetKeys(colorKey, alphaKey);
//            return gradient;
//        }

//        internal void Update(Pawn pawn, float cost)
//        {
//            this.sw.Restart();
//            int index = this.map.cellIndices.CellToIndex(pawn.Position);
//            int curTick = Find.TickManager.TicksGame;
//            if (!this.pawnToCostMap.TryGetValue(pawn, out CostMap mapForCurPawn))
//            {
//                mapForCurPawn = new CostMap(this.numGridCells, curTick);
//                this.pawnToCostMap.Add(pawn, mapForCurPawn);
//            }
//            mapForCurPawn.UpdateCostAtIndex(index, cost, this.coefficient, this.precision, curTick);

//            if (this.shouldShowFor(pawn))
//            {
//                this.filteredTotalCostMap[index] += cost * this.coefficient;
//            }

//            this.sw.Stop();
//            var ticks = this.sw.ElapsedTicks;
//            double coefficient1 = (double)1 / (++this.updateCount);
//            this.updateAvgTicks = this.updateAvgTicks * (1 - coefficient1) + coefficient1 * ticks;

//            this.cellBoolDrawer.SetDirty();
//        }

//        void FalloffForAll(float coefficient, float threshold, int curTick)
//        {
//            foreach (var costMap in this.pawnToCostMap.Values)
//            {
//                costMap.Falloff(coefficient, threshold, curTick);
//            }

//            this.cellBoolDrawer.SetDirty();
//        }

//        public class CostMap
//        {
//            float[] costMap;
//            int lastFalloffTick { get; set; }

//            public CostMap(int size, int curTick)
//            {
//                this.costMap = new float[size];
//                for (int i = 0; i < size; i++)
//                {
//                    this.costMap[i] = 0;
//                }
//                this.lastFalloffTick = curTick;
//            }

//            public void Falloff(float coefficient, float threshold, int curTick)
//            {
//                if (this.lastFalloffTick != curTick)
//                {
//                    for (int i = 0; i < this.costMap.Length; i++)
//                    {
//                        this.FalloffSingleCell(coefficient, threshold, curTick, i);
//                    }
//                    this.lastFalloffTick = curTick;
//                }
//            }

//            private void FalloffSingleCell(float coefficient, float threshold, int curTick, int index)
//            {
//                if (this.costMap[index] > threshold)
//                {
//                    this.costMap[index] *= (float)Math.Pow(1 - coefficient, curTick - this.lastFalloffTick);
//                }
//            }

//            public void UpdateCostAtIndex(int index, float cost, float coefficient, float threshold, int curTick)
//            {
//                if (this.lastFalloffTick != curTick)
//                {
//                    this.FalloffSingleCell(coefficient, threshold, curTick, index);
//                    this.lastFalloffTick = curTick;
//                }
//                this.costMap[index] += cost * coefficient;
//            }
//        }
//    }
//}
