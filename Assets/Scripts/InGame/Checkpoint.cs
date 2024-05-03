using UnityEngine;
using UnityEngine.Events;

public class Checkpoint : MonoBehaviour
{
#region PrivateVariables
    private new Collider collider;
#endregion

#region PublicVariables
    public UnityAction<Player, Checkpoint> OnPlayerEnterCheckpoint;
#endregion

#region PrivateMethod
    private void Awake()
    {
        collider = GetComponent<Collider>();
    }

    private void Start()
    {
        // 콜라이더가 트리거인지 확인
        collider.isTrigger = true;
    }

    private void OnTriggerEnter(Collider _other)
    {
        Player player = _other.GetComponentInParent<Player>();

        if (player != null)
            OnPlayerEnterCheckpoint?.Invoke(player, this);
    }
#endregion
}