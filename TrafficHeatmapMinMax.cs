//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Threading.Tasks;
//using RimWorld;
//using UnityEngine;
//using Verse;

//namespace TrafficHeatmap
//{
//    public class TrafficHeatmapMinMax : MapComponent, ICellBoolGiver
//    {
//        public static bool ShowHeatMap, ShowHeatMapCost;
//        private readonly CellBoolDrawer cellBoolDrawer;
//        private readonly float coefficient;
//        private readonly int numGridCells;
//        private readonly int sampleInterval = 180;
//        private readonly float forgetThreshold;
//        private readonly int windowSizeTicks = GenDate.TicksPerDay;
//        private double cellBoolDrawerUpdateAvgTicks, updateAvgTicks, globalFalloffAvgTicks, dirtyUpdateAvgTicks, setDisplayTicks;
//        private int cellBoolDrawerUpdateCount, updateCount, globalFalloffCount, dirtyUpdateCount;
//        private float[] cellCostGridToDisplay;
//        private bool dirty;
//        private int forgetInDays = 1;
//        private Gradient gradient;
//        private int lastGlobalDecayTick;
//        private float maxCost, minCost;
//        private float[] multiPawnsCellCostGrid;
//        private HashSet<Pawn> multiPawnsToDisplayFor = new HashSet<Pawn>();
//        private Dictionary<Pawn, CellCostGrid> pawnToCellCostGridMap = new Dictionary<Pawn, CellCostGrid>();
//        private Stopwatch sw = new Stopwatch();
//        private List<CellCostGrid> tmpCellCostGrids;
//        private List<Pawn> tmpPawns;

//        public TrafficHeatmapMinMax(Map map) : base(map)
//        {
//            this.cellBoolDrawer = new CellBoolDrawer(this, map.Size.x, map.Size.z);
//            this.gradient = this.GetGradient();
//            // Use exponential moving average to calculate average cost over the moving window, see https://en.wikipedia.org/wiki/Moving_average#Application_to_measuring_computer_performance
//            this.coefficient = 1f - (float)Math.Exp(-(double)this.sampleInterval / this.windowSizeTicks);
//            this.forgetThreshold = (float)(20f / this.sampleInterval * this.coefficient * Math.Pow(1 - this.coefficient, (double)this.forgetInDays * GenDate.TicksPerDay / this.sampleInterval));
//            this.maxCost = this.forgetThreshold;
//            this.minCost = 1f;
//            this.numGridCells = this.map.cellIndices.NumGridCells;
//            this.multiPawnsCellCostGrid = new float[this.numGridCells];
//            this.cellCostGridToDisplay = this.multiPawnsCellCostGrid;
//        }

//        public Color Color => Color.white;

//        public override void ExposeData()
//        {
//            base.ExposeData();
//            Scribe_Collections.Look(ref this.pawnToCellCostGridMap, "pawnToCellCostGridMap", LookMode.Reference, LookMode.Deep, ref this.tmpPawns, ref this.tmpCellCostGrids);
//        }

//        public bool GetCellBool(int index)
//        {
//            return this.cellCostGridToDisplay[index] > this.forgetThreshold;
//        }

//        public Color GetCellExtraColor(int index)
//        {
//            return this.GetColorForCost(this.cellCostGridToDisplay[index]);
//        }

//        float Normalize(float cost)
//        {
//            float normalized;
//            if (cost <= this.minCost)
//            {
//                normalized = 0;
//            }
//            else if (cost >= this.maxCost)
//            {
//                normalized = 1;
//            }
//            else
//            {
//                normalized = (cost - this.minCost) / (this.maxCost - this.minCost);
//            }
//            return normalized;
//        }
//#if DEBUG
//        public override void MapComponentOnGUI()
//        {
//            base.MapComponentOnGUI();
//            if (ShowHeatMapCost)
//            {
//                for (int i = 0; i < this.numGridCells; i++)
//                {
//                    float cost = this.cellCostGridToDisplay[i];
//                    if (cost > this.forgetThreshold)
//                    {// TODO: center text when camera is close, see DebugDrawerOnGUI()
//                        var cell = this.map.cellIndices.IndexToCell(i);
//                        var drawTopLeft = GenMapUI.LabelDrawPosFor(cell);
//                        var labelRect = new Rect(drawTopLeft.x - 20f, drawTopLeft.y, 40f, 20f);
//                        Widgets.Label(labelRect, this.Normalize(cost).ToString());
//                    }
//                }
//            }

//            Widgets.Label(new Rect(10, Screen.height * 1 / 2f, 300, 300),
//                       $"{this.GetType().Name}(down) CellBoolDrawerUpdate avg ticks: {this.cellBoolDrawerUpdateAvgTicks:N0}\n" +
//                       $"Update avg ticks: {this.updateAvgTicks:N0}\n" +
//                       $"GlobalDecay avg ticks: {this.globalFalloffAvgTicks:N0}\n" +
//                       $"Max cost: {this.maxCost}, Min cost: {minCost}\n" +
//                       $"Dirty upgrade avg: {this.dirtyUpdateAvgTicks}\n" +
//                       $"Set display ticks: {this.setDisplayTicks}\n" +
//                       $"threshold: {this.forgetThreshold}\n" +
//                       $"Selected pawns: {String.Join(", ", Find.Selector.SelectedPawns)}\n");
//        }
//#endif

//        public override void MapComponentTick()
//        {
//            base.MapComponentTick();
//            int curTick = Find.TickManager.TicksGame;
//            if (curTick >= this.lastGlobalDecayTick + this.sampleInterval)
//            {
//                this.sw.Restart();
//                this.GlobalDecay();
//                this.lastGlobalDecayTick = curTick;

//                this.sw.Stop();
//                var ticks = this.sw.ElapsedTicks;
//                double coefficient = (double)1 / (++this.globalFalloffCount);
//                this.globalFalloffAvgTicks = this.globalFalloffAvgTicks * (1 - coefficient) + coefficient * ticks;

//                this.RemoveInvalidPawns();
//            }
//            //if (curTick % GenDate.TicksPerHour == 0)
//            //{
//            //    Log.Message("Dumping Stats");
//            //    this.DumpStats();
//            //}
//        }

//        private void RemoveInvalidPawns()
//        {
//            var toRemove = this.pawnToCellCostGridMap.Keys.Where(pawn => !pawn.IsColonist || pawn.Dead);

//            if (toRemove.Any())
//            {
//                foreach (var pawn in toRemove)
//                {
//                    this.pawnToCellCostGridMap.Remove(pawn);
//                    this.multiPawnsToDisplayFor.Remove(pawn);
//                }
//                this.SetDirty();
//            }
//        }

//        public override void MapComponentUpdate()
//        {
//            base.MapComponentUpdate();

//            if (ShowHeatMap)
//            {
//                this.SetCellCostGridToDisplay();
//                this.UpdateDisplay();
//                this.sw.Restart();
//                this.cellBoolDrawer.MarkForDraw();
//                this.cellBoolDrawer.CellBoolDrawerUpdate();
//                this.sw.Stop();
//                var ticks = this.sw.ElapsedTicks;
//                double coefficient = (double)1 / (++this.cellBoolDrawerUpdateCount);
//                this.cellBoolDrawerUpdateAvgTicks = this.cellBoolDrawerUpdateAvgTicks * (1 - coefficient) + coefficient * ticks;
//            }
//        }

//        public void Update(Pawn pawn, float cost)
//        {
//            this.sw.Restart();
//            int index = this.map.cellIndices.CellToIndex(pawn.Position);
//            if (!this.pawnToCellCostGridMap.TryGetValue(pawn, out CellCostGrid gridForCurPawn))
//            {
//                gridForCurPawn = new CellCostGrid(this.map);
//                this.pawnToCellCostGridMap.Add(pawn, gridForCurPawn);
//            }
//            float costToAdd = cost / this.sampleInterval * this.coefficient;
//            gridForCurPawn.Grid[index] += costToAdd;
//            if (this.multiPawnsToDisplayFor.Contains(pawn))
//            {
//                this.multiPawnsCellCostGrid[index] += costToAdd;
//            }
//            this.sw.Stop();
//            var ticks = this.sw.ElapsedTicks;
//            double coefficient1 = (double)1 / (++this.updateCount);
//            this.updateAvgTicks = this.updateAvgTicks * (1 - coefficient1) + coefficient1 * ticks;

//            this.SetDirty();
//        }

//        private void UpdateDisplay()
//        {
//            if (this.dirty)
//            {
//                this.sw.Restart();
//                this.dirty = false;
//                float max = 0;
//                float min = 1f;
//                foreach (float i in this.cellCostGridToDisplay)
//                {
//                    if (i > this.forgetThreshold)
//                    {
//                        if (i > max)
//                        {
//                            max = i;
//                        }
//                        else if (i < min)
//                        {
//                            min = i;
//                        }
//                    }
//                }
//                this.maxCost = max;
//                this.minCost = min;
//                this.cellBoolDrawer.SetDirty();

//                this.sw.Stop();
//                var ticks = this.sw.ElapsedTicks;
//                double coefficient = (double)1 / (++this.dirtyUpdateCount);
//                this.dirtyUpdateAvgTicks = this.dirtyUpdateAvgTicks * (1 - coefficient) + coefficient * ticks;
//            }
//        }

//        //private void DumpStats()
//        //{
//        //    var curTick = Find.TickManager.TicksGame;
//        //    foreach (var kv in this.pawnToCellCostGridMap)
//        //    {
//        //        File.WriteAllLines($@"C:\Users\lixinqin\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\{kv.Key}-{curTick}.txt", kv.Value.Grid.Where(f => f > this.forgetThreshold).Select(f => f.ToString()));
//        //    }

//        //    File.WriteAllLines($@"C:\Users\lixinqin\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\multi-{curTick}.txt", this.multiPawnsCellCostGrid.Where(f => f > this.forgetThreshold).Select(f => f.ToString()));
//        //}

//        private Color GetColorForCost(float cost)
//        {
//            return this.gradient.Evaluate(this.Normalize(cost));
//        }

//        private Gradient GetGradient()
//        {
//            var gradient = new Gradient();

//            var colorKey = new GradientColorKey[4];
//            colorKey[0].color = Color.blue;
//            colorKey[0].time = 0f;
//            colorKey[1].color = Color.green;
//            colorKey[1].time = 1f / 3f;
//            colorKey[2].color = Color.yellow;
//            colorKey[2].time = 2f / 3f;
//            colorKey[3].color = Color.red;
//            colorKey[3].time = 1f;

//            var alphaKey = new GradientAlphaKey[2];
//            alphaKey[0].alpha = 1f;
//            alphaKey[0].time = 0f;
//            alphaKey[1].alpha = 1f;
//            alphaKey[1].time = 1f;

//            gradient.SetKeys(colorKey, alphaKey);
//            return gradient;
//        }

//        private void GlobalDecay()
//        {
//            float decayCoefficient = 1 - this.coefficient;
//            IEnumerable<float[]> grids = this.pawnToCellCostGridMap.Values.Select(val => val.Grid).Concat(this.multiPawnsCellCostGrid);
//            Parallel.ForEach(grids, grid =>
//            {
//                for (int i = 0; i < this.numGridCells; i++)
//                {
//                    if (grid[i] > this.forgetThreshold)
//                    {
//                        grid[i] *= decayCoefficient;
//                    }
//                }
//            });
//            this.SetDirty();
//        }

//        private void SetCellCostGridToDisplay()
//        {
//            this.sw.Restart();
//            List<Pawn> selectedPawns = Find.Selector.SelectedPawns;
//            if (selectedPawns.Count == 0 || !this.pawnToCellCostGridMap.ContainsKey(selectedPawns[0]))
//            {
//                this.UpdatePawnMultiSelection(this.pawnToCellCostGridMap.Keys);
//                this.cellCostGridToDisplay = this.multiPawnsCellCostGrid;
//            }
//            else if (selectedPawns.Count == 1 && this.cellCostGridToDisplay != this.pawnToCellCostGridMap[selectedPawns[0]].Grid)
//            {
//                this.cellCostGridToDisplay = this.pawnToCellCostGridMap[selectedPawns[0]].Grid;
//                this.SetDirty();
//            }
//            else
//            {
//                this.UpdatePawnMultiSelection(selectedPawns.Intersect(this.pawnToCellCostGridMap.Keys));
//                this.cellCostGridToDisplay = this.multiPawnsCellCostGrid;
//            }
//            this.sw.Stop();
//            this.setDisplayTicks = this.sw.ElapsedTicks;
//        }

//        private void SetDirty()
//        {
//            this.dirty = true;
//        }

//        private void UpdatePawnMultiSelection(IEnumerable<Pawn> selected)
//        {
//            if (!this.multiPawnsToDisplayFor.SetEquals(selected))
//            {
//                this.multiPawnsToDisplayFor = selected.ToHashSet();
//                Array.Clear(this.multiPawnsCellCostGrid, 0, this.numGridCells);
//                foreach (Pawn pawn in selected)
//                {
//                    for (int i = 0; i < this.numGridCells; i++)
//                    {
//                        this.multiPawnsCellCostGrid[i] += this.pawnToCellCostGridMap[pawn].Grid[i];
//                    }
//                }
//                this.SetDirty();
//            }
//        }
//    }
//}