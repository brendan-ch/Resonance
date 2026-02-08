using Resonance.Assemblies.Match;

namespace Resonance.Assemblies.Arena
{
    [System.Serializable]
    public class PlayerRanking
    {
        public ulong player;
        public PlayerMatchStats stats;
        public int rank;
        
        public override string ToString()
        {
            return $"Rank {rank}: {player} - {stats.ToString()}";
        }
    }
}
