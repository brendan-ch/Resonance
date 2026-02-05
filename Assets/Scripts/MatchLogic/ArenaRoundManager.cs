using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Resonance.Assemblies.Match;
using System.Threading.Tasks;
using PurrNet;

namespace Resonance.Match
{
    public class ArenaRoundManager : MonoBehaviour
    {
        public static ArenaRoundManager Instance { get; private set; }

        #region Inspector Fields
        [Header("Win Condition")]
        [SerializeField] private int eliminationsToWin = 10;

        [Header("Match Settings")]
        [SerializeField] private float matchEndDelay = 3f;
        [SerializeField] private bool autoStartNextRound = false;
        #endregion

        #region State
        private bool matchActive = false;
        private bool matchEnded = false;
        private PlayerID? currentLeader = null;
        private int highestEliminations = 0;
        #endregion

        #region Events
        public event System.Action OnMatchStart;
        public event System.Action<PlayerID?> OnMatchEnd; // Winner
        public event System.Action<PlayerID, int> OnLeaderChanged; // New leader, their eliminations
        #endregion

        #region Properties
        public bool IsMatchActive => matchActive;
        public bool IsMatchEnded => matchEnded;
        public int EliminationsToWin => eliminationsToWin;
        public PlayerID? CurrentLeader => currentLeader;
        public int HighestEliminations => highestEliminations;
        #endregion

        #region Startup
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        #endregion

        #region Event Subscriptions
        private void SubscribeToEvents()
        {
            if (MatchStatBridge.Instance != null)
            {
                MatchStatBridge.Instance.OnPlayerKill.AddListener(OnPlayerKilled);
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (MatchStatBridge.Instance != null)
            {
                MatchStatBridge.Instance.OnPlayerKill.RemoveListener(OnPlayerKilled);
            }
        }
        #endregion

        #region Match Control
        public void StartMatch()
        {
            if (matchActive)
            {
                Debug.LogWarning("[ArenaRoundManager] Match is already active!");
                return;
            }

            matchActive = true;
            matchEnded = false;
            currentLeader = null;
            highestEliminations = 0;

            // Reset all player stats
            if (MatchStatBridge.Instance != null)
            {
                MatchStatBridge.Instance.ResetAllStats();
            }

            Debug.Log($"[ArenaRoundManager] Match started! First to {eliminationsToWin} eliminations wins!");
            OnMatchStart?.Invoke();
        }

        public async void EndMatch(PlayerID? winner)
        {
            if (!matchActive || matchEnded) return;

            matchActive = false;
            matchEnded = true;

            if (winner is PlayerID id)
            {
                PlayerMatchStats? stats = await MatchStatBridge.Instance.GetStats(id);
                Debug.Log($"[ArenaRoundManager] Match ended! Winner: {id} with {stats?.kills} eliminations!");
                Debug.Log($"[ArenaRoundManager] Final Stats: {stats}");
            }
            else
            {
                Debug.Log("[ArenaRoundManager] Match ended with no winner.");
            }

            OnMatchEnd?.Invoke(winner);

            if (autoStartNextRound)
            {
                Invoke(nameof(StartMatch), matchEndDelay);
            }
        }

        public void ReloadLevel()
        {
            Debug.Log("[ArenaRoundManager] Reloading level...");
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
            );
        }
        #endregion

        #region Kill Event Handling
        private async void OnPlayerKilled(PlayerID killer, PlayerID victim)
        {
            if (!matchActive || matchEnded) return;

            if (killer == null || MatchStatBridge.Instance == null) return;

            PlayerMatchStats? killerStats = await MatchStatBridge.Instance.GetStats(killer);
            if (killerStats is PlayerMatchStats stats)
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
                        Debug.Log($"[ArenaRoundManager] New leader: {killer} with {currentEliminations} eliminations!");
                        OnLeaderChanged?.Invoke(killer, currentEliminations);
                    }
                }

                // Check win condition
                if (currentEliminations >= eliminationsToWin)
                {
                    EndMatch(killer);
                }
            }

        }
        #endregion

        #region Leaderboard Queries
        public async Task<List<PlayerRanking>> GetLeaderboard()
        {
            if (MatchStatBridge.Instance == null) return new List<PlayerRanking>();

            var allStats = await MatchStatBridge.Instance.GetAllStats();
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

        public async Task<int> GetPlayerRank(GameObject player)
        {
            var leaderboard = await GetLeaderboard();

            var didExtractPlayerId = OwnerIDExtractor.TryExtractPlayerIds(player, out ulong id);
            var playerRanking = leaderboard.FirstOrDefault(r => r.player.id.value == id);
            return playerRanking?.rank ?? -1;
        }

        public async Task<string> GetLeaderboardString()
        {
            var leaderboard = await GetLeaderboard();
            string result = "=== LEADERBOARD ===\n";

            foreach (var ranking in leaderboard)
            {
                result += $"#{ranking.rank} {ranking.player}: {ranking.stats}\n";
            }

            return result;
        }
        #endregion

        #region Debug
        [ContextMenu("Start Match")]
        private void DebugStartMatch()
        {
            StartMatch();
        }

        [ContextMenu("End Match")]
        private void DebugEndMatch()
        {
            EndMatch(currentLeader);
        }

        [ContextMenu("Print Leaderboard")]
        private void DebugPrintLeaderboard()
        {
            Debug.Log(GetLeaderboardString());
        }
        #endregion
    }
}
