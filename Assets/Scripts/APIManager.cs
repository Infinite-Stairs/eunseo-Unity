using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

[Serializable]
public class GameStartRequest
{
    public int status; // 1: 게임 시작
}

[Serializable]
public class GameEndRequest
{
    public int status;      // 0: 게임 종료
    public int stairCount;  // 계단 수
}

[Serializable]
public class ScoreSubmitRequest
{
    public int score;           // 점수 (계단 수)
    public int characterIndex;  // 캐릭터 인덱스
    public int money;           // 획득한 코인
    public string timestamp;    // 게임 종료 시간
}

[Serializable]
public class GameResponse
{
    public bool success;
    public string message;
    public object data;
}

[Serializable]
public class ScoreData
{
    public int rank;
    public bool isNewBestScore;
    public int bestScore;
}

public class APIManager : MonoBehaviour
{
    // TODO: 백엔드 서버 URL로 변경해주세요
    private string baseURL = "https://dowhile001.vercel.app";

    // 게임 세션 ID (백엔드에서 받아올 수도 있습니다)
    private string currentSessionId;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 게임 시작 시 호출 - 상태값 1 전송
    /// </summary>
    public void SendGameStart(Action<bool, string> callback = null)
    {
        StartCoroutine(PostGameStart(callback));
    }

    /// <summary>
    /// 게임 종료 시 호출 - 상태값 0과 계단 수 전송
    /// </summary>
    /// <param name="stairCount">도달한 계단 수</param>
    public void SendGameEnd(int stairCount, Action<bool, string> callback = null)
    {
        StartCoroutine(PostGameEnd(stairCount, callback));
    }

    /// <summary>
    /// 게임 점수 제출 - 게임 한 판 끝날 때마다 호출
    /// </summary>
    /// <param name="score">점수 (계단 수)</param>
    /// <param name="characterIndex">사용한 캐릭터 인덱스</param>
    /// <param name="money">획득한 코인</param>
    /// <param name="callback">콜백 함수</param>
    public void SubmitScore(int score, int characterIndex, int money, Action<bool, ScoreData> callback = null)
    {
        StartCoroutine(PostScore(score, characterIndex, money, callback));
    }

    private IEnumerator PostGameStart(Action<bool, string> callback)
    {
        GameStartRequest requestData = new GameStartRequest
        {
            status = 1
        };

        string jsonData = JsonConvert.SerializeObject(requestData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        UnityWebRequest request = new UnityWebRequest(baseURL + "/game/start", "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
        bool isSuccess = request.result == UnityWebRequest.Result.Success;
#else
        bool isSuccess = !request.isNetworkError && !request.isHttpError;
#endif

        if (isSuccess)
        {
            Debug.Log("게임 시작 전송 성공: " + request.downloadHandler.text);

            // 응답에서 세션 ID를 받아올 수 있습니다
            try
            {
                GameResponse response = JsonConvert.DeserializeObject<GameResponse>(request.downloadHandler.text);
                callback?.Invoke(true, request.downloadHandler.text);
            }
            catch (Exception e)
            {
                Debug.LogError("응답 파싱 에러: " + e.Message);
                callback?.Invoke(false, e.Message);
            }
        }
        else
        {
            Debug.LogError("게임 시작 전송 실패: " + request.error);
            callback?.Invoke(false, request.error);
        }

        request.Dispose();
    }

    private IEnumerator PostGameEnd(int stairCount, Action<bool, string> callback)
    {
        GameEndRequest requestData = new GameEndRequest
        {
            status = 0,
            stairCount = stairCount
        };

        string jsonData = JsonConvert.SerializeObject(requestData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        UnityWebRequest request = new UnityWebRequest(baseURL + "/game/end", "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
        bool isSuccess = request.result == UnityWebRequest.Result.Success;
#else
        bool isSuccess = !request.isNetworkError && !request.isHttpError;
#endif

        if (isSuccess)
        {
            Debug.Log("게임 종료 전송 성공: " + request.downloadHandler.text);

            try
            {
                GameResponse response = JsonConvert.DeserializeObject<GameResponse>(request.downloadHandler.text);
                callback?.Invoke(true, request.downloadHandler.text);
            }
            catch (Exception e)
            {
                Debug.LogError("응답 파싱 에러: " + e.Message);
                callback?.Invoke(false, e.Message);
            }
        }
        else
        {
            Debug.LogError("게임 종료 전송 실패: " + request.error);
            callback?.Invoke(false, request.error);
        }

        request.Dispose();
    }

    private IEnumerator PostScore(int score, int characterIndex, int money, Action<bool, ScoreData> callback)
    {
        ScoreSubmitRequest requestData = new ScoreSubmitRequest
        {
            score = score,
            characterIndex = characterIndex,
            money = money,
            timestamp = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };

        string jsonData = JsonConvert.SerializeObject(requestData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        UnityWebRequest request = new UnityWebRequest(baseURL + "/score/submit", "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
        bool isSuccess = request.result == UnityWebRequest.Result.Success;
#else
        bool isSuccess = !request.isNetworkError && !request.isHttpError;
#endif

        if (isSuccess)
        {
            Debug.Log("점수 제출 성공: " + request.downloadHandler.text);

            try
            {
                GameResponse response = JsonConvert.DeserializeObject<GameResponse>(request.downloadHandler.text);

                // data를 ScoreData로 변환
                ScoreData scoreData = null;
                if (response.data != null)
                {
                    string dataJson = JsonConvert.SerializeObject(response.data);
                    scoreData = JsonConvert.DeserializeObject<ScoreData>(dataJson);
                }

                callback?.Invoke(true, scoreData);
            }
            catch (Exception e)
            {
                Debug.LogError("응답 파싱 에러: " + e.Message);
                callback?.Invoke(false, null);
            }
        }
        else
        {
            Debug.LogError("점수 제출 실패: " + request.error);
            callback?.Invoke(false, null);
        }

        request.Dispose();
    }

    /// <summary>
    /// 베이스 URL 설정 (필요시 사용)
    /// </summary>
    public void SetBaseURL(string url)
    {
        baseURL = url;
    }
}
