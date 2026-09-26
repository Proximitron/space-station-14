using Content.Server._Starlight.Computers.RemoteControl;

namespace Content.Server.Verbs;

public sealed partial class VerbSystem
{
    [Dependency] private RemoteControlConsoleSystem _remoteControl = default!;

    protected override EntityUid? ResolveVerbUser(EntityUid attachedEntity)
        => _remoteControl.TryGetControlledEntity(attachedEntity, out var remoteEntity)
            ? remoteEntity
            : attachedEntity;
}
