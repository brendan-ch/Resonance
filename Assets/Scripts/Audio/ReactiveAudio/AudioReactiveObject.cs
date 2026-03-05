using UnityEngine;

namespace Resonance.Audio
{
    /// <summary>
    /// Reacts to audio waves with Attack-Sustain-Release envelope
    /// Creates "alive" effect with fast attack, held intensity, then slow decay
    /// </summary>
    public class AudioReactiveObject : MonoBehaviour
    {
        [Header("Material Settings")]
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color emissionColor = Color.cyan;
        [SerializeField] private float emissionIntensity = 5f;
        
        [Header("Audio Feedback")]
        [SerializeField] private bool enableAudioFeedback = true;
        
        [Header("Detection")]
        [Tooltip("Maximum distance from sound source to detect waves")]
        [SerializeField] private float maxDistance = 30f;
        
        [Header("Envelope (ADSR)")]
        [Tooltip("How fast it lights up when wave hits")]
        [SerializeField] private float attackSpeed = 30f;
        [Tooltip("How long to hold at max intensity (seconds)")]
        [SerializeField] private float sustainTime = 1f;
        [Tooltip("How fast it fades after sustain (lower = longer tail)")]
        [SerializeField] private float releaseSpeed = 0.5f;

        [Header("Threshold")]
        [Tooltip("Minimum intensity to start reacting (0-1)")]
        [SerializeField] private float threshold = 0.05f;

        [Header("Debug")]
        [SerializeField] private bool debugLog = false;

        private Material materialInstance;
        private float currentIntensity = 0f;
        private float targetIntensity = 0f;
        private float peakIntensity = 0f;
        private float sustainTimer = 0f;
        private bool inSustain = false;
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

            // Find loudest sound wave nearby
            AudioSourceData nearestSource = AudioSourceTracker.Instance.FindLoudestNearby(
                transform.position,
                maxDistance
            );

            // Calculate target intensity from wave
            if (nearestSource != null)
            {
                float distance = Vector3.Distance(transform.position, nearestSource.Position);
                float sourceIntensity = nearestSource.GetCurrentIntensity();
                
                // Distance attenuation
                float maxDist = maxDistance;
                float distanceAttenuation = 1f - Mathf.Clamp01(distance / maxDist);
                
                targetIntensity = sourceIntensity * distanceAttenuation;
            }
            else
            {
                targetIntensity = 0f;
            }

            // Apply threshold
            if (targetIntensity < threshold)
            {
                targetIntensity = 0f;
            }

            // ADSR Envelope Logic
            if (targetIntensity > currentIntensity)
            {
                // ATTACK - fast rise
                currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * attackSpeed);
                
                // Track peak for sustain
                if (currentIntensity > peakIntensity)
                {
                    peakIntensity = currentIntensity;
                    sustainTimer = sustainTime; // Reset sustain timer
                    inSustain = true;
                }
            }
            else if (inSustain && sustainTimer > 0f)
            {
                // SUSTAIN - hold at peak
                currentIntensity = peakIntensity;
                sustainTimer -= Time.deltaTime;
                
                if (sustainTimer <= 0f)
                {
                    inSustain = false;
                }
            }
            else
            {
                // RELEASE - slow decay
                currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * releaseSpeed);
                
                // Reset peak when fully faded
                if (currentIntensity < 0.01f)
                {
                    peakIntensity = 0f;
                }
            }

            if (debugLog)
            {
                Debug.Log($"[AudioReactiveObject] Target: {targetIntensity:F3}, Current: {currentIntensity:F3}, Sustain: {sustainTimer:F2}s");
            }

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
            bool shouldPlay = intensity > 0f;

            if (shouldPlay && !isFeedbackPlaying)
            {
                StartAudioFeedback();
            }
            else if (!shouldPlay && isFeedbackPlaying)
            {
                StopAudioFeedback();
            }

            // Update volume based on intensity (hardcoded RTPC name)
            if (isFeedbackPlaying)
            {
                // Scale intensity to 0-100 range for RTPC
                float volumeValue = Mathf.Clamp01(intensity) * 100f;
                AkSoundEngine.SetRTPCValue("Reactive_Feedback_Volume", volumeValue, gameObject);
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