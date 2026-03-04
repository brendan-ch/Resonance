using UnityEngine;

namespace Resonance.Audio
{
    /// <summary>
    /// Simplified audio-reactive object - reacts to Wwise bus levels in real-time
    /// No manual sound registration needed!
    /// </summary>
    public class AudioReactiveObject : MonoBehaviour
    {
        [Header("Material Settings")]
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color emissionColor = Color.cyan;
        [SerializeField] private float emissionIntensity = 5f;
        
        [Header("Smoothing")]
        [SerializeField] private float smoothSpeed = 10f;

        [Header("Threshold")]
        [Tooltip("Minimum intensity to start reacting (0-1)")]
        [SerializeField] private float threshold = 0.05f;

        [Header("Debug")]
        [SerializeField] private bool debugLog = false;

        private Material materialInstance;
        private float currentIntensity = 0f;

        void Start()
        {
            SetupMaterial();
            
            // Start with no emission
            currentIntensity = 0f;
            ApplyEmission(0f);
        }

        void Update()
        {
            if (AudioBusMonitor.Instance == null)
            {
                Debug.LogWarning("[AudioReactiveObject] AudioBusMonitor not found in scene!");
                return;
            }

            // Always react to the loudest bus (Foley, SFX, or Environment)
            float targetIntensity = AudioBusMonitor.Instance.GetMaxBusIntensity();

            if (debugLog)
            {
                Debug.Log($"[AudioReactiveObject] Raw intensity: {targetIntensity:F3}, Current: {currentIntensity:F3}");
            }

            // Apply threshold
            if (targetIntensity < threshold)
            {
                targetIntensity = 0f;
            }

            // Smooth
            currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * smoothSpeed);

            // Apply to material
            ApplyEmission(currentIntensity);
        }

        void SetupMaterial()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponent<Renderer>();
            }

            if (targetRenderer != null)
            {
                materialInstance = targetRenderer.material;
                materialInstance.EnableKeyword("_EMISSION");
                materialInstance.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                Debug.LogError($"[AudioReactiveObject] No Renderer found on {gameObject.name}!");
            }
        }

        void ApplyEmission(float intensity)
        {
            if (materialInstance == null) return;

            Color finalEmission = emissionColor * (intensity * emissionIntensity);
            materialInstance.SetColor("_EmissionColor", finalEmission);
        }

        void OnDestroy()
        {
            if (materialInstance != null)
            {
                Destroy(materialInstance);
            }
        }
    }
}