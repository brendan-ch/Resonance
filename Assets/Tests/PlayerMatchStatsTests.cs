using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using Resonance.Match;

public class PlayerMatchStatsTests
{
    // A Test behaves as an ordinary method
    [Test]
    public void PlayerMatchStatsTestsSimplePasses()
    {
        // Use the Assert class to test conditions
        Assert.AreEqual(5, 5);
    }

    // A UnityTest behaves like a coroutine in Play Mode. In Edit Mode you can use
    // `yield return null;` to skip a frame.
    [UnityTest]
    public IEnumerator PlayerMatchStatsTestsWithEnumeratorPasses()
    {
        // Use the Assert class to test conditions.
        // Use yield to skip a frame.
        yield return null;
    }
}
