using UnityEngine;

namespace Resonance.Audio
{
    public class AudioReactiveObject : MonoBehaviour
    {
        [Header("Material Settings")]
        [SerializeField] private Renderer targetRenderer;
        [SerializeField] private Color emissionColor = Color.cyan;
        [SerializeField] private float emissionIntensity = 5f;
        
        [Header("Audio Feedback")]
        [SerializeField] private bool enableAudioFeedback = true;
        
        [Header("Envelope (ADSR)")]
        [SerializeField] private float attackSpeed = 30f;
        [SerializeField] private float sustainTime = 1f;
        [SerializeField] private float releaseSpeed = 0.5f;

        [Header("Threshold")]
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

            AudioSourceData nearestSource = AudioSourceTracker.Instance.FindLoudestNearby(
                transform.position,
                AudioSourceTracker.Instance.BaseWaveDistance
            );

            if (nearestSource != null)
            {
                float distance = Vector3.Distance(transform.position, nearestSource.Position);
                float sourceIntensity = nearestSource.GetCurrentIntensity();
                float waveMaxDistance = AudioSourceTracker.Instance.BaseWaveDistance * nearestSource.PeakIntensity;
                float distanceAttenuation = 1f - Mathf.Clamp01(distance / waveMaxDistance);
                
                targetIntensity = sourceIntensity * distanceAttenuation;
            }
            else
            {
                targetIntensity = 0f;
            }

            if (targetIntensity < threshold)
            {
                targetIntensity = 0f;
            }

            // ADSR Envelope
            if (targetIntensity > currentIntensity)
            {
                currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * attackSpeed);
                
                if (currentIntensity > peakIntensity)
                {
                    peakIntensity = currentIntensity;
                    sustainTimer = sustainTime;
                    inSustain = true;
                }
            }
            else if (inSustain && sustainTimer > 0f)
            {
                currentIntensity = peakIntensity;
                sustainTimer -= Time.deltaTime;
                
                if (sustainTimer <= 0f)
                {
                    inSustain = false;
                }
            }
            else
            {
                currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, Time.deltaTime * releaseSpeed);
                
                if (currentIntensity < 0.01f)
                {
                    peakIntensity = 0f;
                }
            }

            if (debugLog)
            {
                Debug.Log($"[AudioReactiveObject] Target: {targetIntensity:F3}, Current: {currentIntensity:F3}, Sustain: {sustainTimer:F2}s");
            }

            if (enableAudioFeedback)
            {
                UpdateAudioFeedback(currentIntensity);
            }

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

            if (isFeedbackPlaying)
            {
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

            if (isFeedbackPlaying)
            {
                StopAudioFeedback();
            }
        }
    }
}