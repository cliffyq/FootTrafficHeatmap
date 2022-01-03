using System;

namespace TrafficHeatmap
{
    public interface ISettingsObserver : IDisposable
    {
        void OnSettingsChanged(TrafficHeatmapModSettings settings);
    }
}
