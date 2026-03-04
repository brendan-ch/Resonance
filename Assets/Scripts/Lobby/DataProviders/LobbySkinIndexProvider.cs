using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace Resonance.LobbySystem
{
    /// <summary>
    /// Provides the user's selected skin to the lobby.
    /// </summary>
    public class LobbySkinIndexProvider : MonoBehaviour
    {
        [SerializeField] private LobbyManager lobbyManager;
        [SerializeField] private int skinIndex;

        public int SkinIndex { get; private set; }

        public UnityEvent<int> OnSkinIndexChanged = new();

        public void Start()
        {
            lobbyManager.OnRoomJoined.AddListener(OnRoomJoined);
            SetSkinIndex(skinIndex);
        }

        public void SetSkinIndex(int index)
        {
            skinIndex = index;
            SkinIndex = index;
            OnSkinIndexChanged?.Invoke(index);
            PushToLobbyIfJoined();
        }

        private async void PushToLobbyIfJoined()
        {
            if (!lobbyManager.CurrentLobby.IsValid)
            {
                return;
            }

            var localUserId = await lobbyManager.GetLocalUserId();
            lobbyManager.SetSkinIndex(localUserId, skinIndex);
        }

        private async void OnRoomJoined(Lobby arg0)
        {
            await Task.Delay(500);
            SkinIndex = skinIndex;
            var localUserId = await lobbyManager.GetLocalUserId();
            lobbyManager.SetSkinIndex(localUserId, skinIndex);
        }
    }
}
