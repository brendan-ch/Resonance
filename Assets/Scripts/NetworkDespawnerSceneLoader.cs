using PurrNet;

public class NetworkDespawnerSceneLoader : NetworkBehaviour
{
    public void LoadNetworkDespawnerSceneForEveryone()
    {
        networkManager.sceneModule.LoadSceneAsync("NetworkDespawnerScene");
    }
}
