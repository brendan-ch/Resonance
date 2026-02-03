using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PurrNet;
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

            tracker_Server = new MatchStatTracker(assistTimeWindow, assistDamageThreshold);
            tracker_Server.OnAllStatsUpdated += (allStats) =>
            {
                FirePlayerStatObservers(JsonConvert.SerializeObject(allStats));
            };
            tracker_Server.OnPlayerKill += FireOnKillObservers;
        }
        #endregion

        #region Server to client methods
        [ObserversRpc]
        private void FirePlayerStatObservers(string serializedPlayerData)
        {
            Debug.Log($"Stats: {serializedPlayerData}");

            var allStats = JsonConvert.DeserializeObject<Dictionary<ulong, PlayerMatchStats>>(serializedPlayerData);

            Dictionary<PlayerID, PlayerMatchStats> toPropagate = UlongDictionaryToPlayerIDDictionary(allStats);
            OnAllStatsUpdate?.Invoke(toPropagate);

        }

        [ObserversRpc]
        private void FireOnKillObservers(ulong killer, ulong victim)
        {
            OnPlayerKill?.Invoke(PlayerIDExtractor.UlongToPlayerId(killer), PlayerIDExtractor.UlongToPlayerId(victim));
        }
        #endregion

        #region Client to server actions
        public void RecordKill(GameObject killer, GameObject victim)
        {
            if (PlayerIDExtractor.TryExtractPlayerIds(killer, victim, out ulong killerId, out ulong victimId))
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
            if (PlayerIDExtractor.TryExtractPlayerIds(attacker, victim, out ulong attackerId, out ulong victimId))
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
            if (PlayerIDExtractor.TryExtractPlayerIds(victim, out ulong victimId))
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
            if (PlayerIDExtractor.TryExtractPlayerIds(player, out ulong id))
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
            if (PlayerIDExtractor.TryExtractPlayerIds(player, out ulong id))
            {
                UnregisterPlayer_Server(id);
            }
        }

        private void UnregisterPlayer_Server(ulong id)
        {
            tracker_Server?.UnregisterPlayer(id);
        }

        [ServerRpc]
        public void ResetAllStats()
        {
            tracker_Server?.ResetAllStats();
        }

        #endregion

        #region Getters for client
        public async Task<PlayerMatchStats?> GetStats(GameObject player)
        {
            if (PlayerIDExtractor.TryExtractPlayerIds(player, out ulong playerId))
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
            return UlongDictionaryToPlayerIDDictionary(tracker_Server.GetAllStats());
        }
        #endregion

        #region Conversion helpers
        private Dictionary<PlayerID, PlayerMatchStats> UlongDictionaryToPlayerIDDictionary(Dictionary<ulong, PlayerMatchStats> allStats)
        {
            var toPropagate = new Dictionary<PlayerID, PlayerMatchStats>();
            foreach (var (id, stats) in allStats)
            {
                toPropagate.Add(PlayerIDExtractor.UlongToPlayerId(id), stats);
            }

            return toPropagate;
        }
        #endregion
    }
}
