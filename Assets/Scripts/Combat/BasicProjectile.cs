using UnityEngine;
using Resonance.Player;
using Resonance.Entities;

namespace Resonance.Combat
{
    [RequireComponent(typeof(Rigidbody))]
    public class BasicProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 20f;
        [SerializeField] private float damage = 10f;
        [SerializeField] private float lifetime = 5f;
        
        private Rigidbody _rb;
        private GameObject _shooter;
        private float _spawnTime;
        
        public void Initialize(GameObject shooter)
        {
            _shooter = shooter;
        }
        
        private void Awake()
        {
            if (!Application.isPlaying) return;
            
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
            _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
        
        private void Start()
        {
            if (!Application.isPlaying) return;
            
            _spawnTime = Time.time;
            _rb.linearVelocity = transform.forward * speed;
        }
        
        private void Update()
        {
            if (!Application.isPlaying) return;
            
            // Manual lifetime check instead of Destroy(gameObject, lifetime)
            if (Time.time - _spawnTime >= lifetime)
            {
                DestroyProjectile();
            }
        }
        
        private void OnCollisionEnter(Collision collision)
        {
            if (!Application.isPlaying) return;
            
            // Ignore collision with shooter
            if (_shooter != null && collision.gameObject == _shooter)
                return;
            
            if (collision.gameObject.CompareTag("Player"))
            {
                TargetDummy dummy = collision.gameObject.GetComponent<TargetDummy>();
                if (dummy != null)
                {
                    dummy.TakeDamage(damage, _shooter);
                    DestroyProjectile();
                    return;
                }
                
                PlayerStats player = collision.gameObject.GetComponent<PlayerStats>();
                if (player != null)
                {
                    // Pass the shooter reference for damage tracking
                    player.TakeDamage(damage, _shooter);
                    DestroyProjectile();
                    return;
                }
            }
            
            DestroyProjectile();
        }
        
        private void DestroyProjectile()
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
        }
    }
}