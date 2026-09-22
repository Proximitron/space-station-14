using Robust.Shared.Serialization;

namespace Content.Shared._Starlight.Computers.RemoteControl;

[Serializable, NetSerializable]
public enum RemoteControlDirection : byte
{
    Up,
    Down,
    Left,
    Right,
}

[Serializable, NetSerializable]
public sealed class RemoteControlMoveMessage : BoundUserInterfaceMessage
{
    public required RemoteControlDirection Direction { get; init; }
    public required bool Pressed { get; init; }
}

[Serializable, NetSerializable]
public sealed class RemoteControlToggleMessage : BoundUserInterfaceMessage;
