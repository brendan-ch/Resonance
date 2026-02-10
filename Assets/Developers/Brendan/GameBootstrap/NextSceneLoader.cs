using PurrNet;
using Resonance.LobbySystem;
using UnityEngine;

public class NextSceneLoader : NetworkBehaviour
{
    // [SerializeField] private NetworkManager networkManager;

    private LobbyDataHolder lobbyDataHolder;

    protected override void OnSpawned()
    {
        base.OnSpawned();

        lobbyDataHolder = FindFirstObjectByType<LobbyDataHolder>();
        if (!lobbyDataHolder)
        {
            Debug.LogError($"Unable to find {nameof(LobbyDataHolder)} component");
        }

        networkManager.onPlayerJoined += ConditionallyLoadNextScene;
    }

    private void ConditionallyLoadNextScene(PlayerID player, bool isReconnect, bool asServer)
    {
        var numPlayersInLobby = lobbyDataHolder.CurrentLobby.Members.Count;
        if (networkManager.playerCount == numPlayersInLobby)
        {
            var sceneToSwitchTo = lobbyDataHolder.CurrentLobby.SceneName;
            networkManager.sceneModule.LoadSceneAsync(sceneToSwitchTo);
        }
    }
}
