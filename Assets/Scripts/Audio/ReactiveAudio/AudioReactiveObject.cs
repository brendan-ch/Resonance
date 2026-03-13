using NUnit.Framework;
using PurrNet;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

namespace Resonance.Audio
{
    public class AudioReactiveObject : NetworkBehaviour
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

        private AudioSourceData clientReportedSource;
        private int numFramesBetweenServerToClientPropagation = 10;
        private int currentNumFramesFromLastServerToClientPropagation = 0;

        void Start()
        {
            SetupMaterial();
        }

        protected override void OnSpawned(bool asServer)
        {
            base.OnSpawned(asServer);

            if (asServer)
            {
                currentIntensity = 0f;
                ApplyEmissionForAllClients(0f);
            }
        }

        void Update()
        {
            if (isServer && currentNumFramesFromLastServerToClientPropagation >= numFramesBetweenServerToClientPropagation)
            {
                CalculateAudioState();
                ApplyEmissionForAllClients(currentIntensity);
                currentNumFramesFromLastServerToClientPropagation = 0;
            }
            else if (currentNumFramesFromLastServerToClientPropagation < numFramesBetweenServerToClientPropagation)
            {
                CalculateAudioState();
                currentNumFramesFromLastServerToClientPropagation++;
            }

            if (isClient)
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

                // TODO: rate limit this
                SetNearestAudioSourceOnServer(nearestSource);
            }
        }

        [ServerRpc(PurrNet.Transports.Channel.ReliableOrdered, requireOwnership: false)]
        public void SetNearestAudioSourceOnServer(AudioSourceData source)
        {
            clientReportedSource = source;
        }

        [ServerRpc(PurrNet.Transports.Channel.ReliableOrdered, requireOwnership: false)]
        private void CalculateAudioState()
        {
            if (clientReportedSource != null)
            {
                float distance = Vector3.Distance(transform.position, clientReportedSource.Position);
                float sourceIntensity = clientReportedSource.GetCurrentIntensity();
                float waveMaxDistance = AudioSourceTracker.Instance.BaseWaveDistance * clientReportedSource.PeakIntensity;
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

        }

        [ServerRpc(PurrNet.Transports.Channel.ReliableOrdered, requireOwnership: false)]
        private void ResetTargetIntensity()
        {
            targetIntensity = 0f;
        }

        [ObserversRpc(PurrNet.Transports.Channel.ReliableOrdered)]
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

        [ObserversRpc(PurrNet.Transports.Channel.ReliableOrdered)]
        void ApplyEmissionForAllClients(float intensity)
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
