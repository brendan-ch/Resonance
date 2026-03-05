using UnityEngine;
using System.Collections.Generic;

namespace Resonance.Audio
{
    /// <summary>
    /// Tracks spatial audio events for reactive objects
    /// Louder sounds propagate further and intensity decreases with distance
    /// </summary>
    public class AudioSourceTracker : MonoBehaviour
    {
        public static AudioSourceTracker Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private float defaultDuration = 3f; // Longer sustain
        [SerializeField] private float propagationSpeed = 50f; // meters per second
        [SerializeField] private float baseWaveDistance = 150f; // Distance a full-volume sound travels
        
        private List<AudioSourceData> activeSources = new List<AudioSourceData>();

        // Public accessor
        public float BaseWaveDistance => baseWaveDistance;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        void Update()
        {
            // Remove expired sources
            activeSources.RemoveAll(source => source.IsExpired());
        }
        
        public void RegisterSound(Vector3 position, float duration = -1f)
        {
            if (duration < 0f)
            {
                duration = defaultDuration;
            }

            // Get current bus intensity from AudioBusMonitor
            float intensity = 0f;
            if (AudioBusMonitor.Instance != null)
            {
                intensity = AudioBusMonitor.Instance.GetMaxBusIntensity();
            }

            activeSources.Add(new AudioSourceData(position, duration, intensity));
        }
        
        public AudioSourceData FindLoudestNearby(Vector3 position, float searchRadius)
        {
            AudioSourceData loudest = null;
            float maxWeightedIntensity = 0f;

            foreach (var source in activeSources)
            {
                float distance = Vector3.Distance(position, source.Position);
                
                // Wave size is directly based on sound intensity
                // Louder sound (1.0) = 150m wave, quiet sound (0.3) = 45m wave
                float waveMaxDistance = baseWaveDistance * source.PeakIntensity;
                
                // Wave propagation - sound wave travels at propagationSpeed
                float soundAge = source.GetAge();
                float waveRadius = soundAge * propagationSpeed;
                
                // Sound hasn't propagated this far yet
                if (distance > waveRadius)
                    continue;
                    
                // Sound wave has dissipated (traveled beyond its max range)
                if (distance > waveMaxDistance)
                    continue;
                
                // Check if within detection range
                if (distance > searchRadius)
                    continue;

                // Get current intensity (decays over time)
                float intensity = source.GetCurrentIntensity();
                
                // Distance attenuation - sound gets quieter as it travels
                float distanceAttenuation = 1f - (distance / waveMaxDistance);
                distanceAttenuation = Mathf.Clamp01(distanceAttenuation);
                
                float weightedIntensity = intensity * distanceAttenuation;

                if (weightedIntensity > maxWeightedIntensity)
                {
                    maxWeightedIntensity = weightedIntensity;
                    loudest = source;
                }
            }

            return loudest;
        }

        void OnDrawGizmos()
        {
            if (activeSources == null) return;

            foreach (var source in activeSources)
            {
                float intensity = source.GetCurrentIntensity();
                
                // Draw origin point
                Gizmos.color = Color.yellow * intensity;
                Gizmos.DrawWireSphere(source.Position, 0.5f);
                
                // Draw propagating wave
                float waveRadius = source.GetAge() * propagationSpeed;
                float waveMaxDistance = baseWaveDistance * source.PeakIntensity;
                
                if (waveRadius < waveMaxDistance)
                {
                    Gizmos.color = new Color(1f, 1f, 0f, intensity * 0.3f);
                    Gizmos.DrawWireSphere(source.Position, waveRadius);
                }
            }
        }
    }
    
    public class AudioSourceData
    {
        public Vector3 Position;
        public float Timestamp;
        public float Duration;
        public float PeakIntensity;

        public AudioSourceData(Vector3 position, float duration, float peakIntensity)
        {
            Position = position;
            Timestamp = Time.time;
            Duration = duration;
            PeakIntensity = peakIntensity;
        }

        public float GetAge()
        {
            return Time.time - Timestamp;
        }

        public bool IsExpired()
        {
            return GetAge() > Duration;
        }

        public float GetCurrentIntensity()
        {
            float age = GetAge();
            float normalizedAge = age / Duration;
            
            // Slower exponential fade out (much longer tail)
            // Using e^(-1.5x) instead of e^(-3x) for slower decay
            float fade = Mathf.Exp(-1.5f * normalizedAge);
            
            return PeakIntensity * fade;
        }
    }
}