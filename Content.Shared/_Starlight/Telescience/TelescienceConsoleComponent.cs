// ReSharper disable CheckNamespace

namespace Content.Shared._Starlight.Telescience;

[RegisterComponent]
public sealed partial class TelescienceConsoleComponent : Component
{
    [DataField] public int MaxTries = 8;

    [DataField] public float MinPingOvershoot = 12f;

    [DataField] public float MaxPingOvershoot = 64f;

    [DataField] public float PinpointRadius = 32f;
}
