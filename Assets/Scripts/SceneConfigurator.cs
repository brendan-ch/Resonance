using Resonance.LobbySystem;
using UnityEngine;

public class SceneConfigurator : MonoBehaviour
{
    public static AppConfig Current { get; private set; }

    [SerializeField] AppConfig config;
    [SerializeField] LobbyManager lobbyManager;
    [SerializeField] GameObject steamProvider;
    [SerializeField] GameObject dummyProvider;

    void Awake()
    {
        Current = config;
        if (steamProvider != null)
        {
            steamProvider.SetActive(config.enableSteamLobby);
            if (dummyProvider != null)
            {
                dummyProvider.SetActive(!config.enableSteamLobby);
            }
        }
    }

    void Start()
    {
        if (lobbyManager == null)
        {
            return;
        }

        var provider = config.enableSteamLobby
            ? steamProvider.GetComponent<ILobbyProvider>()
            : dummyProvider.GetComponent<ILobbyProvider>();
        lobbyManager.SetProvider(provider);
    }
}
