using UnityEngine;
using UnityEngine.InputSystem;

namespace Resonance.DebugTools
{
    public class DebugMenuManager : MonoBehaviour
    {
        #region Singleton
        public static DebugMenuManager Instance { get; private set; }
        #endregion

        #region Class Variables
        [Header("Settings")]
        [SerializeField] private bool showOnStart = false;
        
        private bool _showMenu = false;
        private Keyboard _keyboard;
        
        private PerformanceDebugPanel _performancePanel;
        #endregion

        #region Startup
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            _keyboard = Keyboard.current;
            _showMenu = showOnStart;
            
            // Add panels
            _performancePanel = gameObject.AddComponent<PerformanceDebugPanel>();
        }
        #endregion

        #region Update
        private void Update()
        {
            if (_keyboard != null && _keyboard.f1Key.wasPressedThisFrame)
            {
                _showMenu = !_showMenu;
                
                if (_showMenu)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }
        #endregion

        #region OnGUI
        private void OnGUI()
        {
            if (!_showMenu) return;
            
            GUI.Window(0, new Rect(50, 50, 450, 600), DrawDebugWindow, "Resonance Debug Menu");
        }

        private void DrawDebugWindow(int windowID)
        {
            GUILayout.Label("Press F1 to toggle this menu");
            GUILayout.Space(10);
            
            // Draw performance panel
            _performancePanel.DrawPanel();
            
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("Close Menu", GUILayout.Height(30)))
            {
                _showMenu = false;
            }
            
            GUI.DragWindow();
        }
        #endregion
    }
}