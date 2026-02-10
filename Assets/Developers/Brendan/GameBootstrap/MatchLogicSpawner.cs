using PurrNet;
using UnityEngine;
using UnityEngine.Events;

public class MatchLogicSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject matchLogicPrefab;

    public UnityEvent OnMatchLogicSpawned = new();

    protected override void OnSpawned()
    {
        base.OnSpawned();
    }

    public void SpawnMatchLogic()
    {
        // must be spawned on all clients to access RPC
        GameObject instance = Instantiate(matchLogicPrefab);
        DontDestroyOnLoad(instance);

        OnMatchLogicSpawned.Invoke();
    }
}
