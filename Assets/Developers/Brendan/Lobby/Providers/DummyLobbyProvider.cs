using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Events;
using WebSocketSharp;

namespace Resonance.LobbySystem
{
    public class DummyLobbyProvider : MonoBehaviour, ILobbyProvider
    {
        class Content : HttpContent
        {
            private readonly string _data;
            public Content(string data)
            {
                _data = data;
            }

            // Minimal implementation needed for an HTTP request content,
            // i.e. a content that will be sent via HttpClient, contains the 2 following methods.
            protected override bool TryComputeLength(out long length)
            {
                // This content doesn't support pre-computed length and
                // the request will NOT contain Content-Length header.
                length = 0;
                return false;
            }

            // SerializeToStream* methods are internally used by CopyTo* methods
            // which in turn are used to copy the content to the NetworkStream.
            protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
                => stream.WriteAsync(Encoding.UTF8.GetBytes(_data)).AsTask();
        }

        public event UnityAction<string> OnLobbyJoinFailed;
        public event UnityAction OnLobbyLeft;
        public event UnityAction<Lobby> OnLobbyUpdated;
        public event UnityAction<List<LobbyUser>> OnLobbyPlayerListUpdated;
        public event UnityAction<List<FriendUser>> OnFriendListPulled;
        public event UnityAction<string> OnError;

        private DummyLobbyServer server;
        private HttpClient client;

        private void Start()
        {
            server = new DummyLobbyServer();
            server.AttemptStart();

            client = new HttpClient
            {
                BaseAddress = new System.Uri("http://localhost:5001")
            };
        }

        private void OnApplicationQuit()
        {
            server.Stop();
        }

        public async Task<Lobby> CreateLobbyAsync(int maxPlayers, Dictionary<string, string> lobbyProperties = null)
        {
            var response = await client.PostAsync("lobby", new Content(""));
            var lobby = JsonConvert.DeserializeObject<DummyLobbyServer.Lobby>(response.Content.ToString());
            if (lobby.LobbyId.IsNullOrEmpty())
            {
                return new Lobby { IsValid = false };
            }
            
            return LobbyFactory.Create(
                name: lobby.Name,
                lobbyId: lobby.LobbyId,
                maxPlayers: lobby.MaxPlayers,
                isOwner: lobby.IsOwner,
                members: new List<LobbyUser>(),
                properties: new Dictionary<string, string>()
            );
        }

        public async Task<List<FriendUser>> GetFriendsAsync(LobbyManager.FriendFilter filter)
        {
            return new List<FriendUser>();
        }

        public Task<string> GetLobbyDataAsync(string key)
        {
            throw new System.NotImplementedException();
        }

        public Task<List<LobbyUser>> GetLobbyMembersAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task<string> GetLocalUserIdAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task InitializeAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task InviteFriendAsync(FriendUser user)
        {
            throw new System.NotImplementedException();
        }

        public Task<Lobby> JoinLobbyAsync(string lobbyId)
        {
            throw new System.NotImplementedException();
        }

        public Task LeaveLobbyAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task LeaveLobbyAsync(string lobbyId)
        {
            throw new System.NotImplementedException();
        }

        public Task<List<Lobby>> SearchLobbiesAsync(int maxRoomsToFind = 10, Dictionary<string, string> filters = null)
        {
            throw new System.NotImplementedException();
        }

        public Task SetAllReadyAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task SetIsReadyAsync(string userId, bool isReady)
        {
            throw new System.NotImplementedException();
        }

        public Task SetLobbyDataAsync(string key, string value)
        {
            throw new System.NotImplementedException();
        }

        public Task SetLobbyStartedAsync()
        {
            throw new System.NotImplementedException();
        }

        public void Shutdown()
        {
            throw new System.NotImplementedException();
        }
    }
}
