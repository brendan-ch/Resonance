using UnityEngine;
using System.Collections.Generic;

namespace Resonance.Audio
{
    // Monitors audio bus intensity from Wwise RTPCs (Foley, SFX, Environment)
    // Provides normalized 0-1 values for reactive objects
    public class AudioBusMonitor : MonoBehaviour
    {
        #region Singleton
        public static AudioBusMonitor Instance { get; private set; }
        #endregion

        #region Inspector Fields
        [Header("RTPC Configuration")]
        [SerializeField] private AK.Wwise.RTPC foleyRTPC;
        [SerializeField] private AK.Wwise.RTPC sfxRTPC;
        [SerializeField] private AK.Wwise.RTPC environmentRTPC;
        #endregion

        #region Private Fields
        private Dictionary<BusType, float> _intensities;
        private Dictionary<BusType, AK.Wwise.RTPC> _rtpcs;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            InitializeSingleton();
            InitializeBusData();
        }

        private void Update()
        {
            UpdateBusIntensities();
        }
        #endregion

        #region Initialization
        private void InitializeSingleton()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void InitializeBusData()
        {
            _intensities = new Dictionary<BusType, float>();
            _rtpcs = new Dictionary<BusType, AK.Wwise.RTPC>();
            
            _rtpcs[BusType.Foley] = foleyRTPC;
            _rtpcs[BusType.SFX] = sfxRTPC;
            _rtpcs[BusType.Environment] = environmentRTPC;
            
            foreach (BusType busType in System.Enum.GetValues(typeof(BusType)))
            {
                _intensities[busType] = 0f;
            }
        }
        #endregion

        #region Update Logic
        private void UpdateBusIntensities()
        {
            foreach (BusType busType in System.Enum.GetValues(typeof(BusType)))
            {
                _intensities[busType] = QueryRTPCValue(busType);
            }
        }

        private float QueryRTPCValue(BusType busType)
        {
            if (!_rtpcs.ContainsKey(busType) || _rtpcs[busType] == null)
            {
                Debug.LogWarning($"[AudioBusMonitor] RTPC not assigned for {busType}!");
                return 0f;
            }

            // Get RTPC value using Wwise wrapper API
            float rtpcValue = _rtpcs[busType].GetGlobalValue();

            // Wwise Meter outputs in dB range: -48 (silence) to 0 (full scale)
            // Normalize to 0-1 range
            float normalizedValue = (rtpcValue + 48f) / 48f;
            
            return Mathf.Clamp01(normalizedValue);
        }
        #endregion

        #region Public API
        public float GetBusIntensity(BusType busType)
        {
            return _intensities.TryGetValue(busType, out float value) ? value : 0f;
        }
        
        public float GetMaxBusIntensity()
        {
            float maxIntensity = 0f;
            
            foreach (float intensity in _intensities.Values)
            {
                maxIntensity = Mathf.Max(maxIntensity, intensity);
            }
            
            return maxIntensity;
        }
        
        public BusType GetLoudestBus()
        {
            BusType loudestBus = BusType.Foley;
            float maxIntensity = 0f;
            
            foreach (var kvp in _intensities)
            {
                if (kvp.Value > maxIntensity)
                {
                    maxIntensity = kvp.Value;
                    loudestBus = kvp.Key;
                }
            }

            return loudestBus;
        }
        
        public IReadOnlyDictionary<BusType, float> GetAllBusIntensities()
        {
            return _intensities;
        }
        #endregion
    }
}