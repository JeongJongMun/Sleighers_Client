using System;
using System.Collections;
using System.Collections.Generic;
using Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
/* InGameUI.cs
 * - 인게임 UI 관리
 * - 타이머, 속도계, 랭킹, 결과창 등을 관리
 */
public class InGameUI : MonoBehaviour
{
#region PublicVariables
    public static InGameUI instance;

    [Space(10), Header("타이머 관련")]
    public TextMeshProUGUI text_timer;
    public TextMeshProUGUI text_countDown;
    public TextMeshProUGUI text_gameEndCountDown;

    [Space(10), Header("속도계")]
    public TextMeshProUGUI text_speedLabel;

    [Space(10), Header("랭킹 관련"), Tooltip("1/2 LAP")] 
    public TextMeshProUGUI text_lab;
    [Tooltip("1/5 (등수)")]
    public TextMeshProUGUI text_rank;
    public Transform rankHolder;        // 랭킹 요소를 가지고 있는 부모
    public RankManager rankManager;

    [Space(10), Header("결과창 관련")]
    public GameObject resultPanel;      // 결과창 패널
    public GameObject resultElem;       // 결과창 요소 프리팹
    public Transform resultElemHolder;  // 결과창 요소를 가지고 있는 부모
#endregion

#region PrivateVariables
    // 랭킹 관련
    private GameObject rankElementPrefab;     // 랭킹 프리팹
    private Dictionary<string, GameObject> rankElements; // <닉네임, RankElement>
    private float countDownDuration = 3.0f;
    private float speed = 0.0f;
    private float timer = 0.0f;

    // 게임 결과 저장 변수
    private List<PlayerResult> playerResults;
    private bool isGoal = false;
    // Blink 코루틴 변수
    private float blinkDuration = 0.1f;         // 블링크 지속 시간 (초)
    private Color originalColor = Color.white;  // 원래 색상
    private Color blinkColor = Color.gray;      // 블링크 색상
    private float blinkInterval = 0.02f;        // 블링크 간격
    private const float highlightScale = 1.5f;  // 블링크 크기
    // ResultElem 텍스트 인덱스
    private const int rankIndex = 0;
    private const int nicknameIndex = 2;
    private const int timeIndex = 3;
#endregion

#region PrivateMethod
    private void Awake()
    {
        instance = this;
    }
    private void Start()
    {
        GameManager.InGame += UpdateTimer;
        GameManager.InGame += UpdateSpeedometer;
        if (rankManager != null)
            rankManager.OnLapComplete += OnLapComplete;
        else
            Debug.Log("[InGameUI] LapManager가 없습니다.");
        UpdateLapText(1);
        rankElements = new Dictionary<string, GameObject>();
        GameManager.End += GameResultUI;
        rankElementPrefab = Resources.Load<GameObject>("UI/RankElement");
    }

    private void OnDestroy()
    {
        CancelInvoke("ShowGameResultUI"); // InGameUI 객체가 파괴되기 전에 예약된 메서드 호출 취소
    }

    // 텍스트 숨기기
    private void HideCountDown()
    {
        if(text_countDown.text == "GO!")
            text_countDown.gameObject.SetActive(false);
        if(text_gameEndCountDown.text == "Game End")
            text_gameEndCountDown.gameObject.SetActive(false);
    }
    // 플레이어가 랩을 통과하면 호출
    private void OnLapComplete(Player _player, RankInfo _lapInfo)
    {
        // 다른 플레이어가 랩을 완료하면 반환
        if (WorldManager.instance.GetMyPlayer() != _player)
            return;      
        // Lap 텍스트 업데이트
        int lapsCompleted = rankManager.AddOrGetRankInfo(_player).lap;
        int currentLap = Mathf.Min(lapsCompleted + 1, rankManager.laps);
        UpdateLapText(currentLap);
    }

    private void UpdateLapText(int _currentLap)
    {
        if (rankManager != null)
            text_lab.text = $"<size=160>{_currentLap}</size=160>/{rankManager.laps} LAP";
    }
    // 랭킹 UI 깜박임 효과
    private IEnumerator RankEffectCoroutine(Image _target)
    {
        // 점차 어두워짐
        for (float t = 0; t <= blinkDuration; t += blinkInterval)
        {
            _target.color = Color.Lerp(originalColor, blinkColor, t / blinkDuration);
            _target.transform.localScale = Vector3.one * Mathf.Lerp(1.0f, highlightScale, t / blinkDuration);
            yield return new WaitForSeconds(blinkInterval);
        }

        yield return new WaitForSeconds(0.01f);
        // 점차 밝아짐
        for (float t = 0; t <= blinkDuration; t += blinkInterval)
        {
            _target.color = Color.Lerp(blinkColor, originalColor, t / blinkDuration);
            _target.transform.localScale = Vector3.one * Mathf.Lerp(highlightScale, 1.0f, t / blinkDuration);
            yield return new WaitForSeconds(blinkInterval);
        }

    }

    private void GameResultUI(List<PlayerResult> _playerResults)
    {
        playerResults = _playerResults; // 게임 결과를 저장

        if (rankElements.Count != _playerResults.Count) {
            List<string> keys = new List<string>(rankElements.Keys);
            for (int i = _playerResults.Count; i < rankElements.Count; i++)
            {
                string key = keys[i];
                playerResults.Add(new PlayerResult { nickname = key, rank = i+1, goalTime = 0 });
            }
        }

        if (this != null && enabled)
        {
            Invoke("ShowGameResultUI", 3f); // 2초 후에 ShowGameResultUI 메서드 호출
        }
    }

    private void ShowGameResultUI()
    {
        resultPanel.SetActive(true);
        GameManager.Instance().soundManager.Play("BGM/GameEndFanfare", SoundType.BGM);
        for (int i = 0; i < playerResults.Count; i++)
        {
            GameObject resultElemObj = Instantiate(resultElem, resultElemHolder);
            string resultTime;
            if ((long)playerResults[i].goalTime == 0)
                resultTime = "Game Over";
            else
                resultTime = string.Format("{0:D2}:{1:D2}:{2:D2}", TimeSpan.FromMilliseconds((long)playerResults[i].goalTime).Minutes, TimeSpan.FromMilliseconds((long)playerResults[i].goalTime).Seconds, TimeSpan.FromMilliseconds((long)playerResults[i].goalTime).Milliseconds / 10);

            resultElemObj.transform.SetSiblingIndex(i);
            resultElemObj.transform.GetChild(rankIndex).GetComponent<TextMeshProUGUI>().text = playerResults[i].rank.ToString();
            resultElemObj.transform.GetChild(nicknameIndex).GetComponent<TextMeshProUGUI>().text = playerResults[i].nickname;
            resultElemObj.transform.GetChild(timeIndex).GetComponent<TextMeshProUGUI>().text = resultTime;
        }
    }

#endregion

#region PublicMethod
    public void CreateRankUI(string _nickname)
    {
        GameObject rankElem = Instantiate(rankElementPrefab, rankHolder);
        RankElement rankElement = rankElem.GetComponent<RankElement>();
        rankElement.SetRankElement(0, _nickname);
        rankElements.Add(_nickname, rankElem);

        // 내 랭킹 요소면 투명도와 글자 색 조절
        string myNickname = WorldManager.instance.GetMyPlayer().nickname;
        if (_nickname == myNickname)
            rankElement.SetMyRankElement();
    }
    public void UpdateRankUI(List<RankInfo> _ranking)
    {
        string myNickname = WorldManager.instance.GetMyPlayer().nickname;
        int totalPlayer = _ranking.Count;
        for (int i = 0; i < totalPlayer; i++)
        {
            string nickname = _ranking[i].nickname;
            int lap = _ranking[i].lap;
            int checkpoint = _ranking[i].checkpoint;
            rankElements[nickname].GetComponent<RankElement>().SetRankElement(i + 1, nickname + "-" + checkpoint);
            rankElements[nickname].transform.SetSiblingIndex(i);

            // 내 등수 업데이트
            if (nickname == myNickname)
            {
                int currentRank = i + 1;
                // 내 등수가 이전 등수보다 높으면 깜박임 효과
                // if (currentRank < RankManager.instance.previousRank)
                // {
                //     StartCoroutine(RankEffectCoroutine(rankElements[nickname].transform.GetComponent<Image>()));
                // }
                text_rank.text = $"<size=160>{currentRank}</size>/{totalPlayer}";
                RankManager.instance.previousRank = currentRank;
            }
        }
    }
    public void DeleteRankUI(string _nickname)
    {
        Destroy(rankElements[_nickname]);
        rankElements.Remove(_nickname);
    }
    public void UpdateTimer()
    {
        if (WorldManager.instance.isRaceFinish)
            return;

        timer += Time.deltaTime;
        int minutes = (int)(timer / 60 % 60);
        int seconds = (int)(timer % 60);
        int miliseconds = (int)(timer * 1000 % 1000);
        text_timer.text = string.Format("TIME  {0:D2} : {1:D2} : {2:D3}", minutes, seconds, miliseconds);
    }

    // 카운트 다운 설정
    public void SetCountDown(int _count)
    {
        countDownDuration = _count;

        if(text_countDown != null)
        {
            if(countDownDuration > 0)
            {
                GameManager.Instance().soundManager.Play("Effect/StartCount", SoundType.EFFECT);
                text_countDown.text = countDownDuration.ToString();
                text_countDown.gameObject.SetActive(true);
            }
            else
            {
                GameManager.Instance().soundManager.Play("Effect/StartCountGo", SoundType.EFFECT);
                text_countDown.text = "GO!";
                Invoke("HideCountDown", 0.4f);
            }
        }
        else
        {
            text_countDown.gameObject.SetActive(false);         
        }
    }

    public void SetGameEndCountDown(int _count)
    {
        if(!WorldManager.instance.isRaceFinish)
        {

            if(text_gameEndCountDown != null)
            {
                if(_count > 0)
                {
                    GameManager.Instance().soundManager.Play("Effect/EndCount", SoundType.EFFECT);
                    text_gameEndCountDown.text = _count.ToString();
                    text_gameEndCountDown.gameObject.SetActive(true);
                }
                else
                {
                    GameManager.Instance().soundManager.Play("Effect/Retire", SoundType.EFFECT);
                    text_gameEndCountDown.text = "Game End";
                    Invoke("HideCountDown", 1f);
                }
            }
        }
        else if(WorldManager.instance.isRaceFinish && !isGoal)
        {
            isGoal = true;
            GameManager.Instance().soundManager.Stop("BGM/Wind", SoundType.WIND);
            AudioSource sledFriction = FindObjectOfType<SledFrictionAudioEffect>().sledFrictionAudioSource;
            GameManager.Instance().soundManager.Stop(sledFriction);
            GameManager.Instance().soundManager.Play("Effect/Goal", SoundType.EFFECT);
            text_gameEndCountDown.gameObject.SetActive(true);
            text_gameEndCountDown.text = "완주!";
        }
    }
    public void UpdateSpeedometer()
    {
        speed = WorldManager.instance.GetMyPlayer().GetSpeed();
        if(text_speedLabel != null){
            text_speedLabel.text = string.Format("{0}", (int)speed);
            if ((int)speed >= 100)
                text_speedLabel.color = Color.red;
            else
                text_speedLabel.color = Color.white;
        } 
    }
    public void LoadOutGameScene()
    {
        ServerManager.Instance().DisconnectInGame();
        GameManager.Instance().ChangeState(GameManager.GameState.Lobby);
        SceneManager.LoadScene("OutGame");
    }
#endregion
}
