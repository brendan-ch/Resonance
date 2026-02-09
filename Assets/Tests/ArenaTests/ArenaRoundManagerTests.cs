using System.Collections;
using NUnit.Framework;
using Resonance.Assemblies.MatchStat;
using Resonance.Assemblies.Arena;

public class ArenaRoundManagerTests
{
    private MatchStatTracker statTracker;
    private ArenaRoundManager roundManager;

    [SetUp]
    public void Setup()
    {
        statTracker = new();
        roundManager = new(statTracker);
    }

    #region OnPlayerKilled
    [Test]
    public void OnPlayerKilled_UpdatesLeaderIfRoundStarted()
    {
        roundManager.StartMatch();

        ulong killerId = 1;

        statTracker.RecordKill(killerId, 2);
        statTracker.RecordKill(killerId, 3);

        Assert.AreEqual(killerId, roundManager.CurrentLeader);
    }

    [Test]
    public void OnPlayerKilled_DoesNotUpdateLeaderIfRoundNotActive()
    {
        ulong killerId = 1;

        statTracker.RecordKill(killerId, 2);
        statTracker.RecordKill(killerId, 3);

        Assert.IsNull(roundManager.CurrentLeader);
    }

    [Test]
    public void OnPlayerKilled_EndMatchIfEliminationThresholdPassed()
    {
        roundManager.StartMatch();
        ulong killerId = 1;

        for (int i = 0; i < roundManager.EliminationsToWin; i++)
        {
            statTracker.RecordKill(killerId, 2);
        }

        Assert.AreEqual(true, roundManager.IsMatchEnded);
    }
    #endregion
}
