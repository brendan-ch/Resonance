using System;
using Resonance.PlayerController;
using Resonance.UI;
using Resonance.Match;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PurrNet;
using Resonance.Helper;
using UnityEngine.Serialization;

namespace Resonance.Player
{
    public class PlayerStats : NetworkBehaviour, IDamageable
    {
        #region Inspector Fields
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float baseHealthRegen = 0f;
        
        [SerializeField] private float maxDamageReduction = 0.75f;
        [SerializeField] private float baseDamageReduction = 0f;
        
        [SerializeField] private float playerBaseSpeed = 1f;
        
        [SerializeField] private bool respawnOnDeath = true;

        public HealthBar healthBar;
        #endregion

        #region Properties
        public ObservableValue<float> CurrentHealth { get; private set; } = new ObservableValue<float>();
        public float MaxHealth => maxHealth;

        public float BaseHealthRegen {get => baseHealthRegen; set => baseHealthRegen = value; } 
        //Damage Reduction
        public float DamageReduction {get => currentDamageReduction;}
        public float BaseDamageReduction { get => baseDamageReduction; set => baseDamageReduction = Mathf.Clamp(value, 0f, maxDamageReduction); }
        
        //Speed
        public float PlayerSpeed => (currentSpeed);
        public float BaseSpeed { get => playerBaseSpeed; set => playerBaseSpeed = value; }
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

        protected override void OnSpawned()
        {
            base.OnSpawned();
            enabled = isOwner;
        }

        public void Awake()
        {
            _playerState = GetComponent<PlayerState>();
            _characterController = GetComponent<CharacterController>();
            _playerController = GetComponent<PlayerController.PlayerController>();
            GiveOwnership(_playerController.owner);

            _animator = GetComponent<Animator>();
        }

        private void Start()
        {
            CurrentHealth.Value = maxHealth;

            if (healthBar != null)
            {
                healthBar.SetSliderMax(maxHealth);
                CurrentHealth.ChangeEvent += value => healthBar.SetSlider(value);
            }

            // Register with match stat tracker
            if (MatchStatBridge.Instance != null)
            {
                MatchStatBridge.Instance.RegisterPlayer(gameObject);
            }
            
            //Stats
            CalculateSpeed();
            CalculateDamageReduction();
            CalculateRegen();
        }

        private void OnDestroy()
        {
            // Unregister from match stat tracker
            if (MatchStatBridge.Instance != null)
            {
                MatchStatBridge.Instance.UnregisterPlayer(gameObject);
            }
        }
        #endregion

            #region Health Management

            private List<float> regenModifiers = new List<float>();
            private float currentHealthRegen;
            
            private void Update()
            {
                if (currentHealthRegen > 0)
                {
                    Heal(currentHealthRegen * Time.deltaTime);
                }
            }
            public void TakeDamage(float amount)
            {
                TakeDamage(amount, null);
            }

            public void TakeDamage(float amount, GameObject attacker)
            {
                if (IsDead) return;

                if (attacker != null && attacker != gameObject && MatchStatBridge.Instance != null)
                {
                    MatchStatBridge.Instance.RecordDamage(attacker, gameObject, amount);
                    lastAttacker = attacker;
                    lastDamageTime = Time.time;
                }
    
                float finalAmount = amount * (1f - currentDamageReduction);
    
                CurrentHealth.Value = Mathf.Max(0, CurrentHealth.Value - finalAmount);

                if (CurrentHealth.Value <= 0)
                    Die(attacker);
            }

            public void Heal(float amount)
            {
                if (IsDead) return;
                CurrentHealth.Value = Mathf.Min(CurrentHealth.Value + amount, maxHealth);
            }
            
            public void AddRegenModifier(float modifier)
            {
                regenModifiers.Add(modifier);
                CalculateRegen();
            }

            public void RemoveRegenModifier(float modifier)
            {
                regenModifiers.Remove(modifier);
                CalculateRegen();
            }
            
            private void CalculateRegen()
            {
                currentHealthRegen = baseHealthRegen + regenModifiers.Sum();
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
                if (MatchStatBridge.Instance != null)
                {
                    if (killer != null && killer != gameObject)
                    {
                        MatchStatBridge.Instance.RecordKill(killer, gameObject);
                    }
                    else
                    {
                        // Suicide or environmental death
                        MatchStatBridge.Instance.RecordDeath(gameObject);
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
                
                CurrentHealth.Value = maxHealth;

                Debug.Log($"[PlayerStats] {gameObject.name} respawned!");

                OnPlayerRespawn?.Invoke();
            }
            #endregion

        #region Speed Management
        //Speed Properties
        private List<float> speedModifiers = new List<float>();
        private float currentSpeed;
        
        public void AddSpeedModifier(float modifier)
        {
            speedModifiers.Add(modifier);
            CalculateSpeed();
        }

        public void RemoveSpeedModifier(float modifier)
        {
            speedModifiers.Remove(modifier);
            CalculateSpeed();
        }

        private void CalculateSpeed()
        {
           currentSpeed = (playerBaseSpeed * speedModifiers.Aggregate(1f, (combinedModifier, nextModifier) => combinedModifier * nextModifier));
        }

        #endregion
        
        #region Damage Reduction Management

        private List<float> damageReductionModifiers = new List<float>();
        private float currentDamageReduction;
        
        public void AddDamageReductionModifier(float modifier)
        {
            damageReductionModifiers.Add(modifier);
            CalculateDamageReduction();
        }

        public void RemoveDamageReductionModifier(float modifier)
        {
            damageReductionModifiers.Remove(modifier);
            CalculateDamageReduction();
        }
        
        private void CalculateDamageReduction()
        {
            float damageTaken = damageReductionModifiers.Aggregate(1f - baseDamageReduction, (combined, next) => combined * next);
            currentDamageReduction = Mathf.Clamp(1f - damageTaken, 0f, maxDamageReduction);
        }
        
        #endregion
    }
}
