using System.Collections;
using NUnit.Framework;
using Resonance.Assemblies.MatchStat;
using Resonance.Assemblies.Arena;
using System.Threading.Tasks;

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

    #region EndMatch
    [Test]
    public async Task EndMatch_FiresOnMatchEndEvent()
    {
        roundManager.StartMatch();

        ulong? capturedWinner = null;
        int eventCallCount = 0;

        roundManager.OnMatchEnd += (winner) =>
        {
            capturedWinner = winner;
            eventCallCount++;
        };

        ulong expectedWinner = 1;
        await roundManager.EndMatch(expectedWinner);

        Assert.AreEqual(1, eventCallCount);
        Assert.AreEqual(expectedWinner, capturedWinner);
    }

    [Test]
    public async Task EndMatch_UpdatesTheMatchStatus()
    {
        roundManager.StartMatch();
        await roundManager.EndMatch(1);

        Assert.AreEqual(false, roundManager.IsMatchActive);
        Assert.AreEqual(true, roundManager.IsMatchEnded);
    }


    [Test]
    public async Task EndMatch_AutoStartsNextRoundIfConfigured()
    {
        var config = new ArenaRoundManager.ArenaRoundManagerConfig
        {
            eliminationsToWin = 10,
            autoStartNextMatch = true,
            matchEndDelaySeconds = 1
        };
        var autoStartManager = new ArenaRoundManager(statTracker, config);
        autoStartManager.StartMatch();
        await autoStartManager.EndMatch(1);

        Assert.AreEqual(true, autoStartManager.IsMatchActive);
        Assert.AreEqual(false, autoStartManager.IsMatchEnded);
    }

    [Test]
    public async Task EndMatch_DoesNothingIfMatchNotActive()
    {
        int eventCallCount = 0;
        roundManager.OnMatchEnd += (_) => eventCallCount++;

        await roundManager.EndMatch(1);

        Assert.AreEqual(0, eventCallCount);
        Assert.AreEqual(false, roundManager.IsMatchActive);
        Assert.AreEqual(false, roundManager.IsMatchEnded);
    }
    #endregion

    #region StartMatch
    [Test]
    public void StartMatch_UpdatesTheMatchStatus()
    {
        roundManager.StartMatch();

        Assert.AreEqual(true, roundManager.IsMatchActive);
        Assert.AreEqual(false, roundManager.IsMatchEnded);
    }


    [Test]
    public void StartMatch_FiresOnMatchStartEvent()
    {
        int eventCallCount = 0;
        roundManager.OnMatchStart += () => eventCallCount++;

        roundManager.StartMatch();

        Assert.AreEqual(1, eventCallCount);
    }

    [Test]
    public void StartMatch_DoesNothingIfMatchAlreadyActive()
    {
        roundManager.StartMatch();

        int eventCallCount = 0;
        roundManager.OnMatchStart += () => eventCallCount++;

        roundManager.StartMatch();

        Assert.AreEqual(0, eventCallCount);
        Assert.AreEqual(true, roundManager.IsMatchActive);
    }

    [Test]
    public void StartMatch_ResetsMatchStatTracker()
    {
        statTracker.RecordKill(1, 2);
        statTracker.RecordKill(3, 4);

        roundManager.StartMatch();

        var allStats = statTracker.GetAllStats();
        foreach (var kvp in allStats)
        {
            Assert.AreEqual(new PlayerMatchStats(), kvp.Value);
        }
    }

    #endregion


    #region GetLeaderboard
    [Test]
    public void GetLeaderboard_GetsAllPlayerRankingsDependentOnStats()
    {
        statTracker.RecordKill(1, 2);
        statTracker.RecordKill(1, 2);
        statTracker.RecordKill(1, 2);
        statTracker.RecordKill(3, 2);
        statTracker.RecordKill(3, 2);
        statTracker.RecordKill(4, 2);

        var leaderboard = roundManager.GetLeaderboard();

        Assert.AreEqual(4, leaderboard.Count);
        Assert.AreEqual((ulong)1, leaderboard[0].player);
        Assert.AreEqual((ulong)3, leaderboard[1].player);
        Assert.AreEqual((ulong)4, leaderboard[2].player);
        Assert.AreEqual((ulong)2, leaderboard[3].player);
    }
    #endregion
}
