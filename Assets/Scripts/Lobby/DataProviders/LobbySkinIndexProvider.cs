using System.Threading.Tasks;
using UnityEngine;

namespace Resonance.LobbySystem
{
    /// <summary>
    /// Provides the user's selected skin to the lobby.
    /// </summary>
    public class LobbySkinIndexProvider : MonoBehaviour
    {
        [SerializeField] private LobbyManager lobbyManager;
        [SerializeField] private int skinIndex;

        public void Start()
        {
            lobbyManager.OnRoomJoined.AddListener(OnRoomJoined);
        }

        private async void OnRoomJoined(Lobby arg0)
        {
            await Task.Delay(500);
            
            var localUserId = await lobbyManager.GetLocalUserId();
            lobbyManager.SetSkinIndex(localUserId, skinIndex);
        }
    }

}
