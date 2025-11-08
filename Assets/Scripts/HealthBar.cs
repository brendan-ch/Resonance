using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [Header("UI References")]
    public Slider healthSlider;
    public Image fillImage;
    
    [Header("Color Settings")]
    [SerializeField] private Color highHealthColor = Color.green;
    [SerializeField] private Color mediumHealthColor = Color.yellow;
    [SerializeField] private Color lowHealthColor = Color.red;
    
    [Header("Thresholds (0-1)")]
    [SerializeField] [Range(0f, 1f)] private float mediumHealthThreshold = 0.6f;
    [SerializeField] [Range(0f, 1f)] private float lowHealthThreshold = 0.3f;
    
    [Header("Animation Settings")]
    [SerializeField] private float lerpSpeed = 5f;
    
    private float targetHealthValue;
    private float displayedHealthValue;

    private void Start()
    {
        if (healthSlider != null)
        {
            displayedHealthValue = healthSlider.value;
            targetHealthValue = displayedHealthValue;
            UpdateHealthColor();
        }
    }

    private void Update()
    {
        if (Mathf.Abs(displayedHealthValue - targetHealthValue) > 0.01f)
        {
            displayedHealthValue = Mathf.Lerp(displayedHealthValue, targetHealthValue, Time.deltaTime * lerpSpeed);
            healthSlider.value = displayedHealthValue;
            UpdateHealthColor();
        }
        else if (displayedHealthValue != targetHealthValue)
        {
            displayedHealthValue = targetHealthValue;
            healthSlider.value = displayedHealthValue;
            UpdateHealthColor();
        }
    }

    public void SetSlider(float amount)
    {
        targetHealthValue = amount;
    }

    public void SetSliderMax(float amount)
    {
        healthSlider.maxValue = amount;
        targetHealthValue = amount;
        displayedHealthValue = amount;
        healthSlider.value = amount;
        UpdateHealthColor();
    }
    
    private void UpdateHealthColor()
    {
        if (fillImage == null || healthSlider == null) return;
        
        float healthPercentage = healthSlider.value / healthSlider.maxValue;
        
        Color newColor;
        
        if (healthPercentage > mediumHealthThreshold)
        {
            newColor = highHealthColor;
        }
        else if (healthPercentage > lowHealthThreshold)
        {
            float gradientPosition = (healthPercentage - lowHealthThreshold) / (mediumHealthThreshold - lowHealthThreshold);
            newColor = Color.Lerp(mediumHealthColor, highHealthColor, gradientPosition);
        }
        else
        {
            float gradientPosition = healthPercentage / lowHealthThreshold;
            newColor = Color.Lerp(lowHealthColor, mediumHealthColor, gradientPosition);
        }
        
        fillImage.color = newColor;
    }
}