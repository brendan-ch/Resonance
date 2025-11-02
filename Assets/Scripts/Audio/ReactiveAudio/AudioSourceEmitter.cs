using UnityEngine;
using Resonance.Audio;

namespace Resonance.Audio
{
    public class AudioSourceEmitter : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Emitter Settings")]
        [SerializeField] private BusType busType = BusType.SFX;
        [SerializeField] private float soundDuration = 0f;
        #endregion
        
        #region Public API
        public void RegisterSound()
        {
            if (AudioSourceTracker.Instance != null)
            {
                float duration = soundDuration > 0 ? soundDuration : -1f;
                AudioSourceTracker.Instance.RegisterSound(transform.position, busType, duration);
            }
        }
        
        public void RegisterSoundWithDuration(float duration)
        {
            if (AudioSourceTracker.Instance != null)
            {
                AudioSourceTracker.Instance.RegisterSound(transform.position, busType, duration);
            }
        }
        
        public void PostEventAndRegister(string eventName)
        {
            AkUnitySoundEngine.PostEvent(eventName, gameObject);
            RegisterSound();
        }
        
        public void PostWwiseEventAndRegister(AK.Wwise.Event wwiseEvent)
        {
            wwiseEvent.Post(gameObject);
            RegisterSound();
        }
        
        public void PostEventAndRegisterWithDuration(string eventName, float duration)
        {
            AkUnitySoundEngine.PostEvent(eventName, gameObject);
            RegisterSoundWithDuration(duration);
        }
        #endregion
    }
}