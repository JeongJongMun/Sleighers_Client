using UnityEngine;
using TMPro;
using Cinemachine;
using UnityEngine.Rendering;

/* Player.cs
 * - 플레이어의 이동, 회전, 속도 조절
 * - 플레이어 스크립트는 여러 개 생성되기에 여기서 입력 처리를 하지 않는다.
 */
public class Player : MonoBehaviour
{
#region PrivateVariables

    //최대속도 제한, 드리프트
    private float maxSpeed = 1000f;
    private float speed = 100f;
    private float currentSpeed;
    private float rotate;

    private float currentRotate;
    private int playerId = 0;
    [SerializeField] private bool isMe = false;
    public bool IsMe
    {
        get { return isMe; }
        set { isMe = value; }
    }
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

    public Rigidbody rb;
    public Transform sledModel;
    public Transform sledNormal;
    
    [Header("Parameters")]
    public float acceleration = 40f;
    public float steering = 40f;
    public float gravity = 10f;
    public float amount;

    [field: SerializeField] public Vector3 moveVector { get; private set; }
    [field: SerializeField] public bool isMove { get; private set; }
    public GameObject nameObject;
    public MiniMapComponent miniMapComponent;
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
        
        SledPosition();
        SteerHandle();
        GetVerticalSpeed();
        CuerrentValue();
        CheckVelocity();
    }

    private void FixedUpdate()
    {
        if(isMove)
            ApplyPhysics();
    }

    private void GetVerticalSpeed()
    {
        if (moveVector.z > 0)
            speed = acceleration;
        else if (moveVector.z < 0)
            speed = -acceleration;
        else
            speed = 0;
    }

    private void SteerHandle()
    {
        if (moveVector.x != 0)
        {
            int dir = moveVector.x > 0 ? 1 : -1;
            amount = Mathf.Abs(moveVector.x);
            Steer(dir, amount);
        }
    }
    private void CuerrentValue()
    {
        currentSpeed = Mathf.SmoothStep(currentSpeed, speed , Time.deltaTime * 10f);
        speed = 0f; // Reset for next frame
        currentRotate = Mathf.Lerp(currentRotate, rotate, Time.deltaTime * 4f);
        rotate = 0f; // Reset for next frame
    }

    private Vector3 GetNameUIPos()
    {
        return this.transform.position + (Vector3.up * 2.0f);
    }

    private void ApplyPhysics()
    {
        rb.AddForce(sledModel.forward * currentSpeed, ForceMode.Acceleration);

        rb.AddForce(Vector3.down * gravity, ForceMode.Acceleration); // Apply gravity

        //steering, 썰매를 모델링한 오브젝트를 회전시키기 위해 사용
        sledModel.eulerAngles = Vector3.Lerp(sledModel.eulerAngles, new Vector3(0, sledModel.eulerAngles.y + currentRotate, 0), Time.deltaTime * 5f);

        RaycastHit hitOn, hitNear;

        Physics.Raycast(transform.position, Vector3.down, out hitOn, 1.1f);
        Physics.Raycast(transform.position, Vector3.down, out hitNear, 2.0f);

        sledNormal.up = Vector3.Lerp(sledNormal.up, hitNear.normal, Time.deltaTime * 8.0f);
        sledNormal.Rotate(0, transform.eulerAngles.y, 0);
        isMove = false;
    }

    public void Steer(int direction, float amount)
    {
        rotate = (steering * direction) * amount;
    }

    private void SledPosition()
    {
        sledModel.transform.position = rb.transform.position - new Vector3(0, 1f, 0);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Finish")
        {
            WorldManager.instance.OnSend(Protocol.Type.PlayerGoal);
            Debug.LogFormat("플레이어 {0} 도착", playerId);
        }
    }

    private void CheckVelocity()
    {
        if (rb.velocity.magnitude > maxSpeed)
        {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        }
    }
#endregion


#region PublicMethod
    // 내 플레이어와 다른 플레이어 객체 초기화
    public void Initialize(bool _isMe, int _playerId, string _nickName)
    {
        IsMe = _isMe;
        this.playerId = _playerId;
        this.nickName = _nickName;

        miniMapComponent.enabled = true;
        playerModelObject = sledModel.gameObject;

        nameObject = Instantiate(nameObject, Vector3.zero, Quaternion.identity, playerModelObject.transform);
        nameObject.GetComponent<TMP_Text>().text = this.nickName;
        nameObject.transform.position = GetNameUIPos();

        if (IsMe)
            CinemachineCore.Instance.GetActiveBrain(0).ActiveVirtualCamera.Follow = sledModel.transform;

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
    public Vector3 GetVelocity()
    {
        return rb.velocity;
    }

    public float GetSpeed()
    {   
        // km/h로 변환
        return rb.velocity.magnitude * 3.6f;
    }

    // 앞으로 나아가는 차량 속도의 양
    public float ForwardSpeed => Vector3.Dot(rb.velocity, transform.forward);

    // 차량의 최대 속도에 상대적인 전진 속도를 반환 
    // 반환되는 값은 [-1, 1] 범위
    
    public float NormalizedForwardSpeed
    {
        get => (Mathf.Abs(ForwardSpeed) > 0.1f) ? ForwardSpeed / maxSpeed : 0.0f;
    }
#endregion
}