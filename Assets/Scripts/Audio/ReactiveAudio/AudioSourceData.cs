using UnityEngine;

namespace Resonance.Audio
{
    public class AudioSourceData
    {
        public Vector3 Position;
        public BusType BusType;
        public float Timestamp;
        public float Duration;
        public float PeakIntensity;

        public AudioSourceData(Vector3 position, BusType busType, float duration, float peakIntensity)
        {
            Position = position;
            BusType = busType;
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
            float normalizedAge = GetAge() / Duration;
            float fadeMultiplier = 1f - Mathf.Clamp01(normalizedAge);
            
            return PeakIntensity * fadeMultiplier;
        }
    }
}