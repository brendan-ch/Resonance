using System;
using System.Collections.Generic;

namespace Resonance.Assemblies.Match
{
    public class DamageTracker
    {
        private float assistTimeWindowMs;
        private float assistDamageThreshold;
        private Dictionary<ulong, List<DamageContribution>> recentDamage = new();

        public DamageTracker(float assistTimeWindowMs, float assistDamageThreshold)
        {
            this.assistTimeWindowMs = assistTimeWindowMs;
            this.assistDamageThreshold = assistDamageThreshold;
        }

        public void RecordDamage(ulong attackerId, ulong victimId, float damageAmount)
        {
            if (attackerId == 0 || victimId == 0 || attackerId == victimId) return;

            if (!recentDamage.ContainsKey(victimId))
            {
                recentDamage[victimId] = new List<DamageContribution>();
            }

            recentDamage[victimId].Add(new DamageContribution
            {
                attackerId = attackerId,
                damageAmount = damageAmount,
                timestampUnixTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            });

            CleanupOldDamage(victimId);
        }

        private void CleanupOldDamage(ulong victimId)
        {
            if (!recentDamage.ContainsKey(victimId)) return;

            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            recentDamage[victimId].RemoveAll(d => currentTime - d.timestampUnixTimeMs > assistTimeWindowMs);
        }

        public HashSet<ulong> GetAssistAttackersForVictim(ulong victimId, ulong killerId)
        {
            var assisters = new HashSet<ulong>();

            if (!recentDamage.ContainsKey(victimId)) return assisters;

            CleanupOldDamage(victimId);

            foreach (var contribution in recentDamage[victimId])
            {
                // Skip if the contributor is the killer
                if (contribution.attackerId == killerId) continue;

                // Check if damage meets threshold
                if (contribution.damageAmount >= assistDamageThreshold)
                {
                    assisters.Add(contribution.attackerId);
                }
            }

            return assisters;
        }

        public void ClearDamageForVictim(ulong victimId)
        {
            if (recentDamage.ContainsKey(victimId))
            {
                recentDamage[victimId].Clear();
            }
        }

        public void Clear()
        {
            recentDamage.Clear();
        }
    }
}
