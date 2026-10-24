namespace MapboxMaui.Offline;

using Com.Mapbox.Bindgen;
using Com.Mapbox.Common;
using Com.Mapbox.Maps;
using Java.Text;

public partial class OfflineManager: Java.Lang.Object
{
    Com.Mapbox.Maps.OfflineManager nativeManager;
    TileStore tileStore;

    partial void InitializePlatformManager()
    {
        tileStore = TileStore.Create();
        tileStore.SetOption(
            TileStoreOptions.MAPBOX_ACCESS_TOKEN,
            Value.ValueOf(accessToken));

        nativeManager = new Com.Mapbox.Maps.OfflineManager();
    }

    public bool IsMapboxStackConnected
    {
        get => OfflineSwitch.Instance.MapboxStackConnected;
        set => OfflineSwitch.Instance.MapboxStackConnected = value;
    }

    public void DownloadStyle(
        string styleUri,
        StylePackLoadOptions options,
        Action<StylePackLoadProgress> progressHandler,
        Action<StylePack, Exception> completionHandler)
    {
        nativeManager.LoadStylePack(
            styleUri,
            options.ToNative(),
            new StylePackLoadProgressCallback(progressHandler),
            new StylePackCallback(completionHandler)
            );
    }

    public void DownloadTile(
        string tileId,
        TileRegionLoadOptions xoptions,
        Action<TileRegionLoadProgress> progressHandler,
        Action<TileRegion, Exception> completionHandler
    )
    {
        var tilesetDescriptors = xoptions.TilesetDescriptors?
            .Select(
                x => nativeManager.CreateTilesetDescriptor(x.ToNative()))
            .ToList() ?? [];

        var options = new Com.Mapbox.Common.TileRegionLoadOptions.Builder()
            .ExtraOptions(xoptions.ExtraOptions?.Wrap())
            .AcceptExpired(xoptions.AcceptsExpired)
            .AverageBytesPerSecond(xoptions.AvarageBytesPerSecond.HasValue
                ? new Java.Lang.Integer(xoptions.AvarageBytesPerSecond.Value)
                : null)
            .Descriptors(tilesetDescriptors)
            .Geometry(xoptions.Geometry?.ToPlatform())
            .Metadata(xoptions.Metadata?.Wrap())
            .NetworkRestriction(xoptions.NetworkRestriction.ToNative())
            .StartLocation(xoptions.StartLocation?.ToNative())
            .Build();

        tileStore?.LoadTileRegion(
            tileId,
            options,
            new TileRegionLoadProgressCallback(progressHandler),
            new TileRegionCallback(completionHandler)
            );
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            nativeManager?.Dispose();
            nativeManager = null;

            tileStore?.Dispose();
            tileStore = null;
        }
    }

    class TileRegionLoadProgressCallback : Java.Lang.Object, ITileRegionLoadProgressCallback
    {
        private Action<TileRegionLoadProgress> progressHandler;

        public TileRegionLoadProgressCallback(Action<TileRegionLoadProgress> progressHandler)
        {
            this.progressHandler = progressHandler;
        }

        public void Run(Com.Mapbox.Common.TileRegionLoadProgress progress)
        {
            progressHandler?.Invoke(new TileRegionLoadProgress
            {
                CompletedResourceCount = (ulong)progress.CompletedResourceCount,
                CompletedResourceSize = (ulong)progress.CompletedResourceSize,
                ErroredResourceCount = (ulong)progress.ErroredResourceCount,
                LoadedResourceCount = (ulong)progress.LoadedResourceCount,
                LoadedResourceSize = (ulong)progress.LoadedResourceSize,
                RequiredResourceCount = (ulong)progress.RequiredResourceCount,
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                progressHandler = null;
            }
        }
    }

    class TileRegionCallback : Java.Lang.Object, ITileRegionCallback
    {
        private Action<TileRegion, Exception> completionHandler;

        public TileRegionCallback(Action<TileRegion, Exception> completionHandler)
        {
            this.completionHandler = completionHandler;
        }

        public void Run(Expected expected)
        {
            var xstylePack = expected.IsValue
                && expected.Value is Com.Mapbox.Common.TileRegion tileRegion
                ? new TileRegion
                {
                    CompletedResourceCount = (ulong)tileRegion.CompletedResourceCount,
                    CompletedResourceSize = (ulong)tileRegion.CompletedResourceSize,
                    Expires = tileRegion.Expires != null
                        ? DateTime.Parse(new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSSZ").Format(tileRegion.Expires))
                        : null,
                    RequiredResourceCount = (ulong)tileRegion.RequiredResourceCount,
                    Id = tileRegion.Id,
                }
                : null;
            var xerror = expected.IsError
                ? new TileRegionCallBackException((TileRegionError)expected.Error)
                : null;

            completionHandler?.Invoke(xstylePack, xerror);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                completionHandler = null;
            }
        }
    }

    class StylePackLoadProgressCallback : Java.Lang.Object, IStylePackLoadProgressCallback
    {
        private Action<StylePackLoadProgress> progressHandler;

        public StylePackLoadProgressCallback(Action<StylePackLoadProgress> progressHandler)
        {
            this.progressHandler = progressHandler;
        }

        public void Run(Com.Mapbox.Maps.StylePackLoadProgress progress)
        {
            progressHandler?.Invoke(new StylePackLoadProgress
            {
                CompletedResourceCount = (ulong)progress.CompletedResourceCount,
                CompletedResourceSize = (ulong)progress.CompletedResourceSize,
                ErroredResourceCount = (ulong)progress.ErroredResourceCount,
                LoadedResourceCount = (ulong)progress.LoadedResourceCount,
                LoadedResourceSize = (ulong)progress.LoadedResourceSize,
                RequiredResourceCount = (ulong)progress.RequiredResourceCount,
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                progressHandler = null;
            }
        }
    }

    class StylePackCallback : Java.Lang.Object, IStylePackCallback
    {
        private Action<StylePack, Exception> completionHandler;

        public StylePackCallback(Action<StylePack, Exception> completionHandler)
        {
            this.completionHandler = completionHandler;
        }

        public void Run(Expected expected)
        {
            var xstylePack = expected.IsValue
                && expected.Value is Com.Mapbox.Maps.StylePack stylePack
                ? new StylePack
                {
                    CompletedResourceCount = (ulong)stylePack.CompletedResourceCount,
                    CompletedResourceSize = (ulong)stylePack.CompletedResourceSize,
                    Expires = stylePack.Expires != null
                        ? DateTime.Parse(new SimpleDateFormat("yyyy-MM-dd'T'HH:mm:ss.SSSZ").Format(stylePack.Expires))
                        : null,
                    RequiredResourceCount = (ulong)stylePack.RequiredResourceCount,
                    StyleUri = stylePack.StyleURI,
                    GlyphsRasterizationMode = GetGlyphsRasterizationMode(stylePack.GlyphsRasterizationMode),
                }
                : null;
            var xerror = expected.IsError
                ? new StylePackCallBackException((StylePackError)expected.Error)
                : null;

            completionHandler?.Invoke(xstylePack, xerror);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                completionHandler = null;
            }
        }

        private GlyphsRasterizationMode? GetGlyphsRasterizationMode(Com.Mapbox.Maps.GlyphsRasterizationMode mode)
        {
            switch (mode)
            {
                case Com.Mapbox.Maps.GlyphsRasterizationMode x
                    when x == Com.Mapbox.Maps.GlyphsRasterizationMode.NoGlyphsRasterizedLocally:
                    return GlyphsRasterizationMode.NoGlyphsRasterizedLocally;
                case Com.Mapbox.Maps.GlyphsRasterizationMode x
                    when x == Com.Mapbox.Maps.GlyphsRasterizationMode.IdeographsRasterizedLocally:
                    return GlyphsRasterizationMode.IdeographsRasterizedLocally;
                case Com.Mapbox.Maps.GlyphsRasterizationMode x
                    when x == Com.Mapbox.Maps.GlyphsRasterizationMode.AllGlyphsRasterizedLocally:
                    return GlyphsRasterizationMode.AllGlyphsRasterizedLocally;
                default:
                    return null;
            };
        }

    }

    class TileRegionCallBackException : Exception
    {
        private readonly TileRegionError error;

        public TileRegionCallBackException(TileRegionError error)
        {
            this.error = error;
        }
    }

    class StylePackCallBackException : Exception
    {
        private readonly StylePackError error;

        public StylePackCallBackException(StylePackError error)
        {
            this.error = error;
        }
    }
}

