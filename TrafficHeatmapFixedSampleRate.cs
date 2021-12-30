using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace TrafficHeatmap
{
    public class TrafficHeatmapFixedSampleRate : MapComponent, ICellBoolGiver
    {
        public static bool ShowHeatMap, ShowHeatMapCost;
        Dictionary<Pawn, float[]> singlePawnCostMap = new Dictionary<Pawn, float[]>();
        private List<Pawn> tmpPawns;
        private List<float[]> tmpCostMaps;
        float[] defaultTotalCostMap, multiPawnsCostMap;
        float[] costMapToDisplay;
        HashSet<Pawn> multiSelectedPawns = new HashSet<Pawn>();
        readonly CellBoolDrawer cellBoolDrawer;
        Gradient gradient;
        float maxCost;
        readonly float precision;
        readonly float coefficient;
        readonly int windowSizeTicks = GenDate.TicksPerDay;
        int lastGlobalDecayTick;
        readonly int sampleInterval = 100;
        readonly int numGridCells;
        Stopwatch sw = new Stopwatch();
        double cellBoolDrawerUpdateAvgTicks, updateAvgTicks, globalFalloffAvgTicks;
        int cellBoolDrawerUpdateCount, updateCount, globalFalloffCount;

        public TrafficHeatmapFixedSampleRate(Map map) : base(map)
        {
            this.cellBoolDrawer = new CellBoolDrawer(this, map.Size.x, map.Size.z);
            this.gradient = this.GetGradient();
            this.coefficient = (float)this.sampleInterval / this.windowSizeTicks;
            this.precision = 1f / this.windowSizeTicks;
            this.maxCost = this.precision;
            this.numGridCells = this.map.cellIndices.NumGridCells;
            this.defaultTotalCostMap = new float[this.numGridCells];
            this.multiPawnsCostMap = new float[this.numGridCells];
            this.costMapToDisplay = this.defaultTotalCostMap;
        }

        public override void ExposeData()
        {
            base.ExposeData();

            //if (Scribe.mode == LoadSaveMode.LoadingVars)
            //{
            //    this.defaultTotalCostMap = new float[this.map.cellIndices.NumGridCells];
            //    multiPawnsCostMap = new float[this.map.cellIndices.NumGridCells];
            //}
            MapExposeUtility.ExposeUshort(this.map, (IntVec3 c) => TrafficHeatmapFixedSampleRate.CostFloatToShort(this.GetDefaultTotalCost(c)), delegate (IntVec3 c, ushort val)
            {
                this.defaultTotalCostMap[this.map.cellIndices.CellToIndex(c)] = TrafficHeatmapFixedSampleRate.CostShortToFloat(val);
            }, "depthGrid");
            Scribe_Values.Look(ref this.defaultTotalCostMap, "defaultTotalCostMap", new float[this.map.cellIndices.NumGridCells]);
            Scribe_Values.Look(ref this.multiPawnsCostMap, "multiPawnsCostMap", new float[this.map.cellIndices.NumGridCells]);
            Scribe_Collections.Look(ref this.singlePawnCostMap, "singlePawnCostMap", LookMode.Reference, LookMode.Value, ref this.tmpPawns, ref this.tmpCostMaps);
        }

        private float GetDefaultTotalCost(IntVec3 c)
        {
            if (!c.InBounds(this.map))
            {
                return 0f;
            }
            return this.defaultTotalCostMap[this.map.cellIndices.CellToIndex(c)];
        }

        public Color Color => Color.white;

        public bool GetCellBool(int index)
        {
            return this.costMapToDisplay[index] > this.precision;
        }

        public Color GetCellExtraColor(int index)
        {
            return this.GetColorForCost(this.costMapToDisplay[index]);
        }

        public override void MapComponentUpdate()
        {
            base.MapComponentUpdate();
            int curTick = Find.TickManager.TicksGame;
            // TODO: use ticks instead for accuraty when speeded up
            if (curTick > this.lastGlobalDecayTick + this.sampleInterval)
            {
                this.sw.Restart();
                this.GlobalDecay();
                this.lastGlobalDecayTick = curTick;

                this.sw.Stop();
                var ticks = this.sw.ElapsedTicks;
                double coefficient = (double)1 / (++this.globalFalloffCount);
                this.globalFalloffAvgTicks = this.globalFalloffAvgTicks * (1 - coefficient) + coefficient * ticks;

                this.UpdateStatistics();
            }
            if (ShowHeatMap)
            {
                this.sw.Restart();
                this.SetCostMapToDisplay();
                this.cellBoolDrawer.MarkForDraw();
                this.cellBoolDrawer.CellBoolDrawerUpdate();
                this.sw.Stop();
                var ticks = this.sw.ElapsedTicks;
                double coefficient = (double)1 / (++this.cellBoolDrawerUpdateCount);
                this.cellBoolDrawerUpdateAvgTicks = this.cellBoolDrawerUpdateAvgTicks * (1 - coefficient) + coefficient * ticks;
            }
        }

        private void UpdateStatistics()
        {
            this.maxCost = this.costMapToDisplay.Max();
            this.cellBoolDrawer.SetDirty();
        }

        void DumpStats()
        {
            foreach (var kv in this.singlePawnCostMap)
            {

            }
        }

        private void SetCostMapToDisplay()
        {
            var selectedPawns = Find.Selector.SelectedPawns;
            var previousCostMapToDisplay = this.costMapToDisplay;
            if (selectedPawns.Count == 0 || !this.singlePawnCostMap.ContainsKey(selectedPawns[0]))
            {
                this.costMapToDisplay = this.defaultTotalCostMap;
            }
            else if (selectedPawns.Count == 1)
            {
                this.costMapToDisplay = this.singlePawnCostMap[selectedPawns[0]];
            }
            else
            {
                this.UpdatePawnMultiSelection(selectedPawns);
                this.costMapToDisplay = this.multiPawnsCostMap;
            }
            if (previousCostMapToDisplay != this.costMapToDisplay)
            {
                this.UpdateStatistics();
                this.cellBoolDrawer.SetDirty();
            }
        }

        private void UpdatePawnMultiSelection(List<Pawn> selectedPawns)
        {
            var selected = selectedPawns.Intersect(this.singlePawnCostMap.Keys).ToHashSet();
            if (!selected.SetEquals(this.multiSelectedPawns))
            {
                this.multiSelectedPawns = selected;
                Array.Clear(this.multiPawnsCostMap, 0, this.numGridCells);
                foreach (Pawn pawn in selected)
                {
                    for (int i = 0; i < this.numGridCells; i++)
                    {
                        this.multiPawnsCostMap[i] += this.singlePawnCostMap[pawn][i];
                    }
                }
                this.cellBoolDrawer.SetDirty();
            }
        }

        public override void MapComponentOnGUI()
        {
            base.MapComponentOnGUI();
            if (ShowHeatMapCost)
            {
                for (int i = 0; i < this.numGridCells; i++)
                {
                    float cost = this.costMapToDisplay[i];
                    if (cost > this.precision)
                    {// TODO: center text when camera is close, see DebugDrawerOnGUI()
                        var cell = this.map.cellIndices.IndexToCell(i);
                        var drawTopLeft = GenMapUI.LabelDrawPosFor(cell);
                        var labelRect = new Rect(drawTopLeft.x - 20f, drawTopLeft.y, 40f, 20f);
                        Widgets.Label(labelRect, (cost / this.maxCost).ToString());
                    }
                }
            }

            Widgets.Label(new Rect(10, Screen.height * 1 / 2f, 300, 300),
                       $"{this.GetType().Name} CellBoolDrawerUpdate avg ticks: {this.cellBoolDrawerUpdateAvgTicks:N0}\n" +
                       $"Update avg ticks: {this.updateAvgTicks:N0}\n" +
                       $"GlobalDecay avg ticks: {this.globalFalloffAvgTicks:N0}\n" +
                       $"Max cost: {this.maxCost}\n" +
                       $"Selected pawns: {String.Join(", ", Find.Selector.SelectedPawns)}\n");
        }

        private Color GetColorForCost(float cost)
        {
            return this.gradient.Evaluate(Math.Min(cost / this.maxCost, 1f));
        }

        //private IEnumerable<KeyValuePair<Pawn, float[]>> GetFilteredCostMaps()
        //{
        //    return this.pawnToCostMap.Where(kv => this.shouldShowFor(kv.Key));
        //}

        //private float GetFilteredTotalCost(int index)
        //{
        //    return this.GetFilteredCostMaps().Sum(kv => kv.Value[index]);
        //}

        Gradient GetGradient()
        {
            var gradient = new Gradient();

            var colorKey = new GradientColorKey[4];
            colorKey[0].color = Color.blue;
            colorKey[0].time = 0f;
            colorKey[1].color = Color.green;
            colorKey[1].time = 1f / 3f;
            colorKey[2].color = Color.yellow;
            colorKey[2].time = 2f / 3f;
            colorKey[3].color = Color.red;
            colorKey[3].time = 1f;

            var alphaKey = new GradientAlphaKey[2];
            alphaKey[0].alpha = 1f;
            alphaKey[0].time = 0f;
            alphaKey[1].alpha = 1f;
            alphaKey[1].time = 1f;

            gradient.SetKeys(colorKey, alphaKey);
            return gradient;
        }

        internal void Update(Pawn pawn, float cost)
        {
            this.sw.Restart();
            int index = this.map.cellIndices.CellToIndex(pawn.Position);
            if (!this.singlePawnCostMap.TryGetValue(pawn, out float[] mapForCurPawn))
            {
                mapForCurPawn = new float[this.numGridCells];
                this.singlePawnCostMap.Add(pawn, mapForCurPawn);
            }
            float costToAdd = cost * this.coefficient;
            mapForCurPawn[index] += costToAdd;
            this.defaultTotalCostMap[index] += costToAdd;
            if (this.multiSelectedPawns.Contains(pawn))
            {
                this.multiPawnsCostMap[index] += costToAdd;
            }
            this.sw.Stop();
            var ticks = this.sw.ElapsedTicks;
            double coefficient1 = (double)1 / (++this.updateCount);
            this.updateAvgTicks = this.updateAvgTicks * (1 - coefficient1) + coefficient1 * ticks;

            this.cellBoolDrawer.SetDirty();
        }

        void GlobalDecay()
        {
            if (this.coefficient == 0)
            {
                Log.Message("coefficient == 0");
            }
            float decayCoefficient = 1 - this.coefficient;
            if (this.singlePawnCostMap == null)
            {
                Log.Message("singlePawnCostMap == null");
            }
            foreach (var costMap in this.singlePawnCostMap.Values)
            {
                for (int i = 0; i < this.numGridCells; i++)
                {
                    if (costMap[i] > this.precision)
                    {
                        costMap[i] *= decayCoefficient;
                    }
                }
            }
            if (this.defaultTotalCostMap == null)
            {
                Log.Message("defaultTotalCostMap == null");
            }
            if (this.multiPawnsCostMap == null)
            {
                Log.Message("multiPawnsCostMap == null");
            }
            for (int i = 0; i < this.numGridCells; i++)
            {
                if (this.defaultTotalCostMap[i] > this.precision)
                {
                    this.defaultTotalCostMap[i] *= decayCoefficient;
                }
                if (this.multiPawnsCostMap[i] > this.precision)
                {
                    this.multiPawnsCostMap[i] *= decayCoefficient;
                }
            }
            if (this.cellBoolDrawer == null)
            {
                Log.Message("cellBoolDrawer == null");
            }
            this.cellBoolDrawer.SetDirty();
        }
    }
}
