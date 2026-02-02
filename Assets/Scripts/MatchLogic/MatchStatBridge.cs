using PurrNet;
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
        #endregion

        #region Kill/Death/Assist Recording
        [ServerRpc]
        public void RecordKill(GameObject killer, GameObject victim)
        {
            if (!killer.TryGetComponent(out PlayerController.PlayerController controller))
            {
                
            }
        }
        #endregion
    }
}
