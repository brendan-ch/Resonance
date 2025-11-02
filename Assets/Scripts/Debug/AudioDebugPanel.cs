using UnityEngine;
using Resonance.Audio;

namespace Resonance.DebugTools
{
    public class AudioDebugPanel : MonoBehaviour
    {
        #region Inspector Fields
        [Header("Test Sound Settings")]
        [SerializeField] private string testEvent = "Play_Test_Sound";
        [SerializeField] private GameObject emitter;
        #endregion
        
        #region Private Fields
        private string _customEvent = "";
        #endregion
        
        #region Unity Lifecycle
        private void Start()
        {
            FindSoundEmitter();
        }
        #endregion
        
        #region Initialization
        private void FindSoundEmitter()
        {
            if (emitter == null)
            {
                emitter = GameObject.FindGameObjectWithTag("Player");
                
                if (emitter == null)
                {
                    Debug.LogWarning("[AudioDebugPanel] No emitter assigned and no Player found!");
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
            
            DrawTestSoundSection();
            GUILayout.Space(15);
            DrawBusIntensitySection();
            
            GUILayout.EndVertical();
        }
        #endregion
        
        #region Test Sound Section
        private void DrawTestSoundSection()
        {
            GUILayout.Label("Test Sound Trigger:");
            GUILayout.Space(5);
            
            GUILayout.BeginVertical("box");
            
            GUILayout.Label($"Event: {testEvent}");
            DrawEmitterStatus();
            GUILayout.Space(10);
            
            if (GUILayout.Button("Trigger Test Sound", GUILayout.Height(40)))
            {
                TriggerTestSound();
            }
            
            GUILayout.Space(10);
            DrawCustomEventInput();
            
            GUILayout.EndVertical();
        }

        private void DrawEmitterStatus()
        {
            if (emitter != null)
            {
                GUILayout.Label($"Emitter: {emitter.name}");
            }
            else
            {
                GUI.color = Color.yellow;
                GUILayout.Label("Warning: No emitter!");
                GUI.color = Color.white;
            }
        }

        private void DrawCustomEventInput()
        {
            GUILayout.Label("Custom Event Name:");
            _customEvent = GUILayout.TextField(_customEvent, GUILayout.Height(25));
            
            if (GUILayout.Button("Trigger Custom Event", GUILayout.Height(30)))
            {
                TriggerCustomSound(_customEvent);
            }
        }
        #endregion
        
        #region Bus Intensity Section
        private void DrawBusIntensitySection()
        {
            GUILayout.Label("Bus Intensity Monitor:");
            GUILayout.Space(5);
            
            GUILayout.BeginVertical("box");
            
            if (!IsAudioBusMonitorAvailable())
            {
                DrawAudioBusMonitorWarning();
                GUILayout.EndVertical();
                return;
            }
            
            DrawBusIntensityBars();
            GUILayout.Space(10);
            DrawLoudestBusInfo();
            
            GUILayout.EndVertical();
        }

        private bool IsAudioBusMonitorAvailable()
        {
            return AudioBusMonitor.Instance != null;
        }

        private void DrawAudioBusMonitorWarning()
        {
            GUI.color = Color.yellow;
            GUILayout.Label("AudioBusMonitor not found in scene!");
            GUILayout.Label("Add AudioBusMonitor component to use bus monitoring.");
            GUI.color = Color.white;
        }

        private void DrawBusIntensityBars()
        {
            foreach (BusType busType in System.Enum.GetValues(typeof(BusType)))
            {
                // Use RAW values for debug menu - want to see real-time response
                float rawIntensity = AudioBusMonitor.Instance.GetBusIntensityRaw(busType);
                
                GUILayout.Label($"{busType}: {rawIntensity:F3}");
                DrawIntensityBar(busType, rawIntensity);
                GUILayout.Space(5);
            }
        }

        private void DrawIntensityBar(BusType busType, float intensity)
        {
            Rect barRect = GUILayoutUtility.GetRect(380, 20);
            GUI.Box(barRect, "");
            
            Rect fillRect = new Rect(
                barRect.x + 2, 
                barRect.y + 2, 
                (barRect.width - 4) * intensity, 
                barRect.height - 4
            );
            
            GUI.color = BusTypeUtility.GetBusColor(busType);
            GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void DrawLoudestBusInfo()
        {
            // Find loudest bus based on raw values
            BusType loudestBus = BusType.Foley;
            float maxIntensity = 0f;
            
            foreach (BusType busType in System.Enum.GetValues(typeof(BusType)))
            {
                float intensity = AudioBusMonitor.Instance.GetBusIntensityRaw(busType);
                if (intensity > maxIntensity)
                {
                    maxIntensity = intensity;
                    loudestBus = busType;
                }
            }
            
            GUI.color = BusTypeUtility.GetBusColor(loudestBus);
            GUILayout.Label($"Loudest Bus: {loudestBus} ({maxIntensity:F3})", GUILayout.Height(25));
            GUI.color = Color.white;
        }
        #endregion
        
        #region Sound Triggering
        private void TriggerTestSound()
        {
            if (!ValidateEmitter()) return;
            
            Debug.Log($"[AudioDebugPanel] Triggering '{testEvent}' on {emitter.name}");
            AkUnitySoundEngine.PostEvent(testEvent, emitter);
        }
        
        private void TriggerCustomSound(string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                Debug.LogWarning("[AudioDebugPanel] Event name is empty!");
                return;
            }
            
            if (!ValidateEmitter()) return;
            
            Debug.Log($"[AudioDebugPanel] Triggering '{eventName}' on {emitter.name}");
            AkUnitySoundEngine.PostEvent(eventName, emitter);
        }

        private bool ValidateEmitter()
        {
            if (emitter == null)
            {
                Debug.LogWarning("[AudioDebugPanel] No emitter assigned!");
                return false;
            }
            return true;
        }
        #endregion
    }
}