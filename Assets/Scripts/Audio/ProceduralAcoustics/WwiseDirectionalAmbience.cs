using UnityEngine;

/// <summary>
/// Directional Ambience System - Battlefield-style approach
/// Places outdoor ambience emitters in open directions for binaural spatial blending
/// </summary>
public class WwiseDirectionalAmbience : MonoBehaviour
{
    [Header("Wwise Events")]
    [Tooltip("Outdoor ambience (played from directional emitters)")]
    public AK.Wwise.Event outdoorAmbienceEvent;
    
    [Tooltip("Indoor ambience (played at listener, non-directional)")]
    public AK.Wwise.Event indoorAmbienceEvent;

    [Header("Scanner Reference")]
    [Tooltip("WwiseSmartReverb component for enclosure data")]
    public WwiseSmartReverb scannerSource;

    [Header("Directional Detection")]
    [Tooltip("Number of directional rays (4 = cardinal, 8 = cardinal + diagonal)")]
    public int directionCount = 4;
    
    [Tooltip("Ray distance for openness detection")]
    public float rayDistance = 30f;
    
    [Tooltip("Environment layer mask")]
    public LayerMask environmentLayer;

    [Header("Emitter Settings")]
    [Tooltip("Distance to place outdoor emitters from listener")]
    public float emitterDistance = 15f;
    
    [Tooltip("Minimum openness to activate emitter (0-1)")]
    [Range(0f, 1f)]
    public float activationThreshold = 0.3f;

    [Header("Update Rate")]
    [Tooltip("Scans per second")]
    [Range(1f, 10f)]
    public float scanRate = 2f;

    [Header("Settings")]
    [Tooltip("Auto-start on scene load")]
    public bool autoStart = true;

    [Header("Debug")]
    public bool drawDebugRays = false;
    public bool debugLog = false;

    // Internal
    private GameObject[] outdoorEmitters;
    private uint[] outdoorPlayingIDs;
    private uint indoorPlayingID;
    private bool isPlaying;
    private float lastScanTime;
    private Vector3[] directions;
    private float globalEnclosure;

    void Start()
    {
        if (scannerSource == null)
        {
            scannerSource = FindAnyObjectByType<WwiseSmartReverb>();
            if (scannerSource == null)
            {
                Debug.LogError("[DirectionalAmbience] No WwiseSmartReverb found!");
                enabled = false;
                return;
            }
        }

        InitializeDirections();
        CreateEmitters();
        lastScanTime = -1f;

        if (autoStart)
        {
            StartAmbience();
        }
    }

    void Update()
    {
        if (!isPlaying) return;

        globalEnclosure = scannerSource != null ? scannerSource.EnclosureFactor : 0f;

        // Time-sliced scanning
        if (Time.time - lastScanTime >= 1f / scanRate)
        {
            UpdateDirectionalEmitters();
            lastScanTime = Time.time;
        }

        UpdateIndoorVolume();
    }

    void InitializeDirections()
    {
        directions = new Vector3[directionCount];
        float angleStep = 360f / directionCount;

        for (int i = 0; i < directionCount; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            directions[i] = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
        }
    }

    void CreateEmitters()
    {
        outdoorEmitters = new GameObject[directionCount];
        outdoorPlayingIDs = new uint[directionCount];

        for (int i = 0; i < directionCount; i++)
        {
            GameObject emitter = new GameObject($"OutdoorEmitter_{i}");
            emitter.transform.SetParent(transform);
            emitter.AddComponent<AkGameObj>();
            outdoorEmitters[i] = emitter;
            outdoorPlayingIDs[i] = AkUnitySoundEngine.AK_INVALID_PLAYING_ID;
        }
    }

    void UpdateDirectionalEmitters()
    {
        Vector3 listenerPos = transform.position;

        for (int i = 0; i < directionCount; i++)
        {
            Vector3 worldDir = directions[i];
            float openness = CalculateOpenness(listenerPos, worldDir);

            // Position emitter in this direction
            Vector3 emitterPos = listenerPos + worldDir * emitterDistance;
            outdoorEmitters[i].transform.position = emitterPos;

            // Activate/deactivate based on openness
            if (openness > activationThreshold)
            {
                // Start if not playing
                if (outdoorPlayingIDs[i] == AkUnitySoundEngine.AK_INVALID_PLAYING_ID)
                {
                    if (outdoorAmbienceEvent != null && outdoorAmbienceEvent.IsValid())
                    {
                        outdoorPlayingIDs[i] = outdoorAmbienceEvent.Post(outdoorEmitters[i]);
                    }
                }

                // Set volume based on openness
                float volume = Mathf.Lerp(-96f, 0f, openness);
                AkUnitySoundEngine.SetRTPCValue("DirectionalAmbienceVolume", volume, outdoorEmitters[i]);
            }
            else
            {
                // Stop if playing
                if (outdoorPlayingIDs[i] != AkUnitySoundEngine.AK_INVALID_PLAYING_ID)
                {
                    AkUnitySoundEngine.ExecuteActionOnPlayingID(
                        AkActionOnEventType.AkActionOnEventType_Stop,
                        outdoorPlayingIDs[i],
                        500,
                        AkCurveInterpolation.AkCurveInterpolation_Linear
                    );
                    outdoorPlayingIDs[i] = AkUnitySoundEngine.AK_INVALID_PLAYING_ID;
                }
            }

            if (debugLog)
            {
                Debug.Log($"[DirectionalAmbience] Dir {i}: Openness {openness:F2}");
            }
        }
    }

    float CalculateOpenness(Vector3 origin, Vector3 direction)
    {
        RaycastHit hit;
        bool didHit = Physics.Raycast(origin, direction, out hit, rayDistance, environmentLayer);

        if (drawDebugRays)
        {
            Color rayColor = didHit ? Color.red : Color.green;
            Vector3 endPoint = didHit ? hit.point : origin + direction * rayDistance;
            Debug.DrawLine(origin, endPoint, rayColor, 1f / scanRate);
        }

        if (!didHit)
        {
            return 1f; // Fully open
        }

        // Openness based on distance (far hit = more open)
        float normalizedDist = hit.distance / rayDistance;
        return normalizedDist;
    }

    void UpdateIndoorVolume()
    {
        if (indoorPlayingID == AkUnitySoundEngine.AK_INVALID_PLAYING_ID)
            return;

        // Indoor volume based on global enclosure
        float indoorVolume = Mathf.Lerp(-96f, 0f, globalEnclosure);
        AkUnitySoundEngine.SetRTPCValue("IndoorAmbienceVolume", indoorVolume, gameObject);
    }

    public void StartAmbience()
    {
        if (isPlaying) return;

        // Start indoor ambience at listener (non-directional)
        if (indoorAmbienceEvent != null && indoorAmbienceEvent.IsValid())
        {
            indoorPlayingID = indoorAmbienceEvent.Post(gameObject);
        }

        isPlaying = true;
    }

    public void StopAmbience()
    {
        if (!isPlaying) return;

        // Stop all outdoor emitters
        for (int i = 0; i < outdoorPlayingIDs.Length; i++)
        {
            if (outdoorPlayingIDs[i] != AkUnitySoundEngine.AK_INVALID_PLAYING_ID)
            {
                AkUnitySoundEngine.ExecuteActionOnPlayingID(
                    AkActionOnEventType.AkActionOnEventType_Stop,
                    outdoorPlayingIDs[i],
                    500,
                    AkCurveInterpolation.AkCurveInterpolation_Linear
                );
                outdoorPlayingIDs[i] = AkUnitySoundEngine.AK_INVALID_PLAYING_ID;
            }
        }

        // Stop indoor
        if (indoorPlayingID != AkUnitySoundEngine.AK_INVALID_PLAYING_ID)
        {
            AkUnitySoundEngine.ExecuteActionOnPlayingID(
                AkActionOnEventType.AkActionOnEventType_Stop,
                indoorPlayingID,
                500,
                AkCurveInterpolation.AkCurveInterpolation_Linear
            );
            indoorPlayingID = AkUnitySoundEngine.AK_INVALID_PLAYING_ID;
        }

        isPlaying = false;
    }

    void OnDisable()
    {
        if (isPlaying)
        {
            StopAmbience();
        }
    }

    void OnDestroy()
    {
        // Clean up emitters
        if (outdoorEmitters != null)
        {
            foreach (var emitter in outdoorEmitters)
            {
                if (emitter != null)
                {
                    Destroy(emitter);
                }
            }
        }
    }
}