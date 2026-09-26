using Content.Client._Starlight.Computers.RemoteControl;
using Content.Shared.Hands.EntitySystems;
using Robust.Shared.GameObjects;

namespace Content.Client._Starlight.Computers.RemoteControl;

public sealed class RemoteControlUiRangeSystem : EntitySystem
{
    private RemoteControlInterface _remoteControl = default!;
    private SharedHandsSystem _hands = default!;

    public override void Initialize()
    {
        base.Initialize();
        _remoteControl = EntityManager.System<RemoteControlInterface>();
        _hands = EntityManager.System<SharedHandsSystem>();
        SubscribeLocalEvent<BoundUserInterfaceCheckRangeEvent>(OnCheckRange);
    }

    private void OnCheckRange(ref BoundUserInterfaceCheckRangeEvent args)
    {
        if (_remoteControl.ControlledEntity is not { } remoteEntity
            || (args.Target != remoteEntity
                && (!_hands.TryGetActiveItem(remoteEntity, out var heldItem)
                    || heldItem != args.Target)))
            return;

        args.Result = BoundUserInterfaceRangeResult.Pass;
    }
}
