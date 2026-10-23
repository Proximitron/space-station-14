using System.Numerics;
using Robust.Shared.Serialization;

// ReSharper disable CheckNamespace
namespace Content.Shared._Starlight.Telescience;

[Serializable, NetSerializable]
public enum TelescienceConsoleUiKey : byte
{
    Key
}

[Serializable, NetSerializable]
public enum TelescienceInteractionMode : byte
{
    Ping,
    Pinpoint
}

[Serializable, NetSerializable]
public enum TelescienceActionRejection : byte
{
    InvalidCoordinate,
    InvalidMode,
    RoundNotOver,
    RoundOver,
    InvalidConfiguration
}

[Serializable, NetSerializable]
public sealed class TelescienceAttemptData(
    int id,
    Vector2 coordinate,
    TelescienceInteractionMode mode,
    float displayRadius,
    float pinpointRadius,
    bool hit)
{
    public readonly int Id = id;
    public readonly Vector2 Coordinate = coordinate;
    public readonly TelescienceInteractionMode Mode = mode;
    public readonly float DisplayRadius = displayRadius;
    public readonly float PinpointRadius = pinpointRadius;
    public readonly bool Hit = hit;
}

[Serializable, NetSerializable]
public sealed class TelescienceConsoleBuiState(
    int roundId,
    int maxTries,
    int triesRemaining,
    float minPingOvershoot,
    float maxPingOvershoot,
    float pinpointRadius,
    bool roundOver,
    List<TelescienceAttemptData> history,
    Vector2? revealedTarget) : BoundUserInterfaceState
{
    public readonly int RoundId = roundId;
    public readonly int MaxTries = maxTries;
    public readonly int TriesRemaining = triesRemaining;
    public readonly float MinPingOvershoot = minPingOvershoot;
    public readonly float MaxPingOvershoot = maxPingOvershoot;
    public readonly float PinpointRadius = pinpointRadius;
    public readonly bool RoundOver = roundOver;
    public readonly List<TelescienceAttemptData> History = history;
    public readonly Vector2? RevealedTarget = revealedTarget;
}

[Serializable, NetSerializable]
public sealed class TelescienceInteractionMessage(
    Vector2 coordinate,
    TelescienceInteractionMode mode) : BoundUserInterfaceMessage
{
    public readonly Vector2 Coordinate = coordinate;
    public readonly TelescienceInteractionMode Mode = mode;
}

[Serializable, NetSerializable]
public sealed class TelescienceNewRoundMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class TelescienceActionRejectedMessage(TelescienceActionRejection reason) : BoundUserInterfaceMessage
{
    public readonly TelescienceActionRejection Reason = reason;
}
