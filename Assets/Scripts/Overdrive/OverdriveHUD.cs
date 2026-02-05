using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Resonance.PlayerController;

public class OverdriveHUD : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private OverdriveAbility overdrive;
    [SerializeField] private Image icon;
    [SerializeField] private Image cooldownFill;
    [SerializeField] private TextMeshProUGUI timerText;

    [Header("Colors")]
    [SerializeField] private Color readyColor = Color.white;
    [SerializeField] private Color fadedColor = new Color(1f, 1f, 1f, 0.4f);
    [SerializeField] private Color activeColor = Color.cyan;

    [Header("Pulse Animation")]
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private float pulseAmount = 0.2f;
    private Vector3 originalScale;

    private void Awake()
    {
        if (icon != null)
            originalScale = icon.transform.localScale;

        // No player reference yet — will be set by OverdriveAbility
    }

    private void Update()
    {
        if (overdrive == null) return;

        switch (overdrive.CurrentState)
        {
            case OverdriveAbility.OverdriveState.Ready:
                ShowReady();
                break;
            case OverdriveAbility.OverdriveState.Active:
                ShowActive();
                break;
            case OverdriveAbility.OverdriveState.Cooldown:
                ShowCooldown();
                break;
        }
    }

    #region Display States
    private void ShowReady()
    {
        icon.color = readyColor;
        cooldownFill.fillAmount = 0f;
        SetAlpha(cooldownFill, 0f);

        timerText.text = "";
        timerText.color = readyColor;

        //PulseIcon();
        ResetIconScale();
    }

    private void ShowCooldown()
    {
        float fill = overdrive.CooldownTimeRemaining / overdrive.CooldownDuration;
        icon.color = fadedColor;

        SetAlpha(cooldownFill, 1f);
        cooldownFill.fillAmount = fill;

        timerText.text = $"{overdrive.CooldownTimeRemaining:F1}s";
        timerText.color = fadedColor;

        ResetIconScale();
    }

    private void ShowActive()
    {
        icon.color = activeColor;

        SetAlpha(cooldownFill, 0f);

        timerText.text = $"{overdrive.DurationTimeRemaining:F1}s";
        timerText.color = activeColor;

        ResetIconScale();
    }
    #endregion

    #region Helper Methods
    private void SetAlpha(Image img, float a)
    {
        Color c = img.color;
        c.a = a;
        img.color = c;
    }

    private void PulseIcon()
    {
        float scaleOffset = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
        icon.transform.localScale = originalScale * (1f + scaleOffset);
    }

    private void ResetIconScale()
    {
        icon.transform.localScale = originalScale;
    }
    #endregion

    #region Public Registration
    public void SetOverdriveAbility(OverdriveAbility ability)
    {
        overdrive = ability;
    }
    #endregion
}
