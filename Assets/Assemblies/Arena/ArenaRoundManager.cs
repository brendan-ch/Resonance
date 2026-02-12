using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Resonance.Assemblies.MatchStat;

namespace Resonance.Assemblies.Arena
{
    public class ArenaRoundManager
    {
        #region Configuration
        public struct ArenaRoundManagerConfig
        {
            public int eliminationsToWin;
            public bool autoStartNextMatch;
            public float matchEndDelaySeconds;

            public static ArenaRoundManagerConfig Default => new ArenaRoundManagerConfig
            {
                eliminationsToWin = 10,
                autoStartNextMatch = false,
                matchEndDelaySeconds = 3f
            };
        }
        #endregion

        private int eliminationsToWin = 10;
        private float autoStartDelaySeconds = 3f;
        private bool autoStartNextMatch = false;

        #region State
        private ArenaMatchState matchState = ArenaMatchState.Waiting;

        private ulong? currentLeader = null;
        private int highestEliminations = 0;
        #endregion

        #region Events
        public event System.Action OnMatchStart;
        public event System.Action<ulong?> OnMatchEnd; // Winner
        public event System.Action<ulong, int> OnLeaderChanged; // New leader, their eliminations
        #endregion

        #region Properties
        public bool IsMatchActive => matchState == ArenaMatchState.MatchActive;
        public bool IsMatchEnded => matchState == ArenaMatchState.MatchEnded;
        public int EliminationsToWin => eliminationsToWin;
        public ulong? CurrentLeader => currentLeader;
        public int HighestEliminations => highestEliminations;
        #endregion

        private MatchStatTracker matchStatTracker;

        public ArenaRoundManager(MatchStatTracker tracker)
            : this(tracker, ArenaRoundManagerConfig.Default)
        {
        }

        public ArenaRoundManager(MatchStatTracker tracker, ArenaRoundManagerConfig config)
        {
            matchStatTracker = tracker;
            this.autoStartNextMatch = config.autoStartNextMatch;
            this.eliminationsToWin = config.eliminationsToWin;
            this.autoStartDelaySeconds = config.matchEndDelaySeconds;

            SubscribeToEvents();
        }

        #region Event Subscriptions
        private void SubscribeToEvents()
        {
            if (matchStatTracker != null)
            {
                matchStatTracker.OnPlayerKill += OnPlayerKilled;
            }
        }
        #endregion

        #region Match Control
        public void StartMatch()
        {
            if (IsMatchActive)
            {
                return;
            }

            matchState = ArenaMatchState.MatchActive;
            currentLeader = null;
            highestEliminations = 0;

            // Reset all player stats
            matchStatTracker?.ResetAllStats();

            OnMatchStart?.Invoke();
        }

        /// <summary>
        /// Ends the match, auto-starting the next round if configured.
        /// </summary>
        /// <param name="winner"></param>
        /// <returns></returns>
        public async Task EndMatch(ulong? winner)
        {
            if (!IsMatchActive || IsMatchEnded) return;

            matchState = ArenaMatchState.MatchEnded;
            OnMatchEnd?.Invoke(winner);

            if (autoStartNextMatch)
            {
                await QueueNextMatchStart();
            }
        }

        private async Task QueueNextMatchStart()
        {
            await Task.Delay((int)autoStartDelaySeconds * 1000);
            StartMatch();
        }
        #endregion

        #region Kill Event Handling
        private void OnPlayerKilled(ulong killer, ulong victim)
        {
            if (!IsMatchActive || IsMatchEnded) return;

            PlayerMatchStats? killerStats = matchStatTracker.GetStats(killer);
            if (killerStats is PlayerMatchStats stats)
            {
                ConditionallyUpdateLeader(killer, stats);

                int currentEliminations = stats.kills;
                if (currentEliminations >= eliminationsToWin)
                {
                    _ = EndMatch(killer);
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
                    stats = kvp.Value
                });
            }

            // Sort by kills (descending), then by KDA (descending), then by deaths (ascending)
            rankings = rankings.OrderByDescending(r => r.stats.kills)
                              .ThenByDescending(r => r.stats.KDA)
                              .ThenBy(r => r.stats.deaths)
                              .ToList();

            return rankings;
        }

        public string GetLeaderboardString()
        {
            var leaderboard = GetLeaderboard();
            string result = "=== LEADERBOARD ===\n";

            for (int i = 0; i < leaderboard.Count; i++)
            {
                var ranking = leaderboard[i];
                result += $"#{i + 1} {ranking.player}: {ranking.stats}\n";
            }

            return result;
        }
        #endregion

    }
}
