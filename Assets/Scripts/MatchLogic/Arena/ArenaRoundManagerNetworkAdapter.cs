using System;
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
        // private MatchStatTracker matchStatTracker_Server;
        private MatchStatNetworkAdapter matchStatNetworkAdapter;
        private ArenaRoundManager arenaRoundManager;

        public ArenaRoundManagerNetworkAdapter(MatchStatNetworkAdapter adapter)
        {
            matchStatNetworkAdapter = adapter;
            matchStatNetworkAdapter.OnMatchStatTrackerCreated.AddListener(OnMatchStatTrackerCreated);

            
        }

        public override void OnSpawn(bool asServer)
        {
            base.OnSpawn(asServer);
        }

        private void OnMatchStatTrackerCreated(MatchStatTracker tracker)
        {
            Debug.Log("[ArenaRoundManagerNetworkAdapter] MatchStatTracker instance received, attaching subscribers");

            // TODO: attach subscribers

            arenaRoundManager = new ArenaRoundManager(tracker);
        }
    }
}
