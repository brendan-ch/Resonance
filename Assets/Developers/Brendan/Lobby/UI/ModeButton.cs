using TMPro;
using UnityEngine;

namespace Resonance.LobbySystem
{
    public class ModeButton : MonoBehaviour
    {
        [SerializeField] private TMP_Text modeText;
        private GameMode _gameMode;

        public void Start()
        {
            _gameMode = GameMode.arena;
            UpdateModeText();
        }

        public void UpdateMode(GameMode mode)
        {
            _gameMode = mode;
            UpdateModeText();
        }

        private void UpdateModeText()
        {
            // Surely there's a more C#-like way to do this
            if (_gameMode == GameMode.arena)
            {
                modeText.text = "Arena";
            } else if (_gameMode == GameMode.polarity)
            {
                modeText.text = "Polarity";
            } else
            {
                modeText.text = "Unknown";
            }
        }

    }
}
