using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class OutGameUI : MonoBehaviour
{
#region PublicVariables
    public static OutGameUI instance;
    
    [Space(10), Header("===== Panels =====")]
    public GameObject[] panels;
    [Space(10), Header("===== Button =====")]
    public Button loginBtn;
    public Button matchMakingBtn;
    [Space(10), Header("===== Setting =====")]
    public Toggle soundToggle;
    public Slider volumeSlider;
    public TMP_Text setNameText;
    [Space(10), Header("===== Matching Room =====")]
    public GameObject loadingObject;
    public TMP_Text matchMakingBtnText;
    public GameObject PlayerMatchList;
    public GameObject PlayerMatchListPrefabs;
    public bool speedPostProcessing = true;
    public bool mainPostProcessing = true;

    [Space(10), Header("===== TextField =====")]
    public TMP_InputField idField;
    public TMP_InputField settingNameField;
    [Space(10), Header("===== Bar =====")]
    public GameObject topBar;
    [Space(10), Header("===== Store =====")]
    public GameObject[] sledList;
    public int sledListCnt;
#endregion

#region PrivateMethod
    private void Awake()
    {
        if (instance != null)
            Destroy(instance);
        instance = this;
    }

    private void Start()
    {
        loginBtn.onClick.AddListener(() => GameManager.Instance().ChangeState(GameManager.GameState.Lobby));
        matchMakingBtn.onClick.AddListener(() => GameManager.Instance().ChangeState(GameManager.GameState.MatchMaking));

        soundToggle.onValueChanged.AddListener((value) => SoundOnOff());
        volumeSlider.onValueChanged.AddListener((value) => VolumeSlider()); 
        volumeSlider.value = 0.5f;
        
        Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
        foreach(Button button in buttons)
        {
            button.onClick.AddListener(()=> GameManager.Instance().soundManager.Play("Effect/Click", SoundType.EFFECT));
        }

    }
#endregion

#region PublicMethod
    public void SuccLoginPanel()
    {
        panels[0].SetActive(false);  // auth panel
        panels[1].SetActive(true);   // lobby panel
        topBar.SetActive(true);
    }

    public void MatchMakingUI()
    {
        matchMakingBtnText.text = "매칭중";
        loadingObject.SetActive(true);
    }
    public void ReturnMatchMakingUI()
    {
        matchMakingBtnText.text = "매치메이킹";
        loadingObject.SetActive(false);
    }

    public void DrawMatchPlayer(int name)
    {
        GameObject playerObject = Instantiate(PlayerMatchListPrefabs, PlayerMatchList.transform);
        playerObject.GetComponentInChildren<TMP_Text>().text = name.ToString();
    }

    public void DrawMatchPlayer(string name)
    {
        GameObject playerObject = Instantiate(PlayerMatchListPrefabs, PlayerMatchList.transform);
        playerObject.GetComponentInChildren<TMP_Text>().text = name;
    }

    public void DestroyMatchPlayer()
    {
        foreach(Transform child in PlayerMatchList.transform)
        {
            Destroy(child.gameObject);
        }
    }

    public void PopupMatchMakingPanel()
    {
        panels[6].SetActive(true);
    }
    public void RealExitGame()
    {
        Debug.Log("Exit Game");
        Application.Quit();
    }
    public void ClosePanels()
    {
        foreach (var panel in panels)
        {
            panel.SetActive(false);
        }
    }
    public void ToggleObject(GameObject obj)
    {
        obj.SetActive(!obj.activeSelf);
    }
    public void LoadInGame()
    {
        GameManager.Instance().ChangeState(GameManager.GameState.Ready);
        SceneManager.LoadScene("InGame");
    }
    public void SoundOnOff()
    {
        if(soundToggle.isOn && volumeSlider.value > 0)
        {
            AudioListener.volume = 1;
        }
        else
        {
            AudioListener.volume = 0;
        }
    }
    
    public void VolumeSlider()
    {
        if(soundToggle.isOn)
        {
            AudioListener.volume = volumeSlider.value;
        }
        else
        {
            AudioListener.volume = 0;
        }
    }
    public void ToggleMainPostProcessing()
    {
        mainPostProcessing = !mainPostProcessing;
    }

    public void ToggleSpeedProcessing()
    {  
        speedPostProcessing = !speedPostProcessing;
    }
#endregion
}