using UnityEngine;
using Resonance.Audio;

namespace Resonance.Audio
{
    public class AudioReactiveObject : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Listening Settings")]
        [SerializeField] private float listenRadius = 20f;
        
        [Header("Material Settings")]
        [SerializeField] private Material targetMaterial;
        [SerializeField] private Color emissionColor = Color.cyan;
        [SerializeField] private float emissionIntensity = 5f;
        
        [Header("Propagation Settings")]
        [SerializeField] private float propagationSpeed = 100f;
        [SerializeField] private bool enablePropagationDelay = true;
        
        [Header("Performance (LOD)")]
        [SerializeField] private bool enableLOD = true;
        [SerializeField] private float lodNearDistance = 15f;
        [SerializeField] private float lodMediumDistance = 40f;
        [SerializeField] private float lodFarDistance = 80f;
        
        [Header("Optional Smoothing")]
        [SerializeField] private bool enableSmoothing = false;
        [SerializeField] private float smoothSpeed = 10f;
        
        [Header("Debug")]
        [SerializeField] private bool drawDebugRadius = false;
        #endregion
        
        #region Private Fields
        private Material _materialInstance;
        private float _currentIntensity;
        private float _smoothedIntensity;
        private int _frameCounter = 0;
        private Camera _mainCamera;
        #endregion
        
        #region Unity Lifecycle
        private void Start()
        {
            SetupMaterial();
            _mainCamera = Camera.main;
        }
        
        private void Update()
        {
            if (enableLOD && _mainCamera != null)
            {
                _frameCounter++;
                int interval = GetLODUpdateInterval();
                
                if (_frameCounter < interval)
                    return;
                    
                _frameCounter = 0;
            }
            
            UpdateReactiveEffect();
        }
        
        private void OnDestroy()
        {
            CleanupMaterial();
        }
        #endregion
        
        #region Initialization
        private void SetupMaterial()
        {
            if (targetMaterial == null)
            {
                var renderer = GetComponent<Renderer>();
                if (renderer != null)
                {
                    _materialInstance = renderer.material;
                    targetMaterial = _materialInstance;
                }
                else
                {
                    return;
                }
            }
            
            if (targetMaterial != null)
            {
                targetMaterial.EnableKeyword("_EMISSION");
                targetMaterial.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
        }
        #endregion
        
        #region Update Logic
        private int GetLODUpdateInterval()
        {
            if (_mainCamera == null) return 1;
            
            float distanceToCamera = Vector3.Distance(transform.position, _mainCamera.transform.position);
            
            if (distanceToCamera < lodNearDistance) return 1;
            if (distanceToCamera < lodMediumDistance) return 3;
            if (distanceToCamera < lodFarDistance) return 5;
            return 10;
        }
        
        private void UpdateReactiveEffect()
        {
            if (AudioSourceTracker.Instance == null) return;
            
            if (targetMaterial == null)
            {
                SetupMaterial();
                if (targetMaterial == null) return;
            }
            
            AudioSourceData nearestSource = AudioSourceTracker.Instance.FindLoudestNearby(
                transform.position, 
                listenRadius,
                enablePropagationDelay,
                propagationSpeed
            );

            if (nearestSource != null)
            {
                float distance = Vector3.Distance(transform.position, nearestSource.Position);
                float sourceIntensity = nearestSource.GetCurrentIntensity();
                float attenuation = 1f / (1f + distance * distance);
                
                _currentIntensity = sourceIntensity * attenuation;
            }
            else
            {
                _currentIntensity = 0f;
            }
            
            float finalIntensity = enableSmoothing ? GetSmoothedIntensity() : _currentIntensity;
            
            ApplyEmission(finalIntensity);
        }
        
        private float GetSmoothedIntensity()
        {
            _smoothedIntensity = Mathf.Lerp(_smoothedIntensity, _currentIntensity, smoothSpeed * Time.deltaTime);
            return _smoothedIntensity;
        }
        
        private void ApplyEmission(float intensity)
        {
            if (targetMaterial == null) return;
            
            if (!targetMaterial.IsKeywordEnabled("_EMISSION"))
            {
                targetMaterial.EnableKeyword("_EMISSION");
            }
            
            Color finalEmission = emissionColor * (intensity * emissionIntensity);
            targetMaterial.SetColor("_EmissionColor", finalEmission);
        }
        #endregion
        
        #region Cleanup
        private void CleanupMaterial()
        {
            if (_materialInstance != null)
            {
                Destroy(_materialInstance);
            }
        }
        #endregion
        
        #region Debug
        private void OnDrawGizmosSelected()
        {
            if (!drawDebugRadius) return;
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, listenRadius);
        }
        #endregion
    }
}