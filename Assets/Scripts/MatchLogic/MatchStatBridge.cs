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
    public class MatchStatBridge : NetworkBehaviour
    {
        public static MatchStatBridge Instance { get; private set; }

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
            OnPlayerKill?.Invoke(UlongToPlayerId(killer), UlongToPlayerId(victim));
        }
        #endregion

        #region Kill/Death/Assist recording
        public void RecordKill(GameObject killer, GameObject victim)
        {
            PlayerController.PlayerController killerController, victimController;
            if (TryExtractPlayerControllers(killer, victim, out killerController, out victimController))
            {
                return;
            }

            if (killerController.id?.id.value is ulong killerIdPrimitive
                && victimController.id?.id.value is ulong victimIdPrimitive)
            {
                RecordKill_Server(killerIdPrimitive, victimIdPrimitive);
            }
        }


        [ServerRpc]
        private void RecordKill_Server(ulong killer, ulong victim)
        {
            tracker_Server?.RecordKill(killer, victim);
        }

        public void RecordDamage(GameObject attacker, GameObject victim, float amount)
        {
            PlayerController.PlayerController attackerController, victimController;
            if (TryExtractPlayerControllers(attacker, victim, out attackerController, out victimController))
            {
                return;
            }

            if (attackerController.id?.id.value is ulong attackerIdPrimitive
                && victimController.id?.id.value is ulong victimIdPrimitive)
            {
                RecordDamage_Server(attackerIdPrimitive, victimIdPrimitive, amount);
            }
        }

        [ServerRpc]
        private void RecordDamage_Server(ulong attacker, ulong victim, float amount)
        {
            tracker_Server?.RecordDamage(attacker, victim, amount);
        }

        public void RecordDeath(GameObject victim)
        {
            if (!victim.TryGetComponent(out PlayerController.PlayerController controller))
            {
                return;
            }

            if (controller.id?.id.value is ulong idPrimitive)
            {
                RecordDeath_Server(idPrimitive);
            }
        }

        [ServerRpc]
        private void RecordDeath_Server(ulong idPrimitive)
        {
            tracker_Server?.RecordDeath(idPrimitive);
        }

        #endregion

        #region Getters for clients
        [ServerRpc]
        public async Task<PlayerMatchStats> GetStats(PlayerID playerId)
        {
            return new PlayerMatchStats { };
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

        private static bool TryExtractPlayerControllers(GameObject first, GameObject second, out PlayerController.PlayerController killerController, out PlayerController.PlayerController victimController)
        {
            if (!first.TryGetComponent(out killerController) || !second.TryGetComponent(out victimController))
            {
                victimController = null;
                return false;
            }

            return true;
        }
        #endregion
    }
}
