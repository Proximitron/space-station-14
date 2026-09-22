using Robust.Shared.Serialization;
using Robust.Shared.GameObjects;

namespace Content.Shared._Starlight.Computers.RemoteControl;

[Serializable, NetSerializable]
public sealed class RemoteControlConsoleBuiState : BoundUserInterfaceState
{
    public required bool Connected { get; init; }
    public NetEntity? RemoteEntity { get; init; }
    public NetEntity? Controller { get; init; }
}
