using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Resonance.LobbySystem
{
    public class DummyLobbyServer
    {
        public struct Lobby
        {
            public string Name;
            public bool IsValid;
            public string LobbyId;
            public string LobbyCode;
            public int MaxPlayers;
            public bool IsOwner;
            public Dictionary<string, string> Properties;
        }

        public struct User
        {
            public string DisplayName;
            public string Id;
        }

        private HttpListener httpListener;

        public void AttemptStart()
        {
            httpListener = new HttpListener();
            httpListener.Prefixes.Add("http://localhost:5001/api/");
            httpListener.Start();

            Listen();
        }

        private void Listen()
        {
            Task serverTask = Task.Run(async () =>
            {
                while (httpListener != null)
                {
                    HttpListenerContext context = await httpListener.GetContextAsync();
                    await ProcessRequestAsync(context);
                }
            });

        }

        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            var method = request.HttpMethod;
            var rawUrl = request.RawUrl ?? "";

            var lobbyUserIdMatch = Regex.Match(rawUrl, @"^/lobby/(\d+)/users/(\d+)$");
            if (lobbyUserIdMatch.Success)
            {
                int lobbyId = int.Parse(lobbyUserIdMatch.Groups[1].Value);
                int userId = int.Parse(lobbyUserIdMatch.Groups[2].Value);

                await HandleLobbyIdUserIdEndpoint(lobbyId, userId, method, request, response);
                return;
            }

            var lobbyUsersMatch = Regex.Match(rawUrl, @"^/lobby/(\d+)/users");
            if (lobbyUsersMatch.Success)
            {
                int lobbyId = int.Parse(lobbyUserIdMatch.Groups[1].Value);

                await HandleLobbyIdUserEndpoint(lobbyId, method, request, response);
                return;
            }

            var lobbyMatch = Regex.Match(rawUrl, @"^/lobby/(\d+)");
            if (lobbyMatch.Success)
            {
                int lobbyId = int.Parse(lobbyMatch.Groups[1].Value);

                await HandleLobbyIdEndpoint(lobbyId, method, request, response);
                return;
            }

            if (rawUrl.EndsWith("/lobby") || rawUrl.EndsWith("/lobby/"))
            {
                await HandleLobbyEndpoint(method, request, response);
                return;
            }

            await HandleNotFound(response);
        }

        private async Task HandleLobbyEndpoint(string method, HttpListenerRequest request, HttpListenerResponse response)
        {
            throw new NotImplementedException();
        }

        private async Task HandleLobbyIdEndpoint(int lobbyId, string method, HttpListenerRequest request, HttpListenerResponse response)
        {
            throw new NotImplementedException();
        }

        private async Task HandleLobbyIdUserEndpoint(int lobbyId, string method, HttpListenerRequest request, HttpListenerResponse response)
        {
            throw new NotImplementedException();
        }

        private async Task HandleLobbyIdUserIdEndpoint(int lobbyId, int userId, string method, HttpListenerRequest request, HttpListenerResponse response)
        {
            throw new NotImplementedException();
        }

        private async Task HandleNotFound(HttpListenerResponse response)
        {
            response.Headers.Set("Content-Type", "text/plain");
            response.StatusCode = (int)HttpStatusCode.NotFound;
            response.StatusDescription = "Endpoint not found";

            using Stream ros = response.OutputStream;
            string err = "404 - not found";

            byte[] ebuf = Encoding.UTF8.GetBytes(err);
            response.ContentLength64 = ebuf.Length;

            ros.Write(ebuf, 0, ebuf.Length);
        }

        public void Stop()
        {
            httpListener?.Stop();
            httpListener = null;
        }
    }
}
