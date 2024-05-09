using UnityEngine;

public class SledBuzz : MonoBehaviour
{

#region PublicVariables
    public float rotationSpeed = 20.0f; // ȸ�� �ӵ�

#endregion

#region PrivateVariables
    private Rigidbody rb;

#endregion

#region PrivateMethods
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        transform.Rotate(0f,0.3f,0f,Space.World);
    }
    
#endregion
}
