using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

[Serializable]
public class GameStartRequest {
    public int status; // 1: 게임 시작
}

[Serializable]
public class GameEndRequest {
    public int status;      // 0: 게임 종료
    public int stairCount;  // 계단 수
}

[Serializable]
public class GameResponse {
    public bool success;
    public string message;
    public object data;
}

public class APIManager : MonoBehaviour {
    // TODO: 백엔드 서버 URL로 변경해주세요
    private string baseURL = "http://localhost:3000/api";

    // 게임 세션 ID (백엔드에서 받아올 수도 있습니다)
    private string currentSessionId;

    void Awake() {
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 게임 시작 시 호출 - 상태값 1 전송
    /// </summary>
    public void SendGameStart(Action<bool, string> callback = null) {
        StartCoroutine(PostGameStart(callback));
    }

    /// <summary>
    /// 게임 종료 시 호출 - 상태값 0과 계단 수 전송
    /// </summary>
    /// <param name="stairCount">도달한 계단 수</param>
    public void SendGameEnd(int stairCount, Action<bool, string> callback = null) {
        StartCoroutine(PostGameEnd(stairCount, callback));
    }

    private IEnumerator PostGameStart(Action<bool, string> callback) {
        GameStartRequest requestData = new GameStartRequest {
            status = 1
        };

        string jsonData = JsonConvert.SerializeObject(requestData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        UnityWebRequest request = new UnityWebRequest(baseURL + "/game/start", "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success) {
            Debug.Log("게임 시작 전송 성공: " + request.downloadHandler.text);

            // 응답에서 세션 ID를 받아올 수 있습니다
            try {
                GameResponse response = JsonConvert.DeserializeObject<GameResponse>(request.downloadHandler.text);
                callback?.Invoke(true, request.downloadHandler.text);
            } catch (Exception e) {
                Debug.LogError("응답 파싱 에러: " + e.Message);
                callback?.Invoke(false, e.Message);
            }
        } else {
            Debug.LogError("게임 시작 전송 실패: " + request.error);
            callback?.Invoke(false, request.error);
        }

        request.Dispose();
    }

    private IEnumerator PostGameEnd(int stairCount, Action<bool, string> callback) {
        GameEndRequest requestData = new GameEndRequest {
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

        if (request.result == UnityWebRequest.Result.Success) {
            Debug.Log("게임 종료 전송 성공: " + request.downloadHandler.text);

            try {
                GameResponse response = JsonConvert.DeserializeObject<GameResponse>(request.downloadHandler.text);
                callback?.Invoke(true, request.downloadHandler.text);
            } catch (Exception e) {
                Debug.LogError("응답 파싱 에러: " + e.Message);
                callback?.Invoke(false, e.Message);
            }
        } else {
            Debug.LogError("게임 종료 전송 실패: " + request.error);
            callback?.Invoke(false, request.error);
        }

        request.Dispose();
    }

    /// <summary>
    /// 베이스 URL 설정 (필요시 사용)
    /// </summary>
    public void SetBaseURL(string url) {
        baseURL = url;
    }
}
