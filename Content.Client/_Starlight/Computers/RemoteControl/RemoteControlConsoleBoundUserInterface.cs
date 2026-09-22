using Content.Client.Eye;
using Content.Shared._Starlight.Computers.RemoteControl;
using JetBrains.Annotations;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Shared.GameObjects;
using Robust.Shared.Graphics;
using Robust.Shared.IoC;
using Robust.Client.Player;

namespace Content.Client._Starlight.Computers.RemoteControl;

[UsedImplicitly]
public sealed class RemoteControlConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private readonly IPlayerManager _playerManager = IoCManager.Resolve<IPlayerManager>();
    private EyeLerpingSystem? _eyeLerpingSystem;
    private RemoteControlConsoleWindow? _window;
    private EntityUid? _remoteEntity;

    protected override void Open()
    {
        base.Open();
        _eyeLerpingSystem = EntMan.System<EyeLerpingSystem>();
        _window = new RemoteControlConsoleWindow();
        _window.ToggleControl += ToggleControl;
        _window.InitializeViewport();
        _window.OnClose += Close;
        _window.OpenCentered();
        if (State is { } state)
            UpdateState(state);
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
        }
        else
        {
            if (_remoteEntity is { } oldEntity)
            {
                _eyeLerpingSystem?.RemoveEye(oldEntity);
                _remoteEntity = null;
            }

            _window.SetRemoteEye(null);
        }

        _window.SetConnected(remoteState.Connected);
        var controlling = remoteState.Controller is { } controller
            && EntMan.TryGetEntity(controller, out var controllerEntity)
            && controllerEntity == _playerManager.LocalEntity;
        _window.SetControlState(controlling, remoteState.Controller != null);
    }

    private void ToggleControl() => SendMessage(new RemoteControlToggleMessage());

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            if (_remoteEntity is { } remoteEntity)
                _eyeLerpingSystem?.RemoveEye(remoteEntity);

            _window?.Dispose();
        }
    }
}
