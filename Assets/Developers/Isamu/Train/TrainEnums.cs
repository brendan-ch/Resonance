namespace Resonance.Train
{
    public enum TrainState
    {
        StoppedAtStation = 0,
        Accelerating = 1,
        Cruising = 2,
        Braking = 3
    }
    
    public enum TrainDirection
    {
        Forward = 1,   // Traveling toward higher station indices
        Backward = -1   // Traveling toward lower station indices
    }
}