using System.Linq;
using System.Numerics;
using Content.Shared._Starlight.Telescience;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.Input;
using Robust.Shared.Timing;

// ReSharper disable CheckNamespace
namespace Content.Client._Starlight.Telescience.UI;

public sealed class TelescienceMapControl : Control
{
    private const float AnimationDuration = 0.85f;
    private const int GridDivisions = 8;

    private static readonly Color _background = Color.FromHex("#07151C");
    private static readonly Color _grid = Color.FromHex("#1B3944");
    private static readonly Color _border = Color.FromHex("#4D7883");
    private static readonly Color _pingColor = Color.FromHex("#70DCEB");
    private static readonly Color _pinpointColor = Color.FromHex("#F0B75E");
    private static readonly Color _hitColor = Color.FromHex("#8AFFA5");
    private static readonly Color _targetColor = Color.FromHex("#FFF2A8");

    private readonly List<TelescienceAttemptData> _attempts = [];
    private readonly Dictionary<int, float> _animationElapsed = new();
    private Vector2? _selectedCoordinate;
    private Vector2? _revealedTarget;
    private float _pinpointRadius;
    private TelescienceInteractionMode _mode;
    private int _roundId;

    public event Action<Vector2>? CoordinateSelected;

    public TelescienceMapControl()
    {
        SetSize = new Vector2(float.NaN);
        MinSize = new Vector2(450f);
        HorizontalExpand = true;
        VerticalExpand = true;
        MouseFilter = MouseFilterMode.Stop;
        CanKeyboardFocus = true;
        RectClipContent = true;
    }

    public void SetState(
        int roundId,
        List<TelescienceAttemptData> attempts,
        Vector2? revealedTarget,
        float pinpointRadius)
    {
        var firstState = _roundId == 0;
        if (_roundId != roundId)
        {
            _attempts.Clear();
            _animationElapsed.Clear();
            _selectedCoordinate = null;
        }

        _roundId = roundId;
        var existingIds = new HashSet<int>();
        foreach (var attempt in attempts)
        {
            existingIds.Add(attempt.Id);
            if (_attempts.Exists(existing => existing.Id == attempt.Id))
                continue;

            _attempts.Add(attempt);
            if (!firstState)
                _animationElapsed[attempt.Id] = 0f;
        }

        _attempts.RemoveAll(attempt => !existingIds.Contains(attempt.Id));
        foreach (var id in _animationElapsed.Keys.Where(id => !existingIds.Contains(id)).ToArray())
            _animationElapsed.Remove(id);

        _revealedTarget = revealedTarget;
        _pinpointRadius = pinpointRadius;
    }

    public void SetMode(TelescienceInteractionMode mode)
        => _mode = mode;

    protected override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        foreach (var (id, elapsed) in _animationElapsed.ToArray())
        {
            var next = elapsed + args.DeltaSeconds;
            if (next >= AnimationDuration)
                _animationElapsed.Remove(id);
            else
                _animationElapsed[id] = next;
        }
    }

    protected override void Draw(DrawingHandleScreen handle)
    {
        base.Draw(handle);

        var controlBounds = UIBox2.FromDimensions(Vector2.Zero, PixelSize);
        handle.DrawRect(controlBounds, _background);

        var mapBounds = GetMapBounds();
        if (mapBounds.Width <= 0f)
            return;

        for (var division = 1; division < GridDivisions; division++)
        {
            var offset = mapBounds.Width * division / GridDivisions;
            handle.DrawLine(
                new Vector2(mapBounds.Left + offset, mapBounds.Top),
                new Vector2(mapBounds.Left + offset, mapBounds.Bottom),
                _grid);
            handle.DrawLine(
                new Vector2(mapBounds.Left, mapBounds.Top + offset),
                new Vector2(mapBounds.Right, mapBounds.Top + offset),
                _grid);
        }

        foreach (var attempt in _attempts)
            DrawAttempt(handle, mapBounds, attempt);

        if (_revealedTarget is { } target)
            DrawTarget(handle, mapBounds, target);

        if (_selectedCoordinate is { } selected)
        {
            if (_mode == TelescienceInteractionMode.Pinpoint)
                DrawPinpointPreview(handle, mapBounds, selected);

            DrawSelectedCoordinate(handle, mapBounds, selected);
        }

        handle.DrawRect(mapBounds, _border, filled: false);
    }

    protected override void KeyBindUp(GUIBoundKeyEventArgs args)
    {
        if (args.Function != EngineKeyFunctions.UIClick)
        {
            base.KeyBindUp(args);
            return;
        }

        var mapBounds = GetMapBounds();
        var localPosition = args.PointerLocation.Position - GlobalPixelPosition;
        if (!TelescienceMapGeometry.TryMapFromPixel(
                localPosition,
                mapBounds.TopLeft,
                mapBounds.Width,
                out var coordinate))
            return;

        _selectedCoordinate = coordinate;
        CoordinateSelected?.Invoke(coordinate);
    }

    public void ClearSelection()
        => _selectedCoordinate = null;

    private UIBox2 GetMapBounds()
    {
        var side = MathF.Min(PixelSize.X, PixelSize.Y);
        var origin = (PixelSize - new Vector2(side)) / 2f;
        return UIBox2.FromDimensions(origin, new Vector2(side));
    }

    private void DrawAttempt(DrawingHandleScreen handle, UIBox2 mapBounds, TelescienceAttemptData attempt)
    {
        var center = ToScreen(mapBounds, attempt.Coordinate);
        var targetRadius = attempt.Mode == TelescienceInteractionMode.Ping
            ? attempt.DisplayRadius
            : attempt.PinpointRadius;
        var progress = _animationElapsed.TryGetValue(attempt.Id, out var elapsed)
            ? Math.Clamp(elapsed / AnimationDuration, 0f, 1f)
            : 1f;
        progress = 1f - MathF.Pow(1f - progress, 3f);
        var radius = targetRadius * progress * mapBounds.Width / TelescienceMapGeometry.MapSize;
        var color = attempt.Mode == TelescienceInteractionMode.Ping
            ? _pingColor
            : attempt.Hit
                ? _hitColor
                : _pinpointColor;

        if (radius <= 0.5f)
        {
            handle.DrawCircle(center, 3f, color.WithAlpha(0.75f));
            return;
        }

        var circleBounds = new UIBox2(center - new Vector2(radius), center + new Vector2(radius));
        if (circleBounds.Left >= mapBounds.Left && circleBounds.Top >= mapBounds.Top
            && circleBounds.Right <= mapBounds.Right && circleBounds.Bottom <= mapBounds.Bottom)
            handle.DrawCircle(center, radius, color.WithAlpha(0.12f));

        handle.DrawCircle(center, radius, color.WithAlpha(0.55f), filled: false);
    }

    private static void DrawTarget(DrawingHandleScreen handle, UIBox2 mapBounds, Vector2 target)
    {
        var center = ToScreen(mapBounds, target);
        handle.DrawCircle(center, 8f, _targetColor.WithAlpha(0.25f));
        handle.DrawCircle(center, 5f, _targetColor);
        handle.DrawLine(center - new Vector2(12f, 0f), center + new Vector2(12f, 0f), _targetColor);
        handle.DrawLine(center - new Vector2(0f, 12f), center + new Vector2(0f, 12f), _targetColor);
    }

    private static void DrawSelectedCoordinate(DrawingHandleScreen handle, UIBox2 mapBounds, Vector2 coordinate)
    {
        var center = ToScreen(mapBounds, coordinate);
        const float HalfSize = 7f;
        handle.DrawLine(center - new Vector2(HalfSize, 0f), center + new Vector2(HalfSize, 0f), Color.White);
        handle.DrawLine(center - new Vector2(0f, HalfSize), center + new Vector2(0f, HalfSize), Color.White);
        handle.DrawCircle(center, 2f, Color.White);
    }

    private void DrawPinpointPreview(DrawingHandleScreen handle, UIBox2 mapBounds, Vector2 coordinate)
    {
        var center = ToScreen(mapBounds, coordinate);
        var radius = _pinpointRadius * mapBounds.Width / TelescienceMapGeometry.MapSize;
        if (radius <= 0f)
            return;

        var circleBounds = new UIBox2(center - new Vector2(radius), center + new Vector2(radius));
        if (circleBounds.Left >= mapBounds.Left && circleBounds.Top >= mapBounds.Top
            && circleBounds.Right <= mapBounds.Right && circleBounds.Bottom <= mapBounds.Bottom)
            handle.DrawCircle(center, radius, _pinpointColor.WithAlpha(0.12f));

        handle.DrawCircle(center, radius, _pinpointColor.WithAlpha(0.45f), filled: false);
    }

    private static Vector2 ToScreen(UIBox2 mapBounds, Vector2 coordinate)
        => mapBounds.TopLeft + (new Vector2(
            coordinate.X,
            TelescienceMapGeometry.MapSize - coordinate.Y) / TelescienceMapGeometry.MapSize) * mapBounds.Width;
}
