using PurrNet;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Resonance.LobbySystem
{
    public class SceneChangeButton : MonoBehaviour
    {
        [PurrScene, SerializeField] private string scene;

        public void ChangeScene()
        {
            SceneManager.LoadSceneAsync(scene);
        }
    }
}
