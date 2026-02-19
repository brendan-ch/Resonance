using Resonance.Combat;
using UnityEngine;
using TMPro;

public class AmmoUI : MonoBehaviour
{
    private PlayerProjectileShooter shooter;
    
    private Coroutine reloadRoutine;
    private Coroutine flashRoutine;

    [SerializeField] private TextMeshProUGUI ammoText;

    [Header("Colors")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color halfColor = new Color(1f, 0.6f, 0f); // orange
    [SerializeField] private Color lowColor = Color.red;
    
    private void Start()
    {
        StartCoroutine(WaitForPlayer());
    }

    private System.Collections.IEnumerator WaitForPlayer()
    {
        while (shooter == null)
        {
            shooter = FindObjectOfType<PlayerProjectileShooter>();
            yield return null;
        }

        shooter.OnAmmoChanged += UpdateAmmo;
        shooter.OnReloadStateChanged += HandleReloadState;

        UpdateAmmo(shooter.CurrentAmmo, shooter.MagazineSize);
    }
    
    private void OnEnable()
    {
        if (shooter == null)
        {
            Debug.LogError("AmmoUI: Shooter reference missing.", this);
            return;
        }

        shooter.OnAmmoChanged += UpdateAmmo;
        shooter.OnReloadStateChanged += HandleReloadState;

        UpdateAmmo(shooter.CurrentAmmo, shooter.MagazineSize);
    }

    private void OnDisable()
    {
        if (shooter == null) return;

        shooter.OnAmmoChanged -= UpdateAmmo;
        shooter.OnReloadStateChanged -= HandleReloadState;
    }
    
    void UpdateAmmo(int current, int max)
    {
        ammoText.text = $"{current}/{max}";

        if (max == 0) return;

        float percent = (float)current / max;

        if (percent <= 0.1f)
        {
            ammoText.color = lowColor;

            if (flashRoutine == null)
                flashRoutine = StartCoroutine(FlashText());
        }
        else
        {
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
                flashRoutine = null;
                ammoText.enabled = true;
            }

            ammoText.color = percent <= 0.5f ? halfColor : normalColor;
        }

    }

    void HandleReloadState(bool isReloading)
    {
        if (isReloading)
        {
            if (reloadRoutine != null)
                StopCoroutine(reloadRoutine);

            reloadRoutine = StartCoroutine(ReloadAnimation());
        }
        else
        {
            if (reloadRoutine != null)
            {
                StopCoroutine(reloadRoutine);
                reloadRoutine = null;
            }
        }
    }
    
    System.Collections.IEnumerator ReloadAnimation()
    {
        int startAmmo = shooter.CurrentAmmo;
        int maxAmmo = shooter.MagazineSize;

        float reloadTime = shooter.ReloadDuration;
        
        if (reloadTime <= 0f)
        {
            ammoText.text = $"{maxAmmo}/{maxAmmo}";
            yield break;
        }
        
        float elapsed = 0f;

        while (elapsed < reloadTime)
        {
            elapsed += Time.deltaTime;
            
            float t = elapsed / reloadTime;

            int displayedAmmo = Mathf.RoundToInt(Mathf.Lerp(startAmmo, maxAmmo, t));
            ammoText.text = $"{displayedAmmo}/{maxAmmo}";
            ammoText.color = Color.grey;

            yield return null;
        }
    }
    
    System.Collections.IEnumerator FlashText()
    {
        while (shooter.CurrentAmmo > 0 &&
               (float)shooter.CurrentAmmo / shooter.MagazineSize <= 0.1f)
        {
            ammoText.enabled = !ammoText.enabled;
            yield return new WaitForSeconds(0.2f);
        }

        ammoText.enabled = true;
        flashRoutine = null;
    }

}