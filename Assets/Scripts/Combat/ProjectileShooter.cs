using UnityEngine;
using Resonance.PlayerController;

namespace Resonance.Combat
{
    public class ProjectileShooter : MonoBehaviour
    {
        [SerializeField] private GameObject projectilePrefab;
        [SerializeField] private Transform firePoint;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float maxShootDistance = 100f;
        
        private PlayerActionsInput _playerActionsInput;
        
        private void Awake()
        {
            _playerActionsInput = GetComponent<PlayerActionsInput>();
            
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
        }
        
        private void Update()
        {
            if (_playerActionsInput.AttackPressed)
            {
                Shoot();
                _playerActionsInput.SetAttackPressedFalse();
            }
        }
        
        private void Shoot()
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning("[ProjectileShooter] No projectile prefab assigned!");
                return;
            }
            
            Transform spawnPoint = firePoint != null ? firePoint : transform;
            
            // Raycast from screen center (where crosshair is)
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            Vector3 targetPoint;
            
            if (Physics.Raycast(ray, out RaycastHit hit, maxShootDistance))
            {
                targetPoint = hit.point;
            }
            else
            {
                targetPoint = ray.GetPoint(maxShootDistance);
            }
            
            // Calculate direction from spawn point to target
            Vector3 shootDirection = (targetPoint - spawnPoint.position).normalized;
            
            GameObject projectile = Instantiate(projectilePrefab, spawnPoint.position, Quaternion.LookRotation(shootDirection));
            
            // Initialize projectile with shooter reference
            BasicProjectile projectileScript = projectile.GetComponent<BasicProjectile>();
            if (projectileScript != null)
            {
                projectileScript.Initialize(gameObject);
            }
        }
    }
}