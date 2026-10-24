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

    [Test]
    public void MarkLayerApplied_EqualPropertiesOnANewInstance_AreSkipped()
    {
        var tracker = new StyleObjectTracker();

        Assert.Multiple(() =>
        {
            Assert.That(tracker.MarkLayerApplied(Fill("a", 0.5)), Is.True);
            Assert.That(tracker.MarkLayerApplied(Fill("a", 0.5)), Is.False);
        });
    }

    [Test]
    public void MarkLayerApplied_ChangedPaintOrVisibility_IsApplied()
    {
        var tracker = new StyleObjectTracker();
        tracker.MarkLayerApplied(Fill("a", 0.5));

        var hidden = Fill("a", 0.5);
        hidden.Visibility = MapboxMaui.Visibility.None;

        Assert.Multiple(() =>
        {
            Assert.That(tracker.MarkLayerApplied(Fill("a", 0.6)), Is.True);
            Assert.That(tracker.MarkLayerApplied(hidden), Is.True);
        });
    }

    [Test]
    public void MarkLayerApplied_ChangedFilterExpression_IsApplied()
    {
        var tracker = new StyleObjectTracker();
        var first = Fill("a", 0.5);
        first.Filter = MapboxMaui.Expressions.DslExpression.Eq("type", "x");
        tracker.MarkLayerApplied(first);

        var second = Fill("a", 0.5);
        second.Filter = MapboxMaui.Expressions.DslExpression.Eq("type", "y");

        Assert.That(tracker.MarkLayerApplied(second), Is.True);
    }

    [Test]
    public void MarkLayerApplied_AfterResetOrLayerDropped_IsAppliedAgain()
    {
        var tracker = new StyleObjectTracker();
        tracker.ReplaceLayers([Fill("a", 0.5)]);
        tracker.MarkLayerApplied(Fill("a", 0.5));

        tracker.Reset();
        var afterReset = tracker.MarkLayerApplied(Fill("a", 0.5));

        tracker.ReplaceLayers([Fill("a", 0.5)]);
        tracker.ReplaceLayers([]);
        var afterDrop = tracker.MarkLayerApplied(Fill("a", 0.5));

        Assert.Multiple(() =>
        {
            Assert.That(afterReset, Is.True);
            Assert.That(afterDrop, Is.True);
        });
    }

    [Test]
    public void MarkLayerApplied_ChangedTransition_IsApplied()
    {
        var tracker = new StyleObjectTracker();
        var first = Fill("a", 0.5);
        first.FillOpacityTransition = new MapboxMaui.PropertyValue<StyleTransition>(new StyleTransition(300, 0));
        tracker.MarkLayerApplied(first);

        var second = Fill("a", 0.5);
        second.FillOpacityTransition = new MapboxMaui.PropertyValue<StyleTransition>(new StyleTransition(600, 0));

        Assert.That(tracker.MarkLayerApplied(second), Is.True);
    }

    [Test]
    public void MarkLayerApplied_StringThatLooksLikeAList_DoesNotCollideWithTheList()
    {
        var tracker = new StyleObjectTracker();
        var list = Fill("a", 0.5);
        list.Filter = MapboxMaui.Expressions.DslExpression.Eq("a", "b");
        tracker.MarkLayerApplied(list);

        var text = Fill("a", 0.5);
        text.Filter = MapboxMaui.Expressions.DslExpression.Eq("a\",\"b");

        Assert.That(tracker.MarkLayerApplied(text), Is.True);
    }

    [Test]
    public void MarkLayerApplied_SubByteColorChange_IsApplied()
    {
        var tracker = new StyleObjectTracker();
        var first = Fill("a", 0.5);
        first.FillColor = new MapboxMaui.PropertyValue<Color>(new Color(0.5f, 0.5f, 0.5f));
        tracker.MarkLayerApplied(first);

        var second = Fill("a", 0.5);
        second.FillColor = new MapboxMaui.PropertyValue<Color>(new Color(0.501f, 0.5f, 0.5f));

        Assert.That(tracker.MarkLayerApplied(second), Is.True);
    }

    [Test]
    public void ForgetLayer_FailedWrite_IsAppliedAgainWithTheSameProperties()
    {
        var tracker = new StyleObjectTracker();
        tracker.MarkLayerApplied(Fill("a", 0.5));

        tracker.ForgetLayer("a");

        Assert.That(tracker.MarkLayerApplied(Fill("a", 0.5)), Is.True);
    }

    private static FillLayer Fill(string id, double opacity) => new(id, "s1")
    {
        FillOpacity = new MapboxMaui.PropertyValue<double>(opacity),
        Visibility = MapboxMaui.Visibility.Visible,
    };
}
