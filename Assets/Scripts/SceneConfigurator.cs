using Resonance.LobbySystem;
using UnityEngine;

public class SceneConfigurator : MonoBehaviour
{
    public static AppConfig Current { get; private set; }

    [SerializeField] AppConfig config;
    [SerializeField] GameObject steamLobbyObject;
    [SerializeField] LobbyManager lobbyManager;
    [SerializeField] MonoBehaviour steamProvider;
    [SerializeField] MonoBehaviour dummyProvider;

    void Awake()
    {
        Current = config;
        if (steamLobbyObject != null)
        {
            steamLobbyObject.SetActive(config.enableSteamLobby);
        }
    }

    void Start()
    {
        if (lobbyManager == null)
        {
            return;
        }

        var provider = config.enableSteamLobby
            ? steamProvider as ILobbyProvider
            : dummyProvider as ILobbyProvider;
        lobbyManager.SetProvider(provider);
    }
}
