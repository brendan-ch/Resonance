using UnityEngine;

namespace Resonance.Entities
{
    public class TargetDummy : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float respawnDelay = 3f;
        
        public float CurrentHealth { get; private set; }
        public bool IsDead { get; private set; }
        
        public event System.Action OnDeath;
        public event System.Action OnRespawn;
        
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;

        private void Start()
        {
            _spawnPosition = transform.position;
            _spawnRotation = transform.rotation;
            CurrentHealth = maxHealth;
        }

        public void TakeDamage(float amount)
        {
            if (IsDead) return;
            
            CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
            
            if (CurrentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            if (IsDead) return;
            IsDead = true;
            OnDeath?.Invoke();
            Invoke(nameof(Respawn), respawnDelay);
        }

        private void Respawn()
        {
            IsDead = false;
            transform.position = _spawnPosition;
            transform.rotation = _spawnRotation;
            CurrentHealth = maxHealth;
            OnRespawn?.Invoke();
        }
    }
}