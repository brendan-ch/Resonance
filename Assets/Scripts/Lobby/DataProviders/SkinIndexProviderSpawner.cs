using UnityEngine;

namespace Resonance.LobbySystem
{
    public class SkinIndexProviderSpawner : MonoBehaviour
    {
        public void Start()
        {
            var skinIndexProvider = FindFirstObjectByType<SkinIndexProvider>();
            if (!skinIndexProvider)
            {
                var newObject = new GameObject("SkinIndexProvider");
                newObject.AddComponent<SkinIndexProvider>();
                return;
            }
        }
    }
}
