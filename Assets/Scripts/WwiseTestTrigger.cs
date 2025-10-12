using UnityEngine;
using UnityEngine.InputSystem;

public class WwiseTestTrigger : MonoBehaviour
{
    private Keyboard keyboard;

    private void Awake()
    {
        keyboard = Keyboard.current;
    }

    private void Update()
    {
        if (keyboard == null)
            return;

        // Press 1, 2, 3 to test each bus
        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            Debug.Log("Testing FOLEY bus...");
            AkUnitySoundEngine.PostEvent("Play_FOLEY_Test", gameObject);
        }
        
        if (keyboard.digit2Key.wasPressedThisFrame)
        {
            Debug.Log("Testing SFX bus...");
            AkUnitySoundEngine.PostEvent("Play_SFX_Test", gameObject);
        }
        
        if (keyboard.digit3Key.wasPressedThisFrame)
        {
            Debug.Log("Testing ENV bus...");
            AkUnitySoundEngine.PostEvent("Play_ENV_Test", gameObject);
        }
    }
}