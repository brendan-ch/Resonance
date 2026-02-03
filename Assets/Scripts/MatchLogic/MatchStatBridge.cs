using System;
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
    [DefaultExecutionOrder(-1)]
    public class MatchStatBridge : NetworkBehaviour
    {
        public static MatchStatBridge Instance
        {
            get
            {
                return InstanceHandler.GetInstance<MatchStatBridge>();
            }
        }

        #region Inspector Fields
        [Header("Assist Settings")]
        [SerializeField] private float assistTimeWindow = 5f; // Time window for assists
        [SerializeField] private float assistDamageThreshold = 20f; // Minimum damage for assist credit
        #endregion 

        public UnityEvent<Dictionary<PlayerID, PlayerMatchStats>> OnAllStatsUpdate = new();
        public UnityEvent<PlayerID, PlayerID> OnPlayerKill = new();

        private MatchStatTracker tracker_Server;

        #region Startup
        private void Awake()
        {
            InstanceHandler.RegisterInstance(this);

            if (isServer)
            {
                tracker_Server = new MatchStatTracker(assistTimeWindow, assistDamageThreshold);
                tracker_Server.OnAllStatsUpdated += FirePlayerStatObservers;
                tracker_Server.OnPlayerKill += FireOnKillObservers;
            }
        }
        #endregion

        #region Server to client methods
        [ObserversRpc]
        private void FirePlayerStatObservers(Dictionary<ulong, PlayerMatchStats> allStats)
        {
            var toPropagate = new Dictionary<PlayerID, PlayerMatchStats>();

            foreach (var (id, stats) in allStats)
            {
                toPropagate.Add(UlongToPlayerId(id), stats);
            }
            OnAllStatsUpdate?.Invoke(toPropagate);
        }

        [ObserversRpc]
        private void FireOnKillObservers(ulong killer, ulong victim)
        {
            var allPlayers = networkManager.players;

            OnPlayerKill?.Invoke(UlongToPlayerId(killer), UlongToPlayerId(victim));
        }
        #endregion

        #region Client to server actions
        public void RecordKill(GameObject killer, GameObject victim)
        {
            if (TryExtractPlayerIds(killer, victim, out ulong killerId, out ulong victimId))
            {
                RecordKill_Server(killerId, victimId);
            }
        }


        [ServerRpc]
        private void RecordKill_Server(ulong killer, ulong victim)
        {
            tracker_Server?.RecordKill(killer, victim);
        }

        public void RecordDamage(GameObject attacker, GameObject victim, float amount)
        {
            if (TryExtractPlayerIds(attacker, victim, out ulong attackerId, out ulong victimId))
            {
                RecordDamage_Server(attackerId, victimId, amount);
            }
        }

        [ServerRpc]
        private void RecordDamage_Server(ulong attacker, ulong victim, float amount)
        {
            tracker_Server?.RecordDamage(attacker, victim, amount);
        }

        public void RecordDeath(GameObject victim)
        {
            if (TryExtractPlayerIds(victim, out ulong victimId))
            {
                RecordDeath_Server(victimId);
            }
        }

        [ServerRpc]
        private void RecordDeath_Server(ulong idPrimitive)
        {
            tracker_Server?.RecordDeath(idPrimitive);
        }

        public void RegisterPlayer(GameObject player)
        {
            if (TryExtractPlayerIds(player, out ulong id))
            {
                RegisterPlayer_Server(id);
            }
        }

        [ServerRpc]
        private void RegisterPlayer_Server(ulong id)
        {
            tracker_Server?.RegisterPlayer(id);
        }

        public void UnregisterPlayer(GameObject player)
        {
            if (TryExtractPlayerIds(player, out ulong id))
            {
                UnregisterPlayer_Server(id);
            }
        }

        private void UnregisterPlayer_Server(ulong id)
        {
            tracker_Server?.UnregisterPlayer(id);
        }

        [Obsolete("Will be made private in the future; should only be consumed by game mode manager")]
        [ServerRpc]
        // for compatibility purposes only
        public void ResetAllStats()
        {
            tracker_Server?.ResetAllStats();
        }

        #endregion

        #region Getters for client
        public async Task<PlayerMatchStats?> GetStats(GameObject player)
        {
            if (TryExtractPlayerIds(player, out ulong playerId))
            {
                return await GetStats(playerId);
            }
            return null;
        }

        [ServerRpc]
        public async Task<PlayerMatchStats> GetStats(ulong playerId)
        {
            return tracker_Server.GetStats(playerId);
        }

        [ServerRpc]
        public async Task<Dictionary<PlayerID, PlayerMatchStats>> GetAllStats()
        {
            return new();
        }
        #endregion

        #region Conversion helpers
        private PlayerID UlongToPlayerId(ulong id)
        {
            return new PlayerID(new PackedULong(id), false);
        }

        private bool TryExtractPlayerIds(GameObject gameObject, out ulong playerId)
        {
            playerId = 0;
            if (!gameObject.TryGetComponent(out PlayerController.PlayerController controller))
                return false;

            if (controller.id?.id.value is ulong idPrimitive)
            {
                playerId = idPrimitive;
                return true;
            }

            return false;
        }

        private bool TryExtractPlayerIds(GameObject first, GameObject second, out ulong firstId, out ulong secondId)
        {
            firstId = 0;
            secondId = 0;

            if (!first.TryGetComponent(out PlayerController.PlayerController firstController) ||
                !second.TryGetComponent(out PlayerController.PlayerController secondController))
                return false;

            if (firstController.id?.id.value is ulong firstIdPrimitive &&
                secondController.id?.id.value is ulong secondIdPrimitive)
            {
                firstId = firstIdPrimitive;
                secondId = secondIdPrimitive;
                return true;
            }

            return false;
        }
        #endregion
    }
}
