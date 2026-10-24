
using MapboxCommon;

namespace MapboxMaui;

using Foundation;
using MapboxMapsObjC;
using PlatformView = MapViewContainer;
using UIKit;
using MapboxMaui.Styles;

public partial class MapboxViewHandler
{
    public MapboxMaps.MapView NativeMapView => ((IElementHandler)this).PlatformView is MapViewContainer container
        ? container.MapView
        : null;

    public TMBMapboxMap NativeMap => NativeMapView?.MapboxMap();

    private static void HandleGestureSettingsChanged(MapboxViewHandler handler, IMapboxView view)
    {
        var mapView = handler.GetMapView();
        if (mapView == null) return;

        var gestureOptions = new TMBGestureOptions(
            doubleTapToZoomInEnabled: view.GestureSettings.DoubleTapToZoomInEnabled,
            doubleTouchToZoomOutEnabled: view.GestureSettings.DoubleTouchToZoomOutEnabled,
            focalPoint: view.GestureSettings.FocalPoint?.ToNSValue(),
            quickZoomEnabled: view.GestureSettings.QuickZoomEnabled,
            pitchEnabled: view.GestureSettings.PitchEnabled,
            pinchPanEnabled: view.GestureSettings.PinchScrollEnabled,
            rotateEnabled: view.GestureSettings.RotateEnabled,
            panEnabled: view.GestureSettings.ScrollEnabled,
            panMode: view.GestureSettings.ScrollMode.ToNative(),
            simultaneousRotateAndPinchZoomEnabled: view.GestureSettings.SimultaneousRotateAndPinchToZoomEnabled,

            // iOS Only
            panDecelerationFactor: view.GestureSettings.PanDecelerationFactor
                ?? UIScrollView.DecelerationRateNormal,
            pinchEnabled: view.GestureSettings.PinchEnabled,
            pinchZoomEnabled: view.GestureSettings.PinchZoomEnabled
            );
        mapView.Gestures().GestureOptions = gestureOptions;
    }

    private static void HandleLightChanged(MapboxViewHandler handler, IMapboxView view)
    {
        var mapView = handler.GetMapView();
        if (mapView == null) return;

        var light = view.Light;
        if (light == null) return;

        var platformProperties = light.Properties.Wrap() as NSDictionary<NSString, NSObject>;
        var style = mapView.MapboxMap();

        // TODO SetLightWithProperties
        throw new NotImplementedException();
        //style.SetLightWithProperties(
        //    platformProperties,
        //    (error) =>
        //    {
        //        if (error == null) return;

        //        System.Diagnostics.Debug.WriteLine(error.LocalizedDescription);
        //    });
    }

    private static void HandleImagesChanged(MapboxViewHandler handler, IMapboxView view)
    {
        var mapView = handler.GetMapView();
        if (mapView == null) return;

        var images = view.Images;
        if (images == null) return;

        var style = mapView.MapboxMap();

        foreach (var ximage in images)
        {
            if (!string.IsNullOrWhiteSpace(ximage.Name))
            {
                var image = UIImage.FromBundle(ximage.Name);

                if (image == null) continue;

                if (ximage.IsTemplate)
                {
                    image = image.ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate);
                }

                style.AddImage(
                    image,
                    ximage.Id,
                    ximage.Sdf,
                    UIEdgeInsets.Zero,
                    (error) =>
                    {
                        if (error == null) return;

                        System.Diagnostics.Debug.WriteLine(error.LocalizedDescription);
                    });
            }

            // TODO handle other image types
        }
    }

    private static void HandleLayersChanged(MapboxViewHandler handler, IMapboxView view)
    {
        var mapView = handler.GetMapView();
        if (mapView == null) return;

        var layers = view.Layers;
        if (layers == null) return;

        var style = mapView.MapboxMap();

        foreach (var staleLayerId in handler.styleObjects.ReplaceLayers(layers))
        {
            RemoveLayer(style, staleLayerId);
        }

        foreach (var layer in layers)
        {
            var exists = style.LayerExistsWithId(layer.Id);
            if (!handler.styleObjects.MarkLayerApplied(layer) && exists) continue;

            var properties = layer.ToPlatformValue();
            var layerPosition = layer.LayerPosition.ToPlatformValue(layer.LayerPosition.Parameter?.Wrap());

            if (exists)
            {
                style.SetLayerPropertiesFor(
                    layer.Id,
                    properties,
                    (error) =>
                    {
                        if (error == null) return;

                        handler.styleObjects.ForgetLayer(layer.Id);
                        System.Diagnostics.Debug.WriteLine(error.LocalizedDescription);
                    });

                continue;
            }

            style.AddLayerWith(
                properties,
                layerPosition,
                (error) =>
                {
                    if (error == null) return;

                    System.Diagnostics.Debug.WriteLine(error.LocalizedDescription);
                }
            );
        }
    }

    private static void HandleTerrainChanged(MapboxViewHandler handler, IMapboxView view)
    {
        var mapView = handler.GetMapView();
        if (mapView == null) return;

        var terrain = view.Terrain;
        if (terrain == null) return;

        var platformValue = terrain.ToPlatformValue();

        mapView.MapboxMap().SetTerrain(platformValue, null);
    }

    private static void HandleSourcesChanged(MapboxViewHandler handler, IMapboxView view)
    {
        var mapView = handler.GetMapView();
        if (mapView == null) return;

        var sources = view.Sources;
        if (sources == null) return;

        var style = mapView.MapboxMap();

        var (staleLayerIds, staleSourceIds) = handler.styleObjects.ReplaceSources(sources);
        foreach (var staleLayerId in staleLayerIds)
        {
            RemoveLayer(style, staleLayerId);
        }
        foreach (var staleSourceId in staleSourceIds)
        {
            if (!style.SourceExistsWithId(staleSourceId)) continue;

            style.RemoveSourceWithId(staleSourceId, LogError);
        }

        foreach (var source in sources)
        {
            var platformValue = source.ToPlatformValue();
            var sourceExists = style.SourceExistsWithId(source.Id);

            if (source is GeoJSONSource geojsonSource)
            {
                if (!handler.styleObjects.MarkDataApplied(source.Id, geojsonSource.Data) && sourceExists) continue;

                HandleGeoJSONSource(style, source.Id, platformValue, geojsonSource.Data);
                continue;
            }

            if (sourceExists)
            {
                style.SetSourcePropertiesFor(
                    source.Id, platformValue,
                    (error) =>
                    {
                        if (error == null) return;

                        System.Diagnostics.Debug.WriteLine(error.LocalizedDescription);
                    });
                continue;
            }

            style.AddSourceWithId(
                source.Id,
                platformValue,
                (error) =>
                {
                    if (error == null) return;

                    System.Diagnostics.Debug.WriteLine(error.LocalizedDescription);
                });
        }
    }

    private static void RemoveLayer(TMBMapboxMap style, string layerId)
    {
        if (!style.LayerExistsWithId(layerId)) return;

        style.RemoveLayerWithId(layerId, LogError);
    }

    private static void LogError(NSError error)
    {
        if (error == null) return;

        System.Diagnostics.Debug.WriteLine(error.LocalizedDescription);
    }

    private static void HandleGeoJSONSource(TMBMapboxMap style, string sourceId, NSDictionary<NSString, NSObject> platformValue, GeoJSON.Text.IGeoJSONObject data)
    {
        var sourceExists = style.SourceExistsWithId(sourceId);

        switch (data)
        {
            case RawGeoJSONObject raw:
                
                var sourceData = TMBGeoJSONSourceData.FromString(raw.Data);

                if (sourceExists)
                {
                    style.UpdateGeoJSONSourceWithId(sourceId, sourceData, null);
                    return;
                }
                
                var source = new TMBGeoJSONSource(sourceId);
                source.Data = sourceData;
                
                style.AddSource(source , sourceId,  (error) =>
                {
                    if (error == null) return;
                    System.Diagnostics.Debug.WriteLine(error.LocalizedDescription);
                });
            break;
            
            case GeoJSON.Text.Geometry.IGeometryObject geometry:

                var feature = new MBXFeature(new NSString(sourceId), geometry.ToNative(), platformValue);

                if (sourceExists) {
                    style.UpdateGeoJSONSourceFeaturesForSourceId(
                        sourceId, [feature], null);
                    return;
                }
                
                style.AddGeoJSONSourceFeaturesForSourceId(
                    sourceId, [feature], null);
                
                break;
        }
    }

    private static void HandleCameraOptionsChanged(MapboxViewHandler handler, IMapboxView view)
    {
        var mapView = handler.GetMapView();
        if (mapView == null) return;

        var cameraOptions = view.CameraOptions.ToNative();
        if (cameraOptions == null) return;

        mapView.MapboxMap().SetCameraTo(cameraOptions);
    }

    private static void HandleDebugOptionsChanged(MapboxViewHandler handler, IMapboxView view)
    {
        if (view.DebugOptions == null) return;

        var mapView = handler.GetMapView();
        if (mapView == null) return;

        var debugOptions = view.DebugOptions.ToNative();

        mapView.MapboxMapDebugOptions(
            debugOptions
                .Select(x => NSNumber.FromInt32((int)x))
                .ToArray()
        );
    }

    private static void HandleScaleBarVisibilityChanged(MapboxViewHandler handler, IMapboxView view)
    {
        var mapView = handler.GetMapView();
        if (mapView == null) return;

        var options = mapView.Ornaments().Options;

        options.ScaleBar.Visibility = view.ScaleBarVisibility.ToNative();
        mapView.Ornaments().Options = options;
    }

    private static void HandleMapboxStyleChanged(MapboxViewHandler handler, IMapboxView view)
    {
        var mapView = handler.GetMapView();
        if (mapView == null) return;

        var styleUri = view.MapboxStyle.ToNative();
        if (string.IsNullOrWhiteSpace(styleUri)) return;

        mapView.MapboxMap().LoadStyleWithUri(styleUri, null, (error) =>
        {
            if (error is not null)
            {
                System.Diagnostics.Debug.WriteLine(error.LocalizedDescription);
                return;
            }

            // Loading the URI that is already loaded completes without a new style-loaded event.
            handler.styleObjects.Reset();
            (handler.VirtualView as MapboxView)?.InvokeStyleLoaded();
        });
    }

    protected override PlatformView CreatePlatformView()
        => new PlatformView(
            ACCESS_TOKEN,
            VirtualView.CameraOptions,
            VirtualView.MapboxStyle);
    
    protected override void DisconnectHandler(PlatformView platformView)
    {
        UnRegisterEvents(platformView);

        base.DisconnectHandler(platformView);
    }

    protected override void ConnectHandler(PlatformView platformView)
    {
        base.ConnectHandler(platformView);

        RegisterEvents(platformView);
    }
}