using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Volumetric Raycasting System for Wwise - Smart Reverb
/// Uses Fibonacci Sphere Sampling to calculate room acoustics procedurally
/// No manual reverb zones required!
/// </summary>
[AddComponentMenu("Wwise/Smart Acoustics/Wwise Smart Reverb")]
public class WwiseSmartReverb : MonoBehaviour
{
    [Header("Mode Settings")]
    [Tooltip("If true, sets Global RTPCs affecting the entire mix. If false, sets RTPCs on a specific AkGameObj")]
    public bool isGlobal = true;
    
    [Tooltip("Target AkGameObj for local mode (ignored if IsGlobal is true)")]
    public AkGameObj targetGameObject;

    [Header("RTPC Configuration")]
    [Tooltip("Name of the RTPC in Wwise for Enclosure Factor (0 = outdoor, 1 = indoor)")]
    public string enclosureRTPCName = "Enclosure";
    
    [Tooltip("Name of the RTPC in Wwise for Room Size (0-100 meters)")]
    public string roomSizeRTPCName = "RoomSize";
    
    [Tooltip("Name of the RTPC in Wwise for Reverb Send Level (0-100)")]
    public string reverbSendRTPCName = "ReverbSend";

    [Header("Raycast Configuration")]
    [Tooltip("Number of rays in the Fibonacci sphere (30-60 recommended for quality)")]
    [Range(10, 100)]
    public int rayCount = 30;
    
    [Tooltip("Maximum distance for environment detection")]
    public float maxRayDistance = 50f;
    
    [Tooltip("LayerMask for environment geometry")]
    public LayerMask environmentLayer;

    [Header("Update Settings")]
    [Tooltip("How often to recalculate acoustics (Hz). 5Hz = every 0.2s")]
    [Range(1f, 30f)]
    public float scanRate = 5f;
    
    [Tooltip("Smoothing speed for parameter changes (higher = faster)")]
    [Range(0.1f, 10f)]
    public float smoothingSpeed = 3f;

    [Header("Calibration Curves")]
    [Tooltip("Maps hit ratio to enclosure factor")]
    public AnimationCurve enclosureCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    
    [Tooltip("Maps average distance to room size")]
    public AnimationCurve roomSizeCurve = AnimationCurve.Linear(0f, 5f, 50f, 100f);
    
    [Tooltip("Maps enclosure to reverb send level")]
    public AnimationCurve reverbSendCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 80f);

    [Header("Debug")]
    public bool drawDebugRays = false;
    public Color hitColor = Color.red;
    public Color missColor = Color.green;

    // Internal State
    private Vector3[] rayDirections;
    private float lastScanTime;
    private float currentEnclosure;
    private float currentRoomSize;
    private float currentReverbSend;
    private float targetEnclosure;
    private float targetRoomSize;
    private float targetReverbSend;

    // Public Properties
    public float EnclosureFactor => currentEnclosure;
    public float RoomSize => currentRoomSize;
    public float ReverbSend => currentReverbSend;

    void Awake()
    {
        GenerateFibonacciSphere();
        
        if (!isGlobal && targetGameObject == null)
        {
            targetGameObject = GetComponent<AkGameObj>();
            if (targetGameObject == null)
            {
                Debug.LogWarning($"[WwiseSmartReverb] Local mode enabled but no AkGameObj found on {gameObject.name}. Switching to Global mode.");
                isGlobal = true;
            }
        }
    }

    void Start()
    {
        // Force immediate scan after all Awake() calls complete
        // This ensures AkGameObj is registered before we try to use it
        lastScanTime = -1f;
    }

    void Update()
    {
        if (Time.time - lastScanTime >= 1f / scanRate)
        {
            PerformVolumetricScan();
            lastScanTime = Time.time;
        }

        SmoothParameters();
    }

    void GenerateFibonacciSphere()
    {
        rayDirections = new Vector3[rayCount];
        float goldenRatio = (1f + Mathf.Sqrt(5f)) / 2f;
        float angleIncrement = Mathf.PI * 2f * goldenRatio;

        for (int i = 0; i < rayCount; i++)
        {
            float t = (float)i / rayCount;
            float inclination = Mathf.Acos(1f - 2f * t);
            float azimuth = angleIncrement * i;

            // Convert spherical to Cartesian
            float x = Mathf.Sin(inclination) * Mathf.Cos(azimuth);
            float y = Mathf.Sin(inclination) * Mathf.Sin(azimuth);
            float z = Mathf.Cos(inclination);

            rayDirections[i] = new Vector3(x, y, z);
        }

        Debug.Log($"[WwiseSmartReverb] Generated {rayCount} Fibonacci rays");
    }

    void PerformVolumetricScan()
    {
        int hitCount = 0;
        float totalDistance = 0f;
        Vector3 origin = transform.position;

        foreach (Vector3 direction in rayDirections)
        {
            Ray ray = new Ray(origin, direction);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, maxRayDistance, environmentLayer))
            {
                hitCount++;
                totalDistance += hit.distance;

                if (drawDebugRays)
                    Debug.DrawLine(origin, hit.point, hitColor, 1f / scanRate);
            }
            else
            {
                totalDistance += maxRayDistance;

                if (drawDebugRays)
                    Debug.DrawRay(origin, direction * maxRayDistance, missColor, 1f / scanRate);
            }
        }

        // Calculate acoustic parameters
        float hitRatio = (float)hitCount / rayCount;
        float avgDistance = totalDistance / rayCount;

        targetEnclosure = enclosureCurve.Evaluate(hitRatio);
        targetRoomSize = roomSizeCurve.Evaluate(avgDistance);
        targetReverbSend = reverbSendCurve.Evaluate(targetEnclosure);
    }
    
    void SmoothParameters()
    {
        currentEnclosure = Mathf.Lerp(currentEnclosure, targetEnclosure, Time.deltaTime * smoothingSpeed);
        currentRoomSize = Mathf.Lerp(currentRoomSize, targetRoomSize, Time.deltaTime * smoothingSpeed);
        currentReverbSend = Mathf.Lerp(currentReverbSend, targetReverbSend, Time.deltaTime * smoothingSpeed);

        UpdateWwiseParameters();
    }
    
    void UpdateWwiseParameters()
    {
        if (isGlobal)
        {
            // Global RTPCs (affects entire mix)
            AkSoundEngine.SetRTPCValue(enclosureRTPCName, currentEnclosure);
            AkSoundEngine.SetRTPCValue(roomSizeRTPCName, currentRoomSize);
            AkSoundEngine.SetRTPCValue(reverbSendRTPCName, currentReverbSend);
        }
        else if (targetGameObject != null)
        {
            // Local RTPCs (affects specific game object)
            AkSoundEngine.SetRTPCValue(enclosureRTPCName, currentEnclosure, targetGameObject.gameObject);
            AkSoundEngine.SetRTPCValue(roomSizeRTPCName, currentRoomSize, targetGameObject.gameObject);
            AkSoundEngine.SetRTPCValue(reverbSendRTPCName, currentReverbSend, targetGameObject.gameObject);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;

        // Draw current enclosure state
        Gizmos.color = Color.Lerp(Color.green, Color.red, currentEnclosure);
        Gizmos.DrawWireSphere(transform.position, 0.5f);

        // Draw room size indicator
        Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, currentRoomSize * 0.1f);
    }

    // Public API for external systems
    public void ForceRescan()
    {
        PerformVolumetricScan();
    }

    public void SetScanRate(float newRate)
    {
        scanRate = Mathf.Clamp(newRate, 1f, 30f);
    }

    void OnValidate()
    {
        // Regenerate sphere if ray count changes in editor
        if (rayDirections == null || rayDirections.Length != rayCount)
        {
            GenerateFibonacciSphere();
        }
    }
}