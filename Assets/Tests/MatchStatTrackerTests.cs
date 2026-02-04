using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Resonance.Assemblies.Match;

public class MatchStatTrackerTests
{
    private ulong expectedKillerId = 1;
    private ulong expectedVictimId = 2;
    private MatchStatTracker tracker;

    [SetUp]
    public void Setup()
    {
        tracker = new MatchStatTracker();
    }

    [Test]
    public void RecordKill_FiresOnKillEvent()
    {

        ulong OnPlayerKill_killerCaptured = default;
        ulong OnPlayerKill_victimCaptured = default;

        tracker.OnPlayerKill += (killer, victim) =>
        {
            OnPlayerKill_killerCaptured = killer;
            OnPlayerKill_victimCaptured = victim;
        };


        tracker.RecordKill(expectedKillerId, expectedVictimId);

        // Expect killer stats to have been updated before victim stats
        Assert.AreEqual(expectedKillerId, OnPlayerKill_killerCaptured);
        Assert.AreEqual(expectedVictimId, OnPlayerKill_victimCaptured);
    }

    [Test]
    public void RecordKill_FiresStatsUpdateEvent()
    {
        ulong[] OnStatsUpdate_capturedPlayers = {0, 0};
        PlayerMatchStats[] OnStatsUpdate_stats = {new(), new()};
        int i = 0;

        tracker.OnStatsUpdated += (player, stats) =>
        {
            OnStatsUpdate_capturedPlayers[i] = player; 
            OnStatsUpdate_stats[i] = stats;
            i += 1;
        };

        tracker.RecordKill(expectedKillerId, expectedVictimId);

        // killer, then victim
        ulong[] expectedCapturedPlayers = {1, 2};
        PlayerMatchStats[] expectedCapturedStats =
        {
            new() {
                kills = 1,
                killStreak = 1,
                bestKillStreak = 1,
            },
            new() {
                deaths = 1,
            },
        };
        Assert.AreEqual(expectedCapturedPlayers, OnStatsUpdate_capturedPlayers);
    }

    [Test]
    public void RecordKill_UpdatesPlayerStats()
    {

    }
}
