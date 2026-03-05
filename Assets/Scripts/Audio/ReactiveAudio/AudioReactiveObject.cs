using UnityEngine;

namespace Resonance.Audio
{
    /// <summary>
    /// Reacts to live Wwise bus levels with distance-based attenuation from listener
    /// No manual registration needed - just reacts to real-time audio
    /// </summary>
    public class AudioReactiveObject : MonoBehaviour
    {
        [Header("Material Settings")]
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color emissionColor = Color.cyan;
        [SerializeField] private float emissionIntensity = 5f;
        
        [Header("Audio Feedback")]
        [SerializeField] private bool enableAudioFeedback = true;
        [SerializeField] private float minFeedbackThreshold = 0.1f;
        
        [Header("Detection")]
        [Tooltip("Maximum distance from sound source to detect waves")]
        [SerializeField] private float maxDistance = 30f;
        
        [Header("Smoothing")]
        [Tooltip("How fast it reacts to sound (higher = faster)")]
        [SerializeField] private float attackSpeed = 50f;
        [Tooltip("How fast it fades after sound (lower = longer tail)")]
        [SerializeField] private float releaseSpeed = 1f; // Very slow fade

        [Header("Threshold")]
        [Tooltip("Minimum intensity to start reacting (0-1)")]
        [SerializeField] private float threshold = 0.05f;

        [Header("Debug")]
        [SerializeField] private bool debugLog = false;

        private Material materialInstance;
        private float currentIntensity = 0f;
        private bool isFeedbackPlaying = false;

        void Start()
        {
            SetupMaterial();
            
            // Start with no emission
            currentIntensity = 0f;
            ApplyEmission(0f);
        }

        void Update()
        {
            if (AudioSourceTracker.Instance == null)
            {
                Debug.LogWarning("[AudioReactiveObject] AudioSourceTracker not found in scene!");
                return;
            }

            // Find loudest sound wave nearby (with propagation)
            AudioSourceData nearestSource = AudioSourceTracker.Instance.FindLoudestNearby(
                transform.position,
                maxDistance
            );

            float targetIntensity = 0f;
            
            if (nearestSource != null)
            {
                targetIntensity = nearestSource.GetCurrentIntensity();
            }

            if (debugLog)
            {
                Debug.Log($"[AudioReactiveObject] Target: {targetIntensity:F3}, Current: {currentIntensity:F3}");
            }

            // Apply threshold
            if (targetIntensity < threshold)
            {
                targetIntensity = 0f;
            }

            // Smooth with different attack/release speeds
            float speed = targetIntensity > currentIntensity ? attackSpeed : releaseSpeed;
            currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * speed);

            // Update audio feedback
            if (enableAudioFeedback)
            {
                UpdateAudioFeedback(currentIntensity);
            }

            // Apply to material
            ApplyEmission(currentIntensity);
        }

        void UpdateAudioFeedback(float intensity)
        {
            bool shouldPlay = intensity > minFeedbackThreshold;

            if (shouldPlay && !isFeedbackPlaying)
            {
                StartAudioFeedback();
            }
            else if (!shouldPlay && isFeedbackPlaying)
            {
                StopAudioFeedback();
            }
        }

        void StartAudioFeedback()
        {
            AkUnitySoundEngine.PostEvent("Play_Reactive_Feedback", gameObject);
            isFeedbackPlaying = true;
        }

        void StopAudioFeedback()
        {
            AkUnitySoundEngine.PostEvent("Stop_Reactive_Feedback", gameObject);
            isFeedbackPlaying = false;
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

            // Stop any playing feedback
            if (isFeedbackPlaying)
            {
                StopAudioFeedback();
            }
        }
    }
}