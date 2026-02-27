namespace Resonance.Assemblies.Polarity
{
    public class PolarityRoundManager
    {
        private int teamEliminationsToWin = 10;
        private float matchStartCountdownSeconds = 5f;

        private PolarityMatchState matchState = PolarityMatchState.Waiting;
    }
}
