using UnityEngine;
using System.Collections.Generic;

namespace Resonance.Match
{
    public class MatchStatTracker : MonoBehaviour
    {
        public static MatchStatTracker Instance { get; private set; }
        
        #region Inspector Fields
        [Header("Assist Settings")]
        [SerializeField] private float assistTimeWindow = 5f; // Time window for assists
        [SerializeField] private float assistDamageThreshold = 20f; // Minimum damage for assist credit
        #endregion
        
        #region Player Stats Data
        private Dictionary<GameObject, PlayerMatchStats> playerStats = new Dictionary<GameObject, PlayerMatchStats>();
        private Dictionary<GameObject, List<DamageContribution>> recentDamage = new Dictionary<GameObject, List<DamageContribution>>();
        #endregion
        
        #region Events
        public event System.Action<GameObject, PlayerMatchStats> OnStatsUpdated;
        public event System.Action<GameObject, GameObject> OnPlayerKill; // (killer, victim)
        public event System.Action<GameObject, GameObject> OnPlayerAssist; // (assister, victim)
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
        
        #region Player Registration
        public void RegisterPlayer(GameObject player)
        {
            if (!playerStats.ContainsKey(player))
            {
                playerStats[player] = new PlayerMatchStats();
                recentDamage[player] = new List<DamageContribution>();
                Debug.Log($"[MatchStatTracker] Registered player: {player.name}");
            }
        }
        
        public void UnregisterPlayer(GameObject player)
        {
            playerStats.Remove(player);
            recentDamage.Remove(player);
        }
        #endregion
        
        #region Damage Tracking
        public void RecordDamage(GameObject attacker, GameObject victim, float damageAmount)
        {
            if (attacker == null || victim == null || attacker == victim) return;
            
            RegisterPlayer(attacker);
            RegisterPlayer(victim);
            
            if (!recentDamage.ContainsKey(victim))
            {
                recentDamage[victim] = new List<DamageContribution>();
            }
            
            recentDamage[victim].Add(new DamageContribution
            {
                attacker = attacker,
                damageAmount = damageAmount,
                timestamp = Time.time
            });
            
            CleanupOldDamage(victim);
        }
        
        private void CleanupOldDamage(GameObject victim)
        {
            if (!recentDamage.ContainsKey(victim)) return;
            
            float currentTime = Time.time;
            recentDamage[victim].RemoveAll(d => currentTime - d.timestamp > assistTimeWindow);
        }
        #endregion
        
        #region Kill/Death/Assist Recording
        public void RecordKill(GameObject killer, GameObject victim)
        {
            if (killer == null || victim == null) return;
            
            RegisterPlayer(killer);
            RegisterPlayer(victim);
            
            // Record kill
            playerStats[killer].kills++;
            playerStats[killer].killStreak++;
            
            // Check for best killstreak
            if (playerStats[killer].killStreak > playerStats[killer].bestKillStreak)
            {
                playerStats[killer].bestKillStreak = playerStats[killer].killStreak;
            }
            
            // Record death
            playerStats[victim].deaths++;
            playerStats[victim].killStreak = 0; // Reset victim's killstreak
            
            Debug.Log($"[MatchStatTracker] {killer.name} killed {victim.name}! K/D: {playerStats[killer].kills}/{playerStats[killer].deaths}");
            
            OnPlayerKill?.Invoke(killer, victim);
            
            // Process assists
            ProcessAssists(killer, victim);
            
            // Clear damage contributions for victim
            if (recentDamage.ContainsKey(victim))
            {
                recentDamage[victim].Clear();
            }
            
            // Notify stats updated
            OnStatsUpdated?.Invoke(killer, playerStats[killer]);
            OnStatsUpdated?.Invoke(victim, playerStats[victim]);
        }
        
        private void ProcessAssists(GameObject killer, GameObject victim)
        {
            if (!recentDamage.ContainsKey(victim)) return;
            
            CleanupOldDamage(victim);
            
            foreach (var contribution in recentDamage[victim])
            {
                // Skip if the contributor is the killer
                if (contribution.attacker == killer) continue;
                
                // Check if damage meets threshold
                if (contribution.damageAmount >= assistDamageThreshold)
                {
                    RegisterPlayer(contribution.attacker);
                    playerStats[contribution.attacker].assists++;
                    
                    Debug.Log($"[MatchStatTracker] {contribution.attacker.name} assisted on kill of {victim.name}");
                    
                    OnPlayerAssist?.Invoke(contribution.attacker, victim);
                    OnStatsUpdated?.Invoke(contribution.attacker, playerStats[contribution.attacker]);
                }
            }
        }
        
        public void RecordDeath(GameObject victim)
        {
            if (victim == null) return;
            
            RegisterPlayer(victim);
            playerStats[victim].deaths++;
            playerStats[victim].killStreak = 0;
            
            Debug.Log($"[MatchStatTracker] {victim.name} died. Deaths: {playerStats[victim].deaths}");
            
            OnStatsUpdated?.Invoke(victim, playerStats[victim]);
        }
        #endregion
        
        #region Stats Retrieval
        public PlayerMatchStats GetStats(GameObject player)
        {
            if (!playerStats.ContainsKey(player))
            {
                RegisterPlayer(player);
            }
            return playerStats[player];
        }
        
        public float GetKDA(GameObject player)
        {
            if (!playerStats.ContainsKey(player))
            {
                return 0f;
            }
            
            PlayerMatchStats stats = playerStats[player];
            
            // KDA = (Kills + Assists) / Deaths
            // If deaths is 0, just return kills + assists
            if (stats.deaths == 0)
            {
                return stats.kills + stats.assists;
            }
            
            return (float)(stats.kills + stats.assists) / stats.deaths;
        }
        
        public Dictionary<GameObject, PlayerMatchStats> GetAllStats()
        {
            return new Dictionary<GameObject, PlayerMatchStats>(playerStats);
        }
        #endregion
        
        #region Match Management
        public void ResetAllStats()
        {
            // Create a list of keys to avoid collection modification during enumeration
            var players = new List<GameObject>(playerStats.Keys);
            
            foreach (var player in players)
            {
                playerStats[player] = new PlayerMatchStats();
                if (recentDamage.ContainsKey(player))
                {
                    recentDamage[player].Clear();
                }
            }
            
            Debug.Log("[MatchStatTracker] All stats reset!");
        }
        
        public void ResetPlayerStats(GameObject player)
        {
            if (playerStats.ContainsKey(player))
            {
                playerStats[player] = new PlayerMatchStats();
                recentDamage[player].Clear();
            }
        }
        #endregion
    }
}