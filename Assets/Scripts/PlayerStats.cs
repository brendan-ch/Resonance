using UnityEngine;

namespace Resonance.Player
{
    public class PlayerStats : MonoBehaviour
    {
        [SerializeField] private float maxHealth;

        private float currentHealth;
        
        public HealthBar healthBar;
        
        // Public properties for debug tools
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;

        private void Start()
        {
            currentHealth = maxHealth;
            
            healthBar.SetSliderMax(maxHealth);
        }

        public void TakeDamage(float amount)
        {
            currentHealth -= amount;
            currentHealth = Mathf.Max(0, currentHealth);
            healthBar.SetSlider(currentHealth);
        }
        
        public void Heal(float amount)
        {
            currentHealth += amount;
            currentHealth = Mathf.Min(currentHealth, maxHealth);
            healthBar.SetSlider(currentHealth);
        }

        private void Update()
        {
            if (currentHealth <= 0)
            {
                Die();
            }
        }

        private void Die()
        {
            // TODO: add logic
        }
    }
}