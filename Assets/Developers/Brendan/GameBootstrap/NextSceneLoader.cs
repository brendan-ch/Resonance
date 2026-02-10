using Mono.Cecil.Cil;
using PurrNet;
using Resonance.LobbySystem;
using Resonance.Match;
using UnityEngine;

public class NextSceneLoader : NetworkBehaviour
{
    // [SerializeField] private NetworkManager networkManager;

    private LobbyDataHolder lobbyDataHolder;

    private int playerJoinedCount = 0;
    private bool matchLogicSpawned = false;

    private bool shouldLoadNextScene => matchLogicSpawned && playerJoinedCount == lobbyDataHolder.CurrentLobby.Members.Count;

    protected override void OnSpawned()
    {
        base.OnSpawned();

        lobbyDataHolder = FindFirstObjectByType<LobbyDataHolder>();
        if (!lobbyDataHolder)
        {
            Debug.LogError($"Unable to find {nameof(LobbyDataHolder)} component; scene switching will not work.");
        }

        networkManager.onPlayerJoined += (playerId, isReconnect, isServer) =>
        {
            UpdatePlayerJoinedCount();
            ConditionallyLoadNextScene();
        };
    }

    private void UpdatePlayerJoinedCount()
    {
        playerJoinedCount = networkManager.playerCount;
    }

    public void UpdateMatchLogicSpawnStatus()
    {
        var matchLogicAdapter = FindFirstObjectByType<MatchLogicNetworkAdapter>();
        if (matchLogicAdapter)
        {
            matchLogicSpawned = true;
        }
        ConditionallyLoadNextScene();
    }

    private void ConditionallyLoadNextScene()
    {
        if (shouldLoadNextScene)
        {
            var sceneToSwitchTo = lobbyDataHolder.CurrentLobby.SceneName;
            networkManager.sceneModule.LoadSceneAsync(sceneToSwitchTo);
        }
    }
}
