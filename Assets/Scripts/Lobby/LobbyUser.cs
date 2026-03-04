using UnityEngine;

namespace Resonance.LobbySystem
{
    public struct LobbyUser
    {
        public string Id;
        public string DisplayName;
        public bool IsReady;
        public Texture2D Avatar;

        /// <summary>
        /// Index corresponding to a skin in SkinCatalog.
        /// </summary>
        public int SkinIndex;
    }
}
