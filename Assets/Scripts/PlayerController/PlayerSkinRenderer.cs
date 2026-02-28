using System.Collections.Generic;
using PurrNet;
using UnityEngine;
using UnityEngine.Rendering;

namespace Resonance.PlayerController
{
    public class PlayerSkinRenderer : NetworkBehaviour
    {
        [SerializeField] private List<SkinnedMeshRenderer> renderers = new();
        [SerializeField] private SkinCatalog skinCatalog;

        public SyncVar<int> skinIndex = new SyncVar<int>();

        [SerializeField] private int testSkinIndexToRequest = 0;

        protected override void OnSpawned()
        {
            base.OnSpawned();

            skinIndex.onChanged += OnSkinChanged;

            ApplySkin(skinIndex.value);
        }

        protected override void OnDespawned()
        {
            base.OnDespawned();
            skinIndex.onChanged -= OnSkinChanged;
        }

        private void OnSkinChanged(int newIndex)
        {
            ApplySkin(newIndex);
        }

        private void ApplySkin(int index)
        {
            if (skinCatalog == null || skinCatalog.Count == 0)
            {
                ShowShadowsOnlyIfOwner();
                return;
            }

            var skinData = skinCatalog.Get(index);
            if (skinData == null)
            {
                ShowShadowsOnlyIfOwner();
                return;
            }

            for (int i = 0; i < renderers.Count && i < skinData.slots.Count; i++)
            {
                var slot = skinData.slots[i];

                if (slot.mesh != null)
                {
                    renderers[i].sharedMesh = slot.mesh;
                }

                if (slot.materials != null && slot.materials.Length > 0)
                {
                    renderers[i].sharedMaterials = slot.materials;
                }
            }

            ShowShadowsOnlyIfOwner();
        }

        private void ShowShadowsOnlyIfOwner()
        {
            if (isOwner)
            {
                foreach (var renderer in renderers)
                {
                    renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }
            }
        }

        [ContextMenu("Try request skin")]
        public void TryRequestSkin()
        {
            RequestSkin(testSkinIndexToRequest);
        }

        public void RequestSkin(int index)
        {
            SetSkinServerRpc(index);
        }

        [ServerRpc]
        private void SetSkinServerRpc(int index)
        {
            if (index >= 0 && index < skinCatalog.Count)
            {
                skinIndex.value = index;
            }
        }
    }
}
