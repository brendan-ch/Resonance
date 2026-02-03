using UnityEngine;

/// <summary>
/// Volumetric Occlusion & Diffraction System for Wwise
/// Uses cone-based raycasting to simulate realistic sound obstruction
/// </summary>
[RequireComponent(typeof(AkGameObj))]
[AddComponentMenu("Wwise/Smart Acoustics/Wwise Smart Occlusion")]
public class WwiseSmartOcclusion : MonoBehaviour
{
    [Header("RTPC Configuration")]
    [Tooltip("Name of the RTPC in Wwise for Occlusion amount (0 = clear, 1 = fully blocked)")]
    public string occlusionRTPCName = "Occlusion";
    
    [Tooltip("Name of the RTPC in Wwise for Diffraction amount (0 = none, 1 = max)")]
    public string diffractionRTPCName = "Diffraction";

    [Header("Volumetric Cone Settings")]
    [Tooltip("Number of rays in the cone (6-12 recommended)")]
    [Range(3, 20)]
    public int coneRayCount = 6;
    
    [Tooltip("Cone spread angle in degrees")]
    [Range(5f, 60f)]
    public float coneAngle = 30f;
    
    [Tooltip("Maximum occlusion check distance")]
    public float maxCheckDistance = 100f;
    
    [Tooltip("LayerMask for occluding objects")]
    public LayerMask occlusionLayer;

    [Header("Diffraction Settings")]
    [Tooltip("Enable diffraction simulation (sound wrapping around corners)")]
    public bool enableDiffraction = true;
    
    [Tooltip("Distance threshold for near-field corner detection")]
    public float nearFieldThreshold = 1.5f;
    
    [Tooltip("Maps distance behind obstacle to diffraction amount")]
    public AnimationCurve diffractionCurve = AnimationCurve.EaseInOut(0f, 1f, 5f, 0f);

    [Header("Update Settings")]
    [Tooltip("Scan rate in Hz (5 = every 0.2s)")]
    [Range(1f, 30f)]
    public float scanRate = 5f;
    
    [Tooltip("Smoothing speed for occlusion changes")]
    [Range(0.1f, 10f)]
    public float smoothingSpeed = 5f;

    [Header("Optimization")]
    [Tooltip("Suspend occlusion checks when emitter is not playing")]
    public bool suspendWhenNotPlaying = true;
    
    [Tooltip("Maximum distance from listener to process occlusion")]
    public float cullingDistance = 50f;

    [Header("Debug")]
    public bool drawDebugRays = false;
    public Color clearColor = Color.green;
    public Color occludedColor = Color.red;
    public Color diffractionColor = Color.yellow;

    // Internal State
    private AkGameObj akGameObj;
    private Transform listenerTransform;
    private Vector3[] coneDirections;
    private float lastScanTime;
    private float currentOcclusion;
    private float currentDiffraction;
    private float targetOcclusion;
    private float targetDiffraction;
    private bool isPlayingCached;
    private uint lastPlayingEventID;

    // Public Properties
    public float Occlusion => currentOcclusion;
    public float Diffraction => currentDiffraction;
    public bool IsOccluded => currentOcclusion > 0.5f;

    void Awake()
    {
        akGameObj = GetComponent<AkGameObj>();
        GenerateConeDirections();
        FindListener();
    }

    void OnEnable()
    {
        lastScanTime = -1f;
    }

    void Update()
    {
        if (ShouldSkipProcessing())
            return;

        if (Time.time - lastScanTime >= 1f / scanRate)
        {
            PerformOcclusionScan();
            lastScanTime = Time.time;
        }

        SmoothParameters();
    }
    
    /// Optimization: Skip processing when not needed
    bool ShouldSkipProcessing()
    {
        if (listenerTransform == null)
        {
            FindListener();
            if (listenerTransform == null)
                return true;
        }

        // Distance culling
        float distanceToListener = Vector3.Distance(transform.position, listenerTransform.position);
        if (distanceToListener > cullingDistance)
            return true;

        // Playing state check
        if (suspendWhenNotPlaying)
        {
            // In Wwise 2025, we track playing state via event posting
            // This is a simplified check - you may need to track your own playing state
            if (!isPlayingCached)
                return true;
        }

        return false;
    }
    
    void GenerateConeDirections()
    {
        coneDirections = new Vector3[coneRayCount];
        
        // Center ray (always straight to listener)
        coneDirections[0] = Vector3.zero; // Calculated dynamically

        // Surrounding rays in a cone pattern
        if (coneRayCount > 1)
        {
            float angleStep = 360f / (coneRayCount - 1);
            
            for (int i = 1; i < coneRayCount; i++)
            {
                float angle = angleStep * (i - 1) * Mathf.Deg2Rad;
                float coneRad = coneAngle * Mathf.Deg2Rad;
                
                // Offset perpendicular to listener direction
                coneDirections[i] = new Vector3(
                    Mathf.Cos(angle) * Mathf.Sin(coneRad),
                    Mathf.Sin(angle) * Mathf.Sin(coneRad),
                    Mathf.Cos(coneRad)
                );
            }
        }
    }
    
    void PerformOcclusionScan()
    {
        if (listenerTransform == null)
            return;

        Vector3 origin = transform.position;
        Vector3 toListener = listenerTransform.position - origin;
        float distanceToListener = toListener.magnitude;
        Vector3 dirToListener = toListener / distanceToListener;

        int blockedCount = 0;
        int totalRays = coneRayCount;
        bool centerIsBlocked = false;
        float minDiffractionDist = float.MaxValue;

        // Check center ray first
        RaycastHit centerHit;
        if (Physics.Raycast(origin, dirToListener, out centerHit, distanceToListener, occlusionLayer))
        {
            centerIsBlocked = true;
            blockedCount++;
            
            if (drawDebugRays)
                Debug.DrawLine(origin, centerHit.point, occludedColor, 1f / scanRate);
        }
        else if (drawDebugRays)
        {
            Debug.DrawLine(origin, listenerTransform.position, clearColor, 1f / scanRate);
        }

        // Check surrounding cone rays
        for (int i = 1; i < coneRayCount; i++)
        {
            // Rotate cone direction to face listener
            Vector3 localDir = coneDirections[i];
            Quaternion rotation = Quaternion.LookRotation(dirToListener);
            Vector3 worldDir = rotation * localDir;

            Ray ray = new Ray(origin, worldDir);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, distanceToListener, occlusionLayer))
            {
                // Near-field corner logic: prevent false occlusion when standing next to walls
                float distFromHitToListener = distanceToListener - hit.distance;
                bool isNearField = (distFromHitToListener < nearFieldThreshold) && (!centerIsBlocked);

                if (!isNearField)
                {
                    blockedCount++;
                    minDiffractionDist = Mathf.Min(minDiffractionDist, distFromHitToListener);

                    if (drawDebugRays)
                        Debug.DrawLine(origin, hit.point, occludedColor, 1f / scanRate);
                }
                else if (drawDebugRays)
                {
                    Debug.DrawLine(origin, hit.point, diffractionColor, 1f / scanRate);
                }
            }
            else if (drawDebugRays)
            {
                Debug.DrawRay(origin, worldDir * distanceToListener, clearColor, 1f / scanRate);
            }
        }

        // Calculate occlusion
        float blockRatio = (float)blockedCount / totalRays;
        targetOcclusion = Mathf.Clamp01(blockRatio);

        // Calculate diffraction
        if (enableDiffraction && centerIsBlocked && minDiffractionDist < float.MaxValue)
        {
            targetDiffraction = diffractionCurve.Evaluate(minDiffractionDist);
        }
        else
        {
            targetDiffraction = 0f;
        }
    }
    
    void SmoothParameters()
    {
        currentOcclusion = Mathf.Lerp(currentOcclusion, targetOcclusion, Time.deltaTime * smoothingSpeed);
        currentDiffraction = Mathf.Lerp(currentDiffraction, targetDiffraction, Time.deltaTime * smoothingSpeed);

        UpdateWwiseParameters();
    }
    
    void UpdateWwiseParameters()
    {
        AkSoundEngine.SetRTPCValue(occlusionRTPCName, currentOcclusion, gameObject);
        
        if (enableDiffraction)
        {
            AkSoundEngine.SetRTPCValue(diffractionRTPCName, currentDiffraction, gameObject);
        }
    }
    
    void FindListener()
    {
        // Try to find AkAudioListener first (Wwise 2025)
        AkAudioListener akListener = FindObjectOfType<AkAudioListener>();
        if (akListener != null)
        {
            listenerTransform = akListener.transform;
            return;
        }

        // Fallback to Unity AudioListener
        AudioListener unityListener = FindObjectOfType<AudioListener>();
        if (unityListener != null)
        {
            listenerTransform = unityListener.transform;
        }
    }

    // Public API for tracking playing state
    public void OnEventPosted(uint playingID)
    {
        lastPlayingEventID = playingID;
        isPlayingCached = true;
    }

    public void OnEventStopped()
    {
        isPlayingCached = false;
    }

    public void ForceRescan()
    {
        PerformOcclusionScan();
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || listenerTransform == null)
            return;

        // Draw occlusion state
        Gizmos.color = Color.Lerp(clearColor, occludedColor, currentOcclusion);
        Gizmos.DrawWireSphere(transform.position, 0.3f);

        // Draw line to listener
        Gizmos.color = IsOccluded ? Color.red : Color.green;
        Gizmos.DrawLine(transform.position, listenerTransform.position);

        // Draw diffraction indicator
        if (enableDiffraction && currentDiffraction > 0.1f)
        {
            Gizmos.color = diffractionColor;
            Gizmos.DrawWireSphere(transform.position, 0.5f * currentDiffraction);
        }
    }

    void OnValidate()
    {
        if (coneDirections == null || coneDirections.Length != coneRayCount)
        {
            GenerateConeDirections();
        }
    }
}