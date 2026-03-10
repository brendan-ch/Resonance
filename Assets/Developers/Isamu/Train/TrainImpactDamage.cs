using Resonance.Helper;
using UnityEngine;

namespace Resonance.Train
{
    [RequireComponent(typeof(Collider))]
    public class TrainImpactDamage : MonoBehaviour
    {
        [Header("Train Reference")]
        [SerializeField] private TrainController _trainController;

        [Header("Damage Scaling")]
        [SerializeField] private float _minDamage = 5f;
        [SerializeField] private float _maxDamage = 80f;
        [SerializeField] private float _speedForMaxDamage = 12f;
        [SerializeField] private float _minimumSpeedThreshold = 1f;

        [Header("Cooldown")]
        [SerializeField] private float _damageCooldown = 1f;

        private readonly System.Collections.Generic.Dictionary<Collider, float> _cooldowns
            = new System.Collections.Generic.Dictionary<Collider, float>();

        private void Awake()
        {
            if (_trainController == null)
                _trainController = GetComponentInParent<TrainController>();

            Collider collider = GetComponent<Collider>();
            if (!collider.isTrigger)
            {
                Debug.LogWarning("[TrainImpactDamage] Collider should be a Trigger. Setting isTrigger = true.", this);
                collider.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_trainController == null) return;
            if (_trainController.CurrentSpeed < _minimumSpeedThreshold) return;

            float now = Time.time;
            if (_cooldowns.TryGetValue(other, out float lastHit) && now - lastHit < _damageCooldown)
                return;

            IDamageable target = other.GetComponentInParent<IDamageable>();
            if (target == null) return;

            _cooldowns[other] = now;
            ApplyDamage(target);
        }

        private void ApplyDamage(IDamageable target)
        {
            float normalizedSpeed = Mathf.Clamp01(_trainController.CurrentSpeed / _speedForMaxDamage);
            float damage = Mathf.Lerp(_minDamage, _maxDamage, normalizedSpeed);
            target.TakeDamage(damage, gameObject);
        }
    }
}