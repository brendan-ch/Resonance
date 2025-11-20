using UnityEngine;
using Resonance.PlayerController;
using Resonance.Player;

namespace Resonance.DebugTools
{
    public class PlayerDebugPanel : MonoBehaviour
    {
        #region Class Variables
        private PlayerController.PlayerController _playerController;
        private PlayerState _playerState;
        private PlayerLocomotionInput _playerInput;
        private OverdriveAbility _overdriveAbility;
        private CharacterController _characterController;
        private PlayerStats _playerStats;
        
        // Damage and heal input fields
        private string _damageAmount = "10";
        private string _healAmount = "25";
        #endregion

        #region Startup
        private void Start()
        {
            FindPlayerComponents();
        }

        private void FindPlayerComponents()
        {
            // Find all objects with Player tag
            var players = GameObject.FindGameObjectsWithTag("Player");
            GameObject player = null;
            
            // Find the one with PlayerStats component (the actual player, not dummies)
            foreach (var obj in players)
            {
                if (obj.GetComponent<PlayerStats>() != null)
                {
                    player = obj;
                    break;
                }
            }
            
            if (player == null)
            {
                UnityEngine.Debug.LogWarning("PlayerDebugPanel: No GameObject with PlayerStats found!");
                return;
            }

            _playerController = player.GetComponent<PlayerController.PlayerController>();
            _playerState = player.GetComponent<PlayerState>();
            _playerInput = player.GetComponent<PlayerLocomotionInput>();
            _overdriveAbility = player.GetComponent<OverdriveAbility>();
            _characterController = player.GetComponent<CharacterController>();
            _playerStats = player.GetComponent<PlayerStats>();
        }
        #endregion

        #region Public Methods
        public void DrawPanel()
        {
            if (_playerController == null)
                FindPlayerComponents();

            GUILayout.BeginVertical("box");
            GUILayout.Label("=== PLAYER DEBUG ===");
            GUILayout.Space(10);
            
            if (_playerController == null)
            {
                GUILayout.Label("No player found! (Need PlayerStats component)");
                GUILayout.EndVertical();
                return;
            }
            
            // Movement Stats
            GUILayout.Label("Movement Stats:");
            GUILayout.Space(5);
            DrawMovementStats();
            
            GUILayout.Space(15);
            
            // Overdrive
            GUILayout.Label("Overdrive:");
            GUILayout.Space(5);
            DrawOverdriveStats();
            
            GUILayout.Space(15);
            
            // Health
            GUILayout.Label("Health:");
            GUILayout.Space(5);
            DrawHealthStats();
            
            GUILayout.EndVertical();
        }

        private void DrawMovementStats()
        {
            GUILayout.BeginVertical("box");
            
            Vector3 velocity = _characterController.velocity;
            GUILayout.Label($"Speed: {velocity.magnitude:F2} m/s");
            GUILayout.Label($"Velocity: ({velocity.x:F1}, {velocity.y:F1}, {velocity.z:F1})");
            GUILayout.Label($"State: {_playerState.CurrentPlayerMovementState}");
            GUILayout.Label($"Grounded: {_playerState.InGroundedState()}");
            
            if (_playerInput != null)
            {
                GUILayout.Label($"Input: ({_playerInput.MovementInput.x:F2}, {_playerInput.MovementInput.y:F2})");
            }
            
            GUILayout.EndVertical();
        }

        private void DrawOverdriveStats()
        {
            if (_overdriveAbility == null)
            {
                GUILayout.BeginVertical("box");
                GUILayout.Label("No OverdriveAbility found");
                GUILayout.EndVertical();
                return;
            }

            GUILayout.BeginVertical("box");
            
            string status = _overdriveAbility.CurrentState.ToString();
            Color statusColor = _overdriveAbility.IsInOverdrive ? Color.green :
                               _overdriveAbility.IsOnCooldown ? Color.red : Color.white;
            
            GUI.color = statusColor;
            GUILayout.Label($"Status: {status}");
            GUI.color = Color.white;
            
            if (_overdriveAbility.IsInOverdrive)
            {
                GUILayout.Label($"Duration: {_overdriveAbility.DurationTimeRemaining:F1}s");
            }
            else if (_overdriveAbility.IsOnCooldown)
            {
                GUILayout.Label($"Cooldown: {_overdriveAbility.CooldownTimeRemaining:F1}s");
            }
            else
            {
                GUILayout.Label("Ready!");
            }
            
            GUILayout.Space(10);
            
            // Debug button to reset cooldown
            if (GUILayout.Button("Reset Cooldown (Debug)", GUILayout.Height(30)))
            {
                ResetOverdriveCooldown();
            }
            
            GUILayout.EndVertical();
        }

        private void ResetOverdriveCooldown()
        {
            if (_overdriveAbility == null) return;
            
            // Use reflection to modify private fields
            var type = _overdriveAbility.GetType();
            
            // Reset to Ready state
            var currentStateField = type.GetProperty("CurrentState");
            if (currentStateField != null)
            {
                // Get the OverdriveState enum type
                var overdriveStateType = type.GetNestedType("OverdriveState");
                if (overdriveStateType != null)
                {
                    var readyValue = System.Enum.Parse(overdriveStateType, "Ready");
                    
                    // Use SetState private method
                    var setStateMethod = type.GetMethod("SetState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (setStateMethod != null)
                    {
                        setStateMethod.Invoke(_overdriveAbility, new object[] { readyValue });
                    }
                }
            }
            
            // Reset timers
            var durationField = type.GetProperty("DurationTimeRemaining");
            if (durationField != null && durationField.CanWrite)
            {
                durationField.SetValue(_overdriveAbility, 0f);
            }
            
            var cooldownField = type.GetProperty("CooldownTimeRemaining");
            if (cooldownField != null && cooldownField.CanWrite)
            {
                cooldownField.SetValue(_overdriveAbility, 0f);
            }
            
            UnityEngine.Debug.Log("Overdrive cooldown reset via debug tools");
        }
        
        private void DrawHealthStats()
        {
            if (_playerStats == null)
            {
                GUILayout.BeginVertical("box");
                GUILayout.Label("No PlayerStats found");
                GUILayout.EndVertical();
                return;
            }

            GUILayout.BeginVertical("box");
            
            GUILayout.Label($"Current Health: {_playerStats.CurrentHealth:F0} / {_playerStats.MaxHealth:F0}");
            
            GUILayout.Space(10);
            
            // Damage input and button
            GUILayout.BeginHorizontal();
            GUILayout.Label("Damage Amount:", GUILayout.Width(120));
            _damageAmount = GUILayout.TextField(_damageAmount, GUILayout.Width(60));
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("Apply Damage", GUILayout.Height(30)))
            {
                ApplyDamage();
            }
            
            GUILayout.Space(10);
            
            // Heal input and button
            GUILayout.BeginHorizontal();
            GUILayout.Label("Heal Amount:", GUILayout.Width(120));
            _healAmount = GUILayout.TextField(_healAmount, GUILayout.Width(60));
            GUILayout.EndHorizontal();
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("Apply Heal", GUILayout.Height(30)))
            {
                ApplyHeal();
            }
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("Heal to Max", GUILayout.Height(25)))
            {
                HealToMax();
            }
            
            GUILayout.EndVertical();
        }
        
        private void ApplyDamage()
        {
            if (_playerStats == null) return;
            
            if (float.TryParse(_damageAmount, out float damage))
            {
                _playerStats.TakeDamage(damage);
                UnityEngine.Debug.Log($"Applied {damage} damage to player via debug tools");
            }
            else
            {
                UnityEngine.Debug.LogWarning("Invalid damage amount entered");
            }
        }
        
        private void ApplyHeal()
        {
            if (_playerStats == null) return;
            
            if (float.TryParse(_healAmount, out float healAmount))
            {
                _playerStats.Heal(healAmount);
                UnityEngine.Debug.Log($"Applied {healAmount} heal to player via debug tools");
            }
            else
            {
                UnityEngine.Debug.LogWarning("Invalid heal amount entered");
            }
        }
        
        private void HealToMax()
        {
            if (_playerStats == null) return;
            
            _playerStats.Heal(_playerStats.MaxHealth);
            UnityEngine.Debug.Log("Player healed to max via debug tools");
        }
        #endregion
    }
}