using Resonance.Assemblies.SharedGameLogic;

namespace Resonance.Assemblies.Polarity
{
    public enum PolarityMatchState
    {
        // add additional possible match states specific to Polarity later
        Waiting = BaseMatchState.Waiting,
        Countdown = BaseMatchState.Countdown,
        MatchActive = BaseMatchState.MatchActive,
        MatchEnded = BaseMatchState.MatchEnded,
    }
}
