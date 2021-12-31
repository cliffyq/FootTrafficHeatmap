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
        Dictionary<Pawn, CellCostGrid> pawnToCellCostGridMap = new Dictionary<Pawn, CellCostGrid>();
        private List<Pawn> tmpPawns;
        private List<CellCostGrid> tmpCellCostGrids;
        float[] multiPawnsCellCostGrid;
        float[] CellCostGridToDisplay;
        HashSet<Pawn> multiPawnsToDisplayFor = new HashSet<Pawn>();
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
        double cellBoolDrawerUpdateAvgTicks, updateAvgTicks, globalFalloffAvgTicks, dirtyUpdateAvgTicks, setDisplayTicks;
        private bool dirty;
        int cellBoolDrawerUpdateCount, updateCount, globalFalloffCount, dirtyUpdateCount, setDisplayCount;

        public TrafficHeatmapFixedSampleRate(Map map) : base(map)
        {
            this.cellBoolDrawer = new CellBoolDrawer(this, map.Size.x, map.Size.z);
            this.gradient = this.GetGradient();
            this.coefficient = (float)this.sampleInterval / this.windowSizeTicks;
            this.precision = 1f / this.windowSizeTicks;
            this.maxCost = this.precision;
            this.numGridCells = this.map.cellIndices.NumGridCells;
            this.multiPawnsCellCostGrid = new float[this.numGridCells];
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Log.Message($"Scribe.mode={Scribe.mode}");
            //Log.Message("Before:");
            //LogVars();
            Scribe_Collections.Look(ref this.pawnToCellCostGridMap, "singlePawnCellCostGrids", LookMode.Reference, LookMode.Deep, ref this.tmpPawns, ref this.tmpCellCostGrids);
            //Log.Message("After:");
            this.LogVars();
        }

        void LogVars()
        {
            if (this.pawnToCellCostGridMap == null)
            {
                Log.Message("pawnToCellCostGridMap==null");
            }
            else
            {
                Log.Message($"pawnToCellCostGridMap size {this.pawnToCellCostGridMap.Count}");
            }
            foreach (var kv in this.pawnToCellCostGridMap)
            {
                Log.Message($"Pawn={kv.Key}, Grid={kv.Value}");
            }
        }

        public Color Color => Color.white;

        public bool GetCellBool(int index)
        {
            return this.CellCostGridToDisplay[index] > this.precision;
        }

        public Color GetCellExtraColor(int index)
        {
            return this.GetColorForCost(this.CellCostGridToDisplay[index]);
        }

        public override void MapComponentTick()
        {
            base.MapComponentTick();
            int curTick = Find.TickManager.TicksGame;
            if (curTick >= this.lastGlobalDecayTick + this.sampleInterval)
            {
                this.sw.Restart();
                this.GlobalDecay();
                this.lastGlobalDecayTick = curTick;

                this.sw.Stop();
                var ticks = this.sw.ElapsedTicks;
                double coefficient = (double)1 / (++this.globalFalloffCount);
                this.globalFalloffAvgTicks = this.globalFalloffAvgTicks * (1 - coefficient) + coefficient * ticks;
            }
        }

        public override void MapComponentUpdate()
        {
            base.MapComponentUpdate();

            if (ShowHeatMap)
            {
                this.SetCellCostGridToDisplay();
                this.DirtyUpdate();
                this.sw.Restart();
                this.cellBoolDrawer.MarkForDraw();
                this.cellBoolDrawer.CellBoolDrawerUpdate();
                this.sw.Stop();
                var ticks = this.sw.ElapsedTicks;
                double coefficient = (double)1 / (++this.cellBoolDrawerUpdateCount);
                this.cellBoolDrawerUpdateAvgTicks = this.cellBoolDrawerUpdateAvgTicks * (1 - coefficient) + coefficient * ticks;


            }
        }

        private void DirtyUpdate()
        {
            if (this.dirty)
            {
                this.sw.Restart();
                this.dirty = false;
                this.maxCost = this.CellCostGridToDisplay.Max();
                this.cellBoolDrawer.SetDirty();

                this.sw.Stop();
                var ticks = this.sw.ElapsedTicks;
                double coefficient = (double)1 / (++this.dirtyUpdateCount);
                this.dirtyUpdateAvgTicks = this.dirtyUpdateAvgTicks * (1 - coefficient) + coefficient * ticks;
            }
        }

        void DumpStats()
        {
            foreach (var kv in this.pawnToCellCostGridMap)
            {

            }
        }

        private void SetCellCostGridToDisplay()
        {
            this.sw.Restart();
            List<Pawn> selectedPawns = Find.Selector.SelectedPawns;
            if (selectedPawns.Count == 0 || !this.pawnToCellCostGridMap.ContainsKey(selectedPawns[0]))
            {
                this.UpdatePawnMultiSelection(this.pawnToCellCostGridMap.Keys);
                this.CellCostGridToDisplay = this.multiPawnsCellCostGrid;
            }
            else if (selectedPawns.Count == 1 && this.CellCostGridToDisplay != this.pawnToCellCostGridMap[selectedPawns[0]].Grid)
            {
                this.CellCostGridToDisplay = this.pawnToCellCostGridMap[selectedPawns[0]].Grid;
                this.SetDirty();
            }
            else
            {
                this.UpdatePawnMultiSelection(selectedPawns.Intersect(this.pawnToCellCostGridMap.Keys));
                this.CellCostGridToDisplay = this.multiPawnsCellCostGrid;
            }
            this.sw.Stop();
            this.setDisplayTicks = this.sw.ElapsedTicks;
        }

        private void UpdatePawnMultiSelection(IEnumerable<Pawn> selected)
        {
            if (!this.multiPawnsToDisplayFor.SetEquals(selected))
            {
                this.multiPawnsToDisplayFor = selected.ToHashSet();
                Array.Clear(this.multiPawnsCellCostGrid, 0, this.numGridCells);
                foreach (Pawn pawn in selected)
                {
                    for (int i = 0; i < this.numGridCells; i++)
                    {
                        this.multiPawnsCellCostGrid[i] += this.pawnToCellCostGridMap[pawn].Grid[i];
                    }
                }
                this.SetDirty();
            }
        }

        public override void MapComponentOnGUI()
        {
            base.MapComponentOnGUI();
            if (ShowHeatMapCost)
            {
                for (int i = 0; i < this.numGridCells; i++)
                {
                    float cost = this.CellCostGridToDisplay[i];
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
                       $"Dirty upgrade avg: {this.dirtyUpdateAvgTicks}\n" +
                       $"Set display ticks: {this.setDisplayTicks}\n" +
                       $"Selected pawns: {String.Join(", ", Find.Selector.SelectedPawns)}\n");
        }

        private Color GetColorForCost(float cost)
        {
            return this.gradient.Evaluate(Math.Min(cost / this.maxCost, 1f));
        }

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

        public void Update(Pawn pawn, float cost)
        {
            this.sw.Restart();
            int index = this.map.cellIndices.CellToIndex(pawn.Position);
            if (!this.pawnToCellCostGridMap.TryGetValue(pawn, out CellCostGrid gridForCurPawn))
            {
                gridForCurPawn = new CellCostGrid(this.map);
                this.pawnToCellCostGridMap.Add(pawn, gridForCurPawn);
            }
            float costToAdd = cost * this.coefficient;
            gridForCurPawn.Grid[index] += costToAdd;
            if (this.multiPawnsToDisplayFor.Contains(pawn))
            {
                this.multiPawnsCellCostGrid[index] += costToAdd;
            }
            this.sw.Stop();
            var ticks = this.sw.ElapsedTicks;
            double coefficient1 = (double)1 / (++this.updateCount);
            this.updateAvgTicks = this.updateAvgTicks * (1 - coefficient1) + coefficient1 * ticks;

            this.SetDirty();
        }

        void SetDirty()
        {
            this.dirty = true;
        }

        void GlobalDecay()
        {
            if (this.coefficient == 0)
            {
                Log.Message("coefficient == 0");
            }
            float decayCoefficient = 1 - this.coefficient;
            if (this.pawnToCellCostGridMap == null)
            {
                Log.Message("singlePawnCellCostGrid == null");
            }
            foreach (var CellCostGrid in this.pawnToCellCostGridMap.Values)
            {
                for (int i = 0; i < this.numGridCells; i++)
                {
                    if (CellCostGrid.Grid[i] > this.precision)
                    {
                        CellCostGrid.Grid[i] *= decayCoefficient;
                    }
                }
            }
            if (this.multiPawnsCellCostGrid == null)
            {
                Log.Message("multiPawnsCellCostGrid == null");
            }
            for (int i = 0; i < this.numGridCells; i++)
            {
                if (this.multiPawnsCellCostGrid[i] > this.precision)
                {
                    this.multiPawnsCellCostGrid[i] *= decayCoefficient;
                }
            }
            if (this.cellBoolDrawer == null)
            {
                Log.Message("cellBoolDrawer == null");
            }
            this.SetDirty();
        }
    }
}
