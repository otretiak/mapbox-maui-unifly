using MapboxMaui.Styles;

namespace Mapbox.Maui.Tests;

public class StyleObjectTrackerTests
{
    [Test]
    public void ReplaceLayers_ReturnsOnlyLayersMissingFromTheNextSet()
    {
        var tracker = new StyleObjectTracker();
        tracker.ReplaceLayers([new FillLayer("a", "s1"), new LineLayer("b", "s1")]);

        var stale = tracker.ReplaceLayers([new LineLayer("b", "s1"), new FillLayer("c", "s2")]);

        Assert.That(stale, Is.EquivalentTo(new[] { "a" }));
    }

    [Test]
    public void ReplaceSources_ReturnsStaleSourceAndTheTrackedLayersReadingFromIt()
    {
        var tracker = new StyleObjectTracker();
        tracker.ReplaceSources([new GeoJSONSource("s1"), new GeoJSONSource("s2")]);
        tracker.ReplaceLayers([new FillLayer("a", "s1"), new LineLayer("b", "s2")]);

        var (layerIds, sourceIds) = tracker.ReplaceSources([new GeoJSONSource("s2")]);

        Assert.Multiple(() =>
        {
            Assert.That(sourceIds, Is.EquivalentTo(new[] { "s1" }));
            Assert.That(layerIds, Is.EquivalentTo(new[] { "a" }));
        });
    }

    [Test]
    public void ReplaceSources_LayerRemovedWithItsSourceIsNotReportedAgainByReplaceLayers()
    {
        var tracker = new StyleObjectTracker();
        tracker.ReplaceSources([new GeoJSONSource("s1")]);
        tracker.ReplaceLayers([new FillLayer("a", "s1")]);
        tracker.ReplaceSources([]);

        var stale = tracker.ReplaceLayers([]);

        Assert.That(stale, Is.Empty);
    }

    [Test]
    public void MarkDataApplied_SameInstanceIsSkipped_NewInstanceIsApplied()
    {
        var tracker = new StyleObjectTracker();
        var data = new RawGeoJSONObject("{}");

        Assert.Multiple(() =>
        {
            Assert.That(tracker.MarkDataApplied("s1", data), Is.True);
            Assert.That(tracker.MarkDataApplied("s1", data), Is.False);
            Assert.That(tracker.MarkDataApplied("s1", new RawGeoJSONObject("{}")), Is.True);
        });
    }

    [Test]
    public void MarkDataApplied_AfterResetOrSourceRemoval_DataIsAppliedAgain()
    {
        var tracker = new StyleObjectTracker();
        var data = new RawGeoJSONObject("{}");
        tracker.ReplaceSources([new GeoJSONSource("s1")]);
        tracker.MarkDataApplied("s1", data);

        tracker.Reset();
        var afterReset = tracker.MarkDataApplied("s1", data);

        tracker.ReplaceSources([new GeoJSONSource("s1")]);
        tracker.ReplaceSources([]);
        var afterRemoval = tracker.MarkDataApplied("s1", data);

        Assert.Multiple(() =>
        {
            Assert.That(afterReset, Is.True);
            Assert.That(afterRemoval, Is.True);
        });
    }

    [Test]
    public void MarkDataApplied_NullDataIsNeverSkipped()
    {
        var tracker = new StyleObjectTracker();
        tracker.MarkDataApplied("s1", null);

        Assert.That(tracker.MarkDataApplied("s1", null), Is.True);
    }
}
