using System.Collections.Generic;
using System.Linq;
using Resonance.Assemblies.MatchStat;

namespace Resonance.Assemblies.Arena
{
    public class ArenaRoundManager
    {
        public static ArenaRoundManager Instance { get; private set; }

        private int eliminationsToWin = 10;
        private float matchEndDelaySeconds = 3f;
        private bool autoStartNextRound = false;

        #region State
        private bool matchActive = false;
        private bool matchEnded = false;
        private ulong? currentLeader = null;
        private int highestEliminations = 0;
        #endregion
        
        #region Events
        public event System.Action OnMatchStart;
        public event System.Action<ulong?> OnMatchEnd; // Winner
        public event System.Action<ulong, int> OnLeaderChanged; // New leader, their eliminations
        #endregion

        #region Properties
        public bool IsMatchActive => matchActive;
        public bool IsMatchEnded => matchEnded;
        public int EliminationsToWin => eliminationsToWin;
        public ulong? CurrentLeader => currentLeader;
        public int HighestEliminations => highestEliminations;
        #endregion

        private MatchStatTracker matchStatTracker;

        public ArenaRoundManager(MatchStatTracker tracker)
        {
            matchStatTracker = tracker;
            SubscribeToEvents();
        }

        #region Event Subscriptions
        private void SubscribeToEvents()
        {
            if (matchStatTracker != null)
            {
                // matchStatTracker.OnPlayerKill += OnPlayerKilled;
                matchStatTracker.OnPlayerKill += OnPlayerKilled;
            }
        }
        #endregion

        #region Match Control
        public void StartMatch()
        {
            if (matchActive)
            {
                return;
            }

            matchActive = true;
            matchEnded = false;
            currentLeader = null;
            highestEliminations = 0;

            // Reset all player stats
            matchStatTracker?.ResetAllStats();

            OnMatchStart?.Invoke();
        }

        public void EndMatch(ulong? winner)
        {
            if (!matchActive || matchEnded) return;

            matchActive = false;
            matchEnded = true;

            if (winner is ulong id)
            {
                PlayerMatchStats? stats = matchStatTracker.GetStats(id);
            }
            else
            {
            }

            OnMatchEnd?.Invoke(winner);

            if (autoStartNextRound)
            {
                // TODO
                // Invoke(nameof(StartMatch), matchEndDelaySeconds);
            }
        }

        public void ReloadLevel()
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
            );
        }
        #endregion

        #region Kill Event Handling
        private void OnPlayerKilled(ulong killer, ulong victim)
        {
            if (!matchActive || matchEnded) return;

            PlayerMatchStats? killerStats = matchStatTracker.GetStats(killer);
            if (killerStats is PlayerMatchStats stats)
            {
                ConditionallyUpdateLeader(killer, stats);

                int currentEliminations = stats.kills;
                if (currentEliminations >= eliminationsToWin)
                {
                    EndMatch(killer);
                }
            }

        }

        private int ConditionallyUpdateLeader(ulong killer, PlayerMatchStats stats)
        {
            int currentEliminations = stats.kills;

            // Update leader tracking
            if (currentEliminations > highestEliminations)
            {
                highestEliminations = currentEliminations;
                var previousLeader = currentLeader;
                currentLeader = killer;

                if (previousLeader != killer)
                {
                    OnLeaderChanged?.Invoke(killer, currentEliminations);
                }
            }

            return currentEliminations;
        }
        #endregion

        #region Leaderboard Queries
        public List<PlayerRanking> GetLeaderboard()
        {
            if (matchStatTracker == null) return new List<PlayerRanking>();

            var allStats = matchStatTracker.GetAllStats();
            var rankings = new List<PlayerRanking>();

            foreach (var kvp in allStats)
            {
                rankings.Add(new PlayerRanking
                {
                    player = kvp.Key,
                    stats = kvp.Value,
                    rank = 0 // Will be set after sorting
                });
            }

            // Sort by kills (descending), then by KDA (descending), then by deaths (ascending)
            rankings = rankings.OrderByDescending(r => r.stats.kills)
                              .ThenByDescending(r => r.stats.KDA)
                              .ThenBy(r => r.stats.deaths)
                              .ToList();

            // Assign ranks
            for (int i = 0; i < rankings.Count; i++)
            {
                rankings[i].rank = i + 1;
            }

            return rankings;
        }

        // public async Task<int> GetPlayerRank(GameObject player)
        // {
        //     var leaderboard = await GetLeaderboard();

        //     var didExtractulong = OwnerIDExtractor.TryExtractPlayerIds(player, out ulong id);
        //     var playerRanking = leaderboard.FirstOrDefault(r => r.player.id.value == id);
        //     return playerRanking?.rank ?? -1;
        // }

        public string GetLeaderboardString()
        {
            var leaderboard = GetLeaderboard();
            string result = "=== LEADERBOARD ===\n";

            foreach (var ranking in leaderboard)
            {
                result += $"#{ranking.rank} {ranking.player}: {ranking.stats}\n";
            }

            return result;
        }
        #endregion

    }
}
