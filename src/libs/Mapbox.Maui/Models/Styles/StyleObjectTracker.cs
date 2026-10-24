namespace MapboxMaui.Styles;

/// Remembers the sources and layers a handler added to the style, so that an item
/// dropped from <see cref="IMapboxView.Sources"/> or <see cref="IMapboxView.Layers"/>
/// is removed from the native style instead of lingering there forever.
internal sealed class StyleObjectTracker
{
    private readonly Dictionary<string, string> layerSources = new();
    private readonly HashSet<string> sourceIds = new();

    /// Records the new layer set and returns the ids of tracked layers missing from it.
    public IReadOnlyList<string> ReplaceLayers(IEnumerable<MapboxLayer> layers)
    {
        var next = new Dictionary<string, string>();
        foreach (var layer in layers)
        {
            if (string.IsNullOrEmpty(layer?.Id)) continue;

            next[layer.Id] = layer.Source;
        }

        var stale = layerSources.Keys.Where(id => !next.ContainsKey(id)).ToList();

        layerSources.Clear();
        foreach (var pair in next)
        {
            layerSources[pair.Key] = pair.Value;
        }

        return stale;
    }

    /// Records the new source set and returns the tracked sources missing from it, together with
    /// the tracked layers that still read from them: a source cannot be removed while a layer uses it.
    public (IReadOnlyList<string> LayerIds, IReadOnlyList<string> SourceIds) ReplaceSources(IEnumerable<MapboxSource> sources)
    {
        var next = new HashSet<string>(
            sources.Select(source => source?.Id).Where(id => !string.IsNullOrEmpty(id)));

        var staleSources = sourceIds.Where(id => !next.Contains(id)).ToList();
        var staleLayers = layerSources
            .Where(pair => pair.Value is not null && staleSources.Contains(pair.Value))
            .Select(pair => pair.Key)
            .ToList();

        foreach (var layerId in staleLayers)
        {
            layerSources.Remove(layerId);
        }

        sourceIds.Clear();
        sourceIds.UnionWith(next);

        return (staleLayers, staleSources);
    }

    /// The native style dropped every runtime source and layer (style reload).
    public void Reset()
    {
        layerSources.Clear();
        sourceIds.Clear();
    }
}
