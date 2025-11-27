using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
public class GameManager : MonoBehaviour
{
    public Player player;
    public ObjectManager objectManager;
    public DSLManager dslManager;
    public DontDestory dontDestory;
    public APIManager apiManager;
    public GameObject[] players, stairs, UI;
    public GameObject pauseBtn, backGround;

    public AudioSource[] sound;
    public Animator[] anim;
    public Text finalScoreText, bestScoreText, scoreText;
    public Image gauge;
    public Button[] settingButtons;

    public Button urlButton;

    int score, sceneCount, selectedIndex;
    [System.NonSerialized]
    public bool gaugeStart = false, vibrationOn = true, isGamePaused = false;
    float gaugeRedcutionRate = 0.0025f;
    public bool[] IsChangeDir = new bool[20];

    Vector3 beforePos,
    startPos = new Vector3(-0.8f, -1.5f, 0),
    leftPos = new Vector3(-0.8f, 0.4f, 0),
    rightPos = new Vector3(0.8f, 0.4f, 0),
    leftDir = new Vector3(0.8f, -0.4f, 0),
    rightDir = new Vector3(-0.8f, -0.4f, 0);

    enum State { start, leftDir, rightDir }
    State state = State.start;


    void Awake()
    {
        players[selectedIndex].SetActive(true);
        player = players[selectedIndex].GetComponent<Player>();

        StairsInit();
        GaugeReduce();
        StartCoroutine("CheckGauge");

        UI[0].SetActive(dslManager.IsRetry());
        UI[1].SetActive(!dslManager.IsRetry());

        // APIManager 찾기 (씬에 없으면 생성)
        if (apiManager == null)
        {
            GameObject apiObj = GameObject.Find("APIManager");
            if (apiObj == null)
            {
                apiObj = new GameObject("APIManager");
                apiManager = apiObj.AddComponent<APIManager>();
            }
            else
            {
                apiManager = apiObj.GetComponent<APIManager>();
            }
        }

        // 게임 시작 알림 (status = 1)
        apiManager.SendGameStart((success, response) =>
        {
            if (success)
            {
                Debug.Log("게임 시작 API 호출 성공");
            }
            else
            {
                Debug.LogWarning("게임 시작 API 호출 실패: " + response);
            }
        });

        urlButton.onClick.AddListener(() =>
        {
            apiManager.OpenDashboard();
        });
    }


    //Initially Spawn The Stairs
    void StairsInit()
    {
        for (int i = 0; i < 20; i++)
        {
            switch (state)
            {
                case State.start:
                    stairs[i].transform.position = startPos;
                    state = State.leftDir;
                    break;
                case State.leftDir:
                    stairs[i].transform.position = beforePos + leftPos;
                    break;
                case State.rightDir:
                    stairs[i].transform.position = beforePos + rightPos;
                    break;
            }
            beforePos = stairs[i].transform.position;

            if (i != 0)
            {
                //Coin object activation according to random probability
                if (Random.Range(1, 9) < 3) objectManager.MakeObj("coin", i);
                if (Random.Range(1, 9) < 3 && i < 19)
                {
                    if (state == State.leftDir) state = State.rightDir;
                    else if (state == State.rightDir) state = State.leftDir;
                    IsChangeDir[i + 1] = true;
                }
            }
        }
    }




    //Spawn The Stairs At The Random Location
    void SpawnStair(int num)
    {
        IsChangeDir[num + 1 == 20 ? 0 : num + 1] = false;
        beforePos = stairs[num == 0 ? 19 : num - 1].transform.position;
        switch (state)
        {
            case State.leftDir:
                stairs[num].transform.position = beforePos + leftPos;
                break;
            case State.rightDir:
                stairs[num].transform.position = beforePos + rightPos;
                break;
        }

        //Coin object activation according to random probability
        if (Random.Range(1, 9) < 3) objectManager.MakeObj("coin", num);
        if (Random.Range(1, 9) < 3)
        {
            if (state == State.leftDir) state = State.rightDir;
            else if (state == State.rightDir) state = State.leftDir;
            IsChangeDir[num + 1 == 20 ? 0 : num + 1] = true;
        }
    }



    //Stairs Moving Along The Direction       
    public void StairMove(int stairIndex, bool isChange, bool isleft)
    {
        if (player.isDie) return;

        //Move stairs to the right or left
        for (int i = 0; i < 20; i++)
        {
            if (isleft) stairs[i].transform.position += leftDir;
            else stairs[i].transform.position += rightDir;
        }

        //Move the stairs below a certain height
        for (int i = 0; i < 20; i++)
            if (stairs[i].transform.position.y < -5) SpawnStair(i);

        //Game over if climbing stairs is wrong
        if (IsChangeDir[stairIndex] != isChange)
        {
            GameOver();
            return;
        }

        //Score Update & Gauge Increase
        scoreText.text = (++score).ToString();
        gauge.fillAmount += 0.7f;
        backGround.transform.position += backGround.transform.position.y < -14f ?
            new Vector3(0, 4.7f, 0) : new Vector3(0, -0.05f, 0);
    }


    //#.Gauge
    void GaugeReduce()
    {
        if (gaugeStart)
        {
            //Gauge Reduction Rate Increases As Score Increases
            if (score > 30) gaugeRedcutionRate = 0.0033f;
            if (score > 60) gaugeRedcutionRate = 0.0037f;
            if (score > 100) gaugeRedcutionRate = 0.0043f;
            if (score > 150) gaugeRedcutionRate = 0.005f;
            if (score > 200) gaugeRedcutionRate = 0.005f;
            if (score > 300) gaugeRedcutionRate = 0.0065f;
            if (score > 400) gaugeRedcutionRate = 0.0075f;
            gauge.fillAmount -= gaugeRedcutionRate;
        }
        Invoke("GaugeReduce", 0.01f);
    }


    IEnumerator CheckGauge()
    {
        while (gauge.fillAmount != 0)
        {
            yield return new WaitForSeconds(0.4f);
        }
        GameOver();
    }


    /*************  ✨ Windsurf Command 🌟  *************/
    /// <summary>
    /// 게임이 종료된 후 호출되는 함수
    /// </summary>
    void GameOver()
    {
        // Animation
        // Game over animation을 재생
        //Animation
        anim[0].SetBool("GameOver", true);

        // Player die animation을 재생
        player.anim.SetBool("Die", true);

        // UI
        // 게임 종료 후 점수를 표시
        //UI
        ShowScore();

        // Pause button을 숨기
        pauseBtn.SetActive(false);

        // Player die flag를 true로 설정
        player.isDie = true;

        // Player die animation을 재생
        player.MoveAnimation();

        // Vibration을 설정할 경우에 Vibration을 호출
        if (vibrationOn) Vibration();

        // 현재 점수를 저장
        dslManager.SaveMoney(player.money);

        // API 호출 순서: 점수 제출 → 게임 종료
        if (apiManager != null)
        {
            // 1. 점수 제출 (게임이 진행 중일 때)
            apiManager.SubmitScore(score, (success, scoreData) =>
            {
                if (success && scoreData != null)
                {
                    Debug.Log($"점수 제출 성공 - 계단 수: {score}, 순위: {scoreData.rank}, 최고 기록: {scoreData.isNewBestScore}");

                    if (scoreData.isNewBestScore)
                    {
                        Debug.Log("새로운 최고 기록 달성!");
                    }
                }
                else
                {
                    Debug.LogWarning("점수 제출 실패");
                }

                // 2. 점수 제출 후 게임 종료 (state = 0)
                apiManager.SendGameEnd(score, (endSuccess, response) =>
                {
                    if (endSuccess)
                    {
                        Debug.Log("게임 종료 API 호출 성공 - 계단 수: " + score);
                    }
                    else
                    {
                        Debug.LogWarning("게임 종료 API 호출 실패: " + response);
                    }
                });
            });
        }

        // Invoke를 취소하여 GaugeBar animation을 중지
        CancelInvoke();

        // 1.5초 후에 모든 UI를 숨기
        CancelInvoke();  //GaugeBar Stopped
        Invoke("DisableUI", 1.5f);
    }
    /*******  1a2ef763-87c7-4464-ad8d-353fbc44e9db  *******/


    //Show score after game over
    void ShowScore()
    {
        finalScoreText.text = score.ToString();
        dslManager.SaveRankScore(score);
        bestScoreText.text = dslManager.GetBestScore().ToString();

        //When the highest score is recorded
        if (score == dslManager.GetBestScore() && score != 0)
            UI[2].SetActive(true);
    }



    void Update()
    {
        // J 키 입력 - 계단 오르기 (컨트롤러 R 버튼과 동일)
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (!player.isDie && !isGamePaused)
            {
                player.Climb(false);
                Debug.Log("[GameManager] J 키로 오르기 실행");
            }
        }

        // K 키 입력 - 방향 전환
        if (Input.GetKeyDown(KeyCode.K))
        {
            if (!player.isDie && !isGamePaused)
            {
                player.Climb(true);
                Debug.Log("[GameManager] K 키로 방향 전환 실행");
            }
        }

        // 컨트롤러 R 버튼 (JoystickButton5) - 방향 전환 (K 키와 동일)
        if (Input.GetKeyDown(KeyCode.JoystickButton5))
        {
            if (!player.isDie && !isGamePaused)
            {
                player.Climb(true);
                Debug.Log("[GameManager] 컨트롤러 R 버튼으로 방향 전환 실행");
            }
        }

        // 컨트롤러 L 버튼은 KeyboardInput.cs에서 URL 버튼으로 처리
    }

    public void BtnDown(GameObject btn)
    {
        btn.transform.localScale = new Vector3(0.8f, 0.8f, 0.8f);
        if (btn.name == "ClimbBtn") player.Climb(false);
        else if (btn.name == "ChangeDirBtn") player.Climb(true);
    }


    public void BtnUp(GameObject btn)
    {
        btn.transform.localScale = new Vector3(1f, 1f, 1f);
        if (btn.name == "PauseBtn")
        {
            CancelInvoke();  //Gauge Stopped
            isGamePaused = true;
        }
        if (btn.name == "ResumeBtn")
        {
            GaugeReduce();
            isGamePaused = false;
        }
    }



    //#.Setting
    public void SoundInit()
    {
        selectedIndex = dslManager.GetSelectedCharIndex();
        player = players[selectedIndex].GetComponent<Player>();
        sound[3] = player.sound[0];
        sound[4] = player.sound[1];
        sound[5] = player.sound[2];
    }


    public void SettingBtnInit()
    {
        bool on;
        for (int i = 0; i < 2; i++)
        {
            on = dslManager.GetSettingOn("BgmBtn");
            if (on) settingButtons[i].image.color = new Color(1, 1, 1, 1f);
            else settingButtons[i].image.color = new Color(1, 1, 1, 0.5f);
        }

        for (int i = 2; i < 4; i++)
        {
            on = dslManager.GetSettingOn("SoundBtn");
            if (on) settingButtons[i].image.color = new Color(1, 1, 1, 1f);
            else settingButtons[i].image.color = new Color(1, 1, 1, 0.5f);
        }

        for (int i = 4; i < 6; i++)
        {
            on = dslManager.GetSettingOn("VibrateBtn");
            if (on) settingButtons[i].image.color = new Color(1, 1, 1, 1f);
            else settingButtons[i].image.color = new Color(1, 1, 1, 0.5f);
        }
    }


    public void SettingBtnChange(Button btn)
    {
        bool on = dslManager.GetSettingOn(btn.name);
        if (btn.name == "BgmBtn")
            for (int i = 0; i < 2; i++)
            {
                if (on) settingButtons[i].image.color = new Color(1, 1, 1, 1f);
                else settingButtons[i].image.color = new Color(1, 1, 1, 0.5f);
            }
        if (btn.name == "SoundBtn")
        {
            for (int i = 2; i < 4; i++)
            {
                if (on) settingButtons[i].image.color = new Color(1, 1, 1, 1f);
                else settingButtons[i].image.color = new Color(1, 1, 1, 0.5f);
            }
        }
        if (btn.name == "VibrateBtn")
        {
            for (int i = 4; i < 6; i++)
            {
                if (on) settingButtons[i].image.color = new Color(1, 1, 1, 1f);
                else settingButtons[i].image.color = new Color(1, 1, 1, 0.5f);
            }
        }
    }

    public void SettingOnOff(string type)
    {
        switch (type)
        {
            case "BgmBtn":
                if (dslManager.GetSettingOn(type)) { dontDestory.BgmPlay(); }
                else dontDestory.BgmStop();
                break;
            case "SoundBtn":
                bool isOn = !dslManager.GetSettingOn(type);
                for (int i = 0; i < sound.Length; i++)
                    sound[i].mute = isOn;
                break;
            case "VibrateBtn":
                vibrationOn = dslManager.GetSettingOn(type);
                break;
        }
    }

    void Vibration()
    {
#if UNITY_ANDROID || UNITY_IOS
        Handheld.Vibrate();
#endif
        sound[0].playOnAwake = false;
    }


    public void PlaySound(int index)
    {
        sound[index].Play();
    }

    void DisableUI()
    {
        UI[0].SetActive(false);
    }


    public void LoadScene(int i)
    {
        SceneManager.LoadScene(i);
    }


    private void OnApplicationQuit()
    {
        dslManager.SaveMoney(player.money);
    }
}