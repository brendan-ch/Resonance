using UnityEngine;

/// <summary>
/// Wwise Ambience Manager - Procedural Ambience Blending
/// Automatically blends outdoor/indoor ambience based on WwiseSmartReverb data.
/// No manual trigger zones!
/// </summary>
[RequireComponent(typeof(AkGameObj))]
public class WwiseAmbienceManager : MonoBehaviour
{
    [Header("Wwise Event")]
    [Tooltip("Ambience event (should have indoor/outdoor blend parameter)")]
    public AK.Wwise.Event ambienceEvent;

    [Header("Scanner Reference")]
    [Tooltip("WwiseSmartReverb component (usually on player/listener)")]
    public WwiseSmartReverb scannerSource;

    [Header("Mix Parameter")]
    [Tooltip("RTPC name for ambience mix (0 = outdoor, 100 = indoor)")]
    public string mixParameterName = "AmbienceMix";

    [Header("Settings")]
    
    [Tooltip("Auto-start on scene load")]
    public bool autoStart = true;

    [Header("Debug")]
    [Tooltip("Enable debug logging")]
    public bool debugLog = false;

    // Internal
    private uint playingID;
    private bool isPlaying;

    void Start()
    {
        if (scannerSource == null)
        {
            scannerSource = FindAnyObjectByType<WwiseSmartReverb>();
            if (scannerSource == null)
            {
                Debug.LogError("[WwiseAmbienceManager] No WwiseSmartReverb found in scene!");
                enabled = false;
                return;
            }
        }

        if (autoStart)
        {
            StartAmbience();
        }
    }

    void Update()
    {
        if (debugLog && !isPlaying)
        {
            Debug.LogWarning("[Ambience] Not playing! Check if event stopped.");
        }

        if (!isPlaying || scannerSource == null)
            return;

        // Get enclosure from scanner (0-1) and scale to 0-100
        float enclosure = scannerSource.EnclosureFactor;
        float mix = enclosure * 100f;

        // Update Wwise immediately (no smoothing)
        AkUnitySoundEngine.SetRTPCValue(mixParameterName, mix, gameObject);

        if (debugLog)
        {
            Debug.Log($"[Ambience] Enclosure: {enclosure:F2} | AmbienceMix RTPC: {mix:F1}");
        }
    }

    public void StartAmbience()
    {
        if (isPlaying)
            return;

        if (ambienceEvent != null && ambienceEvent.IsValid())
        {
            playingID = ambienceEvent.Post(gameObject);
            isPlaying = true;
        }
    }

    public void StopAmbience(float fadeTime = 1f)
    {
        if (!isPlaying)
            return;

        if (playingID != AkUnitySoundEngine.AK_INVALID_PLAYING_ID)
        {
            AkUnitySoundEngine.ExecuteActionOnPlayingID(
                AkActionOnEventType.AkActionOnEventType_Stop,
                playingID,
                (int)(fadeTime * 1000),
                AkCurveInterpolation.AkCurveInterpolation_Linear
            );
        }

        isPlaying = false;
    }

    void OnDisable()
    {
        if (isPlaying)
        {
            StopAmbience(0.5f);
        }
    }
}