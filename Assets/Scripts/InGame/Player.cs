using UnityEngine;
using TMPro;
using Cinemachine;

/* Player.cs
 * - 플레이어의 이동, 회전, 속도 조절
 * - 플레이어 스크립트는 여러 개 생성되기에 여기서 입력 처리를 하지 않는다.
 */
public class Player : MonoBehaviour
{
#region PrivateVariables
    private float currentSteerAngle;
    //private bool isDrifting;
    private float motorForce = 2000f;
    private float brakeForce = 5000f;
    private float maxSteerAngle = 20f;

    public WheelCollider frontLeftWheelCollider;
    public WheelCollider frontRightWheelCollider;
    public WheelCollider backLeftWheelCollider;
    public WheelCollider backRightWheelCollider;

    private int playerId = 0;
    private bool isMe = false;
    [SerializeField] private bool isBraking = false;
    public bool IsBraking
    {
        get { return isBraking; }
        set { isBraking = value; }
    }
    private string nickName = string.Empty;
    private GameObject playerModelObject;
#endregion

#region PublicVariables
    [field: SerializeField] public Vector3 moveVector { get; private set; }
    [field: SerializeField] public bool isMove { get; private set; }
    public GameObject nameObject;
#endregion


#region PrivateMethod
    private void Awake()
    {
        nameObject = Resources.Load("Prefabs/PlayerName") as GameObject;
    }
    private void Start()
    {
        // 서버 인스턴스가 없으면 인게임 테스트용으로 초기화
        if (ServerManager.Instance() == null)
            Initialize(true, 0, "TestPlayer");
    }
    private void Update()
    {
        if (ServerManager.Instance() == null)
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");
            Vector3 tmp = new Vector3(h, 0, v);
            tmp = Vector3.Normalize(tmp);
            SetMoveVector(tmp);
        }
    }
    private Vector3 GetNameUIPos()
    {
        return this.transform.position + (Vector3.up * 2.0f);
    }

    private void FixedUpdate()
    {
        if (isMove)
            HandleMotor();
        
        if (IsBraking) // Space 누르고 있을 때
            ApplyBraking();
        else
            ApplyRestart();

        CheckRotate();
        HandleSteering();
    }

    private void HandleMotor() // 엔진 속도 조절
    {
        //추가 사항 : max 속도 제한, AddForce로 속도 조절
        frontLeftWheelCollider.motorTorque = moveVector.z * motorForce;
        frontRightWheelCollider.motorTorque = moveVector.z * motorForce;
        isMove = false;
    }

    private void ApplyBraking() // 브레이크
    {
        IsBraking = false;
        frontLeftWheelCollider.brakeTorque = brakeForce;
        frontRightWheelCollider.brakeTorque = brakeForce;
    }
    
    private void ApplyRestart()//브레이크가 풀렸을 때 엔진 다시 켜기
    {
        frontLeftWheelCollider.brakeTorque = 0;
        frontRightWheelCollider.brakeTorque = 0;
    }

    private void CheckRotate()//차량이 절대값 19도 이상으로 기울지 않게
    {
        if (transform.rotation.z > 0.33f)
        {
            transform.rotation = Quaternion.Euler(transform.rotation.x, transform.rotation.y, 0.33f);

        }
        if (transform.rotation.z < -0.33f)
        {
            transform.rotation = Quaternion.Euler(transform.rotation.x, transform.rotation.y, -0.33f);

        }
    }

    private void HandleSteering()//방향 조정은 전륜만 조정
    {
        currentSteerAngle = maxSteerAngle * moveVector.x;
        frontLeftWheelCollider.steerAngle = currentSteerAngle;
        frontRightWheelCollider.steerAngle = currentSteerAngle;

    }
    private void OnTriggerEnter(Collider other) {
        if (other.gameObject.tag == "Finish")
        {
            WorldManager.instance.OnSend(Protocol.Type.PlayerGoal);
            Debug.LogFormat("플레이어 {0} 도착", playerId);
        }
    }
#endregion


#region PublicMethod
    // 내 플레이어와 다른 플레이어 객체 초기화
    public void Initialize(bool _isMe, int _playerId, string _nickName)
    {
        this.isMe = _isMe;
        this.playerId = _playerId;
        this.nickName = _nickName;

        playerModelObject = this.gameObject;
        playerModelObject.transform.rotation = Quaternion.Euler(0, 0, 0);

        nameObject = Instantiate(nameObject, Vector3.zero, Quaternion.identity, playerModelObject.transform);
        nameObject.GetComponent<TMP_Text>().text = this.nickName;
        nameObject.transform.position = GetNameUIPos();

        if (this.isMe)
        {
            CinemachineCore.Instance.GetActiveBrain(0).ActiveVirtualCamera.Follow = this.transform;
        }

        this.isMove = false;
        this.moveVector = new Vector3(0, 0, 0);
    }
    public void SetMoveVector(float move)
    {
        SetMoveVector(this.transform.forward * move);
    }
    public void SetMoveVector(Vector3 vector)
    {
        moveVector = vector;

        if (vector == Vector3.zero)
        {
            isMove = false;
        }
        else
        {
            isMove = true;
        }
    }

    public void SetPosition(Vector3 pos)
    {
        gameObject.transform.position = pos;
    }

    // isStatic이 true이면 해당 위치로 바로 이동
    public void SetPosition(float x, float y, float z)
    {
        Vector3 pos = new Vector3(x, y, z);
        SetPosition(pos);
    }

    public Vector3 GetPosition()
    {
        return gameObject.transform.position;
    }

    public Vector3 GetRotation()
    {
        return gameObject.transform.rotation.eulerAngles;
    }
#endregion
}