using System.Collections;
using Resonance.Match;
using Resonance.UI;
using UnityEngine;
using Resonance.Helper;
using Resonance.PlayerController;

namespace Resonance.Player
{
    public class PlayerStats : MonoBehaviour, IDamageable
    {
        [SerializeField] float maxHealth = 100f;
        [SerializeField] bool respawnOnDeath = true;
        [SerializeField] HealthBar healthBar;

        public ObservableValue<float> CurrentHealth { get; private set; }
        public float MaxHealth
        {
            get { return maxHealth; }
        }

        public bool IsDead { get; private set; }

        public event System.Action OnPlayerDeath;
        public event System.Action OnPlayerRespawn;

        PlayerState playerState;
        CharacterController characterController;
        PlayerController.PlayerController playerController;
        Animator animator;

        GameObject lastAttacker;
        float lastDamageTime;

        Coroutine respawnCoroutine;

        void Awake()
        {
            playerState = GetComponent<PlayerState>();
            characterController = GetComponent<CharacterController>();
            playerController = GetComponent<PlayerController.PlayerController>();
            animator = GetComponent<Animator>();

            CurrentHealth = new ObservableValue<float>(maxHealth);
        }

        void OnEnable()
        {
            if (MatchStatTracker.Instance != null)
            {
                MatchStatTracker.Instance.RegisterPlayer(gameObject);
            }

            if (healthBar != null)
            {
                healthBar.SetSliderMax(maxHealth);
                CurrentHealth.ChangeEvent += OnHealthChanged;
            }
        }

        void OnDisable()
        {
            if (MatchStatTracker.Instance != null)
            {
                MatchStatTracker.Instance.UnregisterPlayer(gameObject);
            }

            if (healthBar != null)
            {
                CurrentHealth.ChangeEvent -= OnHealthChanged;
            }
        }

        void Start()
        {
            
        }

        void OnHealthChanged(float newHealth)
        {
            if (healthBar != null)
            {
                healthBar.SetSlider(newHealth);
            }
        }

        public void TakeDamage(float amount, GameObject attacker)
        {
            if (IsDead)
            {
                return;
            }

            if (amount <= 0f)
            {
                return;
            }

            if (attacker != null && attacker != gameObject)
            {
                if (MatchStatTracker.Instance != null)
                {
                    MatchStatTracker.Instance.RecordDamage(attacker, gameObject, amount);
                }

                lastAttacker = attacker;
                lastDamageTime = Time.time;
            }

            float newHealth = CurrentHealth.Value - amount;
            if (newHealth < 0f)
            {
                newHealth = 0f;
            }

            CurrentHealth.Value = newHealth;

            if (CurrentHealth.Value <= 0f)
            {
                Die(attacker);
            }
        }
        
        public void TakeDamage(float amount)
        {
            TakeDamage(amount, null);
        }

        public void Heal(float amount)
        {
            if (IsDead)
            {
                return;
            }

            if (amount <= 0f)
            {
                return;
            }

            float newHealth = CurrentHealth.Value + amount;
            if (newHealth > maxHealth)
            {
                newHealth = maxHealth;
            }

            CurrentHealth.Value = newHealth;
        }

        void Die(GameObject killer)
        {
            if (IsDead)
            {
                return;
            }

            IsDead = true;

            if (playerController != null)
            {
                playerController.IsPlayerDead = true;
                playerController.enabled = false;
            }

            if (playerState != null)
            {
                playerState.SetPlayerMovementState(PlayerMovementState.Dead);
            }

            if (characterController != null)
            {
                characterController.enabled = false;
            }

            if (animator != null)
            {
                animator.enabled = false;
            }

            if (MatchStatTracker.Instance != null)
            {
                if (killer != null && killer != gameObject)
                {
                    MatchStatTracker.Instance.RecordKill(killer, gameObject);
                }
                else
                {
                    MatchStatTracker.Instance.RecordDeath(gameObject);
                }
            }

            OnPlayerDeath?.Invoke();

            if (respawnOnDeath)
            {
                if (respawnCoroutine != null)
                {
                    StopCoroutine(respawnCoroutine);
                }

                respawnCoroutine = StartCoroutine(RespawnCoroutine());
            }
        }

        IEnumerator RespawnCoroutine()
        {
            float respawnDelay = 3f;
            if (Resonance.Player.Respawn.Instance != null)
            {
                respawnDelay = Resonance.Player.Respawn.Instance.RespawnDelay;
            }

            yield return new WaitForSeconds(respawnDelay);

            RespawnInternal();
            respawnCoroutine = null;
        }

        public void Respawn()
        {
            if (respawnCoroutine != null)
            {
                StopCoroutine(respawnCoroutine);
                respawnCoroutine = null;
            }

            RespawnInternal();
        }

        void RespawnInternal()
        {
            IsDead = false;
            lastAttacker = null;

            if (playerState != null)
            {
                playerState.SetPlayerMovementState(PlayerMovementState.Idling);
            }

            if (playerController != null)
            {
                playerController.ResetState();
            }

            Transform spawnPoint = null;
            if (Resonance.Player.Respawn.Instance != null)
            {
                spawnPoint = Resonance.Player.Respawn.Instance.GetSpawnPoint();
            }

            if (spawnPoint != null)
            {
                transform.position = spawnPoint.position;
                transform.rotation = spawnPoint.rotation;
            }

            if (characterController != null)
            {
                if (characterController.stepOffset <= 0f)
                {
                    characterController.stepOffset = 0.3f;
                }

                characterController.enabled = true;
            }

            if (playerController != null)
            {
                playerController.enabled = true;
                playerController.IsPlayerDead = false;
            }

            if (animator != null)
            {
                animator.enabled = true;
            }

            CurrentHealth.Value = maxHealth;

            OnPlayerRespawn?.Invoke();
        }
        
        public GameObject GetLastAttacker()
        {
            return lastAttacker;
        }

        public float GetLastDamageTime()
        {
            return lastDamageTime;
        }
    }
}
