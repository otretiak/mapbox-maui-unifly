using Foundation;
using MapboxMapsObjC;
using MapboxMaui.Camera;
using UIKit;

namespace MapboxMaui;

partial class MapboxViewHandler : ICameraPlugin
{
    public CameraOptions CameraState
    {
        get
        {
            var mapView = this.GetMapView();

            if (mapView == null) return default;

            return mapView.MapboxMap().CameraState.ToX();
        }
    }

    public void EaseTo(CameraOptions cameraOptions, AnimationOptions animationOptions = default, Action<AnimationState> completion = default)
    {
        var mapView = this.GetMapView();

        if (mapView == null) return;

        var xcameraOptions = cameraOptions.ToNative();
        var curve = animationOptions?.Curve is not null
            ? animationOptions.Curve switch
            {
                TimingCurve.EaseIn => UIViewAnimationCurve.EaseIn,
                TimingCurve.EaseInOut => UIViewAnimationCurve.EaseInOut,
                TimingCurve.EaseOut => UIViewAnimationCurve.EaseOut,
                TimingCurve.Linear => UIViewAnimationCurve.Linear,
                _ => UIViewAnimationCurve.EaseOut,
            }
            : UIViewAnimationCurve.EaseOut;
        mapView.Camera().EaseTo(
            xcameraOptions,
            (animationOptions?.Duration ?? 0) / 1000d,
            curve,
            (position) =>
            {
                var xposition = position switch
                {
                    UIKit.UIViewAnimatingPosition.Start => AnimatingPosition.Start,
                    UIKit.UIViewAnimatingPosition.End => AnimatingPosition.End,
                    UIKit.UIViewAnimatingPosition.Current => AnimatingPosition.Current,
                    _ => AnimatingPosition.Current,
                };

                completion?.Invoke(new AnimationState(xposition));
            });
    }
    public void FlyTo(CameraOptions cameraOptions, AnimationOptions animationOptions = default, Action<AnimationState> completion = default)
    {
        var mapView = this.GetMapView();

        if (mapView == null) return;

        var xcameraOptions = cameraOptions.ToNative();
        var curve = animationOptions?.Curve is not null
            ? animationOptions.Curve switch
            {
                TimingCurve.EaseIn => UIViewAnimationCurve.EaseIn,
                TimingCurve.EaseInOut => UIViewAnimationCurve.EaseInOut,
                TimingCurve.EaseOut => UIViewAnimationCurve.EaseOut,
                TimingCurve.Linear => UIViewAnimationCurve.Linear,
                _ => UIViewAnimationCurve.EaseOut,
            }
            : UIViewAnimationCurve.EaseOut;
        mapView.Camera().FlyTo(
            xcameraOptions,
            animationOptions?.Duration is { } duration ? NSNumber.FromDouble(duration / 1000d) : null,
            curve,
            (position) =>
            {
                var xposition = position switch
                {
                    UIKit.UIViewAnimatingPosition.Start => AnimatingPosition.Start,
                    UIKit.UIViewAnimatingPosition.End => AnimatingPosition.End,
                    UIKit.UIViewAnimatingPosition.Current => AnimatingPosition.Current,
                    _ => AnimatingPosition.Current,
                };

                completion?.Invoke(new AnimationState(xposition));
            });
    }

    public void CancelAnimations()
    {
        this.GetMapView()?.Camera().CancelAnimations();
    }
}
