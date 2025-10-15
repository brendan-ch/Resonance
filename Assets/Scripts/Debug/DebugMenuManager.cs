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
            
            GUI.Window(0, new Rect(50, 50, 400, 200), DrawDebugWindow, "Resonance Debug Menu");
        }

        private void DrawDebugWindow(int windowID)
        {
            GUILayout.Label("Hello Resonance!");
            GUILayout.Space(10);
            GUILayout.Label("Press F1 to toggle this menu");
            GUILayout.Space(20);
            
            if (GUILayout.Button("Close Menu"))
            {
                _showMenu = false;
            }
            
            GUI.DragWindow();
        }
        #endregion
    }
}