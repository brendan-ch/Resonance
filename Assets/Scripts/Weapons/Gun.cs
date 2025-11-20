using UnityEngine;
using Resonance.PlayerController;

public class Gun : MonoBehaviour
{
    public GunData gunData;
    public PlayerController playerController;
    public Transform cameraTransform;

    private float currentAmmo = 0f;
    private float nextTimeToFire = 0f;
    
    private bool isReloading = false;

    private void Start()
    {
        currentAmmo = gunData.magazineSize;
        
        playerController = transform.root.GetComponent<PlayerController>();
    }
}
