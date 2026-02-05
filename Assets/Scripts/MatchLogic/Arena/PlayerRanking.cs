using PurrNet;
using Resonance.Assemblies.Match;

namespace Resonance.Match
{
    [System.Serializable]
    public class PlayerRanking
    {
        public PlayerID player;
        public PlayerMatchStats stats;
        public int rank;
        
        public override string ToString()
        {
            return $"Rank {rank}: {player} - {stats.ToString()}";
        }
    }
}
