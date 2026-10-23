using System.Numerics;
using Content.Shared._Starlight.Telescience;

// ReSharper disable CheckNamespace
namespace Content.Server._Starlight.Telescience;

public sealed class TelescienceRound
{
    private readonly Vector2 _target;
    private readonly List<TelescienceAttemptData> _history = [];

    public int RoundId { get; }
    public int MaxTries { get; }
    public int TriesRemaining { get; private set; }
    public bool TargetFound { get; private set; }
    public bool IsOver => TargetFound || TriesRemaining <= 0;
    public IReadOnlyList<TelescienceAttemptData> History => _history;
    public Vector2? RevealedTarget => TargetFound ? _target : null;

    public TelescienceRound(int roundId, Vector2 target, int maxTries)
    {
        RoundId = roundId;
        _target = target;
        MaxTries = maxTries;
        TriesRemaining = maxTries;
    }

    public bool TryApply(
        Vector2 coordinate,
        TelescienceInteractionMode mode,
        float overshoot,
        float pinpointRadius,
        out TelescienceActionRejection rejection)
    {
        if (!TelescienceMapGeometry.IsValidCoordinate(coordinate))
        {
            rejection = TelescienceActionRejection.InvalidCoordinate;
            return false;
        }

        if (!Enum.IsDefined(mode))
        {
            rejection = TelescienceActionRejection.InvalidMode;
            return false;
        }

        if (IsOver)
        {
            rejection = TelescienceActionRejection.RoundOver;
            return false;
        }

        float displayRadius;
        float abilityRadius;
        var hit = false;

        switch (mode)
        {
            case TelescienceInteractionMode.Ping:
                if (!float.IsFinite(overshoot) || overshoot < 0f)
                {
                    rejection = TelescienceActionRejection.InvalidConfiguration;
                    return false;
                }

                displayRadius = TelescienceMapGeometry.DisplayRadius(coordinate, _target, overshoot);
                abilityRadius = 0f;
                break;
            case TelescienceInteractionMode.Pinpoint:
                if (!float.IsFinite(pinpointRadius) || pinpointRadius < 0f)
                {
                    rejection = TelescienceActionRejection.InvalidConfiguration;
                    return false;
                }

                displayRadius = 0f;
                abilityRadius = pinpointRadius;
                hit = TelescienceMapGeometry.PinpointIntersectsTarget(coordinate, _target, pinpointRadius);
                break;
            default:
                rejection = TelescienceActionRejection.InvalidMode;
                return false;
        }

        _history.Add(new TelescienceAttemptData(
            _history.Count + 1,
            coordinate,
            mode,
            displayRadius,
            abilityRadius,
            hit));
        TriesRemaining--;
        TargetFound |= hit;
        rejection = default;
        return true;
    }
}
