using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Resonance.PlayerController
{
    public class OverdriveChromaticAberration : MonoBehaviour
    {
        #region Class Variables
        [Header("Post Processing")]
        [SerializeField] private Volume _postProcessVolume;
        
        [Header("Chromatic Aberration Settings")]
        [SerializeField] private float aberrationIntensity = 0.5f;
        [SerializeField] private float aberrationTransitionSpeed = 5f;
        
        private OverdriveAbility _overdriveAbility;
        private ChromaticAberration _chromaticAberration;
        private float _currentAberrationWeight = 0f;
        #endregion
        
        #region Startup
        private void Awake()
        {
            _overdriveAbility = GetComponent<OverdriveAbility>();

            if (_postProcessVolume != null && _postProcessVolume.profile != null)
            {
                if (!_postProcessVolume.profile.TryGet(out _chromaticAberration))
                {
                    _chromaticAberration = _postProcessVolume.profile.Add<ChromaticAberration>();
                }
            }
            else
            {
                Debug.LogWarning("OverdriveChromaticAberration: No Post Process Volume assigned! Chromatic aberration will not work.");
            }
        }

        private void Start()
        {
            if (_chromaticAberration != null)
            {
                _chromaticAberration.intensity.overrideState = true;
                _chromaticAberration.intensity.value = 0f;
            }
        }
        #endregion
        
        #region Update Logic
        private void Update()
        {
            UpdateChromaticAberration();
        }

        private void UpdateChromaticAberration()
        {
            if (_chromaticAberration == null || _overdriveAbility == null) return;

            float targetWeight = _overdriveAbility.IsInOverdrive ? 1f : 0f;
            
            _currentAberrationWeight = Mathf.Lerp(_currentAberrationWeight, targetWeight, aberrationTransitionSpeed * Time.deltaTime);
            
            _chromaticAberration.intensity.value = _currentAberrationWeight * aberrationIntensity;
        }
        #endregion
    }
}