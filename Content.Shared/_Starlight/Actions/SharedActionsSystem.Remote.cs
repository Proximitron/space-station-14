using Content.Shared.Actions.Events;

// ReSharper disable once CheckNamespace
namespace Content.Shared.Actions;

public abstract partial class SharedActionsSystem
{
    // allows the remote-control system to execute an action for its controlled body.
    public bool TryPerformAction(EntityUid user, EntityUid action)
        => TryPerformAction(new RequestPerformActionEvent(GetNetEntity(action)), user);
}
