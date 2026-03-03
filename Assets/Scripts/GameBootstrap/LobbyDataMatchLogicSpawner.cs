using PurrNet;
using Resonance.LobbySystem;
using Resonance.Match;
using UnityEngine;
using UnityEngine.Events;

namespace Resonance.GameBootstrap
{
    public class LobbyDataMatchLogicSpawner : NetworkBehaviour
    {
        [SerializeField] private GameObject matchLogicPrefab;

        private LobbyDataHolder lobbyDataHolder;
        public UnityEvent OnMatchLogicSpawned = new();

        private bool matchLogicSpawned = false;

        protected override void OnSpawned()
        {
            base.OnSpawned();

            lobbyDataHolder = FindFirstObjectByType<LobbyDataHolder>();
            if (!lobbyDataHolder)
            {
                Debug.LogError($"{GetType()} Unable to find {nameof(LobbyDataHolder)} component; match logic spawning will not work");
            }
        }

        public void SpawnMatchLogic()
        {
            if (matchLogicSpawned)
            {
                Debug.Log($"{GetType()} Match logic already spawned for object {id}");
                return;
            }

            // spawn the object, disable it, set the game mode, then make it active,
            // running the Awake method
            var gameMode = lobbyDataHolder.CurrentLobby.GameMode;
            Debug.Log($"{GetType()} Spawning match logic for object {id} and gamemode {gameMode}");

            // must be spawned on all clients to access RPC
            var matchLogicGameObject = Instantiate(matchLogicPrefab);
            matchLogicGameObject.SetActive(false);

            var matchLogicAdapter = matchLogicPrefab.GetComponent<MatchLogicNetworkAdapter>();
            matchLogicAdapter.gameModeToSpawn = gameMode;

            matchLogicGameObject.SetActive(true);

            OnMatchLogicSpawned.Invoke();
            matchLogicSpawned = true;
        }
    }
}
