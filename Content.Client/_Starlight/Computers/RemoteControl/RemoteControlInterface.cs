using Content.Shared.Hands.EntitySystems;
using Content.Shared._Starlight.Computers.RemoteControl;
using Robust.Shared.GameObjects;

namespace Content.Client._Starlight.Computers.RemoteControl;

public sealed partial class RemoteControlInterface : EntitySystem
{
    public EntityUid? ControlledEntity { get; private set; }
    public event Action<EntityUid, bool, RemoteControlInteractionAction>? InteractionRequested;

    public void SetControlledEntity(EntityUid? entity)
        => ControlledEntity = entity;

    public bool IsControlledEntityOrActiveItem(EntityUid entity)
        => ControlledEntity is { } remoteEntity
            && (remoteEntity == entity
                || (EntityManager.System<SharedHandsSystem>().TryGetActiveItem(remoteEntity, out var heldItem)
                    && heldItem == entity));

    public EntityUid? GetRadialMenuTrackingEntity(EntityUid owner, EntityUid? localEntity)
        => ControlledEntity is null
            ? owner
            : IsControlledEntityOrActiveItem(owner)
                ? localEntity ?? owner
                : null;

    public void RequestInteraction(EntityUid target, bool altInteract, RemoteControlInteractionAction action = RemoteControlInteractionAction.Interact)
        => InteractionRequested?.Invoke(target, altInteract, action);
}
