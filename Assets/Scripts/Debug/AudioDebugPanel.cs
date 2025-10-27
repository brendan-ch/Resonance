using UnityEngine;

namespace Resonance.DebugTools
{
    public class AudioDebugPanel : MonoBehaviour
    {
        #region Class Variables
        [Header("Test Sound Settings")]
        [SerializeField] private string testEventName = "Play_Test_Sound";
        [SerializeField] private GameObject soundEmitter;
        
        private string _customEventName = "";
        #endregion
        
        #region Startup
        private void Start()
        {
            // Find player as default sound emitter if not assigned
            if (soundEmitter == null)
            {
                soundEmitter = GameObject.FindGameObjectWithTag("Player");
                if (soundEmitter == null)
                {
                    Debug.LogWarning("AudioDebugPanel: No sound emitter assigned and no Player found!");
                }
            }
        }
        #endregion
        
        #region Public Methods
        public void DrawPanel()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("=== AUDIO DEBUG ===");
            GUILayout.Space(10);
            
            // Test Sound Trigger
            GUILayout.Label("Test Sound Trigger:");
            GUILayout.Space(5);
            DrawTestSoundTrigger();
            
            GUILayout.Space(15);
            
            // Future tools
            GUILayout.Label("Coming Soon:");
            GUILayout.Label("- Bus intensity monitors");
            GUILayout.Label("- RTPC overrides");
            GUILayout.Label("- Visualization controls");
            
            GUILayout.EndVertical();
        }
        
        private void DrawTestSoundTrigger()
        {
            GUILayout.BeginVertical("box");
            
            // Show current event name
            GUILayout.Label($"Event: {testEventName}");
            
            if (soundEmitter != null)
            {
                GUILayout.Label($"Emitter: {soundEmitter.name}");
            }
            else
            {
                GUI.color = Color.yellow;
                GUILayout.Label("Warning: No sound emitter!");
                GUI.color = Color.white;
            }
            
            GUILayout.Space(10);
            
            // Trigger button
            if (GUILayout.Button("Trigger Test Sound", GUILayout.Height(40)))
            {
                TriggerTestSound();
            }
            
            GUILayout.Space(10);
            
            // Custom event input
            GUILayout.Label("Custom Event Name:");
            _customEventName = GUILayout.TextField(_customEventName, GUILayout.Height(25));
            
            if (GUILayout.Button("Trigger Custom Event", GUILayout.Height(30)))
            {
                TriggerCustomSound(_customEventName);
            }
            
            GUILayout.EndVertical();
        }
        
        private void TriggerTestSound()
        {
            if (soundEmitter == null)
            {
                Debug.LogWarning("AudioDebugPanel: Cannot trigger sound - no emitter assigned!");
                return;
            }
            
            Debug.Log($"AudioDebugPanel: Triggering test sound '{testEventName}' on {soundEmitter.name}");
            AkUnitySoundEngine.PostEvent(testEventName, soundEmitter);
        }
        
        private void TriggerCustomSound(string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                Debug.LogWarning("AudioDebugPanel: Cannot trigger sound - event name is empty!");
                return;
            }
            
            if (soundEmitter == null)
            {
                Debug.LogWarning("AudioDebugPanel: Cannot trigger sound - no emitter assigned!");
                return;
            }
            
            Debug.Log($"AudioDebugPanel: Triggering custom sound '{eventName}' on {soundEmitter.name}");
            AkUnitySoundEngine.PostEvent(eventName, soundEmitter);
        }
        #endregion
    }
}