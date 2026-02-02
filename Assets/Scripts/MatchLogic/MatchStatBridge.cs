using System.Collections.Generic;
using System.Threading.Tasks;
using PurrNet;
using PurrNet.Packing;
using Resonance.Assemblies.Match;
using UnityEngine;
using UnityEngine.Events;

namespace Resonance.Match
{
    /// <summary>
    /// Bridge MatchStatTracker with Unity, specifically regarding
    /// RPC calls, to ensure that logic only runs on the server.
    /// </summary>
    public class MatchStatBridge : NetworkBehaviour
    {
        public static MatchStatBridge Instance { get; private set; }

        #region Inspector Fields
        [Header("Assist Settings")]
        [SerializeField] private float assistTimeWindow = 5f; // Time window for assists
        [SerializeField] private float assistDamageThreshold = 20f; // Minimum damage for assist credit
        #endregion 

        public UnityEvent<Dictionary<PlayerID, PlayerMatchStats>> OnAllStatsUpdate = new();

        private MatchStatTracker tracker_Server;

        #region Startup
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (isServer)
            {
                tracker_Server = new MatchStatTracker(assistTimeWindow, assistDamageThreshold);
                tracker_Server.OnAllStatsUpdated += FirePlayerStatObservers;
            }
        }
        #endregion

        #region Server to client methods
        [ObserversRpc]
        private void FirePlayerStatObservers(Dictionary<ulong, PlayerMatchStats> allStats)
        {
            // define and fire UI-related listeners, etc.
            // new PackedULong();
            // new PlayerID();
            
            var toPropagate = new Dictionary<PlayerID, PlayerMatchStats>();
            
            foreach (var (id, stats) in allStats)
            {
                toPropagate.Add(new PlayerID(new PackedULong(id), false), stats);
            }
            OnAllStatsUpdate?.Invoke(toPropagate);
        }
        #endregion

        #region Kill/Death/Assist recording
        [ServerRpc]
        public void RecordKill(GameObject killer, GameObject victim)
        {
            // not sure if GameObject can be sent like this, if not then
            // define another method RecordKill_Server which takes player ID instead

            if (!killer.TryGetComponent(out PlayerController.PlayerController controller))
            {
                return;
            }
        }

        
        #endregion

        #region Getters for clients
        [ServerRpc]
        public async Task<PlayerMatchStats> GetStats(PlayerID playerId)
        {
            return new PlayerMatchStats {};
        }

        [ServerRpc]
        public async Task<Dictionary<PlayerID, PlayerMatchStats>> GetAllStats()
        {
            return new();
        }
        #endregion
    }
}
