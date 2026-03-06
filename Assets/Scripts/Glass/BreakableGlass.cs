using UnityEngine;

namespace Resonance.Environment
{
    [RequireComponent(typeof(Collider))]
    public class BreakableGlass : MonoBehaviour, Resonance.Helper.IDamageable
    {
        [Header("References")]
        [SerializeField] private GlassShatterEffect shatterEffect;

        [Header("Settings")]
        [SerializeField] private float health = 1f;
        [SerializeField] private bool destroyPaneOnShatter = true;

        [Header("Wwise")]
        [SerializeField] private AK.Wwise.Event shatterEvent;

        private bool _broken;

        // Stores spatial data from a projectile collision so TakeDamage can use it.
        // Populated by OnCollisionEnter in the same frame WeaponProjectile calls TakeDamage.
        private Vector3 _pendingHitPoint;
        private Vector3 _pendingHitNormal;
        private Vector3 _pendingHitDirection;
        private bool _hasPendingHitData;

        public void TakeDamage(float damage, GameObject shooter)
        {
            Debug.Log($"[Glass] TakeDamage called. Damage: {damage}, Health: {health}, Broken: {_broken}");
            if (_broken) return;

            health -= damage;
            Debug.Log($"[Glass] Health after damage: {health}");
            if (health > 0f) return;

            if (_hasPendingHitData)
            {
                // Projectile path — use precise contact data captured by OnCollisionEnter.
                BreakNow(_pendingHitPoint, _pendingHitNormal, _pendingHitDirection);
            }
            else
            {
                // Hitscan path — derive direction from shooter position.
                Vector3 hitDirection = shooter != null
                    ? (transform.position - shooter.transform.position).normalized
                    : Vector3.zero;

                BreakNow(transform.position, transform.forward, hitDirection);
            }
        }

        // Captures spatial data from projectile collision.
        // WeaponProjectile.OnCollisionEnter calls TakeDamage in the same frame,
        // so this data will be ready when TakeDamage runs.
        private void OnCollisionEnter(Collision collision)
        {
            if (_broken) return;
            if (collision.collider.transform.IsChildOf(transform)) return;

            ContactPoint contact  = collision.GetContact(0);
            _pendingHitPoint      = contact.point;
            _pendingHitNormal     = contact.normal;
            _pendingHitDirection  = collision.relativeVelocity.normalized;
            _hasPendingHitData    = true;
        }

        private void BreakNow(Vector3 hitPoint, Vector3 hitNormal, Vector3 hitDirection)
        {
            Debug.Log($"[Glass] BreakNow called. HitPoint: {hitPoint}, HasPendingData: {_hasPendingHitData}");
            _broken = true;

            shatterEvent.Post(gameObject);

            if (shatterEffect != null)
                shatterEffect.Shatter(hitPoint, hitNormal, hitDirection);

            if (destroyPaneOnShatter)
            {
                if (TryGetComponent(out MeshRenderer mr)) mr.enabled = false;
                if (TryGetComponent(out Collider col))    col.enabled = false;
                Destroy(gameObject, 3f);
            }
        }
    }
}