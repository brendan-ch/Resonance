using UnityEngine;

namespace Resonance.Match
{
    public class MatchStarter : MonoBehaviour
    {
        [Header("Auto Start Settings")]
        [SerializeField] private bool autoStartOnSceneLoad = true;
        [SerializeField] private float startDelay = 0.5f; // Small delay to ensure everything is initialized
        
        private void Start()
        {
            if (autoStartOnSceneLoad)
            {
                Invoke(nameof(StartMatch), startDelay);
            }
        }
        
        private void StartMatch()
        {
            if (ArenaRoundManager.Instance != null)
            {
                ArenaRoundManager.Instance.StartMatch();
                Debug.Log("[MatchStarter] Match started automatically!");
            }
            else
            {
                Debug.LogError("[MatchStarter] ArenaRoundManager.Instance is null! Make sure ArenaRoundManager is in the scene.");
            }
        }
        
        // Manual start method you can call from a button or inspector
        [ContextMenu("Start Match Now")]
        public void StartMatchManually()
        {
            StartMatch();
        }
    }
}
