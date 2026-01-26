using PurrNet;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Resonance.LobbySystem
{
    public class SceneSwitcher : MonoBehaviour
    {
        [SerializeField] private LobbyManager lobbyManager;
        [PurrScene, SerializeField] private string nextScene;

        public void SwitchScene()
        {
            lobbyManager.SetLobbyStarted();
            SceneManager.LoadSceneAsync(nextScene);
        }
    }
}
