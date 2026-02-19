using UnityEngine;
using Resonance.Helper;

public class PlayerViewModel : MonoBehaviour
{
    public ObservableValue<float> Health { get; private set; }
    public float MaxHealth { get; private set; } = 100f;

    public void Initialize(float maxHealth)
    {
        MaxHealth = maxHealth;
        Health = new ObservableValue<float>(MaxHealth);
    }
    
    void Awake()
    {
        Health = new ObservableValue<float>(MaxHealth);
    }

    public void TakeDamage(float amount)
    {
        Health.Value = Mathf.Max(Health.Value - amount, 0f);
    }

    public void Heal(float amount)
    {
        Health.Value = Mathf.Min(Health.Value + amount, MaxHealth);
    }
}