using PurrNet;
using Resonance.LobbySystem;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NextSceneLoader : MonoBehaviour
{
    [SerializeField] private NetworkManager networkManager;

    private LobbyDataHolder lobbyDataHolder;
    private void Awake()
    {
        lobbyDataHolder = FindFirstObjectByType<LobbyDataHolder>();
        if (!lobbyDataHolder)
        {
            Debug.LogError($"Unable to find {nameof(LobbyDataHolder)} component");
        }
    }

    private void Start()
    {
        networkManager.onPlayerJoined += ConditionallyLoadNextScene;
    }

    private void ConditionallyLoadNextScene(PlayerID player, bool isReconnect, bool asServer)
    {
        var numPlayersInLobby = lobbyDataHolder.CurrentLobby.Members.Count;
        if (networkManager.playerCount == numPlayersInLobby)
        {
            var sceneToLoad = lobbyDataHolder.CurrentLobby.SceneName;
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}
