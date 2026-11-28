using Content.Server.NPC.HTN;
using Content.Shared.Mind.Components;

// ReSharper disable CheckNamespace

namespace Content.Server.NPC.Systems;

public sealed partial class NPCSystem
{
    /// <summary>
    /// Sleeps the NPC when remote control is acquired and wakes it on release only when it is eligible.
    /// </summary>
    public void OnRemoteControlChanged(Entity<HTNComponent?> entity, bool controlled)
    {
        if (!Resolve(entity.Owner, ref entity.Comp, false))
            return;

        if (controlled)
        {
            SleepNPC(entity.Owner, entity.Comp);
            return;
        }

        if (_mobState.IsIncapacitated(entity.Owner) || TerminatingOrDeleted(entity.Owner))
            return;

        if (TryComp<MindContainerComponent>(entity.Owner, out var mindContainer) && mindContainer.HasMind)
            return;

        WakeNPC(entity.Owner, entity.Comp);
    }
}
