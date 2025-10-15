using UnityEngine;

namespace Resonance.DebugTools
{
    public class PerformanceDebugPanel : MonoBehaviour
    {
        #region Class Variables
        [Header("Performance Settings")]
        [SerializeField] private float updateInterval = 0.5f;
        
        // Performance metrics
        private float _currentFPS;
        private float _currentMs;
        private float _worstMs;
        private float _bestMs = float.MaxValue;
        private float _memoryMB;
        
        private int _frameCount;
        private float _fpsAccumulator;
        private float _timeSinceUpdate;
        
        // Time controls
        private bool _isPaused = false;
        #endregion

        #region Update
        private void Update()
        {
            UpdatePerformanceMetrics();
        }

        private void UpdatePerformanceMetrics()
        {
            float currentFrameMs = Time.unscaledDeltaTime * 1000f;

            if (currentFrameMs > _worstMs)
                _worstMs = currentFrameMs;
            
            if (currentFrameMs < _bestMs && currentFrameMs > 0)
                _bestMs = currentFrameMs;

            _timeSinceUpdate += Time.unscaledDeltaTime;
            _fpsAccumulator += Time.timeScale / Time.unscaledDeltaTime;
            _frameCount++;

            if (_timeSinceUpdate >= updateInterval)
            {
                _currentFPS = _fpsAccumulator / _frameCount;
                _currentMs = 1000f / _currentFPS;
                _memoryMB = System.GC.GetTotalMemory(false) / 1048576f;
                
                _timeSinceUpdate = 0f;
                _fpsAccumulator = 0f;
                _frameCount = 0;
            }
        }
        #endregion

        #region Public Methods
        public void DrawPanel()
        {
            GUILayout.BeginVertical("box");
            GUILayout.Label("=== PERFORMANCE STATS ===");
            GUILayout.Space(10);
            
            // Stats
            GUILayout.Label($"FPS: {_currentFPS:F0}");
            GUILayout.Label($"Frame Time: {_currentMs:F1} ms");
            GUILayout.Label($"Worst: {_worstMs:F1} ms");
            GUILayout.Label($"Best: {_bestMs:F1} ms");
            GUILayout.Label($"Memory: {_memoryMB:F1} MB");
            
            GUILayout.Space(15);
            
            GUILayout.Label("=== TIME CONTROLS ===");
            GUILayout.Space(10);
            
            if (GUILayout.Button(_isPaused ? "Resume" : "Pause", GUILayout.Height(35)))
            {
                _isPaused = !_isPaused;
                Time.timeScale = _isPaused ? 0f : 1f;
            }
            
            GUILayout.EndVertical();
        }
        #endregion
    }
}