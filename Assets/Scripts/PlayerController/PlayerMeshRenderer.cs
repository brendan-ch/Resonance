using System.Collections.Generic;
using PurrNet;
using UnityEngine;
using UnityEngine.Rendering;

namespace Resonance.PlayerController
{
    public class PlayerMeshRenderer : NetworkBehaviour
    {
        [SerializeField] private List<Renderer> renderers = new();

        protected override void OnSpawned()
        {
            base.OnSpawned();

            if (isOwner)
            {
                foreach (var renderer in renderers)
                {
                    renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
                }
            }
        }
    }
}
