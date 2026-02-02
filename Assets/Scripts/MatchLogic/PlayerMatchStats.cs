namespace Resonance.Match
{
    [System.Serializable]
    public struct PlayerMatchStats
    {
        public int kills;
        public int deaths;
        public int assists;
        public int killStreak;
        public int bestKillStreak;

        public float KDA => deaths == 0 ? (kills + assists) : (float)(kills + assists) / deaths;

        public override string ToString()
        {
            return $"K/D/A: {kills}/{deaths}/{assists} | KDA: {KDA:F2} | Streak: {killStreak}";
        }

        public PlayerMatchStats RecordKill()
        {
            return new PlayerMatchStats
            {
                assists = assists,
                bestKillStreak = killStreak > bestKillStreak ? killStreak : bestKillStreak,
                deaths = deaths,
                kills = kills + 1,
                killStreak = killStreak + 1
            };
        }

        public PlayerMatchStats RecordDeath()
        {
            return new PlayerMatchStats
            {
                assists = assists,
                bestKillStreak = bestKillStreak,
                deaths = deaths + 1,
                kills = kills,
                killStreak = 0,
            };
        }

        public PlayerMatchStats RecordAssist()
        {
            return new PlayerMatchStats
            {
                assists = assists + 1,
                bestKillStreak = bestKillStreak,
                deaths = deaths,
                kills = kills,
                killStreak = killStreak,
            };
        }
    }
}
