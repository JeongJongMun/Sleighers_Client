using UnityEngine;

public class SledControl : MonoBehaviour
{
    #region PrivateVariables
    //Input setting ������ ����
    private const string HORIZONTAL = "Horizontal";
    private const string VERTICAL = "Vertical";

    private float horizontalInput;
    private float verticalInput;
    private float currentSteerAngle;
    //private bool isDrifting;

    [SerializeField] private float motorForce;
    [SerializeField] private float brakeForce;
    [SerializeField] private float maxSteerAngle;
    [SerializeField] private bool isBraking;


    [SerializeField] private WheelCollider frontLeftWheelCollider;
    [SerializeField] private WheelCollider frontRightWheelCollider;
    [SerializeField] private WheelCollider backLeftWheelCollider;
    [SerializeField] private WheelCollider backRightWheelCollider;


    #endregion


    #region PrivateMethod
    private void FixedUpdate()
    {
        GetInput();
        HandleMotor();
        CheckRotate();
        HandleSteering();

    }

    private void GetInput()//Ű �Է�
    {
        horizontalInput = Input.GetAxis(HORIZONTAL);
        verticalInput = Input.GetAxis(VERTICAL);
        //isDrifting = Input.GetKey(KeyCode.LeftShift);
        
        if(Input.GetButton("Jump"))
            isBraking = true;

        if(!Input.GetButton("Jump"))
            isBraking = false;

    }

    private void HandleMotor()//���� �ӵ� ����
    {
        //�߰� ���� : max �ӵ� ����, AddForce�� �ӵ� ����
        frontLeftWheelCollider.motorTorque = verticalInput * motorForce;
        frontRightWheelCollider.motorTorque = verticalInput * motorForce;

        if (isBraking)//Space ������ ���� ��
            ApplyBraking();
        else
            ApplyRestart();


        //if(isDrifting)
        //    Drift();

    }

    private void ApplyBraking()//�극��ũ
    {
        frontLeftWheelCollider.brakeTorque = brakeForce;
        frontRightWheelCollider.brakeTorque = brakeForce;
    }
    
    private void ApplyRestart()//�극��ũ�� Ǯ���� �� ���� �ٽ� �ѱ�
    {
        frontLeftWheelCollider.brakeTorque = 0;
        frontRightWheelCollider.brakeTorque = 0;
        frontLeftWheelCollider.motorTorque = verticalInput * motorForce;
        frontRightWheelCollider.motorTorque = verticalInput * motorForce; 

    }

    private void CheckRotate()//������ ���밪 19�� �̻����� ����� �ʰ�
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

    private void HandleSteering()//���� ������ ������ ����
    {
        currentSteerAngle = maxSteerAngle * horizontalInput;
        frontLeftWheelCollider.steerAngle = currentSteerAngle;
        frontRightWheelCollider.steerAngle = currentSteerAngle;

    }
    #endregion

    /* �帮��Ʈ �Ϸ��� �ķ��� ���߰� �Ѵ�.-> ���� ������ �ڵ����� �̲�������.
     * stiffness�� �����Ѵ�.
     */

    //private void Drift()
    //{
    //    Drifting();
    //}


    //private void Drifting()
    //{

    //}
}
