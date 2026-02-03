using UnityEngine;

/// <summary>
/// Example: Procedural Ambience Manager for Wwise
/// Automatically blends indoor/outdoor ambience based on WwiseSmartReverb data
/// No manual trigger zones required!
/// </summary>
[RequireComponent(typeof(AkGameObj))]
[AddComponentMenu("Wwise/Smart Acoustics/Wwise Ambience Manager")]
public class WwiseAmbienceManager : MonoBehaviour
{
    [Header("Ambience Configuration")]
    [Tooltip("Wwise Event for ambience (should have indoor/outdoor states or RTPCs)")]
    public AK.Wwise.Event ambienceEvent;
    
    [Tooltip("Name of the RTPC that blends indoor/outdoor (0 = outdoor, 100 = indoor)")]
    public string ambMixRTPCName = "AmbienceMix";

    [Header("Reverb Scanner Reference")]
    [Tooltip("Reference to the WwiseSmartReverb component (usually on player/listener)")]
    public WwiseSmartReverb reverbScanner;

    [Header("Threshold Calibration")]
    [Tooltip("Enclosure value considered 'fully outdoor'")]
    [Range(0f, 1f)]
    public float outdoorThreshold = 0.1f;
    
    [Tooltip("Enclosure value considered 'fully indoor'")]
    [Range(0f, 1f)]
    public float indoorThreshold = 0.7f;
    
    [Tooltip("Curve for mapping enclosure to ambience mix")]
    public AnimationCurve mixCurve = AnimationCurve.Linear(0f, 0f, 1f, 100f);

    [Header("Transition Settings")]
    [Tooltip("How quickly ambience adapts to room changes")]
    [Range(0.1f, 5f)]
    public float transitionSpeed = 1f;

    [Header("Auto-Start")]
    [Tooltip("Automatically start ambience on scene load")]
    public bool autoStart = true;

    // Internal State
    private AkGameObj akGameObj;
    private uint playingAmbienceID;
    private bool isPlaying;
    private float currentMix;
    private float targetMix;

    void Awake()
    {
        akGameObj = GetComponent<AkGameObj>();
        
        if (reverbScanner == null)
        {
            reverbScanner = FindObjectOfType<WwiseSmartReverb>();
            if (reverbScanner == null)
            {
                Debug.LogError("[WwiseAmbienceManager] No WwiseSmartReverb found in scene! This component requires it.");
                enabled = false;
                return;
            }
        }
    }

    void Start()
    {
        if (autoStart)
        {
            StartAmbience();
        }
    }

    void Update()
    {
        if (!isPlaying || reverbScanner == null)
            return;

        // Get current enclosure from reverb scanner
        float enclosure = reverbScanner.EnclosureFactor;
        
        // Map enclosure to mix value (0-100)
        float normalizedEnclosure = Mathf.InverseLerp(outdoorThreshold, indoorThreshold, enclosure);
        targetMix = mixCurve.Evaluate(normalizedEnclosure);

        // Smooth transition
        currentMix = Mathf.Lerp(currentMix, targetMix, Time.deltaTime * transitionSpeed);

        // Update Wwise RTPC
        AkSoundEngine.SetRTPCValue(ambMixRTPCName, currentMix, gameObject);
    }

    public void StartAmbience()
    {
        if (isPlaying)
        {
            Debug.LogWarning("[WwiseAmbienceManager] Ambience is already playing.");
            return;
        }

        if (ambienceEvent != null && ambienceEvent.IsValid())
        {
            playingAmbienceID = ambienceEvent.Post(gameObject);
            isPlaying = true;
            Debug.Log($"[WwiseAmbienceManager] Started ambience (ID: {playingAmbienceID})");
        }
        else
        {
            Debug.LogError("[WwiseAmbienceManager] Ambience Event is not valid!");
        }
    }

    public void StopAmbience(float fadeTime = 1f)
    {
        if (!isPlaying)
            return;

        if (playingAmbienceID != AkSoundEngine.AK_INVALID_PLAYING_ID)
        {
            AkSoundEngine.ExecuteActionOnPlayingID(
                AkActionOnEventType.AkActionOnEventType_Stop,
                playingAmbienceID,
                (int)(fadeTime * 1000), // Convert to milliseconds
                AkCurveInterpolation.AkCurveInterpolation_Linear
            );
        }

        isPlaying = false;
        Debug.Log("[WwiseAmbienceManager] Stopped ambience");
    }

    public void PauseAmbience()
    {
        if (!isPlaying)
            return;

        if (playingAmbienceID != AkSoundEngine.AK_INVALID_PLAYING_ID)
        {
            AkSoundEngine.ExecuteActionOnPlayingID(
                AkActionOnEventType.AkActionOnEventType_Pause,
                playingAmbienceID
            );
        }

        Debug.Log("[WwiseAmbienceManager] Paused ambience");
    }

    public void ResumeAmbience()
    {
        if (!isPlaying)
            return;

        if (playingAmbienceID != AkSoundEngine.AK_INVALID_PLAYING_ID)
        {
            AkSoundEngine.ExecuteActionOnPlayingID(
                AkActionOnEventType.AkActionOnEventType_Resume,
                playingAmbienceID
            );
        }

        Debug.Log("[WwiseAmbienceManager] Resumed ambience");
    }

    void OnDisable()
    {
        if (isPlaying)
        {
            StopAmbience(0.5f);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || reverbScanner == null)
            return;

        // Visualize current indoor/outdoor state
        Gizmos.color = Color.Lerp(
            new Color(0.5f, 1f, 0.5f, 0.5f), // Outdoor (green)
            new Color(1f, 0.5f, 0.2f, 0.5f), // Indoor (orange)
            currentMix / 100f
        );
        
        Gizmos.DrawWireSphere(transform.position, 1f);
    }
}