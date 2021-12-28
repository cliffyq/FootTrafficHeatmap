using System;
using RimWorld;
using UnityEngine;
using Verse;

namespace TrafficHeatmap
{
    public class TrafficHeatmap : MapComponent, ICellBoolGiver
    {
        public static bool ShowHeatMap;
        public static bool ShowHeatMapCost;
        readonly float[] heatmap;
        readonly CellBoolDrawer cellBoolDrawer;
        Gradient gradient;
        float maxCost = 0.01f;
        float precision;
        int windowSizeTicks;
        int lastUpdatedAt;

        public TrafficHeatmap(Map map) : base(map)
        {
            int n = map.cellIndices.NumGridCells;
            this.heatmap = new float[n];
            for (int i = 0; i < n; i++)
            {
                this.heatmap[i] = 0;
            }
            this.cellBoolDrawer = new CellBoolDrawer(this, map.Size.x, map.Size.z);
            this.gradient = this.GetGradient();
            this.windowSizeTicks = GenDate.TicksPerDay;
            this.precision = 1f / this.windowSizeTicks;
        }

        public Color Color => Color.white;

        public bool GetCellBool(int index)
        {
            return this.heatmap[index] > this.precision;
        }

        public Color GetCellExtraColor(int index)
        {
            return this.GetColorForCost(this.heatmap[index]);
        }

        public override void MapComponentUpdate()
        {
            base.MapComponentUpdate();
            if (ShowHeatMap)
            {
                this.cellBoolDrawer.MarkForDraw();
                this.cellBoolDrawer.CellBoolDrawerUpdate();
            }
        }

#if DEBUG
        public override void MapComponentOnGUI()
        {
            base.MapComponentOnGUI();
            if (ShowHeatMapCost)
            {
                for (int i = 0; i < this.heatmap.Length; i++)
                {
                    if (this.heatmap[i] > this.precision)
                    {// TODO: center text when camera is close, see DebugDrawerOnGUI()
                        Text.Font = GameFont.Tiny;
                        var cell = this.map.cellIndices.IndexToCell(i);
                        var drawTopLeft = GenMapUI.LabelDrawPosFor(cell);
                        var labelRect = new Rect(drawTopLeft.x, drawTopLeft.y, 20f, 20f);
                        Widgets.Label(labelRect, (this.heatmap[i] / this.maxCost).ToString());

                        Log.Message($"Cell:{cell}, {(this.heatmap[i] / this.maxCost).ToString()}");
                    }
                }
            }
        }
#endif

        private Color GetColorForCost(float cost)
        {
            return this.gradient.Evaluate(Math.Min(cost / this.maxCost, 1f));
        }

        Gradient GetGradient()
        {
            var gradient = new Gradient();

            var colorKey = new GradientColorKey[2];
            colorKey[0].color = new Color(0.2f, 0.2f, 0.8f);
            colorKey[0].time = 0f;
            colorKey[1].color = new Color(1f, 0.1f, 0f);
            colorKey[1].time = 1f;

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
            int index = this.map.cellIndices.CellToIndex(pawn.Position);
            int curTick = Find.TickManager.TicksGame;
            double coefficient = (double)1 / this.windowSizeTicks;
            if (this.lastUpdatedAt > 0)
            {
                for (int i = 0; i < this.heatmap.Length; i++)
                {
                    if (this.heatmap[i] > this.precision)
                    {
                        this.heatmap[i] *= (float)Math.Pow(1 - coefficient, curTick - this.lastUpdatedAt);
                    }
                }
            }
            this.heatmap[index] += (float)(cost * coefficient);
            this.lastUpdatedAt = curTick;
            this.cellBoolDrawer.SetDirty();
        }
    }
}
