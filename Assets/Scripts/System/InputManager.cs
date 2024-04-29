using UnityEngine;
using Reader;
using Protocol;
/* InputManager.cs
 * - 인게임에서 플레이어 입력 처리
 * - 플레이어 스크립트는 여러 개 생성되기에 여기서 플레이어의 입력을 받아서 서버로 전송 및 플레이어 이동 처리
 */
public class InputManager : MonoBehaviour    
{
#region PrivateVariables
    private const string HORIZONTAL = "Horizontal";
    private const string VERTICAL = "Vertical";
#endregion

#region PublicVariables
    static public InputManager instance;
    public int playerId;
#endregion

#region PrivateMethod
    private void Awake()
    {
        instance = this;
    }
    private void Start()
    {
        GameManager.InGame += PlayerInput;
    }

    private void PlayerInput()
    {
        float h = Input.GetAxis(HORIZONTAL);
        float v = Input.GetAxis(VERTICAL);
        //isDrifting = Input.GetKey(KeyCode.LeftShift);

        if(Input.GetButton("Jump"))
            WorldManager.instance.GetMyPlayerFromId(playerId).isBraking = true;

        if(!Input.GetButton("Jump"))
            WorldManager.instance.GetMyPlayerFromId(playerId).isBraking = false;

        Vector3 moveVector = new Vector3(h, 0, v);
        moveVector = Vector3.Normalize(moveVector);
        PlayerMoveMessage playerMoveMessage = new PlayerMoveMessage(playerId, moveVector);

        byte[] data = new byte[1024];
        ByteWriter bw = new ByteWriter(data);
        bw.WriteBytes(bw.PlayerMoveMessageToByte(playerMoveMessage));
    
        WorldManager.instance.GetMyPlayer().SetMoveVector(moveVector);
        // ServerManager.Instance().SendMessage(data);
    }

#endregion

#region PublicMethod
#endregion
    
}
