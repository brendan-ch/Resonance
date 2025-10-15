using UnityEngine;

namespace Resonance.DebugTools
{
    public class SceneDebugPanel : MonoBehaviour
    {
        #region Public Methods
        public void DrawPanel()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("=== SCENE TOOLS ===");
            GUILayout.Space(10);
            
            // Current scene info
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            GUILayout.Label($"Current Scene: {sceneName}");
            
            GUILayout.Space(15);
            
            // Quick Actions
            if (GUILayout.Button("Reload Current Scene", GUILayout.Height(35)))
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("Quit Application", GUILayout.Height(35)))
            {
                Application.Quit();
            }
            
            GUILayout.Space(15);
            
            // Future: Scene list will go here
            GUILayout.Label("Scene Switching: Coming Soon");
            
            GUILayout.EndVertical();
        }
        #endregion
    }
}