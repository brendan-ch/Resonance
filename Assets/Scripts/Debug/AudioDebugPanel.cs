using UnityEngine;

namespace Resonance.DebugTools
{
    public class AudioDebugPanel : MonoBehaviour
    {
        #region Public Methods
        public void DrawPanel()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("=== AUDIO DEBUG ===");
            GUILayout.Space(10);
            
            GUILayout.Label("Audio system debug tools coming soon!");
            GUILayout.Space(10);
            GUILayout.Label("Will include:");
            GUILayout.Label("- Bus intensity monitors");
            GUILayout.Label("- Manual event triggers");
            GUILayout.Label("- RTPC overrides");
            GUILayout.Label("- Visualization controls");
            
            GUILayout.EndVertical();
        }
        #endregion
    }
}