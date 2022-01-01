namespace TrafficHeatmap
{
    public interface ISettingsObserver
    {
        void OnSettingsChanged(TrafficHeatmapModSettings settings);
    }
}
