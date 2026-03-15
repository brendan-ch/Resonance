using System;
using PurrNet;
using UnityEngine;
using UnityEngine.Rendering;

namespace Resonance.PlayerController
{
    [DefaultExecutionOrder(-2)]
    public class PlayerSkinRenderer : NetworkBehaviour
    {
        [SerializeField] private SkinCatalog skinCatalog;
        [SerializeField] private Animator animator;
        public Action<GameObject> OnNewSkinSpawned;

        public SyncVar<int> skinIndex = new SyncVar<int>();

        [SerializeField] private int testSkinIndexToRequest = 0;

        public GameObject CurrentMeshInstance { get; private set; }
        public SkinData CurrentlyLoadedSkinData { get; private set; }

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
                return;
            }

            var skinData = skinCatalog.Get(index);
            if (skinData == null || skinData.bodyMeshPrefab == null)
            {
                return;
            }

            if (CurrentMeshInstance != null)
            {
                Destroy(CurrentMeshInstance);
            }

            CurrentMeshInstance = Instantiate(skinData.bodyMeshPrefab, transform);
            CurrentlyLoadedSkinData = skinData;

            var innerAnimator = CurrentMeshInstance.GetComponent<Animator>();
            Destroy(innerAnimator);

            if (skinData.bodyAvatar != null)
            {
                animator.avatar = skinData.bodyAvatar;
            }

            animator.Rebind();

            ShowShadowsOnlyIfOwner();
            OnNewSkinSpawned.Invoke(CurrentMeshInstance);
        }

        private void ShowShadowsOnlyIfOwner()
        {
            if (!isOwner)
            {
                return;
            }

            foreach (var renderer in CurrentMeshInstance.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.CompareTag("Gun Equip"))
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
