namespace MapboxMaui;

using PlatformPolygonAnnotationManager = Com.Mapbox.Maps.Plugins.Annotations.Generated.PolygonAnnotationManager;
using PlatformCircleAnnotationManager = Com.Mapbox.Maps.Plugins.Annotations.Generated.CircleAnnotationManager;
using PlatformPointAnnotationManager = Com.Mapbox.Maps.Plugins.Annotations.Generated.PointAnnotationManager;
using PlatformPolylineAnnotationManager = Com.Mapbox.Maps.Plugins.Annotations.Generated.PolylineAnnotationManager;
using Com.Mapbox.Maps.Plugins.Annotations;
using MapboxMaui.Annotations;
using MapboxMaui.Styles;

partial class MapboxViewHandler : IAnnotationController
{
    public IPolygonAnnotationManager CreatePolygonAnnotationManager(
        string id,
        Styles.LayerPosition layerPosition)
    {
        var mapView = mapboxFragment?.MapView;

        if (mapView == null) return null;

        var nativeManager = AnnotationsUtils
            .GetAnnotations(mapView)
            .CreateAnnotationManager(
                AnnotationType.PolygonAnnotation,
                new AnnotationConfig(layerPosition.BelowLayerId(), id, id, null)
            ) as PlatformPolygonAnnotationManager;

        MoveAnnotationLayer(mapView, id, layerPosition);
        return new PolygonAnnotationManager(id, nativeManager);
    }

    public ICircleAnnotationManager CreateCircleAnnotationManager(
        string id,
        Styles.LayerPosition layerPosition)
    {
        var mapView = mapboxFragment?.MapView;

        if (mapView == null) return null;

        var nativeManager = AnnotationsUtils
            .GetAnnotations(mapView)
            .CreateAnnotationManager(
                AnnotationType.CircleAnnotation,
                new AnnotationConfig(layerPosition.BelowLayerId(), id, id, null)
            ) as PlatformCircleAnnotationManager;

        MoveAnnotationLayer(mapView, id, layerPosition);
        return new CircleAnnotationManager(id, nativeManager);
    }

    public IPointAnnotationManager CreatePointAnnotationManager(
        string id,
        Styles.LayerPosition layerPosition,
        Annotations.ClusterOptions clusterOptions = null)
    {
        var mapView = mapboxFragment?.MapView;

        if (mapView == null) return null;

        var xclusterOptions = clusterOptions.ToPlatform();
        var clusterMaxZoom = clusterOptions != null
            ? new Java.Lang.Long((long)clusterOptions.ClusterMaxZoom)
            : null;

        var options = new AnnotationSourceOptions(
            clusterMaxZoom,
            null, null, null,
            xclusterOptions);
        var nativeManager = AnnotationsUtils
            .GetAnnotations(mapView)
            .CreateAnnotationManager(
                AnnotationType.PointAnnotation,
                new AnnotationConfig(layerPosition.BelowLayerId(), id, id, options)
            ) as PlatformPointAnnotationManager;

        MoveAnnotationLayer(mapView, id, layerPosition);
        return new PointAnnotationManager(id, nativeManager);
    }

    public IPolylineAnnotationManager CreatePolylineAnnotationManager(string id, LayerPosition layerPosition)
    {
        var mapView = mapboxFragment?.MapView;

        if (mapView == null) return null;

        var nativeManager = AnnotationsUtils
            .GetAnnotations(mapView)
            .CreateAnnotationManager(
                AnnotationType.PolylineAnnotation,
                new AnnotationConfig(layerPosition.BelowLayerId(), id, id, null)
            ) as PlatformPolylineAnnotationManager;

        MoveAnnotationLayer(mapView, id, layerPosition);
        return new PolylineAnnotationManager(id, nativeManager);
    }

    // AnnotationConfig only knows "below"; Above and At are applied by moving the layer the
    // manager has just added.
    private static void MoveAnnotationLayer(Com.Mapbox.Maps.MapView mapView, string layerId, LayerPosition layerPosition)
    {
        if (layerPosition.Enum is not (LayerPositionEnum.Above or LayerPositionEnum.At)) return;

        var style = mapView.MapboxMap.Style;
        if (style is null || !style.StyleLayerExists(layerId)) return;

        var moveResult = style.MoveStyleLayer(layerId, layerPosition.ToPlatformValue());
        if (moveResult.IsError)
        {
            System.Diagnostics.Debug.WriteLine(moveResult.Error);
        }
    }
}
