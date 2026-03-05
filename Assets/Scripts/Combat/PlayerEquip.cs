using Resonance.Combat.Augments;
using Resonance.Combat.Weapons;
using Resonance.Combat.Weapons.Enums;
using Resonance.Helper;
using Resonance.Inventory;
using Resonance.Player;
using Resonance.PlayerController;
using UnityEditor.EditorTools;
using UnityEngine;

namespace Resonance.Combat
{
    public class PlayerEquip : MonoBehaviour
    {
        private GameObject currentWeaponInstance;
        private PlayerStats playerStats;
        private WeaponStatManager weaponStatManager;
        private PlayerAugmentEquipper playerAugmentEquipper;
     
        private ObservableValue<WeaponProperties> equippedWeaponObservable = new ObservableValue<WeaponProperties>();
        public ObservableValue<WeaponProperties> EquippedWeaponObservable => equippedWeaponObservable;

        [SerializeField] PlayerInventory playerInventory;
        public PlayerInventory PlayerInventory => playerInventory;

        [SerializeField] Transform equipSlot;
        public Transform EquipSlot => equipSlot;

        [SerializeField] private PlayerActionsInput playerActionsInput;

        [SerializeField] private WeaponView currentWeaponView;
        public WeaponView CurrentWeaponView => currentWeaponView;
        
        public WeaponProperties EquippedWeapon { get; private set; }
        
        void Start()
        {
            playerStats = GetComponent<PlayerStats>();
            playerAugmentEquipper = GetComponent<PlayerAugmentEquipper>();
            weaponStatManager = GetComponent<WeaponStatManager>();
            
            StartCoroutine(EquipStartingWeaponNextFrame());
        }

        private WeaponProperties previousWeapon;

        System.Collections.IEnumerator EquipStartingWeaponNextFrame()
        {
            yield return null;

            if (playerInventory == null)
            {
                yield break;
            }

            if (playerInventory.weaponInventory == null || playerInventory.weaponInventory.Length <= 1)
            {
                yield break;
            }

            WeaponProperties startWeapon = playerInventory.weaponInventory[1];
            if (startWeapon != null)
            {
                Equip(startWeapon);
            }
        }


        void Update()
        {
            if (playerActionsInput == null || playerInventory == null)
            {
                return;
            }

            if (playerActionsInput.SwapWeaponPressed)
            {
                SwapWeapon();
                playerActionsInput.SetSwapWeaponPressedFalse();
            }

            if (playerActionsInput.SwapSlotOnePressed)
            {
                EquipFromSlot(0);
                playerActionsInput.SetSlotOnePressedFalse();
            }

            if (playerActionsInput.SwapSlotTwoPressed)
            {
                EquipFromSlot(1);
                playerActionsInput.SetSlotTwoPressedFalse();
            }
        }

        private void UpdateEquipSlotFromSkin(GameObject skinInstance)
        {
            var tagged = skinInstance.GetComponentsInChildren<Transform>()
                .FirstOrDefault(t => t.CompareTag("Gun Equip"));

            if (tagged == null)
            {
                Debug.LogError($"[{GetType()}] No 'Gun Equip' tagged object found on skin.", skinInstance);
                return;
            }

            equipSlot = tagged;
            RefreshWeaponView(EquippedWeapon);
        }

        void SwapWeapon()
        {
            if (EquippedWeapon == null)
            {
                EquipFromSlot(1);
                return;
            }

            if (EquippedWeapon.Slot == WeaponSlot.Primary)
            {
                EquipFromSlot(1);
            }
            else
            {
                EquipFromSlot(0);
            }
        }

        void EquipFromSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= playerInventory.weaponInventory.Length)
            {
                return;
            }

            WeaponProperties weapon = playerInventory.weaponInventory[slotIndex];
            if (weapon == null)
            {
                return;
            }

            Equip(weapon);
        }

        void Equip(WeaponProperties weapon)
        {
            Debug.Log($"Equip called with: {weapon?.name ?? "null"}");

            if (weapon == null)
            {
                return;
            }

            Debug.Log($"EquippedWeapon is currently: {EquippedWeapon?.name ?? "null"}");

            if (EquippedWeapon == weapon)
            {
                return;
            }

            if (EquippedWeapon != null && playerStats != null)
            {
                playerStats.RemoveSpeedModifier(weaponStatManager.GetStat(WeaponStat.Mobility));
            }

            EquippedWeapon = weapon;

            if (weaponStatManager != null)
            {
                weaponStatManager.ManageWeapon(weapon);
            }

            if (equippedWeaponObservable != null)
            {
                equippedWeaponObservable.Value = weapon;
            }

            if (playerStats != null)
            {
                playerStats.AddSpeedModifier(weaponStatManager.Mobility);
            }

            Debug.Log("About to call RefreshWeaponView");
            RefreshWeaponView(weapon);
        }

        void RefreshWeaponView(WeaponProperties weapon)
        {
            if (weapon == null)
            {
                return;
            }

            if (currentWeaponInstance != null)
            {
                Destroy(currentWeaponInstance);
                currentWeaponInstance = null;
                currentWeaponView = null;
            }

            if (equipSlot == null)
            {
                Debug.LogError("PlayerEquip has no equipSlot assigned.", this);
                return;
            }

            if (weapon.WeaponPrefab == null)
            {
                Debug.LogError("WeaponProperties has no WeaponPrefab assigned.", weapon);
                return;
            }

            currentWeaponInstance = Instantiate(weapon.WeaponPrefab, equipSlot);
            currentWeaponInstance.transform.localPosition = Vector3.zero;
            currentWeaponInstance.transform.localRotation = Quaternion.identity;

            // Cancel out inherited parent scale so weapon renders at world scale (1,1,1)
            Vector3 ls = equipSlot.lossyScale;
            currentWeaponInstance.transform.localScale = new Vector3(1f / ls.x, 1f /
            ls.y, 1f / ls.z);

            currentWeaponView = currentWeaponInstance.GetComponent<WeaponView>();
            if (currentWeaponView == null)
            {
                Debug.LogError("WeaponPrefab is missing WeaponView component.", currentWeaponInstance);
            }
        }
        
        public void RemoveWeapon(WeaponSlot slot)
        {
            WeaponProperties existing = slot == WeaponSlot.Primary
                ? playerInventory.weaponInventory[0]
                : playerInventory.weaponInventory[1];

            if (existing == null)
            {
                return;
            }

            if (EquippedWeapon == existing)
            {
                if (playerStats != null)
                {
                    playerStats.RemoveSpeedModifier(existing.Mobility);
                }

                if (weaponStatManager != null)
                {
                    weaponStatManager.ManageWeapon(null);
                }

                EquippedWeapon = null;

                if (equippedWeaponObservable != null)
                {
                    equippedWeaponObservable.Value = null;
                }

                if (currentWeaponInstance != null)
                {
                    Destroy(currentWeaponInstance);
                    currentWeaponInstance = null;
                    currentWeaponView = null;
                }
            }

            playerInventory.RemoveWeapon(slot);
        }

        public void EquipAugment(AugmentProperties augment)
        {
            if (augment == null || playerAugmentEquipper == null)
            {
                return;
            }

            switch (augment.Slot)
            {
                case AugmentSlot.Upper:
                    if (playerInventory.augmentInventory[0] != null)
                    {
                        RemoveAugment(playerInventory.augmentInventory[0]);
                    }
                    
                    playerInventory.AddAugment(augment);
                    playerAugmentEquipper.ApplyAugmentStats(augment);
                    break;
                case AugmentSlot.Lower:
                    if (playerInventory.augmentInventory[1] != null)
                    {
                        RemoveAugment(playerInventory.augmentInventory[1]);
                    }
                    
                    playerInventory.AddAugment(augment);
                    playerAugmentEquipper.ApplyAugmentStats(augment);
                    break;
            }
        }

        public void RemoveAugment(AugmentProperties augment)
        {
                playerAugmentEquipper.RemoveAugmentStats(augment);
                playerInventory.RemoveAugment(augment.Slot);
        }
    }
}
