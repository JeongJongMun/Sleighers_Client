using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AntiRollBar : MonoBehaviour
{
    #region PrivateVariables
        private Rigidbody Car;

    #endregion

    #region PublicVariables
        public WheelCollider wheelL;
        public WheelCollider wheelR;
        public float antiRoll = 5000.0f;

    #endregion
    void Start()
    {
        Car = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        //���� ���� ����
        WheelHit hit;

        float travelL = 1.0f;

        float travelR = 1.0f;

        bool groundedL = wheelL.GetGroundHit(out hit);

        if (groundedL)//���鿡 ����� ��
            travelL = (-wheelL.transform.InverseTransformPoint(hit.point).y - wheelL.radius) / wheelL.suspensionDistance;
        //���� �̻��� �Ÿ��� 0������ ������ �Ǿ��ٸ� ���� ���� ���´�. ���⿡ -�� �ٿ� �󸶳� ���� �Ǿ����� �� �� �ִ�.

        bool groundedR = wheelR.GetGroundHit(out hit);

        if (groundedR)
            travelR = (-wheelR.transform.InverseTransformPoint(hit.point).y - wheelR.radius) / wheelR.suspensionDistance;

        float antiRollForce = (travelL - travelR) * antiRoll;

        if (groundedL)
            Car.AddForceAtPosition(wheelL.transform.up * antiRollForce, wheelL.transform.position);

        if (groundedR)
            Car.AddForceAtPosition(wheelR.transform.up * -antiRollForce, wheelR.transform.position);
    }
}
