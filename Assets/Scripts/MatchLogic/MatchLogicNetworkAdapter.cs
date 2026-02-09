using PurrNet;
using UnityEngine;

namespace Resonance.Match
{
    /// <summary>
    /// Central NetworkBehaviour that hosts all match-related NetworkModules.
    /// Provides singleton access to submodules for match statistics and game mode logic.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public class MatchLogicNetworkAdapter : NetworkBehaviour
    {
        public static MatchLogicNetworkAdapter Instance => InstanceHandler.GetInstance<MatchLogicNetworkAdapter>();

        #region Inspector Fields
        [Header("Match Stats Settings")]
        [SerializeField] private float assistTimeWindow = 5f;
        [SerializeField] private float assistDamageThreshold = 20f;
        #endregion

        #region Modules
        private MatchStatNetworkAdapter _matchStatAdapter;
        public MatchStatNetworkAdapter MatchStats => _matchStatAdapter;

        private ArenaRoundManagerNetworkAdapter _arenaRoundManagerNetworkAdapter;
        public ArenaRoundManagerNetworkAdapter ArenaRoundManager => _arenaRoundManagerNetworkAdapter;
        #endregion

        #region Lifecycle
        private void Awake()
        {
            InstanceHandler.RegisterInstance(this);
            _matchStatAdapter = new MatchStatNetworkAdapter(assistTimeWindow, assistDamageThreshold);
            _arenaRoundManagerNetworkAdapter = new ArenaRoundManagerNetworkAdapter(_matchStatAdapter);
        }

        private void OnDestroy()
        {
            InstanceHandler.UnregisterInstance<MatchLogicNetworkAdapter>();
        }
        #endregion
    }
}
