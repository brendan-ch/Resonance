using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Resonance.Assemblies.Match;

public class DamageTrackerTests
{
    private ulong expectedVictimId = 2;
    private DamageTracker tracker;
    private HashSet<ulong> expectedAssistIds = new()
        {
            3,
            4
        };

    [SetUp]
    public void Setup()
    {
        tracker = new DamageTracker(
            assistTimeWindowMs: 100f,
            assistDamageThreshold: 5
        );
    }

    [Test]
    public void GetAssistAttackersForVictim_RetrievesCorrectAssistersAfterMultipleRecordCalls()
    {

        foreach (var id in expectedAssistIds)
        {
            // test out total damage before death
            tracker.RecordDamage(id, expectedVictimId, 15);
            tracker.RecordDamage(id, expectedVictimId, 15);
        }

        var actualAssistIds = tracker.GetAssistAttackersForVictim(expectedVictimId, 1);
        Assert.AreEqual(expectedAssistIds, actualAssistIds);
    }

    [Test]
    public void GetAssistAttackersForVictim_RetrievesCorrectAssistersAfterOneRecordCall()
    {
        foreach (var id in expectedAssistIds)
        {
            tracker.RecordDamage(id, expectedVictimId, 30);
        }
        var actualAssistIds = tracker.GetAssistAttackersForVictim(expectedVictimId, 1);
        Assert.AreEqual(expectedAssistIds, actualAssistIds);
    }

    [Test]
    public async Task GetAssistAttackersForVictim_RetrievesEmptyIfTimeElapsed()
    {
        foreach (var id in expectedAssistIds)
        {
            tracker.RecordDamage(id, expectedVictimId, 30);
        }

        await Task.Delay(1000);
        var actualAssistIds = tracker.GetAssistAttackersForVictim(expectedVictimId, 1);
        Assert.IsEmpty(actualAssistIds);
    }
}
