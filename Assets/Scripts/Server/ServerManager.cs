using UnityEngine;

public partial class ServerManager : MonoBehaviour
{
#region PrivateMethod
    private void Awake()
    {
        if (instance != null)
            Destroy(instance);
        instance = this;
        DontDestroyOnLoad(this.gameObject);

        GetServerInfo();
        GameManager.Login += ConnectOutGame;
        GameManager.MatchMaking += MatchMaking;
        GameManager.Ready += ConnectInGame;
    }
    private void FixedUpdate()
    {
        if (isConnectInGame && Stream.DataAvailable)
            WorldManager.instance.Polling();
    }
    private void GetServerInfo()
    {
        // inGameServerIP = "localhost"; // 로컬 테스트 용
        inGameServerIP = SecretLoader.ingameServer.ip;
        inGameServerPort = SecretLoader.ingameServer.port;
        // outGameServerIP = "localhost"; // 로컬 테스트 용
        outGameServerIP = SecretLoader.outgameServer.ip;
        outGameServerPort = SecretLoader.outgameServer.port;
    }
#endregion
}