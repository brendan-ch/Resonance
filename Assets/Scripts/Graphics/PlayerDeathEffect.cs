using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Resonance.Player;

namespace Resonance.VFX
{
    public class PlayerDeathEffect : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Effect Settings")]
        [SerializeField] private float explosionDuration = 2.0f;
        [SerializeField] private Material deathShardMaterial;
        
        [Header("Colors")]
        [SerializeField] private Color shardColor = new Color(1, 1, 1, 1);
        [SerializeField] private Color emissionColor = new Color(0, 1, 1, 1);
        
        [Header("Effect Properties")]
        [SerializeField] private float emissionIntensity = 5.0f;
        [SerializeField] private float noiseScale = 5.0f;
        [SerializeField] [Range(0f, 1f)] private float shardDensity = 0.15f;
        #endregion
        
        #region Private Fields
        private PlayerStats _playerStats;
        private SkinnedMeshRenderer[] _meshRenderers;
        private Dictionary<SkinnedMeshRenderer, Material[]> _originalMaterials;
        private Material[] _effectMaterials;
        private bool _isPlayingEffect = false;
        #endregion
        
        #region Startup
        private void Awake()
        {
            _playerStats = GetComponentInParent<PlayerStats>();
            _meshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            _originalMaterials = new Dictionary<SkinnedMeshRenderer, Material[]>();
            
            // Store original materials
            foreach (var renderer in _meshRenderers)
            {
                _originalMaterials[renderer] = renderer.materials;
            }
        }
        
        private void OnEnable()
        {
            if (_playerStats != null)
            {
                _playerStats.OnPlayerDeath += PlayDeathEffect;
                _playerStats.OnPlayerRespawn += ResetEffect;
            }
        }
        
        private void OnDisable()
        {
            if (_playerStats != null)
            {
                _playerStats.OnPlayerDeath -= PlayDeathEffect;
                _playerStats.OnPlayerRespawn -= ResetEffect;
            }
        }
        #endregion
        
        #region Death Effect
        private void PlayDeathEffect()
        {
            if (_isPlayingEffect) return;
            
            StartCoroutine(DeathEffectSequence());
        }
        
        private IEnumerator DeathEffectSequence()
        {
            _isPlayingEffect = true;
            
            // Create material instances for each renderer
            CreateEffectMaterials();
            
            // Immediate explosion with fragmentation
            float elapsed = 0f;
            while (elapsed < explosionDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / explosionDuration;
                
                // Smooth curve for explosion
                float dissolve = Mathf.Pow(t, 1.5f);
                float fragmentation = Mathf.Pow(t, 1.5f) * 4.0f; // More intense explosion
                
                UpdateEffectMaterials(dissolve, fragmentation, emissionIntensity);
                
                yield return null;
            }
            
            // Hide the mesh completely after effect
            foreach (var renderer in _meshRenderers)
            {
                renderer.enabled = false;
            }
            
            _isPlayingEffect = false;
        }
        
        private void CreateEffectMaterials()
        {
            if (deathShardMaterial == null)
            {
                Debug.LogError("[PlayerDeathEffect] Death shard material not assigned!");
                return;
            }
            
            List<Material> materials = new List<Material>();
            
            foreach (var renderer in _meshRenderers)
            {
                // Create material instances
                Material[] instanceMaterials = new Material[renderer.materials.Length];
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    instanceMaterials[i] = new Material(deathShardMaterial);
                    materials.Add(instanceMaterials[i]);
                }
                
                renderer.materials = instanceMaterials;
            }
            
            _effectMaterials = materials.ToArray();
            
            // Set initial material properties
            foreach (var mat in _effectMaterials)
            {
                mat.SetColor("_Color", shardColor);
                mat.SetColor("_EmissionColor", emissionColor);
                mat.SetFloat("_EmissionIntensity", emissionIntensity); // Full glow immediately
                mat.SetFloat("_NoiseScale", noiseScale);
                mat.SetFloat("_ShardDensity", shardDensity);
            }
        }
        
        private void UpdateEffectMaterials(float dissolve, float fragmentation, float emission)
        {
            if (_effectMaterials == null) return;
            
            foreach (var mat in _effectMaterials)
            {
                mat.SetFloat("_DissolveAmount", dissolve);
                mat.SetFloat("_FragmentationAmount", fragmentation);
                mat.SetFloat("_EmissionIntensity", emission);
            }
        }
        
        private void ResetEffect()
        {
            // Stop any running effect
            StopAllCoroutines();
            _isPlayingEffect = false;
            
            // Restore original materials and visibility
            foreach (var renderer in _meshRenderers)
            {
                if (_originalMaterials.ContainsKey(renderer))
                {
                    renderer.materials = _originalMaterials[renderer];
                }
                renderer.enabled = true;
            }
            
            // Clean up effect material instances
            if (_effectMaterials != null)
            {
                foreach (var mat in _effectMaterials)
                {
                    if (mat != null)
                    {
                        Destroy(mat);
                    }
                }
                _effectMaterials = null;
            }
        }
        #endregion
        
        #region Cleanup
        private void OnDestroy()
        {
            // Clean up any material instances
            if (_effectMaterials != null)
            {
                foreach (var mat in _effectMaterials)
                {
                    if (mat != null)
                    {
                        Destroy(mat);
                    }
                }
            }
        }
        #endregion
    }
}