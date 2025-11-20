using UnityEngine;

namespace Resonance.Match
{
    [System.Serializable]
    public class PlayerRanking
    {
        public GameObject player;
        public PlayerMatchStats stats;
        public int rank;
        
        public override string ToString()
        {
            return $"Rank {rank}: {player.name} - {stats.ToString()}";
        }
    }
}