// ReSharper disable CheckNamespace

using Content.Shared._Starlight.Storage;
using Content.Shared._Starlight.Computers.RemoteControl;
using Content.Shared.Storage.Components;
using Content.Shared.Verbs;

namespace Content.Shared.Storage.EntitySystems;

public abstract partial class SharedStorageSystem
{
    [Dependency] private SharedRemoteControlConsoleSystem _remoteControl = default!;
    [Dependency] private SharedPrivateStorageSystem _privateStorage = default!;

    /// <summary>
    /// Mirrors a storage UI to the controller while temporarily setting the nested-storage check flag.
    /// </summary>
    public void OpenRemoteStorageUiMirror(Entity<StorageComponent?> storage, EntityUid controller)
    {
        if (!Resolve(storage.Owner, ref storage.Comp, false))
            return;

        var wasNestedCheck = _nestedCheck;
        _nestedCheck = true;
        try
        {
            UI.OpenUi((storage.Owner, null), StorageComponent.StorageUiKey.Key, controller);
        }
        finally
        {
            _nestedCheck = wasNestedCheck;
        }
    }

    private partial bool HandleStarlightStrippableStorageVerb(
        EntityUid uid,
        StorageComponent component,
        GetVerbsEvent<ActivationVerb> args) =>
        _privateStorage.TryAddStrippableStorageVerb(uid, component, args);
}
