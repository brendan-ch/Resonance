using UnityEngine;
using Resonance.PlayerController;

namespace Resonance.Audio
{
    public class FootstepController : MonoBehaviour
    {
        [Header("Wwise Events")] 
        public AK.Wwise.Event footstepEvent;
        
        [Header("Landing event (uses SurfaceType switch")]
        public AK.Wwise.Event landingEvent;

        [Header("Surface Detection")] 
        [Tooltip("Raycast distance for ground detection")]
        public float raycastDistance = 0.5f;

        [Tooltip("Layer mask for ground")]
        public LayerMask groundLayers;

        [Header("Surface Switches")] 
        public AK.Wwise.Switch concreteSurface;
        public AK.Wwise.Switch metalSurface;
        public AK.Wwise.Switch woodSurface;
        public AK.Wwise.Switch gravelSurface;
        
        private CharacterController characterController;
        private PlayerState playerState;
        private string currentSurface = "Concrete"; // concrete is default
        private bool wasInAir = false;

        void Awake()
        {
            characterController = GetComponent<CharacterController>();
            playerState = GetComponent<PlayerState>();
        }

        void Update()
        {
            
        }

        public void PlayFootstep()
        {
            
        }

        public void PlayLanding()
        {
            
        }

        void DetectSurface()
        {
            
        }

        string GetSurfaceFromTag(string tag)
        {
            if (tag == "Concrete") return "Concrete";
            if (tag == "Metal") return "Metal";
            if (tag == "Wood") return "Wood";
            if (tag == "Gravel") return "Gravel";
            
            // default to concrete if tag not recognized
            return "Concrete";
        }

        void SetSurfaceSwitch()
        {
            switch (currentSurface)
            {
                case "Concrete":
                    concreteSurface?.SetValue(gameObject);
                    break;
                case "Metal":
                    metalSurface?.SetValue(gameObject);
                    break;
                case "Wood":
                    woodSurface?.SetValue(gameObject);
                    break;
                case "Gravel":
                    gravelSurface?.SetValue(gameObject);
                    break;
            }
        }

        void OnDrawGizmosSelected()
        {
            
        }
    }
}

