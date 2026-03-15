using UnityEngine;

[CreateAssetMenu(fileName = "AppConfig", menuName = "Resonance/Build Configuration")]
public class AppConfig : ScriptableObject
{
    public bool enableSteamLobby;
    public bool useProductionRelay;
}
