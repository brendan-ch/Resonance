using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Resonance.Player;
using Resonance.Entities;

namespace Resonance.VFX
{
    public class DeathEffect : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Effect Settings")]
        [SerializeField] private float highlightDuration = 3.0f;
        [SerializeField] private float fragmentationDuration = 1.5f;
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
        private TargetDummy _targetDummy;
        private SkinnedMeshRenderer[] _meshRenderers;
        private Dictionary<SkinnedMeshRenderer, Material[]> _originalMaterials;
        private Material[] _effectMaterials;
        private bool _isPlayingEffect = false;
        #endregion
        
        #region Startup
        private void Awake()
        {
            _playerStats = GetComponentInParent<PlayerStats>();
            _targetDummy = GetComponentInParent<TargetDummy>();
            _meshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();
            _originalMaterials = new Dictionary<SkinnedMeshRenderer, Material[]>();
            
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
            
            if (_targetDummy != null)
            {
                _targetDummy.OnDeath += PlayDeathEffect;
                _targetDummy.OnRespawn += ResetEffect;
            }
        }
        
        private void OnDisable()
        {
            if (_playerStats != null)
            {
                _playerStats.OnPlayerDeath -= PlayDeathEffect;
                _playerStats.OnPlayerRespawn -= ResetEffect;
            }
            
            if (_targetDummy != null)
            {
                _targetDummy.OnDeath -= PlayDeathEffect;
                _targetDummy.OnRespawn -= ResetEffect;
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
            
            CreateEffectMaterials();
            
            // Phase 1: Highlight with glow
            float elapsed = 0f;
            while (elapsed < highlightDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / highlightDuration;
                
                UpdateEffectMaterials(0f, 0f, Mathf.Lerp(0f, emissionIntensity, t));
                
                yield return null;
            }
            
            // Phase 2: Fragmentation and dissolution
            elapsed = 0f;
            while (elapsed < fragmentationDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fragmentationDuration;
                
                float dissolve = Mathf.Pow(t, 1.5f);
                float fragmentation = Mathf.Pow(t, 2f) * 3.0f;
                
                UpdateEffectMaterials(dissolve, fragmentation, emissionIntensity);
                
                yield return null;
            }
            
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
                Debug.LogError("[DeathEffect] Death shard material not assigned!");
                return;
            }
            
            List<Material> materials = new List<Material>();
            
            foreach (var renderer in _meshRenderers)
            {
                Material[] instanceMaterials = new Material[renderer.materials.Length];
                for (int i = 0; i < renderer.materials.Length; i++)
                {
                    instanceMaterials[i] = new Material(deathShardMaterial);
                    materials.Add(instanceMaterials[i]);
                }
                
                renderer.materials = instanceMaterials;
            }
            
            _effectMaterials = materials.ToArray();
            
            foreach (var mat in _effectMaterials)
            {
                mat.SetColor("_Color", shardColor);
                mat.SetColor("_EmissionColor", emissionColor);
                mat.SetFloat("_EmissionIntensity", 0f);
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
            StopAllCoroutines();
            _isPlayingEffect = false;
            
            foreach (var renderer in _meshRenderers)
            {
                if (_originalMaterials.ContainsKey(renderer))
                {
                    renderer.materials = _originalMaterials[renderer];
                }
                renderer.enabled = true;
            }
            
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