using System.Numerics;

// ReSharper disable CheckNamespace
namespace Content.Shared._Starlight.Telescience;

public static class TelescienceMapGeometry
{
    public const float MapSize = 1024f;

    public static bool IsValidCoordinate(Vector2 coordinate)
        => float.IsFinite(coordinate.X)
            && float.IsFinite(coordinate.Y)
            && coordinate is { X: >= 0f and < MapSize, Y: >= 0f and < MapSize };

    public static bool TryMapFromPixel(Vector2 pixel, Vector2 mapOrigin, float mapSide, out Vector2 coordinate)
    {
        coordinate = default;
        if (!float.IsFinite(pixel.X)
            || !float.IsFinite(pixel.Y)
            || !float.IsFinite(mapOrigin.X)
            || !float.IsFinite(mapOrigin.Y)
            || !float.IsFinite(mapSide)
            || mapSide <= 0f)
            return false;

        var local = pixel - mapOrigin;
        if (local.X < 0f || local.Y < 0f || local.X > mapSide || local.Y > mapSide)
            return false;

        var maxCoordinate = MathF.BitDecrement(MapSize);
        var normalized = local / mapSide;
        coordinate = new Vector2(
            MathF.Min(normalized.X * MapSize, maxCoordinate),
            MathF.Min((1f - normalized.Y) * MapSize, maxCoordinate));
        return IsValidCoordinate(coordinate);
    }

    public static float Distance(Vector2 first, Vector2 second)
        => Vector2.Distance(first, second);

    public static float DisplayRadius(Vector2 ping, Vector2 target, float overshoot)
        => Distance(ping, target) + overshoot;

    public static bool PinpointIntersectsTarget(Vector2 pinpoint, Vector2 target, float radius)
        => float.IsFinite(radius)
            && radius >= 0f
            && Vector2.DistanceSquared(pinpoint, target) <= radius * radius;
}
