using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace Resonance.UI
{
    public class HealthBar : MonoBehaviour
    {
        [Header("UI References")]
        public Image healthFill;        // Front bar
        public Image damageFill;        // Delayed damage bar
        public TextMeshProUGUI healthText;

        [Header("Animation")]
        [SerializeField] float damageLerpSpeed = 2f;
        [SerializeField] float numberTickSpeed = 5f;

        [Header("Damage Flash")]
        [SerializeField] Color damageFlashColor = Color.red;
        [SerializeField] float flashDuration = 0.2f;

        float maxHealth;
        float targetHealth;
        float displayedHealth;
        float displayedDamageBar;

        Coroutine flashRoutine;

        public void SetSliderMax(float max)
        {
            maxHealth = max;
            targetHealth = max;
            displayedHealth = max;
            displayedDamageBar = max;

            UpdateUIInstant();
        }

        public void SetSlider(float newHealth)
        {
            if (newHealth < targetHealth)
            {
                if (flashRoutine != null)
                {
                    StopCoroutine(flashRoutine);
                    healthText.color = Color.white;
                }

                flashRoutine = StartCoroutine(FlashNumbers());
            }

            targetHealth = newHealth;
        }
        
        void Update()
        {
            if (maxHealth <= 0) return;

            // Front bar moves immediately
            displayedHealth = Mathf.Lerp(displayedHealth, targetHealth, Time.deltaTime * 15f);

            // Damage bar lags behind
            displayedDamageBar = Mathf.Lerp(displayedDamageBar, targetHealth, Time.deltaTime * damageLerpSpeed);

            UpdateUI();
        }

        void UpdateUI()
        {
            float healthPercent = displayedHealth / maxHealth;
            float damagePercent = displayedDamageBar / maxHealth;

            healthFill.fillAmount = healthPercent;
            damageFill.fillAmount = damagePercent;

            // Update text to match displayed health
            healthText.text = $"{Mathf.RoundToInt(displayedHealth)} / {Mathf.RoundToInt(maxHealth)}";
        }

        void UpdateUIInstant()
        {
            healthFill.fillAmount = 1f;
            damageFill.fillAmount = 1f;
            healthText.text = $"{maxHealth} / {maxHealth}";
        }

        IEnumerator FlashNumbers()
        {
            healthText.color = damageFlashColor;

            yield return new WaitForSeconds(flashDuration);

            healthText.color = Color.white;
        }
    }
}
