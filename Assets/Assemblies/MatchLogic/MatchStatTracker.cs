using UnityEngine;
using System.Collections.Generic;

namespace Resonance.Assemblies.Match
{
    public class MatchStatTracker
    {
        public static MatchStatTracker Instance { get; private set; }
        
        #region Inspector Fields
        [Header("Assist Settings")]
        [SerializeField] private float assistTimeWindow = 5f; // Time window for assists
        [SerializeField] private float assistDamageThreshold = 20f; // Minimum damage for assist credit
        #endregion
        
        #region Player Stats Data
        private Dictionary<ulong, PlayerMatchStats> playerStats = new();
        private Dictionary<ulong, List<DamageContribution>> recentDamage = new();
        #endregion
        
        #region Events
        public event System.Action<ulong, PlayerMatchStats> OnStatsUpdated;
        public event System.Action<ulong, ulong> OnPlayerKill; // (killer, victim)
        public event System.Action<ulong, ulong> OnPlayerAssist; // (assister, victim)
        #endregion
        
        #region Player Registration
        public void RegisterPlayer(ulong playerId)
        {
            if (!playerStats.ContainsKey(playerId))
            {
                playerStats[playerId] = new PlayerMatchStats();
                recentDamage[playerId] = new List<DamageContribution>();
                Debug.Log($"[MatchStatTracker] Registered player {playerId}");
            }
        }
        
        public void UnregisterPlayer(ulong playerId)
        {
            playerStats.Remove(playerId);
            recentDamage.Remove(playerId);
        }
        #endregion
        
        #region Damage Tracking
        public void RecordDamage(ulong attackerId, ulong victimId, float damageAmount)
        {
            if (attackerId == 0 || victimId == 0 || attackerId == victimId) return;
            
            RegisterPlayer(attackerId);
            RegisterPlayer(victimId);
            
            if (!recentDamage.ContainsKey(victimId))
            {
                recentDamage[victimId] = new List<DamageContribution>();
            }
            
            recentDamage[victimId].Add(new DamageContribution
            {
                attackerId = attackerId,
                damageAmount = damageAmount,
                timestamp = Time.time
            });
            
            CleanupOldDamage(victimId);
        }
        
        private void CleanupOldDamage(ulong victimId)
        {
            if (!recentDamage.ContainsKey(victimId)) return;
            
            float currentTime = Time.time;
            recentDamage[victimId].RemoveAll(d => currentTime - d.timestamp > assistTimeWindow);
        }
        #endregion
        
        #region Kill/Death/Assist Recording
        public void RecordKill(ulong killerId, ulong victimId)
        {
            if (killerId == 0 || victimId == 0) return;
            
            RegisterPlayer(killerId);
            RegisterPlayer(victimId);
            
            playerStats[killerId] = playerStats[killerId].RecordKill();
            playerStats[victimId] = playerStats[victimId].RecordDeath();
            
            Debug.Log($"[MatchStatTracker] Killer {killerId} killed victim {victimId}! K/D: {playerStats[killerId].kills}/{playerStats[killerId].deaths}");
            
            OnPlayerKill?.Invoke(killerId, victimId);
            
            // Process assists
            ProcessAssists(killerId, victimId);
            
            // Clear damage contributions for victim
            if (recentDamage.ContainsKey(victimId))
            {
                recentDamage[victimId].Clear();
            }
            
            // Notify stats updated
            OnStatsUpdated?.Invoke(killerId, playerStats[killerId]);
            OnStatsUpdated?.Invoke(victimId, playerStats[victimId]);
        }
        
        private void ProcessAssists(ulong killerId, ulong victimId)
        {
            if (!recentDamage.ContainsKey(victimId)) return;
            
            CleanupOldDamage(victimId);
            
            foreach (var contribution in recentDamage[victimId])
            {
                // Skip if the contributor is the killer
                if (contribution.attackerId == killerId) continue;
                 
                // Check if damage meets threshold
                if (contribution.damageAmount >= assistDamageThreshold)
                {
                    RegisterPlayer(contribution.attackerId);
                    playerStats[contribution.attackerId] = playerStats[contribution.attackerId].RecordAssist();
                     
                    Debug.Log($"[MatchStatTracker] Assister {contribution.attackerId} assisted on kill of {victimId}");
                     
                    OnPlayerAssist?.Invoke(contribution.attackerId, victimId);
                    OnStatsUpdated?.Invoke(contribution.attackerId, playerStats[contribution.attackerId]);
                }
            }
        }
        
        public void RecordDeath(ulong victimId)
        {
            if (victimId == 0) return;
            
            RegisterPlayer(victimId);
            playerStats[victimId] = playerStats[victimId].RecordDeath();
            
            Debug.Log($"[MatchStatTracker] Victim {victimId} died. Deaths: {playerStats[victimId].deaths}");
            
            OnStatsUpdated?.Invoke(victimId, playerStats[victimId]);
        }
        #endregion
        
        #region Stats Retrieval
        public PlayerMatchStats GetStats(ulong playerId)
        {
            if (!playerStats.ContainsKey(playerId))
            {
                RegisterPlayer(playerId);
            }
            return playerStats[playerId];
        }
        
        public float GetKDA(ulong playerId)
        {
            if (!playerStats.ContainsKey(playerId))
            {
                return 0f;
            }
            
            PlayerMatchStats stats = playerStats[playerId];
            
            // KDA = (Kills + Assists) / Deaths
            // If deaths is 0, just return kills + assists
            if (stats.deaths == 0)
            {
                return stats.kills + stats.assists;
            }
            
            return (float)(stats.kills + stats.assists) / stats.deaths;
        }
        
        public Dictionary<ulong, PlayerMatchStats> GetAllStats()
        {
            return new Dictionary<ulong, PlayerMatchStats>(playerStats);
        }
        #endregion
        
        #region Match Management
        public void ResetAllStats()
        {
            // Create a list of keys to avoid collection modification during enumeration
            var playerIds = new List<ulong>(playerStats.Keys);
            
            foreach (var playerId in playerIds)
            {
                playerStats[playerId] = new PlayerMatchStats();
                if (recentDamage.ContainsKey(playerId))
                {
                    recentDamage[playerId].Clear();
                }
            }
            
            Debug.Log("[MatchStatTracker] All stats reset!");
        }
        
        public void ResetPlayerStats(ulong playerId)
        {
            if (playerStats.ContainsKey(playerId))
            {
                playerStats[playerId] = new PlayerMatchStats();
                recentDamage[playerId].Clear();
            }
        }
        #endregion
    }
}
