using Resonance.Combat.Augments;
using Resonance.Inventory;
using UnityEngine;

namespace Resonance.Combat
{
    // Handles augment ability activation and cooldowns during gameplay
    // Reads from PlayerInventory to know what augments are currently equipped
    public class PlayerAugmentManager : MonoBehaviour
    {
        private PlayerInventory playerInventory;

        private void Awake()
        {
            playerInventory = GetComponent<PlayerInventory>();
        }
    }
}