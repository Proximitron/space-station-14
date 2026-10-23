using Content.Shared.Destructible;
using Content.Shared.Mining.Components;
using Robust.Shared.Random;

namespace Content.Server._Starlight.Mining;

public sealed partial class StoneDropSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;

    private const float StoneDropChance = 0.2f;

    [SubscribeLocalEvent]
    private void OnRockDestroyed(Entity<MiningScannerViewableComponent> entity, ref DestructionEventArgs _)
    {
        if (!_random.Prob(StoneDropChance))
            return;

        var coordinates = Transform(entity.Owner).Coordinates;
        Spawn("MaterialStone1", coordinates.Offset(_random.NextVector2(0.2f)));
    }
}
