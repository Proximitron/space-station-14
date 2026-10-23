using System.Numerics;
using Content.Shared._Starlight.Telescience;
using NUnit.Framework;

// ReSharper disable CheckNamespace
namespace Content.Tests.Shared._Starlight.Telescience;

[TestFixture]
public sealed class TelescienceMapGeometryTest
{
    [Test]
    public void MapsSquarePixelsToTheVirtualCoordinateDomain()
    {
        Assert.That(
            TelescienceMapGeometry.TryMapFromPixel(new Vector2(60f, 70f), new Vector2(10f, 20f), 100f, out var center),
            Is.True);
        Assert.That(center, Is.EqualTo(new Vector2(512f, 512f)));

        Assert.That(
            TelescienceMapGeometry.TryMapFromPixel(new Vector2(110f, 120f), new Vector2(10f, 20f), 100f,
                out var bottomRight), Is.True);
        Assert.That(TelescienceMapGeometry.IsValidCoordinate(bottomRight), Is.True);
        Assert.That(bottomRight.X, Is.LessThan(TelescienceMapGeometry.MapSize));
        Assert.That(bottomRight.Y, Is.EqualTo(0f));

        Assert.That(TelescienceMapGeometry.TryMapFromPixel(new Vector2(111f, 70f), new Vector2(10f, 20f), 100f, out _),
            Is.False);
    }

    [Test]
    public void RejectsInvalidVirtualCoordinates()
    {
        Assert.That(TelescienceMapGeometry.IsValidCoordinate(new Vector2(0f, 0f)), Is.True);
        Assert.That(TelescienceMapGeometry.IsValidCoordinate(new Vector2(1023.99f, 1023.99f)), Is.True);
        Assert.That(TelescienceMapGeometry.IsValidCoordinate(new Vector2(1024f, 1f)), Is.False);
        Assert.That(TelescienceMapGeometry.IsValidCoordinate(new Vector2(float.NaN, 1f)), Is.False);
    }

    [Test]
    public void AppliesOvershootOnlyToTheDisplayedRadius()
    {
        var ping = new Vector2(0f, 0f);
        var target = new Vector2(3f, 4f);

        Assert.That(TelescienceMapGeometry.Distance(ping, target), Is.EqualTo(5f));
        Assert.That(TelescienceMapGeometry.DisplayRadius(ping, target, 0f), Is.EqualTo(5f));
        Assert.That(TelescienceMapGeometry.DisplayRadius(ping, target, 12f), Is.EqualTo(17f));
    }

    [Test]
    public void PinpointRadiusIncludesItsBoundary()
    {
        var pinpoint = new Vector2(0f, 0f);
        var target = new Vector2(3f, 4f);

        Assert.That(TelescienceMapGeometry.PinpointIntersectsTarget(pinpoint, target, 5f), Is.True);
        Assert.That(TelescienceMapGeometry.PinpointIntersectsTarget(pinpoint, target, 4.99f), Is.False);
        Assert.That(TelescienceMapGeometry.PinpointIntersectsTarget(pinpoint, target, -1f), Is.False);
    }
}
