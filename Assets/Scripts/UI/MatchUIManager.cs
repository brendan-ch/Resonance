using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Resonance.Match;
using System.Collections.Generic;

namespace Resonance.UI
{
    public class MatchUIManager : MonoBehaviour
    {
        [Header("HUD Elements")]
        [SerializeField] private TextMeshProUGUI kdaText;
        [SerializeField] private TextMeshProUGUI killStreakText;
        [SerializeField] private TextMeshProUGUI eliminationsText;
        
        [Header("Match End UI")]
        [SerializeField] private GameObject matchEndPanel;
        [SerializeField] private TextMeshProUGUI winnerText;
        [SerializeField] private TextMeshProUGUI finalStatsText;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button quitButton;
        
        [Header("Settings")]
        [SerializeField] private GameObject playerObject; // Assign the player to track
        [SerializeField] private bool showKillStreak = true;
        
        private void Start()
        {
            if (matchEndPanel != null)
            {
                matchEndPanel.SetActive(false);
            }
            
            // Initialize kill streak text to show 0
            if (killStreakText != null && showKillStreak)
            {
                killStreakText.text = "Kill Streak: 0";
                killStreakText.gameObject.SetActive(true);
            }
            
            SetupButtons();
            SubscribeToEvents();
            UpdateHUD();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }
        
        private void Update()
        {
            UpdateHUD();
        }
        
        #region Event Subscriptions
        private void SubscribeToEvents()
        {
            if (ArenaRoundManager.Instance != null)
            {
                ArenaRoundManager.Instance.OnMatchEnd += OnMatchEnd;
                ArenaRoundManager.Instance.OnLeaderChanged += OnLeaderChanged;
            }
            
            if (MatchStatTracker.Instance != null)
            {
                MatchStatTracker.Instance.OnPlayerKill += OnPlayerKill;
            }
        }
        
        private void UnsubscribeFromEvents()
        {
            if (ArenaRoundManager.Instance != null)
            {
                ArenaRoundManager.Instance.OnMatchEnd -= OnMatchEnd;
                ArenaRoundManager.Instance.OnLeaderChanged -= OnLeaderChanged;
            }
            
            if (MatchStatTracker.Instance != null)
            {
                MatchStatTracker.Instance.OnPlayerKill -= OnPlayerKill;
            }
        }
        #endregion
        
        #region Button Setup
        private void SetupButtons()
        {
            if (playAgainButton != null)
            {
                playAgainButton.onClick.AddListener(OnPlayAgainClicked);
                Debug.Log("[MatchUI] Play Again button listener added");
            }
            else
            {
                Debug.LogWarning("[MatchUI] Play Again button is null!");
            }
            
            if (quitButton != null)
            {
                quitButton.onClick.AddListener(OnQuitClicked);
                Debug.Log("[MatchUI] Quit button listener added");
            }
            else
            {
                Debug.LogWarning("[MatchUI] Quit button is null!");
            }
        }
        
        private void OnPlayAgainClicked()
        {
            Debug.Log("[MatchUI] Play Again clicked!");
            
            // Reset time scale in case it was paused
            Time.timeScale = 1f;
            
            if (ArenaRoundManager.Instance != null)
            {
                ArenaRoundManager.Instance.ReloadLevel();
            }
        }
        
        private void OnQuitClicked()
        {
            Debug.Log("[MatchUI] Quit clicked!");
            
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }
        #endregion
        
        #region HUD Updates
        private void UpdateHUD()
        {
            if (playerObject == null || MatchStatTracker.Instance == null) return;
            
            PlayerMatchStats stats = MatchStatTracker.Instance.GetStats(playerObject);
            
            // Update KDA
            if (kdaText != null)
            {
                kdaText.text = $"K/D/A: {stats.kills}/{stats.deaths}/{stats.assists} | KDA: {stats.KDA:F2}";
            }
            
            // Update kill streak
            if (killStreakText != null && showKillStreak)
            {
                killStreakText.text = $"Kill Streak: {stats.killStreak}";
                killStreakText.gameObject.SetActive(true);
            }
            
            // Update eliminations progress
            if (eliminationsText != null && ArenaRoundManager.Instance != null)
            {
                int target = ArenaRoundManager.Instance.EliminationsToWin;
                eliminationsText.text = $"Eliminations: {stats.kills}/{target}";
            }
        }
        #endregion
        
        #region Event Handlers
        private void OnMatchEnd(GameObject winner)
        {
            if (matchEndPanel != null)
            {
                matchEndPanel.SetActive(true);
            }
            
            // Unlock cursor so player can click buttons
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            
            // Disable player controls
            if (playerObject != null)
            {
                var playerController = playerObject.GetComponent<PlayerController.PlayerController>();
                if (playerController != null)
                {
                    playerController.enabled = false;
                }
                
                var projectileShooter = playerObject.GetComponent<Resonance.Combat.ProjectileShooter>();
                if (projectileShooter != null)
                {
                    projectileShooter.enabled = false;
                }
            }
            
            // Pause time (optional - uncomment if you want to freeze everything)
            // Time.timeScale = 0f;
            
            if (winnerText != null)
            {
                if (winner != null)
                {
                    string winnerName = winner == playerObject ? "You Win!" : $"{winner.name} Wins!";
                    winnerText.text = winnerName;
                }
                else
                {
                    winnerText.text = "Match Ended";
                }
            }
            
            // Optionally show basic stats in winner text
            if (winner != null && MatchStatTracker.Instance != null)
            {
                PlayerMatchStats stats = MatchStatTracker.Instance.GetStats(winner);
                if (finalStatsText != null)
                {
                    finalStatsText.text = $"Final Score: {stats.kills} Kills";
                }
            }
        }
        
        private void OnLeaderChanged(GameObject newLeader, int eliminations)
        {
            // Leader tracking removed
        }
        
        private void OnPlayerKill(GameObject killer, GameObject victim)
        {
            // Optional: Add kill feed notifications here
        }
        #endregion
        
        #region Public Methods
        public void SetPlayerObject(GameObject player)
        {
            playerObject = player;
            UpdateHUD();
        }
        
        public void ShowMatchEndScreen()
        {
            if (matchEndPanel != null)
            {
                matchEndPanel.SetActive(true);
            }
        }
        
        public void HideMatchEndScreen()
        {
            if (matchEndPanel != null)
            {
                matchEndPanel.SetActive(false);
            }
        }
        #endregion
    }
}