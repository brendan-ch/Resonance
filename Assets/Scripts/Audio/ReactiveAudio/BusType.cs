using UnityEngine;

namespace Resonance.Audio
{
    public enum BusType
    {
        Foley = 0,
        SFX = 1,
        Environment = 2
    }
    
    public static class BusTypeUtility
    {
        public static Color GetBusColor(BusType busType)
        {
            switch (busType)
            {
                case BusType.Foley:
                    return new Color(1f, 0.5f, 0f); // Orange
                case BusType.SFX:
                    return new Color(0f, 1f, 0.5f); // Cyan
                case BusType.Environment:
                    return new Color(0.5f, 0f, 1f); // Purple
                default:
                    return Color.white;
            }
        }
    }
}