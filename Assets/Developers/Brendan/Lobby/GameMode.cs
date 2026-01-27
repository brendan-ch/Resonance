namespace Resonance.LobbySystem
{
    public enum GameMode
    {
        Arena,
        Polarity
    }

    public static class Extensions
    {
        public static GameMode CycleNext(this GameMode gameMode)
        {
            if (gameMode == GameMode.Arena)
            {
                return GameMode.Polarity;
            }
            return GameMode.Arena;
        }
    }
}
