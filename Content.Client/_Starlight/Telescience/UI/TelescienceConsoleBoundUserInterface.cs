using Content.Shared._Starlight.Telescience;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

// ReSharper disable CheckNamespace
namespace Content.Client._Starlight.Telescience.UI;

[UsedImplicitly]
public sealed class TelescienceConsoleBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private TelescienceConsoleWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<TelescienceConsoleWindow>();
        _window.InteractionRequested += (coordinate, mode) =>
            SendMessage(new TelescienceInteractionMessage(coordinate, mode));
        _window.NewRoundRequested += () => SendMessage(new TelescienceNewRoundMessage());

        if (State is TelescienceConsoleBuiState state)
            _window.SetState(state);
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);
        if (state is TelescienceConsoleBuiState telescienceState)
            _window?.SetState(telescienceState);
    }

    protected override void ReceiveMessage(BoundUserInterfaceMessage message)
    {
        base.ReceiveMessage(message);
        if (message is TelescienceActionRejectedMessage rejected)
            _window?.ShowRejection(rejected.Reason);
    }
}
