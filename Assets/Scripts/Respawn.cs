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
        [SerializeField] private Transform defaultSpawnPoint;

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
            if (spawnPoints.Count == 0 && defaultSpawnPoint == null)
            {
                Debug.LogWarning("[Respawn] No spawn points configured! Creating default at origin.");
                GameObject defaultSpawn = new GameObject("DefaultSpawnPoint");
                defaultSpawn.transform.position = Vector3.zero;
                defaultSpawnPoint = defaultSpawn.transform;
            }
        }
        
        public Transform GetSpawnPoint()
        {
            if (spawnPoints.Count > 0)
            {
                // for now this is returning the first spawn point. should be reconfigured later
                return spawnPoints[0];
            }
            
            return defaultSpawnPoint;
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

        public void SetDefaultSpawnPoint(Transform spawnPoint)
        {
            defaultSpawnPoint = spawnPoint;
        }
        #endregion
    }
}
