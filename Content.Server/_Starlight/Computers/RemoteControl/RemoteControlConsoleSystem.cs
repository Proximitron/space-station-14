using Content.Shared._Starlight.Computers.RemoteControl;
using Content.Shared._Starlight.Silicons;
using Content.Shared.Body.Organ;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Movement.Components;
using Content.Shared.Movement.Systems;
using Content.Shared.ActionBlocker;
using Content.Shared.Bed.Sleep;
using Content.Shared.Mobs;
using Content.Shared.Silicons.Borgs.Components;
using Robust.Server.Containers;
using Content.Shared.UserInterface;
using Robust.Server.GameObjects;
using Robust.Server.GameStates;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Player;

namespace Content.Server._Starlight.Computers.RemoteControl;

public sealed partial class RemoteControlConsoleSystem : EntitySystem
{
    [Dependency] private UserInterfaceSystem _ui = default!;
    [Dependency] private SharedMoverController _mover = default!;
    [Dependency] private ContainerSystem _container = default!;
    [Dependency] private PvsOverrideSystem _pvsOverride = default!;
    [Dependency] private SharedViewSubscriberSystem _viewSubscriber = default!;
    [Dependency] private ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        Subs.BuiEvents<RemoteControlConsoleComponent>(RemoteControlUIKey.Key,
            subs =>
            {
                subs.Event<BoundUIClosedEvent>(OnUiClosed);
                subs.Event<RemoteControlToggleMessage>(OnToggleControl);
            });
        _playerManager.PlayerStatusChanged += OnPlayerStatusChanged;
        SubscribeLocalEvent<RemoteControlConsoleComponent, ComponentShutdown>(OnConsoleShutdown);
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<SleepingComponent, SleepStateChangedEvent>(OnSleepStateChanged);
    }

    public override void Shutdown()
        => _playerManager.PlayerStatusChanged -= OnPlayerStatusChanged;

    [SubscribeLocalEvent]
    private void OnNewLink(Entity<RemoteControlConsoleComponent> entity, ref NewLinkEvent args)
    {
        if (args.Source != entity.Owner || !HasComp<RemoteControlBrainComponent>(args.Sink))
            return;

        if (entity.Comp.RemoteBrain is not null && TryGetRemoteEntity(entity.Comp, out var oldRemoteEntity))
            CleanupUsers(entity, oldRemoteEntity);

        entity.Comp.RemoteBrain = args.Sink;
        Dirty(entity);
    }

    [SubscribeLocalEvent]
    private void OnUiOpen(EntityUid uid, RemoteControlConsoleComponent component, AfterActivatableUIOpenEvent args)
    {
        if (!TryGetRemoteEntity(component, out var remoteEntity))
        {
            _ui.SetUiState(uid, RemoteControlUIKey.Key, new RemoteControlConsoleBuiState
            {
                Connected = false,
                RemoteEntity = null,
            });
            return;
        }

        component.Users.Add(args.User);
        if (_playerManager.TryGetSessionByEntity(args.User, out var session))
        {
            _pvsOverride.AddSessionOverride(remoteEntity, session);
            _viewSubscriber.AddViewSubscriber(remoteEntity, session);
        }
        if (component.Controller == null)
            SetController(uid, component, args.User, remoteEntity);

        _ui.SetUiState(uid, RemoteControlUIKey.Key, new RemoteControlConsoleBuiState
        {
            Connected = true,
            RemoteEntity = GetNetEntity(remoteEntity),
            Controller = component.Controller is { } controller ? GetNetEntity(controller) : null,
        });
    }

    private void OnToggleControl(EntityUid uid, RemoteControlConsoleComponent component, RemoteControlToggleMessage args)
    {
        if (!component.Users.Contains(args.Actor)
            || !TryGetRemoteEntity(component, out var remoteEntity))
            return;

        if (component.Controller == args.Actor)
        {
            SetController(uid, component, null, remoteEntity);
            return;
        }

        if (component.Controller != null || !_actionBlocker.CanConsciouslyPerformAction(args.Actor))
            return;

        SetController(uid, component, args.Actor, remoteEntity);
    }

    private void OnUiClosed(EntityUid uid, RemoteControlConsoleComponent component, BoundUIClosedEvent args)
    {
        if (!args.Actor.Valid)
            return;

        if (!component.Users.Remove(args.Actor))
            return;

        if (component.Controller == args.Actor)
            SetController(uid, component, null, TryGetRemoteEntity(component, out var controlledEntity) ? controlledEntity : null);

        if (TryGetRemoteEntity(component, out var remoteEntity)
            && _playerManager.TryGetSessionByEntity(args.Actor, out var session))
        {
            _pvsOverride.RemoveSessionOverride(remoteEntity, session);
            _viewSubscriber.RemoveViewSubscriber(remoteEntity, session);
        }

        if (!TryComp<RelayInputMoverComponent>(args.Actor, out var relay))
            return;

        if (!TryGetBody(component, out var body) || relay.RelayEntity == body)
            RemComp(args.Actor, relay);
    }

    [SubscribeLocalEvent]
    private void OnPortDisconnected(Entity<RemoteControlConsoleComponent> entity, ref PortDisconnectedEvent args)
    {
        if (args.Source != entity.Owner || entity.Comp.RemoteBrain != args.Sink || args.Port != "RemoteControl")
            return;

        if (TryGetRemoteEntity(entity.Comp, out var remoteEntity))
        {
            CleanupUsers(entity, remoteEntity);
        }

        entity.Comp.RemoteBrain = null;
        Dirty(entity);
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        if (args.NewStatus != SessionStatus.Disconnected)
            return;

        foreach (var console in EntityQuery<RemoteControlConsoleComponent>())
        {
            EntityUid? disconnectedUser = null;
            foreach (var user in console.Users)
            {
                if (!_playerManager.TryGetSessionByEntity(user, out var session) || session != args.Session)
                    continue;

                if (TryGetRemoteEntity(console, out var remoteEntity))
                {
                    if (console.Controller == user)
                        SetController(console.Owner, console, null, remoteEntity);

                    _pvsOverride.RemoveSessionOverride(remoteEntity, session);
                    _viewSubscriber.RemoveViewSubscriber(remoteEntity, session);
                }

                disconnectedUser = user;
                break;
            }

            if (disconnectedUser is { } userToRemove)
                console.Users.Remove(userToRemove);
        }
    }

    private void OnMobStateChanged(MobStateChangedEvent args)
        => ReleaseControlIfInvalid(args.Target);

    private void OnSleepStateChanged(Entity<SleepingComponent> ent, ref SleepStateChangedEvent args)
    {
        if (args.FellAsleep)
            ReleaseControlIfInvalid(ent.Owner);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        foreach (var console in EntityQuery<RemoteControlConsoleComponent>())
        {
            if (console.Controller is not { } controller
                || _actionBlocker.CanConsciouslyPerformAction(controller)
                || !TryGetRemoteEntity(console, out var remoteEntity))
                continue;

            SetController(console.Owner, console, null, remoteEntity);
        }
    }

    private void ReleaseControlIfInvalid(EntityUid user)
    {
        foreach (var console in EntityQuery<RemoteControlConsoleComponent>())
        {
            if (console.Controller != user || !TryGetRemoteEntity(console, out var remoteEntity))
                continue;

            SetController(console.Owner, console, null, remoteEntity);
        }
    }

    private void OnConsoleShutdown(Entity<RemoteControlConsoleComponent> entity, ref ComponentShutdown args)
    {
        if (!TryGetRemoteEntity(entity.Comp, out var remoteEntity))
            return;

        CleanupUsers(entity, remoteEntity);
    }

    private void CleanupUsers(Entity<RemoteControlConsoleComponent> entity, EntityUid remoteEntity)
    {
        entity.Comp.Controller = null;
        foreach (var user in entity.Comp.Users)
        {
            RemovePvsOverride(remoteEntity, user);

            if (TryComp<RelayInputMoverComponent>(user, out var relay)
                && relay.RelayEntity == remoteEntity)
            {
                RemComp(user, relay);
            }
        }

        entity.Comp.Users.Clear();
    }

    private void SetController(EntityUid consoleUid, RemoteControlConsoleComponent component, EntityUid? controller,
        EntityUid? remoteEntity)
    {
        if (component.Controller is { } oldController && TryComp<RelayInputMoverComponent>(oldController, out var oldRelay))
            RemComp(oldController, oldRelay);

        component.Controller = controller;

        if (controller is { } newController && remoteEntity is { } remote)
            _mover.SetRelay(newController, remote);

        if (remoteEntity is { } entity)
            _ui.SetUiState(consoleUid, RemoteControlUIKey.Key, new RemoteControlConsoleBuiState
            {
                Connected = true,
                RemoteEntity = GetNetEntity(entity),
                Controller = controller is { } user ? GetNetEntity(user) : null,
            });
    }

    private void RemovePvsOverride(EntityUid remoteEntity, EntityUid user)
    {
        if (_playerManager.TryGetSessionByEntity(user, out var session))
        {
            _pvsOverride.RemoveSessionOverride(remoteEntity, session);
            _viewSubscriber.RemoveViewSubscriber(remoteEntity, session);
        }
    }

    private bool TryGetBody(RemoteControlConsoleComponent component, out EntityUid body)
    {
        body = default;
        if (component.RemoteBrain is not { } brain)
            return false;

        if (TryComp<OrganComponent>(brain, out var organ) && organ.Body is { } organBody)
        {
            body = organBody;
            return true;
        }

        return _container.TryGetContainingContainer(brain, out var container)
            && container.ID == "borg_brain"
            && TryComp<BorgChassisComponent>(container.Owner, out _)
            && (body = container.Owner) != default;
    }

    private bool TryGetRemoteEntity(RemoteControlConsoleComponent component, out EntityUid remoteEntity)
    {
        remoteEntity = default;
        if (component.RemoteBrain is not { } brain)
            return false;

        if (TryGetBody(component, out var body))
        {
            remoteEntity = body;
            return true;
        }

        if (HasComp<EyeComponent>(brain))
        {
            remoteEntity = brain;
            return true;
        }

        return false;
    }
}
