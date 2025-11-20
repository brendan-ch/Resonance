using UnityEngine;

namespace Resonance.Match
{
    [System.Serializable]
    public class PlayerMatchStats
    {
        public int kills = 0;
        public int deaths = 0;
        public int assists = 0;
        public int killStreak = 0;
        public int bestKillStreak = 0;
        
        public float KDA => deaths == 0 ? (kills + assists) : (float)(kills + assists) / deaths;
        
        public override string ToString()
        {
            return $"K/D/A: {kills}/{deaths}/{assists} | KDA: {KDA:F2} | Streak: {killStreak}";
        }
    }
}