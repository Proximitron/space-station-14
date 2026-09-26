using Content.Client.Eye;
using Content.Client.Actions;
using Content.Client._DEN.QuickConstruction.UI;
using Content.Shared._Starlight.Computers.RemoteControl;
using Content.Shared._DEN.QuickConstruction.Events;
using Content.Shared.Actions.Components;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.Graphics;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Client.Player;
using System.Collections.Generic;
using System.Linq;

namespace Content.Client._Starlight.Computers.RemoteControl;

[UsedImplicitly]
public sealed class RemoteControlConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private readonly IPlayerManager _playerManager = IoCManager.Resolve<IPlayerManager>();
    private EyeLerpingSystem? _eyeLerpingSystem;
    private RemoteControlInterface _remoteControl = default!;
    private RemoteControlConsoleWindow? _window;
    private EntityUid? _remoteEntity;
    private QuickConstructionBoundUserInterface? _remoteQuickConstruction;
    private EntityUid? _remoteQuickConstructionItem;
    private readonly List<NetEntity> _remoteActionEntities = new();
    private readonly List<EntityUid> _shownActionEntities = new();
    private EntityUid? _selectedAction;
    private EntityUid? _shownSelectedAction;
    protected override void Open()
    {
        if (IsOpened && _window is { Disposed: false })
            return;

        base.Open();
        _eyeLerpingSystem = EntMan.System<EyeLerpingSystem>();
        _remoteControl = EntMan.System<RemoteControlInterface>();
        _remoteControl.InteractionRequested += OnRemoteMenuInteraction;
        _window = new RemoteControlConsoleWindow(EntMan.System<ActionsSystem>());
        _window.ToggleControl += ToggleControl;
        _window.RemoteInteractionPressed += RemoteInteractionPressed;
        _window.RemoteActionPressed += RemoteActionPressed;
        _window.RemoteHandPressed += RemoteHandPressed;
        _window.RemoteInventoryPressed += RemoteInventoryPressed;
        _window.InitializeViewport();
        _window.OnClose += OnWindowClosed;
        _window.OpenCentered();
    }

    private void OnWindowClosed()
    {
        Close();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not RemoteControlConsoleBuiState remoteState || _window == null)
            return;

        if (remoteState.RemoteEntity is { } entity && EntMan.TryGetEntity(entity, out EntityUid? remoteEntity)
            && remoteEntity is { } remoteUid
            && EntMan.TryGetComponent<EyeComponent>(remoteUid, out var eye))
        {
            if (_remoteEntity != remoteUid)
            {
                if (_remoteEntity is { } oldEntity)
                    _eyeLerpingSystem?.RemoveEye(oldEntity);

                _eyeLerpingSystem?.AddEye(remoteUid);
                _remoteEntity = remoteUid;
            }

            _window.SetRemoteEye((IEye) eye.Eye);
            _window.SetRemoteEntity(remoteUid);
        }
        else
        {
            if (_remoteEntity is { } oldEntity)
            {
                _eyeLerpingSystem?.RemoveEye(oldEntity);
                _remoteEntity = null;
            }

            _window.SetRemoteEye(null);
            _window.SetRemoteEntity(null);
        }

        _window.SetConnected(remoteState.Connected);
        var controlling = remoteState.Controller is { } controller
            && EntMan.TryGetEntity(controller, out var controllerEntity)
            && controllerEntity == _playerManager.LocalEntity;

        _window.SetControlState(controlling, remoteState.Controller != null);
        _remoteControl.SetControlledEntity(controlling ? _remoteEntity : null);

        _remoteActionEntities.Clear();
        _remoteActionEntities.AddRange(remoteState.Actions);
        _selectedAction = remoteState.SelectedAction is { } selected
            && EntMan.TryGetEntity(selected, out var selectedEntity)
            ? selectedEntity
            : null;
        RefreshActions();
        _window.SetHands(remoteState.Hands);
        _window.SetInventory(remoteState.Inventory);

        if (remoteState.QuickConstructionItem is { } quickItem
            && EntMan.TryGetEntity(quickItem, out EntityUid? quickItemEntity)
            && quickItemEntity is { } quickItemUid)
        {
            if (_remoteQuickConstructionItem != quickItemUid)
            {
                _remoteQuickConstruction?.Dispose();
                _remoteQuickConstruction = new QuickConstructionBoundUserInterface(quickItemUid, QuickConstructionUiKey.Key);
                _remoteQuickConstructionItem = quickItemUid;
                _remoteQuickConstruction.OpenRemote();
            }
        }
        else if (_remoteQuickConstruction is not null)
        {
            _remoteQuickConstruction.Dispose();
            _remoteQuickConstruction = null;
            _remoteQuickConstructionItem = null;
        }
    }

    public override void Update()
    {
        base.Update();
        RefreshActions();
    }

    private void RefreshActions()
    {
        if (_window == null)
            return;

        var actions = new List<EntityUid>(_remoteActionEntities.Count);
        foreach (var action in _remoteActionEntities)
        {
            if (EntMan.TryGetEntity(action, out var actionEntity)
                && actionEntity is { } uid
                && EntMan.HasComponent<ActionComponent>(uid))
                actions.Add(uid);
        }

        if (actions.Count == _shownActionEntities.Count
            && actions.SequenceEqual(_shownActionEntities)
            && _selectedAction == _shownSelectedAction)
            return;

        _shownActionEntities.Clear();
        _shownActionEntities.AddRange(actions);
        _shownSelectedAction = _selectedAction;
        _window.SetActions(actions, _selectedAction);
    }

    private void ToggleControl() => SendMessage(new RemoteControlToggleMessage());

    private void RemoteActionPressed(EntityUid action)
        => SendMessage(new RemoteControlActionMessage { Action = EntMan.GetNetEntity(action) });

    private void RemoteInteractionPressed(NetCoordinates coordinates, EntityUid? target, bool altInteract,
        RemoteControlInteractionAction action)
        => SendMessage(new RemoteControlInteractionMessage
        {
            Coordinates = coordinates,
            Target = target is { } entity ? EntMan.GetNetEntity(entity) : null,
            AltInteract = altInteract,
            Action = action,
        });

    private void OnRemoteMenuInteraction(EntityUid target, bool altInteract, RemoteControlInteractionAction action)
    {
        if (_remoteEntity is null)
            return;

        SendMessage(new RemoteControlInteractionMessage
        {
            Coordinates = EntMan.GetNetCoordinates(EntMan.GetComponent<TransformComponent>(target).Coordinates),
            Target = EntMan.GetNetEntity(target),
            AltInteract = altInteract,
            Action = action,
        });
    }

    private void RemoteHandPressed(string hand, RemoteControlHandAction action)
        => SendMessage(new RemoteControlHandMessage { Hand = hand, Action = action });

    private void RemoteInventoryPressed(string slot, RemoteControlInventoryAction action)
        => SendMessage(new RemoteControlInventoryMessage { Slot = slot, Action = action });

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _remoteControl.InteractionRequested -= OnRemoteMenuInteraction;
            _remoteControl.SetControlledEntity(null);

            if (_remoteEntity is { } remoteEntity)
                _eyeLerpingSystem?.RemoveEye(remoteEntity);

            _remoteQuickConstruction?.Dispose();
            _remoteQuickConstruction = null;
            _remoteQuickConstructionItem = null;

            if (_window is { } window)
            {
                window.OnClose -= OnWindowClosed;
                window.ToggleControl -= ToggleControl;
                window.RemoteInteractionPressed -= RemoteInteractionPressed;
                window.RemoteActionPressed -= RemoteActionPressed;
                window.RemoteHandPressed -= RemoteHandPressed;
                window.RemoteInventoryPressed -= RemoteInventoryPressed;
                window.SetRemoteEye(null);
                window.SetRemoteEntity(null);
                window.SetConnected(false);
                window.SetControlState(false, false);
                if (window.IsOpen)
                    window.Close();

                window.Dispose();
            }
        }
    }
}
