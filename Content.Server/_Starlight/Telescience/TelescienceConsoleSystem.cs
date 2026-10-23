using System.Numerics;
using Content.Shared._Starlight.Telescience;
using Robust.Server.GameObjects;
using Robust.Shared.Random;

// ReSharper disable CheckNamespace
namespace Content.Server._Starlight.Telescience;

public sealed partial class TelescienceConsoleSystem : EntitySystem
{
    private const int MaxConfiguredTries = 64;
    private const float MaxConfiguredRadius = TelescienceMapGeometry.MapSize * 2f;

    [Dependency] private UserInterfaceSystem _uiSystem = null!;
    [Dependency] private IRobustRandom _random = null!;

    private readonly Dictionary<EntityUid, TelescienceRound> _rounds = new();

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<TelescienceConsoleComponent>(TelescienceConsoleUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnOpened);
            subs.Event<TelescienceInteractionMessage>(OnInteraction);
            subs.Event<TelescienceNewRoundMessage>(OnNewRound);
        });
    }

    private void OnOpened(EntityUid uid, TelescienceConsoleComponent component, BoundUIOpenedEvent args)
    {
        if (!TryGetActor(uid, args.Actor, out var actor))
            return;

        if (!TryGetRound(uid, component, out var round))
        {
            Reject(uid, actor, TelescienceActionRejection.InvalidConfiguration);
            return;
        }

        SetUiState(uid, component, round);
    }

    private void OnInteraction(EntityUid uid, TelescienceConsoleComponent component, TelescienceInteractionMessage args)
    {
        if (!TryGetActor(uid, args.Actor, out var actor))
            return;

        if (!TryGetRound(uid, component, out var round))
        {
            Reject(uid, actor, TelescienceActionRejection.InvalidConfiguration);
            return;
        }

        var overshoot = args.Mode == TelescienceInteractionMode.Ping
            ? component.MinPingOvershoot +
            (_random.NextFloat() * (component.MaxPingOvershoot - component.MinPingOvershoot))
            : 0f;

        if (!round.TryApply(args.Coordinate, args.Mode, overshoot, component.PinpointRadius, out var rejection))
        {
            Reject(uid, actor, rejection);
            return;
        }

        SetUiState(uid, component, round);
    }

    private void OnNewRound(EntityUid uid, TelescienceConsoleComponent component, TelescienceNewRoundMessage args)
    {
        if (!TryGetActor(uid, args.Actor, out var actor))
            return;

        if (!TryGetRound(uid, component, out var round))
        {
            Reject(uid, actor, TelescienceActionRejection.InvalidConfiguration);
            return;
        }

        if (!round.IsOver)
        {
            Reject(uid, actor, TelescienceActionRejection.RoundNotOver);
            return;
        }

        var nextRound = CreateRound(round.RoundId + 1, component);
        if (nextRound == null)
        {
            Reject(uid, actor, TelescienceActionRejection.InvalidConfiguration);
            return;
        }

        _rounds[uid] = nextRound;
        SetUiState(uid, component, nextRound);
    }

    [SubscribeLocalEvent]
    private void OnShutdown(EntityUid uid, TelescienceConsoleComponent component, ComponentShutdown args)
        => _rounds.Remove(uid);

    private bool TryGetActor(EntityUid uid, EntityUid? actor, out EntityUid validActor)
    {
        if (actor is { Valid: true } candidate
            && _uiSystem.IsUiOpen(uid, TelescienceConsoleUiKey.Key, candidate))
        {
            validActor = candidate;
            return true;
        }

        validActor = default;
        return false;
    }

    private bool TryGetRound(EntityUid uid, TelescienceConsoleComponent component, out TelescienceRound round)
    {
        if (!HasValidConfiguration(component))
        {
            round = null!;
            return false;
        }

        if (_rounds.TryGetValue(uid, out var existing))
        {
            round = existing;
            return true;
        }

        var created = CreateRound(1, component);
        if (created == null)
        {
            round = null!;
            return false;
        }

        round = created;
        _rounds.Add(uid, created);
        return true;
    }

    private TelescienceRound? CreateRound(int roundId, TelescienceConsoleComponent component)
    {
        if (!HasValidConfiguration(component))
            return null;

        var target = new Vector2(
            MathF.Min(MathF.Floor(_random.NextFloat() * TelescienceMapGeometry.MapSize),
                TelescienceMapGeometry.MapSize - 1f),
            MathF.Min(MathF.Floor(_random.NextFloat() * TelescienceMapGeometry.MapSize),
                TelescienceMapGeometry.MapSize - 1f));
        return new TelescienceRound(roundId, target, component.MaxTries);
    }

    private static bool HasValidConfiguration(TelescienceConsoleComponent component)
        => component.MaxTries is >= 1 and <= MaxConfiguredTries
            && float.IsFinite(component.MinPingOvershoot)
            && float.IsFinite(component.MaxPingOvershoot)
            && component.MinPingOvershoot >= 0f
            && component.MaxPingOvershoot >= component.MinPingOvershoot
            && component.MaxPingOvershoot <= MaxConfiguredRadius
            && float.IsFinite(component.PinpointRadius)
            && component.PinpointRadius >= 0f
            && component.PinpointRadius <= MaxConfiguredRadius;

    private void SetUiState(EntityUid uid, TelescienceConsoleComponent component, TelescienceRound round)
        => _uiSystem.SetUiState(uid, TelescienceConsoleUiKey.Key, new TelescienceConsoleBuiState(
            round.RoundId,
            round.MaxTries,
            round.TriesRemaining,
            component.MinPingOvershoot,
            component.MaxPingOvershoot,
            component.PinpointRadius,
            round.IsOver,
            [.. round.History],
            round.RevealedTarget));

    private void Reject(EntityUid uid, EntityUid actor, TelescienceActionRejection reason)
        => _uiSystem.ServerSendUiMessage(
            uid,
            TelescienceConsoleUiKey.Key,
            new TelescienceActionRejectedMessage(reason),
            actor);
}
