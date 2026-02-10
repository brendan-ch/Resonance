using System;
using PurrNet;
using PurrNet.Modules;
using Resonance.LobbySystem;
using UnityEngine;
using UnityEngine.Events;

public class NetworkPlayerCounter : NetworkBehaviour
{
    public UnityEvent OnAllPlayersJoined = new();
    private LobbyDataHolder lobbyDataHolder;
    private int MemberCount => lobbyDataHolder.CurrentLobby.Members.Count;

    protected override void OnSpawned(bool asServer)
    {
        base.OnSpawned(asServer);

        lobbyDataHolder = FindFirstObjectByType<LobbyDataHolder>();
        if (!lobbyDataHolder)
        {
            Debug.LogError($"Unable to find {nameof(LobbyDataHolder)} component; scene switching will not work.");
        }

        if (asServer)
        {
            networkManager.onPlayerJoined += (_, _, _) => ConditionallyFireAllPlayersEvent();
            networkManager.onPlayerLeft += (_, _) => ConditionallyFireAllPlayersEvent();
        }
    }

    private void ConditionallyFireAllPlayersEvent()
    {
        var playerJoinedCount = networkManager.playerCount;
        if (playerJoinedCount == MemberCount)
        {
            OnAllPlayersJoined.Invoke();
        }
    }


}
