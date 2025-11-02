using UnityEngine;
using System.Collections.Generic;
using Resonance.Audio;

namespace Resonance.Audio
{
    public class AudioSourceTracker : MonoBehaviour
    {
        #region Singleton
        public static AudioSourceTracker Instance { get; private set; }
        #endregion

        #region Inspector Fields
        [Header("Settings")]
        [SerializeField] private float defaultDuration = 1f;
        #endregion

        #region Private Fields
        private List<AudioSourceData> _activeSources = new List<AudioSourceData>();
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            CleanupOldSources();
        }
        #endregion

        #region Public API
        public void RegisterSound(Vector3 position, BusType busType, float duration = -1f)
        {
            if (duration < 0)
            {
                duration = defaultDuration;
            }
            
            _activeSources.RemoveAll(source => 
                Vector3.Distance(source.Position, position) < 1f
            );
            
            float peakIntensity = 0f;
            if (AudioBusMonitor.Instance != null)
            {
                peakIntensity = AudioBusMonitor.Instance.GetMaxBusIntensity();
            }
            
            _activeSources.Add(new AudioSourceData(position, busType, duration, peakIntensity));
        }

        public List<AudioSourceData> GetActiveSources()
        {
            return _activeSources;
        }

        public AudioSourceData FindLoudestNearby(Vector3 listenerPosition, float maxDistance, bool checkPropagation = false, float propagationSpeed = 100f)
        {
            AudioSourceData loudest = null;
            float maxWeightedIntensity = 0f;

            foreach (var source in _activeSources)
            {
                float distance = Vector3.Distance(listenerPosition, source.Position);
                
                if (distance > maxDistance) continue;

                if (checkPropagation)
                {
                    float soundAge = source.GetAge();
                    float soundWaveRadius = soundAge * propagationSpeed;
                    
                    if (distance > soundWaveRadius) continue;
                }

                float intensity = source.GetCurrentIntensity();
                float attenuation = 1f / (1f + distance * distance);
                float weightedIntensity = intensity * attenuation;

                if (weightedIntensity > maxWeightedIntensity)
                {
                    maxWeightedIntensity = weightedIntensity;
                    loudest = source;
                }
            }

            return loudest;
        }
        #endregion

        #region Cleanup
        private void CleanupOldSources()
        {
            _activeSources.RemoveAll(source => source.IsExpired());
        }
        #endregion

        #region Debug
        private void OnDrawGizmos()
        {
            if (_activeSources == null) return;

            foreach (var source in _activeSources)
            {
                Gizmos.color = BusTypeUtility.GetBusColor(source.BusType);
                Gizmos.DrawWireSphere(source.Position, 0.5f);
            }
        }
        #endregion
    }
}