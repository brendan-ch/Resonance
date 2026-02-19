using PurrNet.Packing;
using Resonance.Assemblies.MatchStat;

namespace Resonance.Assemblies.Arena
{
    [System.Serializable]
    public struct PlayerRanking : IPackedAuto
    {
        public ulong player;
        public PlayerMatchStats stats;
    }
}
