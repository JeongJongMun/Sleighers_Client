using System.Collections.Generic;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.Events;
using Protocol;
using Reader;
using System.Collections;
using System.Threading.Tasks;
/* WorldManager.cs
 * - 인게임 내의 모든 것을 관리
 * - 인게임 내에서 프로토콜 수신 및 처리
 * - 인게임 내에서 플레이어 생성 및 삭제
 */
public class WorldManager : MonoBehaviour
{
    class SessionInfo
    {
        public int totalPlayerCount;
    }
#region PrivateVariables
    private RankManager lapManager;
    private GameObject playerPrefab;
    private SessionInfo sessionInfo;
    private Dictionary<int, Player> players;
    [SerializeField] private int myPlayerId = -1;
    public int MyPlayerId
    {
        get { return myPlayerId; }
        set { myPlayerId = value; }
    }
    private bool isGameStart = false;
    private bool isRaceFinish = false;
    public bool IsRaceFinish { get { return isRaceFinish; } }
    private Transform[] startingPoints;

    private Queue<byte[]> messageQueue = new Queue<byte[]>();
#endregion

#region PublicVariables
    static public WorldManager instance;
    // private Transform minimapTarget;
    public GameObject playerPool;
    public Transform startingPointHolder;
    public MiniMapController miniMapController;
    // 레이스가 종료되면 호출되는 액션
    public UnityAction OnRaceFinished { get; set; }

#endregion

#region PrivateMethod
    // 애플리케이션 종료 시
    private void OnApplicationQuit()
    {
        Debug.Log("OnApplicationQuit");
        ServerManager.Instance().IsConnect = false;
        if (ServerManager.Instance().Stream != null)
            ServerManager.Instance().CloseStream();
        if (ServerManager.Instance().Client != null)
            ServerManager.Instance().CloseTcpClient();
    }
    private void Awake()
    {
        instance = this;
        lapManager = GetComponent<RankManager>();
        players = new Dictionary<int, Player>();
        lapManager.OnLapComplete += OnLapComplete;
        OnRaceFinished += FinishRace;
    }
    private void Start()
    {
        LogManager.instance.Log("[WorldManager] Start()");
        InitializeGame();
        StartCoroutine(OnReceive());
    }

    // 인게임 초기화
    private void InitializeGame()
    {

        if (!playerPool)
        {
            Debug.Log("[World Manager] Player Pool이 존재하지 않습니다.");
            return;
        }
        Debug.Log("[World Manager] 게임 초기화 진행");
        isRaceFinish = false;
        sessionInfo = new SessionInfo();
        playerPrefab = Resources.Load("Prefabs/Player") as GameObject;
        startingPoints = new Transform[startingPointHolder.childCount];
        for (int i = 0; i < startingPointHolder.childCount; i++)
            startingPoints[i] = startingPointHolder.GetChild(i);
    }
    // 플레이어가 한 바퀴를 완주했을 때 호출되는 콜백
    private void OnLapComplete(Player _player, RankInfo _lapInfo)
    {
        // 플레이어가 레이스를 완료했나 확인
        if (_player.IsMe && _lapInfo.lap == lapManager.Laps)
        {
            // 아직 완료하지 못했다면 레이스를 완료
            if (!isRaceFinish)
            {
                OnRaceFinished?.Invoke(); 
            }
        }
    }

    // 레이스를 완료하고 아웃트로 타임라인을 재생
    private void FinishRace()
    {
        isRaceFinish = true;
        OnSend(Protocol.Type.PlayerGoal);
    }

#region Receive 프로토콜 처리
    // 서버로부터 받은 데이터 처리 핸들러
    private IEnumerator OnReceive()
    {
        while (true)
        {
            if (messageQueue.Count > 0)
            {
                byte[] data;
                lock (messageQueue)
                    data = messageQueue.Dequeue();

                Message msg = DataParser.ReadJsonData<Message>(data);
                if (msg == null)
                {
                    Debug.LogWarning("[OnReceive] 메세지가 비어있습니다.");
                    continue;
                }
                if (msg.from == MyPlayerId)
                {
                    Debug.LogWarning("[OnReceive] 내 플레이어의 메세지입니다.");
                    continue;
                }
                if (msg.type != Protocol.Type.Key)
                    LogManager.instance.Log("[OnReceive] 메세지 타입 :" + msg.type.ToString());
                Debug.LogFormat("[OnReceive] 메세지 타입 : {0}", msg.type);
                switch (msg.type)
                {
                    case Protocol.Type.LoadGameScene:
                        LoadGameSceneMessage loadMessage = DataParser.ReadJsonData<LoadGameSceneMessage>(data);
                        ReceiveLoadGameSceneEvent(loadMessage);
                        break;

                    case Protocol.Type.GameStartCountDown:
                        GameCountDownMessage startCountMessage = DataParser.ReadJsonData<GameCountDownMessage>(data);
                        ReceiveGameStartCountDownEvent(startCountMessage);
                        break;

                    case Protocol.Type.GameEndCountDown:
                        GameCountDownMessage endCountMessage = DataParser.ReadJsonData<GameCountDownMessage>(data);
                        ReceiveGameEndCountDownEvent(endCountMessage);
                        break;

                    case Protocol.Type.GameStart:
                        ReceiveGameStartEvent();
                        break;

                    case Protocol.Type.GameEnd:
                        KeyMessage endMessage = DataParser.ReadJsonData<KeyMessage>(data);
                        ReceiveGameEndEvent(endMessage);
                        break;

                    case Protocol.Type.Key:
                        KeyMessage keyMessage = DataParser.ReadJsonData<KeyMessage>(data);
                        ReceiveKeyEvent(keyMessage);
                        break;

                    case Protocol.Type.PlayerReconnect:
                        ReceivePlayerReconnectEvent(msg);
                        break;

                    case Protocol.Type.PlayerDisconnect:
                        ReceivePlayerDisconnectEvent(msg);
                        break;

                    default:
                        Debug.LogWarning("[OnReceive] 알 수 없는 메시지 타입: " + msg.type);
                        break;
                }
            }
            yield return null;
        }
    }


    // 키 입력 이벤트 처리
    private void ReceiveKeyEvent(KeyMessage keyMessage)
    {
        int id = keyMessage.from;

        Vector3 position = keyMessage.position;
        Vector3 velocity = keyMessage.velocity;
        Vector3 acceleration = keyMessage.acceleration;
        long timeStamp = keyMessage.timeStamp;

        players[id].SetServerData(position, velocity, acceleration, timeStamp);
        players[id].SetMoveVector(acceleration);
    }
    // 다른 플레이어 접속 이벤트 처리
    private void ReceivePlayerReconnectEvent(Message msg)
    {
        int newId = msg.from;
        Transform sp = startingPoints[sessionInfo.totalPlayerCount];
        GameObject newInstance = Instantiate(playerPrefab, sp.position, sp.rotation, playerPool.transform);
        newInstance.GetComponent<Player>().Initialize(false, newId, "Player" + newId);
        players.Add(newId, newInstance.GetComponent<Player>());
        sessionInfo.totalPlayerCount++;
    }
    // 게임 씬 로드 이벤트 처리
    private void ReceiveLoadGameSceneEvent(LoadGameSceneMessage msg)
    {
        myPlayerId = msg.from;
        int totalPlayerCount = msg.count;
        sessionInfo.totalPlayerCount = totalPlayerCount + 1;
        List<int> userList = msg.list;

        Transform sp = startingPoints[totalPlayerCount].transform;
        GameObject myPlayer = Instantiate(playerPrefab, sp.position, sp.rotation, playerPool.transform);
        myPlayer.GetComponent<Player>().Initialize(true, myPlayerId, "Player" + myPlayerId);
        Transform miniMapTarget = myPlayer.transform.Find("Sled");
        miniMapController.SetTarget(miniMapTarget);
        players.Add(myPlayerId, myPlayer.GetComponent<Player>());
        Debug.LogFormat("[WorldManager] 내 플레이어 생성 완료 : {0}", myPlayerId);

        for (int i = 0; i < totalPlayerCount; i++)
        {
            int otherPlayerId = userList[i];
            Transform _sp = startingPoints[i].transform;
            GameObject otherPlayer = Instantiate(playerPrefab, _sp.position, _sp.rotation, playerPool.transform);
            otherPlayer.GetComponent<Player>().Initialize(false, otherPlayerId, "Player" + otherPlayerId);
            players.Add(otherPlayerId, otherPlayer.GetComponent<Player>());
        }
    }
    // 게임 시작 카운트 다운 이벤트 처리
    private void ReceiveGameStartCountDownEvent(GameCountDownMessage msg)
    {
        int count = msg.count;
        InGameUI.instance.SetCountDown(count);
    }
    // 게임 시작 이벤트 처리
    private void ReceiveGameStartEvent()
    {
        GameManager.Instance().ChangeState(GameManager.GameState.InGame);
    }
    // 게임 종료 카운트 다운 이벤트 처리
    private void ReceiveGameEndCountDownEvent(GameCountDownMessage msg)
    {
        int count = msg.count;
        Debug.LogFormat("[OnReceive] SendGameEndCountDownEvent : {0}", count);
        InGameUI.instance.SetGameEndCountDown(count);        
    }
    // 게임 종료 이벤트 처리
    private void ReceiveGameEndEvent(KeyMessage msg)
    {
        int userId = msg.from;
        Debug.LogFormat("플레이어 {0}가 승리했습니다.", userId);  
        GameManager.Instance().ChangeState(GameManager.GameState.End);
    }
    // 다른 플레이어 접속 끊김 이벤트 처리
    private void ReceivePlayerDisconnectEvent(Message msg)
    {
        int userId = msg.from;
        Destroy(players[userId].gameObject);
        players.Remove(userId);
        sessionInfo.totalPlayerCount--;
    }
#endregion

#region Send 프로토콜 처리
    // 게임 시작 이벤트를 서버에 알림
    private async Task SendGameStartEvent()
    {
        if (isGameStart)
            return;
        isGameStart = true;
        Message msg = new Message(Protocol.Type.GameStart, myPlayerId);
        await ServerManager.Instance().SendDataToInGame(msg);
    }
    // 내 플레이어가 골인했음을 서버에 알림
    private async Task SendPlayerGoalEvent()
    {
        Message msg = new Message(Protocol.Type.PlayerGoal, myPlayerId);
        await ServerManager.Instance().SendDataToInGame(msg);
    }
    // 임시 서버 리셋
    private async Task SendResetServerEvent()
    {
        Message msg = new Message(Protocol.Type.ResetServer, myPlayerId);
        await ServerManager.Instance().SendDataToInGame(msg);
    }
#endregion


#endregion

#region PublicMethod
    // 서버로부터 데이터를 받아오는 함수
    public void Polling()
    {
        NetworkStream stream = ServerManager.Instance().Stream;
        byte[] size = new byte[4];
        stream.Read(size, 0, size.Length);

        ByteReader br = new ByteReader(size);
        int jsonSize = br.ReadInt();

        byte[] data = new byte[jsonSize];
        int receiveSize = stream.Read(data, 0, data.Length);

        if (receiveSize == 0)
        {
            Debug.LogWarning("[Polling] 빈 데이터가 수신되었습니다.");
            return;
        }

        if (players == null)
        {
            Debug.LogWarning("[Polling] 플레이어 리스트가 존재하지 않습니다.");
            return;
        }
        if (data != null)
        {
            lock (messageQueue)
                messageQueue.Enqueue(data);
        }
    }
    // 서버로 보내는 데이터 처리 핸들러
    public async void OnSend(Protocol.Type _type)
    {
        Debug.LogFormat("[OnSend] 메세지 타입 : {0}", _type);
        LogManager.instance.Log("[OnSend] 메세지 타입 : " + _type.ToString());
        switch (_type)
        {
            case Protocol.Type.PlayerReconnect:
                break;

            case Protocol.Type.LoadGameScene:
                break;

            case Protocol.Type.GameStart:
                await SendGameStartEvent();
                break;

            case Protocol.Type.PlayerGoal:
                await SendPlayerGoalEvent();
                break;

            case Protocol.Type.ResetServer:
                await SendResetServerEvent();
                break;

            default:
                Debug.LogWarning("[OnSend] 알 수 없는 프로토콜 : "+ _type);
                break;
        }
    }

    public Player GetMyPlayer()
    {
        return players[myPlayerId];
    }
    public GameObject GetMySled()
    {
        return players[myPlayerId].gameObject.transform.Find("Sled").gameObject;
    }
    public Vector3 GetMyPlayerPosition()
    {
        return players[myPlayerId].GetPosition();
    }
    public Vector3 GetMyPlayerVelocity()
    {
        return players[myPlayerId].GetVelocity();
    }
    public Player GetPlayerFromId(int _id)
    {
        return players[_id];
    }
#endregion
}