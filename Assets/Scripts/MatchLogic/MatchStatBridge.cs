using System.Collections.Generic;
using System.Threading.Tasks;
using PurrNet;
using Resonance.Assemblies.Match;
using UnityEngine;

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
                tracker_Server = new MatchStatTracker();
            }
        }
        #endregion

        #region Kill/Death/Assist Recording
        [ServerRpc]
        public void RecordKill(GameObject killer, GameObject victim)
        {
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
