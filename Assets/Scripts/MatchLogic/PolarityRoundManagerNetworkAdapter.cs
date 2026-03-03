using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PurrNet;
using Resonance.Assemblies.MatchStat;
using Resonance.Assemblies.Polarity;
using Resonance.Assemblies.SharedGameLogic;
using UnityEngine;

namespace Resonance.Match
{
    [Serializable]
    public class PolarityRoundManagerNetworkAdapter : NetworkModule
    {
        private MatchStatNetworkAdapter matchStatNetworkAdapter;
        private PolarityRoundManager.PolarityRoundManagerConfig config;

        #region Cached Client-Side State
        private int cachedTeamEliminationsToWin;
        private BaseMatchState cachedMatchState;
        private double cachedSecondsUntilRoleSwitch;

        public int TeamEliminationsToWin => cachedTeamEliminationsToWin;
        public bool IsMatchActive => cachedMatchState == BaseMatchState.MatchActive;
        public bool IsMatchEnded => cachedMatchState == BaseMatchState.MatchEnded;
        public double SecondsRemainingUntilRoleSwitch => cachedSecondsUntilRoleSwitch;
        #endregion

        private PolarityRoundManager polarityRoundManager;

        // TODO: unify shared events with base class
        #region Events
        public event Action<BaseMatchState, BaseMatchState> OnMatchStateChange;
        public event Action<float> OnMatchCountdownStart;
        public event Action OnMatchStart;
        public event Action<TeamId> OnMatchEnd;
        public event Action OnRoleSwitch;
        public event Action<double> OnRoleSwitchTimerElapsed;
        #endregion

        #region Constructor
        public PolarityRoundManagerNetworkAdapter(
            MatchStatNetworkAdapter adapter,
            PolarityRoundManager.PolarityRoundManagerConfig config)
        {
            matchStatNetworkAdapter = adapter;
            this.config = config;
            matchStatNetworkAdapter.OnMatchStatTrackerCreated.AddListener(OnMatchStatTrackerCreated);
        }

        public PolarityRoundManagerNetworkAdapter(MatchStatNetworkAdapter adapter)
            : this(adapter, PolarityRoundManager.PolarityRoundManagerConfig.Default)
        {
        }
        #endregion

        #region Initialization
        public override void OnSpawn(bool asServer)
        {
            base.OnSpawn(asServer);
        }

        public override void OnDespawned(bool asServer)
        {
            base.OnDespawned(asServer);

            matchStatNetworkAdapter?.OnMatchStatTrackerCreated.RemoveListener(OnMatchStatTrackerCreated);

            if (asServer && polarityRoundManager != null)
            {
                DestroyPolarityRoundManager();
            }
        }

        private void OnMatchStatTrackerCreated(MatchStatTracker tracker)
        {
            if (polarityRoundManager == null)
            {
                CreatePolarityRoundManagerWithMatchStatTracker(tracker);
            }
            else
            {
                Debug.LogWarning("[PolarityRoundManagerNetworkAdapter] MatchStatTracker instance received but PolarityRoundManager instance already exists; re-creating with new match stat tracker");
                DestroyPolarityRoundManager();
                CreatePolarityRoundManagerWithMatchStatTracker(tracker);
            }
        }

        private void DestroyPolarityRoundManager()
        {
            if (polarityRoundManager != null)
            {
                polarityRoundManager.OnMatchStart -= OnPolarityMatchStart;
                polarityRoundManager.OnMatchEnd -= OnPolarityMatchEnd;
                polarityRoundManager.OnMatchCountdownStart -= OnPolarityMatchCountdownStart;
                polarityRoundManager.OnMatchStateChange -= OnPolarityMatchStateChange;
                polarityRoundManager.OnRoleSwitch -= OnPolarityRoleSwitch;
                polarityRoundManager.OnRoleSwitchTimerElapsed -= OnPolarityRoleSwitchTimerElapsed;
                polarityRoundManager = null;
            }
        }

        private void CreatePolarityRoundManagerWithMatchStatTracker(MatchStatTracker tracker)
        {
            Debug.Log("[PolarityRoundManagerNetworkAdapter] MatchStatTracker instance received, creating PolarityRoundManager and attaching subscribers");
            polarityRoundManager = new PolarityRoundManager(tracker, config);

            polarityRoundManager.OnMatchStart += OnPolarityMatchStart;
            polarityRoundManager.OnMatchEnd += OnPolarityMatchEnd;
            polarityRoundManager.OnMatchCountdownStart += OnPolarityMatchCountdownStart;
            polarityRoundManager.OnMatchStateChange += OnPolarityMatchStateChange;
            polarityRoundManager.OnRoleSwitch += OnPolarityRoleSwitch;
            polarityRoundManager.OnRoleSwitchTimerElapsed += OnPolarityRoleSwitchTimerElapsed;
        }
        #endregion

        #region Server Event Handlers
        private void OnPolarityMatchCountdownStart(float countdownSeconds)
        {
            FireMatchCountdownStartObservers(countdownSeconds);
        }

        private void OnPolarityMatchStart()
        {
            FireMatchStartObservers(polarityRoundManager.TeamEliminationsToWin);
        }

        private void OnPolarityMatchEnd(TeamId winningTeamId)
        {
            FireMatchEndObservers((int)winningTeamId);
        }

        private void OnPolarityMatchStateChange(BaseMatchState oldState, BaseMatchState newState)
        {
            FireMatchStateChangeObservers((int)oldState, (int)newState);
        }

        private void OnPolarityRoleSwitch()
        {
            FireRoleSwitchObservers();
        }

        private void OnPolarityRoleSwitchTimerElapsed(double secondsRemaining)
        {
            FireRoleSwitchTimerElapsedObservers(secondsRemaining);
        }
        #endregion

        #region Server to Client RPCs
        [ObserversRpc]
        private void FireMatchCountdownStartObservers(float countdownSeconds)
        {
            Debug.Log($"[PolarityRoundManagerNetworkAdapter] Match countdown of {countdownSeconds}s started");
            OnMatchCountdownStart?.Invoke(countdownSeconds);
        }

        [ObserversRpc]
        private void FireMatchStartObservers(int teamEliminationsToWin)
        {
            Debug.Log($"[PolarityRoundManagerNetworkAdapter] Match started, teamEliminationsToWin: {teamEliminationsToWin}");
            cachedTeamEliminationsToWin = teamEliminationsToWin;
            OnMatchStart?.Invoke();
        }

        [ObserversRpc]
        private void FireMatchEndObservers(int winningTeamId)
        {
            Debug.Log($"[PolarityRoundManagerNetworkAdapter] Match ended, winning team: {winningTeamId}");
            OnMatchEnd?.Invoke((TeamId)winningTeamId);
        }

        [ObserversRpc]
        private void FireMatchStateChangeObservers(int oldState, int newState)
        {
            Debug.Log($"[PolarityRoundManagerNetworkAdapter] Match state changed from {oldState} to {newState}");
            cachedMatchState = (BaseMatchState)newState;
            OnMatchStateChange?.Invoke((BaseMatchState)oldState, (BaseMatchState)newState);
        }

        [ObserversRpc]
        private void FireRoleSwitchObservers()
        {
            Debug.Log("[PolarityRoundManagerNetworkAdapter] Roles switched");
            OnRoleSwitch?.Invoke();
        }

        [ObserversRpc]
        private void FireRoleSwitchTimerElapsedObservers(double secondsRemaining)
        {
            cachedSecondsUntilRoleSwitch = secondsRemaining;
            OnRoleSwitchTimerElapsed?.Invoke(secondsRemaining);
        }
        #endregion

        #region Client to Server Actions (Public API)
        public void RegisterPlayersForTeamA(List<PlayerID> players)
        {
            RegisterPlayersForTeamA_Server(OwnerIDExtractor.PlayerIdListToUlongList(players));
        }

        [ServerRpc]
        private void RegisterPlayersForTeamA_Server(List<ulong> players)
        {
            polarityRoundManager?.RegisterPlayersForTeamA(new HashSet<ulong>(players));
        }

        public void RegisterPlayersForTeamB(List<PlayerID> players)
        {
            RegisterPlayersForTeamB_Server(OwnerIDExtractor.PlayerIdListToUlongList(players));
        }

        [ServerRpc]
        private void RegisterPlayersForTeamB_Server(List<ulong> players)
        {
            polarityRoundManager?.RegisterPlayersForTeamB(new HashSet<ulong>(players));
        }

        public void StartMatchCountdown()
        {
            Debug.Log("[PolarityRoundManagerNetworkAdapter] StartMatchCountdown requested");
            StartMatchCountdown_Server();
        }

        [ServerRpc]
        private void StartMatchCountdown_Server()
        {
            polarityRoundManager?.StartMatchCountdown();
        }

        public void EndMatch(TeamId winningTeamId)
        {
            Debug.Log($"[PolarityRoundManagerNetworkAdapter] EndMatch requested, winning team: {winningTeamId}");
            EndMatch_Server((int)winningTeamId);
        }

        [ServerRpc]
        private void EndMatch_Server(int winningTeamId)
        {
            _ = polarityRoundManager?.EndMatch((TeamId)winningTeamId);
        }
        #endregion

        #region Getters (Client Callable)
        [ServerRpc]
        public async Task<Dictionary<int, List<PlayerRanking>>> GetLeaderboard_Server()
        {
            var leaderboard = polarityRoundManager?.GetLeaderboard() ?? new Dictionary<TeamId, List<PlayerRanking>>();
            var result = new Dictionary<int, List<PlayerRanking>>();
            foreach (var kvp in leaderboard)
            {
                result[(int)kvp.Key] = kvp.Value;
            }
            return result;
        }

        public async Task<Dictionary<TeamId, List<PlayerRanking>>> GetLeaderboard()
        {
            var raw = await GetLeaderboard_Server();
            var result = new Dictionary<TeamId, List<PlayerRanking>>();
            foreach (var kvp in raw)
            {
                result[(TeamId)kvp.Key] = kvp.Value;
            }
            return result;
        }

        public async Task<PolarityRoundManager.Team> GetTeam(TeamId teamId)
        {
            return await GetTeam_Server((int)teamId);
        }

        [ServerRpc]
        private async Task<PolarityRoundManager.Team> GetTeam_Server(int teamId)
        {
            return polarityRoundManager != null
                ? polarityRoundManager.GetTeam((TeamId)teamId)
                : default;
        }

        public async Task<List<PlayerID>> GetPlayersForTeam(TeamId teamId)
        {
            var ulongs = await GetPlayersForTeam_Server((int)teamId);
            var result = new List<PlayerID>();
            foreach (var id in ulongs)
            {
                result.Add(OwnerIDExtractor.UlongToPlayerId(id));
            }
            return result;
        }

        [ServerRpc]
        private async Task<List<ulong>> GetPlayersForTeam_Server(int teamId)
        {
            var players = polarityRoundManager?.GetPlayersForTeam((TeamId)teamId) ?? new HashSet<ulong>();
            return new List<ulong>(players);
        }

        [ServerRpc]
        public async Task<double> GetSecondsUntilNextRoleSwitch()
        {
            return polarityRoundManager?.SecondsUntilNextRoleSwitch ?? 0;
        }
        #endregion

    }
}
