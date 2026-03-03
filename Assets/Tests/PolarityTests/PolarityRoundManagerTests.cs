using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;
using Resonance.Assemblies.MatchStat;
using Resonance.Assemblies.Polarity;

public class PolarityRoundManagerTests
{
    private MatchStatTracker statTracker;
    private PolarityRoundManager roundManager;

    [SetUp]
    public void Setup()
    {
        statTracker = new();
        roundManager = new(statTracker);
    }

    private PolarityRoundManager CreateManagerWithConfig(
        int timeBetweenRoleSwitchSeconds = 90,
        int teamEliminationsToWin = 10,
        float matchStartCountdownSeconds = 5f)
    {
        return new PolarityRoundManager(statTracker, new()
        {
            timeBetweenRoleSwitchSeconds = timeBetweenRoleSwitchSeconds,
            teamEliminationsToWin = teamEliminationsToWin,
            matchStartCountdownSeconds = matchStartCountdownSeconds,
        });
    }

    private void SetupTwoPlayerTeams(PolarityRoundManager manager)
    {
        manager.RegisterPlayersForTeamA(new() { 1 });
        manager.RegisterPlayersForTeamB(new() { 2 });
    }

    #region Properties
    [Test]
    public void MatchState_DefaultsToWaiting()
    {
        Assert.AreEqual(PolarityMatchState.Waiting, roundManager.MatchState);
    }

    [Test]
    public void IsMatchActive_ReturnsFalseByDefault()
    {
        Assert.AreEqual(false, roundManager.IsMatchActive);
    }

    [Test]
    public void IsMatchEnded_ReturnsFalseByDefault()
    {
        Assert.AreEqual(false, roundManager.IsMatchEnded);
    }

    [Test]
    public void TeamEliminationsToWin_ReturnsConfiguredValue()
    {
        Assert.AreEqual(10, roundManager.TeamEliminationsToWin);
    }

    [Test]
    public void MatchStartCountdownSeconds_ReturnsConfiguredValue()
    {
        Assert.AreEqual(5f, roundManager.MatchStartCountdownSeconds);
    }

    [Test]
    public void TeamA_DefaultsToTaggersRole()
    {
        Assert.AreEqual(PolarityRoundManager.PolarityTeamRole.Taggers, roundManager.TeamA.currentRole);
    }

    [Test]
    public void TeamB_DefaultsToRunnersRole()
    {
        Assert.AreEqual(PolarityRoundManager.PolarityTeamRole.Runners, roundManager.TeamB.currentRole);
    }

    [Test]
    public void TeamA_HasEmptyPlayersSetByDefault()
    {
        Assert.IsNotNull(roundManager.TeamA.players);
        Assert.AreEqual(0, roundManager.TeamA.players.Count);
    }

    [Test]
    public void TeamB_HasEmptyPlayersSetByDefault()
    {
        Assert.IsNotNull(roundManager.TeamB.players);
        Assert.AreEqual(0, roundManager.TeamB.players.Count);
    }

    [Test]
    public void TeamAEliminations_DefaultsToZero()
    {
        Assert.AreEqual(0, roundManager.TeamAEliminations);
    }

    [Test]
    public void TeamBEliminations_DefaultsToZero()
    {
        Assert.AreEqual(0, roundManager.TeamBEliminations);
    }

    [Test]
    public void SecondsUntilNextRoleSwitch_ReturnsZeroIfMatchNotActive()
    {
        Assert.AreEqual(0, roundManager.SecondsUntilNextRoleSwitch);
    }
    #endregion

    #region RegisterPlayers
    [Test]
    public void RegisterPlayersForTeamA_UpdatesPlayersSet()
    {
        roundManager.RegisterPlayersForTeamA(new() { 1, 2, 3, 4 });
        Assert.AreEqual(new HashSet<ulong>() { 1, 2, 3, 4 }, roundManager.TeamA.players);
    }

    [Test]
    public void RegisterPlayersForTeamB_UpdatesPlayersSet()
    {
        roundManager.RegisterPlayersForTeamB(new() { 1, 2, 3, 4 });
        Assert.AreEqual(new HashSet<ulong>() { 1, 2, 3, 4 }, roundManager.TeamB.players);
    }
    #endregion

    #region StartMatchWithoutCountdown
    [Test]
    public void StartMatchWithoutCountdown_UpdatesMatchState()
    {
        roundManager.StartMatchWithoutCountdown();

        Assert.AreEqual(true, roundManager.IsMatchActive);
        Assert.AreEqual(false, roundManager.IsMatchEnded);
    }

    [Test]
    public void StartMatchWithoutCountdown_FiresOnMatchStartEvent()
    {
        int eventCallCount = 0;
        roundManager.OnMatchStart += () => eventCallCount++;

        roundManager.StartMatchWithoutCountdown();

        Assert.AreEqual(1, eventCallCount);
    }

    [Test]
    public void StartMatchWithoutCountdown_FiresMatchStateChangeEvent()
    {
        PolarityMatchState capturedOldState = default;
        PolarityMatchState capturedNewState = default;
        int eventCallCount = 0;
        roundManager.OnMatchStateChange += (oldState, newState) =>
        {
            capturedOldState = oldState;
            capturedNewState = newState;
            eventCallCount++;
        };

        roundManager.StartMatchWithoutCountdown();

        Assert.AreEqual(PolarityMatchState.Waiting, capturedOldState);
        Assert.AreEqual(PolarityMatchState.MatchActive, capturedNewState);
        Assert.AreEqual(1, eventCallCount);
    }

    [Test]
    public void StartMatchWithoutCountdown_DoesNothingIfMatchAlreadyActive()
    {
        roundManager.StartMatchWithoutCountdown();

        int eventCallCount = 0;
        roundManager.OnMatchStart += () => eventCallCount++;

        roundManager.StartMatchWithoutCountdown();

        Assert.AreEqual(0, eventCallCount);
        Assert.AreEqual(true, roundManager.IsMatchActive);
    }

    [Test]
    public void StartMatchWithoutCountdown_ResetsMatchStatTracker()
    {
        statTracker.RecordKill(1, 2);
        statTracker.RecordKill(3, 4);

        roundManager.StartMatchWithoutCountdown();

        var allStats = statTracker.GetAllStats();
        foreach (var kvp in allStats)
        {
            Assert.AreEqual(new PlayerMatchStats(), kvp.Value);
        }
    }

    [Test]
    public void StartMatchWithoutCountdown_ResetsTeamEliminations()
    {
        roundManager.StartMatchWithoutCountdown();

        Assert.AreEqual(0, roundManager.TeamAEliminations);
        Assert.AreEqual(0, roundManager.TeamBEliminations);
    }

    [Test]
    public void StartMatchWithoutCountdown_ResetsTeamRolesToDefault()
    {
        roundManager.StartMatchWithoutCountdown();

        Assert.AreEqual(PolarityRoundManager.PolarityTeamRole.Taggers, roundManager.TeamA.currentRole);
        Assert.AreEqual(PolarityRoundManager.PolarityTeamRole.Runners, roundManager.TeamB.currentRole);
    }

    [Test]
    public void StartMatchWithoutCountdown_RecordsTimeOfLastRoleSwitch()
    {
        roundManager.StartMatchWithoutCountdown();

        Assert.IsNotNull(roundManager.TimeOfLastRoleSwitch);
    }
    #endregion

    #region SwitchRoles
    [Test]
    public void SwitchRoles_SwapsTeamRoles()
    {
        roundManager.StartMatchWithoutCountdown();

        Assert.AreEqual(PolarityRoundManager.PolarityTeamRole.Taggers, roundManager.TeamA.currentRole);
        Assert.AreEqual(PolarityRoundManager.PolarityTeamRole.Runners, roundManager.TeamB.currentRole);

        roundManager.SwitchRoles();

        Assert.AreEqual(PolarityRoundManager.PolarityTeamRole.Runners, roundManager.TeamA.currentRole);
        Assert.AreEqual(PolarityRoundManager.PolarityTeamRole.Taggers, roundManager.TeamB.currentRole);
    }

    [Test]
    public void SwitchRoles_FiresOnRoleSwitchEvent()
    {
        roundManager.StartMatchWithoutCountdown();

        int eventCallCount = 0;
        roundManager.OnRoleSwitch += () => eventCallCount++;

        roundManager.SwitchRoles();

        Assert.AreEqual(1, eventCallCount);
    }

    [Test]
    public void SwitchRoles_DoesNothingIfMatchNotActive()
    {
        int eventCallCount = 0;
        roundManager.OnRoleSwitch += () => eventCallCount++;

        roundManager.SwitchRoles();

        Assert.AreEqual(0, eventCallCount);
        Assert.AreEqual(PolarityRoundManager.PolarityTeamRole.Taggers, roundManager.TeamA.currentRole);
    }

    [Test]
    public void SwitchRoles_DoubleSwitchRestoresOriginalRoles()
    {
        roundManager.StartMatchWithoutCountdown();

        roundManager.SwitchRoles();
        roundManager.SwitchRoles();

        Assert.AreEqual(PolarityRoundManager.PolarityTeamRole.Taggers, roundManager.TeamA.currentRole);
        Assert.AreEqual(PolarityRoundManager.PolarityTeamRole.Runners, roundManager.TeamB.currentRole);
    }

    [Test]
    public async Task RoleSwitchTimer_SwitchesRolesAfterConfiguredTime()
    {
        var manager = CreateManagerWithConfig(timeBetweenRoleSwitchSeconds: 2);
        manager.StartMatchWithoutCountdown();
        Assert.AreEqual(PolarityRoundManager.PolarityTeamRole.Taggers, manager.TeamA.currentRole);

        await Task.Delay(3000);

        Assert.AreEqual(PolarityRoundManager.PolarityTeamRole.Runners, manager.TeamA.currentRole);
        Assert.AreEqual(PolarityRoundManager.PolarityTeamRole.Taggers, manager.TeamB.currentRole);
    }

    [Test]
    public async Task RoleSwitchTimer_FiresOnRoleSwitchTimerElapsedEvent()
    {
        var manager = CreateManagerWithConfig(timeBetweenRoleSwitchSeconds: 4);

        int eventCallCount = 0;
        manager.OnRoleSwitchTimerElapsed += (_) => eventCallCount++;

        manager.StartMatchWithoutCountdown();

        await Task.Delay(2500);

        Assert.GreaterOrEqual(eventCallCount, 2);
    }
    #endregion

    #region EndMatch
    [Test]
    public async Task EndMatch_UpdatesMatchState()
    {
        roundManager.StartMatchWithoutCountdown();
        await roundManager.EndMatch(roundManager.TeamA);

        Assert.AreEqual(false, roundManager.IsMatchActive);
        Assert.AreEqual(true, roundManager.IsMatchEnded);
    }

    [Test]
    public async Task EndMatch_FiresOnMatchEndEvent()
    {
        roundManager.StartMatchWithoutCountdown();

        PolarityRoundManager.Team? capturedWinner = null;
        int eventCallCount = 0;
        roundManager.OnMatchEnd += (winner) =>
        {
            capturedWinner = winner;
            eventCallCount++;
        };

        await roundManager.EndMatch(roundManager.TeamA);

        Assert.AreEqual(1, eventCallCount);
        Assert.AreEqual(PolarityRoundManager.PolarityTeamRole.Taggers, capturedWinner.Value.currentRole);
    }

    [Test]
    public async Task EndMatch_FiresMatchStateChangeEvent()
    {
        roundManager.StartMatchWithoutCountdown();

        PolarityMatchState capturedOldState = default;
        PolarityMatchState capturedNewState = default;
        int eventCallCount = 0;
        roundManager.OnMatchStateChange += (oldState, newState) =>
        {
            capturedOldState = oldState;
            capturedNewState = newState;
            eventCallCount++;
        };

        await roundManager.EndMatch(roundManager.TeamA);

        Assert.AreEqual(PolarityMatchState.MatchActive, capturedOldState);
        Assert.AreEqual(PolarityMatchState.MatchEnded, capturedNewState);
        Assert.AreEqual(1, eventCallCount);
    }

    [Test]
    public async Task EndMatch_StopsRoleSwitchTimer()
    {
        var manager = CreateManagerWithConfig(timeBetweenRoleSwitchSeconds: 1);
        manager.StartMatchWithoutCountdown();
        await manager.EndMatch(manager.TeamA);

        var roleAtEnd = manager.TeamA.currentRole;
        await Task.Delay(1500);

        Assert.AreEqual(roleAtEnd, manager.TeamA.currentRole);
    }

    [Test]
    public async Task EndMatch_DoesNothingIfMatchNotActive()
    {
        int eventCallCount = 0;
        roundManager.OnMatchEnd += (_) => eventCallCount++;

        await roundManager.EndMatch(roundManager.TeamA);

        Assert.AreEqual(0, eventCallCount);
        Assert.AreEqual(false, roundManager.IsMatchActive);
        Assert.AreEqual(false, roundManager.IsMatchEnded);
    }
    #endregion

    #region StartMatchCountdown
    [Test]
    public async Task StartMatchCountdown_UpdatesMatchStateDuringCountdown()
    {
        _ = roundManager.StartMatchCountdown();
        await Task.Delay(1000);
        Assert.AreEqual(PolarityMatchState.Countdown, roundManager.MatchState);
    }

    [Test]
    public async Task StartMatchCountdown_FiresEventWithCountdownSeconds()
    {
        float capturedSeconds = 0;
        int eventCallCount = 0;
        roundManager.OnMatchCountdownStart += (seconds) =>
        {
            capturedSeconds = seconds;
            eventCallCount++;
        };

        _ = roundManager.StartMatchCountdown();
        await Task.Delay(1000);

        Assert.AreEqual(roundManager.MatchStartCountdownSeconds, capturedSeconds);
        Assert.AreEqual(1, eventCallCount);
    }

    [Test]
    public async Task StartMatchCountdown_FiresMatchStateChangeEvent()
    {
        PolarityMatchState capturedOldState = default;
        PolarityMatchState capturedNewState = default;
        int eventCallCount = 0;
        roundManager.OnMatchStateChange += (oldState, newState) =>
        {
            capturedOldState = oldState;
            capturedNewState = newState;
            eventCallCount++;
        };

        _ = roundManager.StartMatchCountdown();

        Assert.AreEqual(PolarityMatchState.Waiting, capturedOldState);
        Assert.AreEqual(PolarityMatchState.Countdown, capturedNewState);
        Assert.AreEqual(1, eventCallCount);
    }

    [Test]
    public async Task StartMatchCountdown_ActuallyStartsTheMatch()
    {
        roundManager = CreateManagerWithConfig(matchStartCountdownSeconds: 0.5f);

        await roundManager.StartMatchCountdown();
        Assert.AreEqual(PolarityMatchState.MatchActive, roundManager.MatchState);
    }
    #endregion

    #region OnPlayerKilled
    [Test]
    public void OnPlayerKilled_DoesNothingIfMatchNotActive()
    {
        SetupTwoPlayerTeams(roundManager);

        statTracker.RecordKill(1, 2);

        Assert.AreEqual(0, roundManager.TeamAEliminations);
        Assert.AreEqual(0, roundManager.TeamBEliminations);
    }

    [Test]
    public void OnPlayerKilled_IncrementsKillerTeamEliminations()
    {
        SetupTwoPlayerTeams(roundManager);
        roundManager.StartMatchWithoutCountdown();

        statTracker.RecordKill(1, 2);
        statTracker.RecordKill(1, 2);
        statTracker.RecordKill(2, 1);

        Assert.AreEqual(2, roundManager.TeamAEliminations);
        Assert.AreEqual(1, roundManager.TeamBEliminations);
    }

    [Test]
    public void OnPlayerKilled_IncrementsCorrectTeamAfterRoleSwitch()
    {
        SetupTwoPlayerTeams(roundManager);
        roundManager.StartMatchWithoutCountdown();
        roundManager.SwitchRoles();

        statTracker.RecordKill(2, 1);
        statTracker.RecordKill(2, 1);

        Assert.AreEqual(0, roundManager.TeamAEliminations);
        Assert.AreEqual(2, roundManager.TeamBEliminations);
    }

    [Test]
    public void OnPlayerKilled_EndsMatchWhenTeamReachesEliminationThreshold()
    {
        var manager = CreateManagerWithConfig(teamEliminationsToWin: 3);
        SetupTwoPlayerTeams(manager);
        manager.StartMatchWithoutCountdown();

        statTracker.RecordKill(1, 2);
        statTracker.RecordKill(1, 2);
        statTracker.RecordKill(1, 2);

        Assert.AreEqual(true, manager.IsMatchEnded);
    }

    [Test]
    public void OnPlayerKilled_FiresOnMatchEndWithCorrectWinningTeam()
    {
        var manager = CreateManagerWithConfig(teamEliminationsToWin: 2);
        SetupTwoPlayerTeams(manager);
        manager.StartMatchWithoutCountdown();

        PolarityRoundManager.Team? capturedWinner = null;
        manager.OnMatchEnd += (winner) => capturedWinner = winner;

        statTracker.RecordKill(2, 1);
        statTracker.RecordKill(2, 1);

        Assert.IsNotNull(capturedWinner);
        Assert.IsTrue(capturedWinner.Value.players.Contains(2ul));
    }

    [Test]
    public void OnPlayerKilled_DoesNotCountKillsFromUnassignedPlayers()
    {
        roundManager.RegisterPlayersForTeamA(new() { 1 });
        roundManager.StartMatchWithoutCountdown();

        statTracker.RecordKill(99, 1);
        statTracker.RecordKill(99, 1);
        statTracker.RecordKill(1, 99);

        Assert.AreEqual(1, roundManager.TeamAEliminations);
        Assert.AreEqual(0, roundManager.TeamBEliminations);
    }
    #endregion
}
