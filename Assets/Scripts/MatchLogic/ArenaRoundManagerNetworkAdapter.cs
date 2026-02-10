using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PurrNet;
using Resonance.Assemblies.Arena;
using Resonance.Assemblies.MatchStat;
using UnityEngine;

namespace Resonance.Match
{
    /// <summary>
    /// NetworkModule adapter which bridges ArenaRoundManager with PurrNet, handling
    /// RPC calls appropriately. On receiving MatchStatNetworkAdapter, subscribes
    /// to the creation of MatchStatTracker to receive match stat events.
    /// </summary>
    [Serializable]
    public class ArenaRoundManagerNetworkAdapter : NetworkModule
    {
        private MatchStatNetworkAdapter matchStatNetworkAdapter;
        private ArenaRoundManager arenaRoundManager;

        #region Cached Client-Side State
        private int _cachedEliminationsToWin;
        private bool _cachedIsMatchActive;
        private bool _cachedIsMatchEnded;

        public int EliminationsToWin => _cachedEliminationsToWin;
        public bool IsMatchActive => _cachedIsMatchActive;
        public bool IsMatchEnded => _cachedIsMatchEnded;
        #endregion

        #region Events
        public event Action OnMatchStart;
        public event Action<PlayerID?> OnMatchEnd;
        public event Action<PlayerID, int> OnLeaderChanged;
        #endregion

        #region Constructor
        public ArenaRoundManagerNetworkAdapter(MatchStatNetworkAdapter adapter)
        {
            matchStatNetworkAdapter = adapter;
            matchStatNetworkAdapter.OnMatchStatTrackerCreated.AddListener(OnMatchStatTrackerCreated);
        }
        #endregion

        #region NetworkModule Lifecycle
        public override void OnSpawn(bool asServer)
        {
            base.OnSpawn(asServer);
        }

        public override void OnDespawned(bool asServer)
        {
            base.OnDespawned(asServer);

            matchStatNetworkAdapter?.OnMatchStatTrackerCreated.RemoveListener(OnMatchStatTrackerCreated);

            if (asServer && arenaRoundManager != null)
            {
                arenaRoundManager.OnMatchStart -= OnArenaMatchStart;
                arenaRoundManager.OnMatchEnd -= OnArenaMatchEnd;
                arenaRoundManager.OnLeaderChanged -= OnArenaLeaderChanged;
                arenaRoundManager = null;
            }
        }
        #endregion

        #region Server Initialization
        private void OnMatchStatTrackerCreated(MatchStatTracker tracker)
        {
            if (arenaRoundManager == null)
            {
                Debug.Log("[ArenaRoundManagerNetworkAdapter] MatchStatTracker instance received, creating ArenaRoundManager and attaching subscribers");
                arenaRoundManager = new ArenaRoundManager(tracker);

                arenaRoundManager.OnMatchStart += OnArenaMatchStart;
                arenaRoundManager.OnMatchEnd += OnArenaMatchEnd;
                arenaRoundManager.OnLeaderChanged += OnArenaLeaderChanged;
            } else
            {
                Debug.LogWarning("[ArenaRoundManagerNetworkAdapter] MatchStatTracker instance received, but ArenaRoundManager instance already exists");
            }
        }
        #endregion

        #region Server Event Handlers
        private void OnArenaMatchStart()
        {
            FireMatchStartObservers(arenaRoundManager.EliminationsToWin);
        }

        private void OnArenaMatchEnd(ulong? winner)
        {
            FireMatchEndObservers(winner);
        }

        private void OnArenaLeaderChanged(ulong newLeader, int eliminations)
        {
            FireLeaderChangedObservers(newLeader, eliminations);
        }
        #endregion

        #region Server to Client RPCs
        [ObserversRpc]
        private void FireMatchStartObservers(int eliminationsToWin)
        {
            Debug.Log($"[ArenaRoundManagerNetworkAdapter] Match started, eliminationsToWin: {eliminationsToWin}");
            _cachedEliminationsToWin = eliminationsToWin;
            _cachedIsMatchActive = true;
            _cachedIsMatchEnded = false;
            OnMatchStart?.Invoke();
        }

        [ObserversRpc]
        private void FireMatchEndObservers(ulong? winner)
        {
            Debug.Log($"[ArenaRoundManagerNetworkAdapter] Match ended, winner: {winner}");
            _cachedIsMatchActive = false;
            _cachedIsMatchEnded = true;
            PlayerID? winnerPlayerId = OwnerIDExtractor.UlongNullableToPlayerIdNullable(winner);
            OnMatchEnd?.Invoke(winnerPlayerId);
        }

        [ObserversRpc]
        private void FireLeaderChangedObservers(ulong newLeader, int eliminations)
        {
            Debug.Log($"[ArenaRoundManagerNetworkAdapter] Leader changed: {newLeader} with {eliminations} eliminations");
            OnLeaderChanged?.Invoke(
                OwnerIDExtractor.UlongToPlayerId(newLeader),
                eliminations
            );
        }
        #endregion

        #region Client to Server Actions (Public API)
        public void StartMatch()
        {
            Debug.Log("[ArenaRoundManagerNetworkAdapter] StartMatch requested");
            StartMatch_Server();
        }

        [ServerRpc]
        private void StartMatch_Server()
        {
            arenaRoundManager?.StartMatch();
        }

        /// <summary>
        /// Ends the match with a winner. Note that in most scenarios, this won't
        /// need to be called directly; the class 
        /// </summary>
        /// <param name="winner"></param>
        public void EndMatch(PlayerID? winner)
        {
            ulong? winnerUlong = winner?.id.value;
            Debug.Log($"[ArenaRoundManagerNetworkAdapter] EndMatch requested, winner: {winnerUlong}");
            EndMatch_Server(winnerUlong);
        }

        [ServerRpc]
        private void EndMatch_Server(ulong? winner)
        {
            arenaRoundManager?.EndMatch(winner);
        }
        #endregion

        #region Getters (Client Callable)
        [ServerRpc]
        public async Task<bool> GetIsMatchActive()
        {
            return arenaRoundManager?.IsMatchActive ?? false;
        }

        [ServerRpc]
        public async Task<bool> GetIsMatchEnded()
        {
            return arenaRoundManager?.IsMatchEnded ?? false;
        }

        [ServerRpc]
        public async Task<int> GetEliminationsToWin()
        {
            return arenaRoundManager?.EliminationsToWin ?? 0;
        }

        [ServerRpc]
        public async Task<PlayerID?> GetCurrentLeader()
        {
            return OwnerIDExtractor.UlongNullableToPlayerIdNullable(arenaRoundManager?.CurrentLeader);
        }

        [ServerRpc]
        public async Task<int> GetHighestEliminations()
        {
            return arenaRoundManager?.HighestEliminations ?? 0;
        }

        [ServerRpc]
        public async Task<List<PlayerRanking>> GetLeaderboard()
        {
            if (arenaRoundManager == null) return new List<PlayerRanking>();
            return arenaRoundManager.GetLeaderboard();
        }

        [ServerRpc]
        public async Task<string> GetLeaderboardString()
        {
            return arenaRoundManager?.GetLeaderboardString() ?? "";
        }
        #endregion
    }
}
