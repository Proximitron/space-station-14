using Content.Shared._Starlight.Hands;
using Content.Shared.Atmos.Components;
using Content.Shared.CombatMode;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Item;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Stunnable;
using Content.Shared.UserInterface;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

// ReSharper disable CheckNamespace

namespace Content.Shared._Starlight.Computers.RemoteControl;

public abstract partial class SharedRemoteControlConsoleSystem
{
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private PullingSystem _pulling = default!;
    [Dependency] private SharedStunSystem _stun = default!;

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<RemoteControlConsoleComponent>(RemoteControlUIKey.Key,
            subs =>
            {
                subs.Event<RemoteControlToggleKnockdownMessage>(OnRemoteToggleKnockdown);
                subs.Event<RemoteControlReleasePulledObjectMessage>(OnRemoteReleasePulledObject);
                subs.Event<RemoteControlInteractionMessage>(OnRemoteInteraction);
                subs.Event<RemoteControlHandMessage>(OnRemoteHand);
            });
    }

    protected bool TryGetActionableControlledEntity(Entity<RemoteControlConsoleComponent> console, EntityUid actor,
        out EntityUid remoteEntity)
    {
        remoteEntity = default;
        return console.Comp.Controller == actor
            && TryGetRemoteEntity(console.AsNullable(), out remoteEntity)
            && CanRemoteControlledEntityAct(remoteEntity);
    }

    protected virtual void UpdateUi(EntityUid uid, RemoteControlConsoleComponent component)
    {
    }

    protected virtual void MovePulledObject(EntityUid remoteEntity, EntityCoordinates coordinates)
    {
    }

    protected virtual void SetRemotePipeLayer(EntityUid item, AtmosPipeLayer pipeLayer)
    {
    }

    private void OnRemoteToggleKnockdown(EntityUid uid, RemoteControlConsoleComponent component,
        RemoteControlToggleKnockdownMessage args)
    {
        if (!TryGetActionableControlledEntity((uid, component), args.Actor, out var remoteEntity))
            return;

        _stun.ToggleKnockdown(remoteEntity);
        UpdateUi(uid, component);
    }

    private void OnRemoteReleasePulledObject(EntityUid uid, RemoteControlConsoleComponent component,
        RemoteControlReleasePulledObjectMessage args)
    {
        if (!TryGetActionableControlledEntity((uid, component), args.Actor, out var remoteEntity)
            || !TryComp<PullerComponent>(remoteEntity, out var puller)
            || puller.Pulling is not { } pulledEntity
            || !TryComp<PullableComponent>(pulledEntity, out var pullable))
            return;

        _pulling.TryStopPull(pulledEntity, pullable, user: remoteEntity);
        UpdateUi(uid, component);
    }

    private void OnRemoteInteraction(EntityUid uid, RemoteControlConsoleComponent component,
        RemoteControlInteractionMessage args)
    {
        if (!TryGetActionableControlledEntity((uid, component), args.Actor, out var remoteEntity))
            return;

        var target = args.Target is { } targetNet ? GetEntity(targetNet) : (EntityUid?) null;

        if (args.Action == RemoteControlInteractionAction.Drop)
        {
            _hands.TryDrop(remoteEntity, GetCoordinates(args.Coordinates));
            UpdateUi(uid, component);
            return;
        }

        if (args.Action == RemoteControlInteractionAction.Throw)
        {
            EntityManager.System<PredictedHandsSystem>()
                .ThrowHeldItem(remoteEntity, GetCoordinates(args.Coordinates));
            UpdateUi(uid, component);
            return;
        }

        if (args.Action == RemoteControlInteractionAction.Shoot)
        {
            var guns = EntityManager.System<SharedGunSystem>();
            if (TryComp<CombatModeComponent>(remoteEntity, out var shootCombatMode)
                && shootCombatMode.IsInCombatMode
                && guns.TryGetGun(remoteEntity, out var gun)
                && gun.Comp.UseKey != args.AltInteract
                && guns.CanShoot(gun.Comp))
            {
                if (gun.Comp.BurstActivated)
                    guns.ContinueBurst(remoteEntity, gun);
                else
                    guns.AttemptShoot(remoteEntity, gun, GetCoordinates(args.Coordinates), target);
            }

            UpdateUi(uid, component);
            return;
        }

        if (args.Action == RemoteControlInteractionAction.TryPull
            && target is { } pullTarget)
        {
            _interaction.TryPullObject(remoteEntity, pullTarget);
            UpdateUi(uid, component);
            return;
        }

        if (args.Action == RemoteControlInteractionAction.MovePulledObject)
        {
            MovePulledObject(remoteEntity, GetCoordinates(args.Coordinates));
            UpdateUi(uid, component);
            return;
        }

        if (args.ActivateInWorld && target is { } activateTarget)
        {
            _interaction.InteractionActivate(remoteEntity, activateTarget, complexInteractions: true);
            UpdateUi(uid, component);
            return;
        }

        var activeItem = TryComp<HandsComponent>(remoteEntity, out var remoteHands)
            ? _hands.GetActiveItem((remoteEntity, remoteHands))
            : null;
        if (target == remoteEntity && activeItem is { } heldItem)
        {
            if (args.AltInteract)
                _hands.TryUseItemInHand(remoteEntity, true);
            else
                _interaction.UseInHandInteraction(remoteEntity, heldItem);

            UpdateUi(uid, component);
            return;
        }

        if (target is { } item
            && TryGetRemotePickupCandidate(remoteEntity, item, out var hands, out var itemComp)
            && _hands.TryPickupAnyHand(remoteEntity, item, handsComp: hands, item: itemComp))
        {
            UpdateUi(uid, component);
            return;
        }

        if (!args.AltInteract
            && target is { } attackTarget
            && TryComp<CombatModeComponent>(remoteEntity, out var combatMode)
            && combatMode.IsInCombatMode
            && !HasComp<MapGridComponent>(attackTarget)
            && !HasComp<MapComponent>(attackTarget)
            && EntityManager.System<SharedMeleeWeaponSystem>()
                .TryGetWeapon(remoteEntity, out var weaponUid, out var weapon))
        {
            EntityManager.System<SharedMeleeWeaponSystem>()
                .TryAttemptRemoteLightAttack(remoteEntity, (weaponUid, weapon), attackTarget);
            UpdateUi(uid, component);
            return;
        }

        if (args.PipeLayer is { } pipeLayer && activeItem is { } rcdItem)
            SetRemotePipeLayer(rcdItem, pipeLayer);

        _interaction.UserInteraction(remoteEntity, GetCoordinates(args.Coordinates), target, args.AltInteract);
        UpdateUi(uid, component);
    }

    private void OnRemoteHand(EntityUid uid, RemoteControlConsoleComponent component, RemoteControlHandMessage args)
    {
        if (!TryGetActionableControlledEntity((uid, component), args.Actor, out var remoteEntity))
            return;

        switch (args.Action)
        {
            case RemoteControlHandAction.SetActive:
                _hands.TrySetActiveHand(new Entity<HandsComponent?>(remoteEntity, null), args.Hand);
                break;
            case RemoteControlHandAction.Use:
                _hands.TryUseItemInHand(remoteEntity, handName: args.Hand);
                break;
            case RemoteControlHandAction.Activate:
                _hands.TryActivateItemInHand(remoteEntity, handName: args.Hand);
                break;
            case RemoteControlHandAction.AltUse:
                _hands.TryUseItemInHand(remoteEntity, true, handName: args.Hand);
                break;
            case RemoteControlHandAction.MoveToActive:
                _hands.TryMoveHeldEntityToActiveHand(remoteEntity, args.Hand);
                break;
            case RemoteControlHandAction.Drop:
                _hands.TryDrop(remoteEntity, args.Hand);
                break;
            case RemoteControlHandAction.CycleActive:
                _hands.TryCycleActiveHand(new Entity<HandsComponent?>(remoteEntity, null));
                break;
            case RemoteControlHandAction.CycleActiveReverse:
                _hands.TryCycleActiveHandReverse(remoteEntity);
                break;
            case RemoteControlHandAction.InteractWithHand:
                _hands.TryInteractHandWithActiveHand(remoteEntity, args.Hand);
                break;
        }

        UpdateUi(uid, component);
    }

    protected bool TryGetRemotePickupCandidate(
        EntityUid remoteEntity,
        EntityUid item,
        out HandsComponent? hands,
        out ItemComponent? itemComp)
    {
        hands = null;
        itemComp = null;

        if (!TryComp<ItemComponent>(item, out var itemComponent)
            || !itemComponent.AllowDirectHandPickup
            || HasComp<ActivatableUIComponent>(item)
            || !TryComp<HandsComponent>(remoteEntity, out var handsComponent)
            || _hands.TryGetActiveItem((remoteEntity, handsComponent), out _)
            || !_interaction.InRangeAndAccessible(remoteEntity, item))
            return false;

        hands = handsComponent;
        itemComp = itemComponent;
        return true;
    }
}
