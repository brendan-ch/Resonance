using System.Collections;
using NUnit.Framework;
using Resonance.Assemblies.Match;
using UnityEngine.TestTools;

public class MatchStatTrackerTests
{
    private MatchStatTracker tracker;

    [SetUp]
    public void Setup()
    {
        tracker = new MatchStatTracker();
    }

    // A Test behaves as an ordinary method
    [Test]
    public void RecordKill_FiresEvent()
    {
        ulong killerCaptured = default;
        ulong victimCaptured = default;

        tracker.OnPlayerKill += (killer, victim) =>
        {
            killerCaptured = killer;
            victimCaptured = victim;
        };

        tracker.RecordKill(1, 2);

        Assert.AreEqual(1, killerCaptured);
        Assert.AreEqual(2, victimCaptured);
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator MatchStatTrackerTestsWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }
}
