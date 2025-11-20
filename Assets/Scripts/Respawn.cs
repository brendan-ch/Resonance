using UnityEngine;
using System.Collections.Generic;

namespace Resonance.Player
{
    public class Respawn : MonoBehaviour
    {
        public static Respawn Instance { get; private set; }
        
        #region Inspector Fields
        [Header("Spawn Points")]
        [SerializeField] private List<Transform> spawnPoints = new List<Transform>();

        [Header("Respawn Settings")]
        [SerializeField] private float respawnDelay = 3f;
        #endregion
        
        #region Properties
        public float RespawnDelay => respawnDelay;
        #endregion
        
        #region Startup
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            ValidateSpawnPoints();
        }
        #endregion

        #region Spawn Point Management

        private void ValidateSpawnPoints()
        {
            if (spawnPoints.Count == 0)
            {
                Debug.LogError("[Respawn] No spawn points configured! Please add spawn points in the inspector.");
            }
        }
        
        public Transform GetSpawnPoint()
        {
            if (spawnPoints.Count == 0)
            {
                Debug.LogError("[Respawn] No spawn points available!");
                return null;
            }

            int randomIndex = Random.Range(0, spawnPoints.Count);
            return spawnPoints[randomIndex];
        }

        public void AddSpawnPoint(Transform spawnPoint)
        {
            if (!spawnPoints.Contains(spawnPoint))
            {
                spawnPoints.Add(spawnPoint);
            }
        }

        public void RemoveSpawnPoint(Transform spawnPoint)
        {
            spawnPoints.Remove(spawnPoint);
        }
        #endregion
    }
}