using Resonance.PlayerController;
using Resonance.UI;
using Resonance.Match;
using UnityEngine;
using System.Collections;

namespace Resonance.Player
{
    public class PlayerStats : MonoBehaviour
    {
        #region Inspector Fields
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private bool respawnOnDeath = true;
        
        public HealthBar healthBar;
        #endregion
        
        #region Properties
        public float CurrentHealth { get; private set; }
        public float MaxHealth => maxHealth;
        public bool IsDead { get; private set; }
        #endregion
        
        #region Events
        public event System.Action OnPlayerDeath;
        public event System.Action OnPlayerRespawn;
        #endregion
        
        #region Component References
        private PlayerState _playerState;
        private CharacterController _characterController;
        private PlayerController.PlayerController _playerController;
        private Animator _animator;
        #endregion
        
        #region Damage Tracking
        private GameObject lastAttacker;
        private float lastDamageTime;
        #endregion

        #region Startup

        public void Awake()
        {
            _playerState = GetComponent<PlayerState>();
            _characterController = GetComponent<CharacterController>();
            _playerController = GetComponent<PlayerController.PlayerController>();
            _animator = GetComponent<Animator>();
        }
        
        private void Start()
        {
            CurrentHealth = maxHealth;

            if (healthBar != null)
            {
                healthBar.SetSliderMax(maxHealth);
            }
            
            // Register with match stat tracker
            if (MatchStatTracker.Instance != null)
            {
                MatchStatTracker.Instance.RegisterPlayer(gameObject);
            }
        }
        
        private void OnDestroy()
        {
            // Unregister from match stat tracker
            if (MatchStatTracker.Instance != null)
            {
                MatchStatTracker.Instance.UnregisterPlayer(gameObject);
            }
        }
        #endregion

        #region Health Management
        public void TakeDamage(float amount)
        {
            TakeDamage(amount, null);
        }
        
        public void TakeDamage(float amount, GameObject attacker)
        {
            if (IsDead) return;
            
            // Track damage for assists
            if (attacker != null && attacker != gameObject && MatchStatTracker.Instance != null)
            {
                MatchStatTracker.Instance.RecordDamage(attacker, gameObject, amount);
                lastAttacker = attacker;
                lastDamageTime = Time.time;
            }
            
            CurrentHealth -= amount;
            CurrentHealth = Mathf.Max(0, CurrentHealth);
            
            if (healthBar != null)
            {
                healthBar.SetSlider(CurrentHealth);
            }
            
            if (CurrentHealth <= 0)
            {
                Die(attacker);
            }
        }
        
        public void Heal(float amount)
        {
            if (IsDead) return;
            
            CurrentHealth += amount;
            CurrentHealth = Mathf.Min(CurrentHealth, maxHealth);
            
            if (healthBar != null)
            {
                healthBar.SetSlider(CurrentHealth);
            }
        }
        #endregion

        #region Death & Respawn
        private void Die(GameObject killer = null)
        {
            if (IsDead) return;

            IsDead = true;

            if (_playerController != null)
            {
                _playerController.IsPlayerDead = true;
            }

            if (_playerState != null)
            {
                _playerState.SetPlayerMovementState(PlayerMovementState.Dead);
            }

            if (_playerController != null)
            {
                _playerController.enabled = false;
            }

            if (_characterController != null)
            {
                _characterController.enabled = false;
            }

            if (_animator != null)
            {
                _animator.enabled = false;
            }

            Debug.Log($"[PlayerStats] {gameObject.name} died!");
            
            // Record kill/death in match stats
            if (MatchStatTracker.Instance != null)
            {
                if (killer != null && killer != gameObject)
                {
                    MatchStatTracker.Instance.RecordKill(killer, gameObject);
                }
                else
                {
                    // Suicide or environmental death
                    MatchStatTracker.Instance.RecordDeath(gameObject);
                }
            }
            
            OnPlayerDeath?.Invoke();

            if (respawnOnDeath)
            {
                StartCoroutine(RespawnCoroutine());
            }
        }

        private IEnumerator RespawnCoroutine()
        {
            float respawnDelay = Resonance.Player.Respawn.Instance != null ? 
                                 Resonance.Player.Respawn.Instance.RespawnDelay : 3f;
            
            Debug.Log($"[PlayerStats] {gameObject.name} respawning in {respawnDelay}s");
            yield return new WaitForSeconds(respawnDelay);
            Respawn();
        }

        public void Respawn()
        {
            StartCoroutine(RespawnSequence());
        }
        
        private IEnumerator RespawnSequence()
        {
            IsDead = false;
            
            // Clear damage tracking
            lastAttacker = null;
            
            if (_playerState != null)
            {
                _playerState.SetPlayerMovementState(PlayerMovementState.Idling);
            }
            
            if (_playerController != null)
            {
                _playerController.ResetState();
            }
            
            Transform spawnPoint = Resonance.Player.Respawn.Instance?.GetSpawnPoint();

            if (spawnPoint != null && _characterController != null)
            {
                transform.position = spawnPoint.position;
                transform.rotation = spawnPoint.rotation;
            }
            
            if (_characterController != null)
            {
                if (_characterController.stepOffset <= 0)
                {
                    _characterController.stepOffset = 0.3f;
                }
                
                _characterController.enabled = true;
            }
            
            if (_playerController != null)
            {
                _playerController.enabled = true;
            }
            
            if (_animator != null)
            {
                _animator.enabled = true;
            }
            
            yield return null;
            
            if (_playerController != null)
            {
                _playerController.IsPlayerDead = false;
            }
            
            CurrentHealth = maxHealth;
            if (healthBar != null)
            {
                healthBar.SetSlider(CurrentHealth);
            }
            
            Debug.Log($"[PlayerStats] {gameObject.name} respawned!");
            
            OnPlayerRespawn?.Invoke();
        }
        #endregion
    }
}