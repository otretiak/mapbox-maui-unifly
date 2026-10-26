using Android.OS;

namespace MapboxMaui;

/// The initial camera in the fragment arguments as primitive values: the arguments are parcelled whenever the
/// fragment state is saved, and a restored fragment must read back exactly what was written.
internal static class CameraOptionsBundle
{
    private const string Latitude = "mapbox-maui.camera.latitude";
    private const string Longitude = "mapbox-maui.camera.longitude";
    private const string Zoom = "mapbox-maui.camera.zoom";
    private const string Bearing = "mapbox-maui.camera.bearing";
    private const string Pitch = "mapbox-maui.camera.pitch";
    private const string PaddingLeft = "mapbox-maui.camera.padding.left";
    private const string PaddingTop = "mapbox-maui.camera.padding.top";
    private const string PaddingRight = "mapbox-maui.camera.padding.right";
    private const string PaddingBottom = "mapbox-maui.camera.padding.bottom";
    private const string AnchorX = "mapbox-maui.camera.anchor.x";
    private const string AnchorY = "mapbox-maui.camera.anchor.y";

    private static readonly string[] Keys =
    [
        Latitude, Longitude, Zoom, Bearing, Pitch,
        PaddingLeft, PaddingTop, PaddingRight, PaddingBottom,
        AnchorX, AnchorY,
    ];

    public static void Put(Bundle bundle, CameraOptions cameraOptions)
    {
        foreach (var key in Keys)
        {
            bundle.Remove(key);
        }

        if (cameraOptions.Center is { } center)
        {
            bundle.PutDouble(Latitude, center.Latitude);
            bundle.PutDouble(Longitude, center.Longitude);
        }

        if (cameraOptions.Zoom is { } zoom) bundle.PutFloat(Zoom, zoom);
        if (cameraOptions.Bearing is { } bearing) bundle.PutFloat(Bearing, bearing);
        if (cameraOptions.Pitch is { } pitch) bundle.PutFloat(Pitch, pitch);

        if (cameraOptions.Padding is { } padding)
        {
            bundle.PutDouble(PaddingLeft, padding.Left);
            bundle.PutDouble(PaddingTop, padding.Top);
            bundle.PutDouble(PaddingRight, padding.Right);
            bundle.PutDouble(PaddingBottom, padding.Bottom);
        }

        if (cameraOptions.Anchor is { } anchor)
        {
            bundle.PutDouble(AnchorX, anchor.X);
            bundle.PutDouble(AnchorY, anchor.Y);
        }
    }

    public static CameraOptions? Get(Bundle bundle)
    {
        if (bundle is null || !Keys.Any(bundle.ContainsKey)) return null;

        return new CameraOptions
        {
            Center = bundle.ContainsKey(Latitude) && bundle.ContainsKey(Longitude)
                ? new MapPosition(bundle.GetDouble(Latitude), bundle.GetDouble(Longitude))
                : null,
            Zoom = bundle.ContainsKey(Zoom) ? bundle.GetFloat(Zoom) : null,
            Bearing = bundle.ContainsKey(Bearing) ? bundle.GetFloat(Bearing) : null,
            Pitch = bundle.ContainsKey(Pitch) ? bundle.GetFloat(Pitch) : null,
            Padding = bundle.ContainsKey(PaddingLeft)
                ? new Thickness(
                    bundle.GetDouble(PaddingLeft),
                    bundle.GetDouble(PaddingTop),
                    bundle.GetDouble(PaddingRight),
                    bundle.GetDouble(PaddingBottom))
                : null,
            Anchor = bundle.ContainsKey(AnchorX)
                ? new ScreenPosition(bundle.GetDouble(AnchorX), bundle.GetDouble(AnchorY))
                : null,
        };
    }
}
