using Content.Shared.Database;

// ReSharper disable CheckNamespace
namespace Content.Server.Cargo.Systems;

public sealed partial class CargoSystem
{
    private void CompleteBountiesInEntity(EntityUid uid)
    {
        if (TryGetBountyLabel(uid, out _, out var component) &&
            component.AssociatedStationId is { } station &&
            TryGetBountyFromId(station, component.Id, out var bounty) &&
            IsBountyComplete(uid, bounty.Value))
        {
            TryRemoveBounty(station, bounty.Value, false);
            FillBountyDatabase(station);
            _adminLogger.Add(LogType.Action, LogImpact.Low, $"Bounty \"{bounty.Value.Bounty}\" (id:{bounty.Value.Id}) was fulfilled");
        }

        if (!_containerQuery.TryGetComponent(uid, out var containerMan))
            return;

        foreach (var container in containerMan.Containers.Values)
        {
            foreach (var contained in container.ContainedEntities)
            {
                CompleteBountiesInEntity(contained);
            }
        }
    }
}
