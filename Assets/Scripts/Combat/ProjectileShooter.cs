using Resonance.Combat.Weapons;
using Resonance.PlayerController;
using UnityEngine;

namespace Resonance.Combat
{
    public class ProjectileShooter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] PlayerEquip playerEquip;
        [SerializeField] PlayerActionsInput playerActionsInput;
        [SerializeField] Camera playerCamera;

        [Header("Debug")]
        [SerializeField] bool debugAimRays;
        [SerializeField] bool debugAmmoLogs;

        float nextFireTime;
        int currentAmmo;
        bool isReloading;
        float reloadEndTime;

        WeaponProperties lastWeapon;

        void Awake()
        {
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
        }

        void Start()
        {
            RefreshAmmoFromEquippedWeapon(force: true);
        }

        void Update()
        {
            if (playerActionsInput == null)
            {
                return;
            }

            RefreshAmmoFromEquippedWeapon(force: false);
            TickReload();

            if (playerActionsInput.ReloadPressed)
            {
                TryStartReload();
                playerActionsInput.SetReloadPressedFalse();
            }
            
            if (playerActionsInput.AttackPressed)
            {
                TryShoot();
                playerActionsInput.SetAttackPressedFalse();
            }
            
            if (playerActionsInput.AttackHeld)
            {
                TryShoot();
            }

        }

        void TryShoot()
        {
            if (isReloading)
            {
                return;
            }

            if (playerEquip == null)
            {
                return;
            }

            WeaponProperties weapon = playerEquip.EquippedWeapon;
            if (weapon == null)
            {
                return;
            }

            WeaponView view = playerEquip.CurrentWeaponView;
            if (view == null || view.Muzzle == null)
            {
                return;
            }

            BulletProperties bullet = weapon.BulletProperties;
            if (bullet == null || bullet.BulletPrefab == null)
            {
                return;
            }

            float fireRate = weapon.FireRate;
            if (fireRate > 0f)
            {
                if (Time.time < nextFireTime)
                {
                    return;
                }

                nextFireTime = Time.time + (1f / fireRate);
            }

            if (weapon.MagazineSize > 0)
            {
                if (currentAmmo <= 0)
                {
                    playerActionsInput.RequestReload();
                    return;
                }


                currentAmmo -= 1;

                if (debugAmmoLogs)
                {
                    Debug.Log($"[Shooter] Fired. Ammo: {currentAmmo}/{weapon.MagazineSize}", this);
                }
            }

            Vector3 baseDirection = GetAimDirection(view.Muzzle);

            WeaponPayload payload = new WeaponPayload();
            payload.Shooter = gameObject;
            payload.Damage = weapon.Damage;

            // Using your current model: bullet archetype speed * weapon multiplier
            float speedMultiplier = weapon.MuzzleVelocity;
            float finalBulletSpeed = bullet.BulletBaseSpeed * speedMultiplier;
            payload.BulletSpeed = finalBulletSpeed;

            payload.BulletGravity = bullet.BulletGravity;

            payload.DamageOverTime = bullet.DamageOverTime;
            payload.SpeedReduction = bullet.SpeedReduction;
            payload.Lifesteal = bullet.Lifesteal;

            int count = weapon.ProjectilesPerShot;
            if (count < 1)
            {
                count = 1;
            }

            if (debugAimRays)
            {
                Debug.DrawRay(view.Muzzle.position, baseDirection * 5f, Color.red, 1f);
                Debug.DrawRay(view.Muzzle.position, view.Muzzle.forward * 5f, Color.blue, 1f);
                Debug.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * 5f, Color.green, 1f);
            }

            for (int i = 0; i < count; i++)
            {
                Vector3 direction = ApplySpread(baseDirection, weapon.Spread);
                SpawnProjectile(bullet.BulletPrefab, view.Muzzle, payload, direction);
            }

            // TODO implement: recoil, accuracy, control
        }

        void TickReload()
        {
            if (!isReloading)
            {
                return;
            }

            if (Time.time < reloadEndTime)
            {
                return;
            }

            FinishReload();
        }

        void TryStartReload()
        {
            if (isReloading)
            {
                return;
            }

            if (playerEquip == null)
            {
                return;
            }

            WeaponProperties weapon = playerEquip.EquippedWeapon;
            if (weapon == null)
            {
                return;
            }

            if (weapon.MagazineSize <= 0)
            {
                return;
            }

            if (currentAmmo >= weapon.MagazineSize)
            {
                return;
            }

            float reloadTime = weapon.ReloadTime;
            if (reloadTime <= 0f)
            {
                currentAmmo = weapon.MagazineSize;

                if (debugAmmoLogs)
                {
                    Debug.Log($"[Shooter] Reload complete (instant). Ammo: {currentAmmo}/{weapon.MagazineSize}", this);
                }

                return;
            }

            isReloading = true;
            reloadEndTime = Time.time + reloadTime;

            if (debugAmmoLogs)
            {
                Debug.Log($"[Shooter] Reloading... {reloadTime:0.00}s", this);
            }
        }

        void FinishReload()
        {
            isReloading = false;

            if (playerEquip == null)
            {
                return;
            }

            WeaponProperties weapon = playerEquip.EquippedWeapon;
            if (weapon == null)
            {
                return;
            }

            currentAmmo = weapon.MagazineSize;

            if (debugAmmoLogs)
            {
                Debug.Log($"[Shooter] Reload complete. Ammo: {currentAmmo}/{weapon.MagazineSize}", this);
            }
        }

        void RefreshAmmoFromEquippedWeapon(bool force)
        {
            if (playerEquip == null)
            {
                return;
            }

            WeaponProperties weapon = playerEquip.EquippedWeapon;
            if (weapon == null)
            {
                return;
            }

            if (!force && weapon == lastWeapon)
            {
                return;
            }

            lastWeapon = weapon;
            isReloading = false;

            if (weapon.MagazineSize > 0)
            {
                currentAmmo = weapon.MagazineSize;
            }
            else
            {
                currentAmmo = 0;
            }

            if (debugAmmoLogs && weapon.MagazineSize > 0)
            {
                Debug.Log($"[Shooter] Equipped {weapon.name}. Ammo: {currentAmmo}/{weapon.MagazineSize}", this);
            }
        }

        Vector3 GetAimDirection(Transform muzzle)
        {
            if (playerCamera == null)
            {
                return muzzle.forward;
            }

            return playerCamera.transform.forward.normalized;
        }

        Vector3 ApplySpread(Vector3 dir, float spreadDegrees)
        {
            if (spreadDegrees <= 0f)
            {
                return dir;
            }

            float yaw = Random.Range(-spreadDegrees, spreadDegrees);
            float pitch = Random.Range(-spreadDegrees, spreadDegrees);

            Vector3 result = Quaternion.Euler(pitch, yaw, 0f) * dir;
            if (result.sqrMagnitude < 0.0001f)
            {
                return dir;
            }

            return result.normalized;
        }

        void SpawnProjectile(GameObject prefab, Transform muzzle, WeaponPayload payload, Vector3 direction)
        {
            GameObject go = Instantiate(prefab, muzzle.position, Quaternion.LookRotation(direction));

            WeaponProjectile projectile = go.GetComponent<WeaponProjectile>();
            if (projectile == null)
            {
                Debug.LogError("BulletPrefab is missing WeaponProjectile component.", go);
                return;
            }

            projectile.Initialize(payload, direction);
        }
    }
}
