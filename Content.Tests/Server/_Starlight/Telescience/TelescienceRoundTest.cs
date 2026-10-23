using System.Numerics;
using Content.Server._Starlight.Telescience;
using Content.Shared._Starlight.Telescience;
using NUnit.Framework;

// ReSharper disable CheckNamespace
namespace Content.Tests.Server._Starlight.Telescience;

[TestFixture]
public sealed class TelescienceRoundTest
{
    [Test]
    public void EachAttemptConsumesOneTryAndIsSharedInHistory()
    {
        var round = new TelescienceRound(1, new Vector2(500f, 500f), 2);

        Assert.That(round.TryApply(new Vector2(100f, 100f), TelescienceInteractionMode.Ping, 10f, 32f, out _), Is.True);
        Assert.That(round.TriesRemaining, Is.EqualTo(1));
        Assert.That(round.History, Has.Count.EqualTo(1));
        Assert.That(round.History[0].Coordinate, Is.EqualTo(new Vector2(100f, 100f)));
        Assert.That(round.History[0].DisplayRadius, Is.EqualTo(TelescienceMapGeometry.Distance(
            new Vector2(100f, 100f), new Vector2(500f, 500f)) + 10f));

        Assert.That(round.TryApply(new Vector2(0f, 0f), TelescienceInteractionMode.Ping, 0f, 32f, out _), Is.True);
        Assert.That(round.TriesRemaining, Is.Zero);
        Assert.That(round.IsOver, Is.True);
        Assert.That(round.TryApply(new Vector2(20f, 20f), TelescienceInteractionMode.Ping, 0f, 32f, out var rejection),
            Is.False);
        Assert.That(rejection, Is.EqualTo(TelescienceActionRejection.RoundOver));
    }

    [Test]
    public void PinpointRevealsOnlyWhenItsAreaContainsTheTarget()
    {
        var target = new Vector2(300f, 400f);
        var round = new TelescienceRound(1, target, 3);

        Assert.That(round.RevealedTarget, Is.Null);
        Assert.That(round.TryApply(Vector2.Zero, TelescienceInteractionMode.Pinpoint, 0f, 32f, out _), Is.True);
        Assert.That(round.History[0].Hit, Is.False);
        Assert.That(round.RevealedTarget, Is.Null);

        Assert.That(round.TryApply(target, TelescienceInteractionMode.Pinpoint, 0f, 0f, out _), Is.True);
        Assert.That(round.History[1].Hit, Is.True);
        Assert.That(round.RevealedTarget, Is.EqualTo(target));
        Assert.That(round.IsOver, Is.True);
    }

    [Test]
    public void InvalidCoordinatesDoNotConsumeAnAttempt()
    {
        var round = new TelescienceRound(1, Vector2.Zero, 2);

        Assert.That(round.TryApply(new Vector2(TelescienceMapGeometry.MapSize, 10f),
            TelescienceInteractionMode.Ping, 0f, 32f, out var rejection), Is.False);
        Assert.That(rejection, Is.EqualTo(TelescienceActionRejection.InvalidCoordinate));
        Assert.That(round.TriesRemaining, Is.EqualTo(2));
        Assert.That(round.History, Is.Empty);
    }
}
