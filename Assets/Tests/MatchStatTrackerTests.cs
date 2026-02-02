using System.Collections;
using NUnit.Framework;
using Resonance.Match;
using UnityEngine;
using UnityEngine.TestTools;

public class MatchStatTrackerTests
{
    private GameObject trackerObject;
    private MatchStatTracker tracker;
    private GameObject player1;
    private GameObject player2;

    [SetUp]
    public void Setup()
    {
        trackerObject = new GameObject("MatchStatTracker");
        tracker = trackerObject.AddComponent<MatchStatTracker>();

        player1 = new GameObject("Player1");
        player2 = new GameObject("Player2");
    }

    // A Test behaves as an ordinary method
    [Test]
    public void RecordKill_FiresEvent()
    {
        GameObject killerCaptured = null;
        GameObject victimCaptured = null;

        tracker.OnPlayerKill += (killer, victim) =>
        {
            killerCaptured = killer;
            victimCaptured = victim;
        };

        tracker.RecordKill(player1, player2);

        Assert.IsNotNull(killerCaptured);
        Assert.IsNotNull(victimCaptured);
        Assert.AreEqual(player1, killerCaptured);
        Assert.AreEqual(player2, victimCaptured);
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
