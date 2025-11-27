using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

// WebSocket용 요청 (state 필드: bool)
[Serializable]
public class GameStateRequest
{
    public bool state; // true: 게임 시작, false: 게임 종료
}

[Serializable]
public class ScoreSubmitRequest
{
    public int stairCount;  // 계단 수
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
    // API 호출용 백엔드 서버 URL
    private string backendURL = "http://192.168.147.60:8000";

    // WebSocket URL (ws로 변환)
    private string websocketURL = "ws://192.168.147.60:8000/ws/unity";

    // 대시보드 열기용 프론트엔드 URL
    private string frontendURL = "https://dowhile001.vercel.app";

    // WebSocket 연결
    private WebSocketConnection wsConnection;

    void Awake()
    {
        gameObject.name = "ReceiverObject";
        DontDestroyOnLoad(gameObject);
    }

    public void OnReturnFromDashboard(string data)
    {
        Debug.Log("대시보드에서 돌아옴: " + data);
    }

    public void OnStartGameFromWeb(string data)
    {
        Debug.Log("웹에서 게임 시작 요청: " + data);
        SendGameStart();
    }

    public void ReceiveMessage(string message)
    {
        Debug.Log("웹에서 메시지 수신: " + message);
    }

    void Start()
    {
        ConnectWebSocket();
    }

    /// <summary>
    /// WebSocket 연결
    /// </summary>
    public void ConnectWebSocket()
    {
        if (wsConnection == null)
        {
            wsConnection = gameObject.AddComponent<WebSocketConnection>();
            wsConnection.OnConnected += OnWebSocketConnected;
            wsConnection.OnError += OnWebSocketError;
        }
        wsConnection.Connect(websocketURL);
    }

    private void OnWebSocketConnected()
    {
        Debug.Log("✅ WebSocket 연결 성공!");
    }

    private void OnWebSocketError(string error)
    {
        Debug.LogError($"❌ WebSocket 에러: {error}");
        if (gameObject != null && gameObject.activeInHierarchy)
        {
            StartCoroutine(ReconnectAfterDelay(3f));
        }
    }

    private IEnumerator ReconnectAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log("🔄 WebSocket 재연결 시도...");
        ConnectWebSocket();
    }

    /// <summary>
    /// 게임 시작 - WebSocket 우선, HTTP API 백업
    /// </summary>
    public void SendGameStart(Action<bool, string> callback = null)
    {
        StartCoroutine(SendGameStartCoroutine(callback));
    }

    private IEnumerator SendGameStartCoroutine(Action<bool, string> callback)
    {
        Debug.Log("========================================");
        Debug.Log("🎮 게임 시작 신호 전송 시작");
        Debug.Log("========================================");

        bool wsSuccess = false;

        // 1. WebSocket으로 전송 시도 (우선)
        if (wsConnection != null && wsConnection.IsConnected)
        {
            try
            {
                var wsRequest = new GameStateRequest { state = true };
                string wsJsonData = JsonConvert.SerializeObject(wsRequest);
                wsConnection.Send(wsJsonData);
                Debug.Log("✅ [WebSocket] 게임 시작 전송 성공");
                Debug.Log($"   전송 데이터: {wsJsonData}");
                wsSuccess = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ [WebSocket] 전송 실패: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ [WebSocket] 연결되지 않음, HTTP API로 전환");
        }

        // 2. HTTP API로 전송 (백업 또는 추가)
        string url = backendURL + "/api/game/start";
        Debug.Log($"📡 [HTTP API] 요청 URL: {url}");

        var httpRequest = new GameStateRequest { state = true };
        string jsonData = JsonConvert.SerializeObject(httpRequest);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        Debug.Log($"   요청 Body: {jsonData}");

        yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
        bool isSuccess = request.result == UnityWebRequest.Result.Success;
#else
        bool isSuccess = !request.isNetworkError && !request.isHttpError;
#endif

        Debug.Log($"📊 [HTTP API] 응답 코드: {request.responseCode}");
        Debug.Log($"   응답 내용: {request.downloadHandler?.text}");

        if (isSuccess)
        {
            Debug.Log("✅ [HTTP API] 게임 시작 전송 성공");
            callback?.Invoke(true, "게임 시작 성공");
        }
        else
        {
            Debug.LogError($"❌ [HTTP API] 게임 시작 전송 실패");
            Debug.LogError($"   에러: {request.error}");

            // WebSocket이 성공했다면 전체적으로는 성공으로 간주
            if (wsSuccess)
            {
                callback?.Invoke(true, "게임 시작 성공 (WebSocket)");
            }
            else
            {
                callback?.Invoke(false, request.error);
            }
        }

        Debug.Log("========================================\n");
        request.Dispose();
    }

    /// <summary>
    /// 게임 종료 - WebSocket 우선, HTTP API 백업
    /// </summary>
    public void SendGameEnd(int stairCount, Action<bool, string> callback = null)
    {
        StartCoroutine(SendGameEndCoroutine(stairCount, callback));
    }

    private IEnumerator SendGameEndCoroutine(int stairCount, Action<bool, string> callback)
    {
        Debug.Log("========================================");
        Debug.Log("🛑 게임 종료 신호 전송 시작");
        Debug.Log($"   최종 점수: {stairCount}");
        Debug.Log("========================================");

        bool wsSuccess = false;

        // 1. WebSocket으로 전송 시도 (우선)
        if (wsConnection != null && wsConnection.IsConnected)
        {
            try
            {
                var wsRequest = new GameStateRequest { state = false };
                string wsJsonData = JsonConvert.SerializeObject(wsRequest);
                wsConnection.Send(wsJsonData);
                Debug.Log("✅ [WebSocket] 게임 종료 전송 성공");
                Debug.Log($"   전송 데이터: {wsJsonData}");
                wsSuccess = true;
            }
            catch (Exception e)
            {
                Debug.LogError($"❌ [WebSocket] 전송 실패: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning("⚠️ [WebSocket] 연결되지 않음, HTTP API로 전환");
        }

        // 2. HTTP API로 전송 (백업 또는 추가)
        ScoreSubmitRequest requestData = new ScoreSubmitRequest
        {
            stairCount = stairCount
        };

        string jsonData = JsonConvert.SerializeObject(requestData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        string url = backendURL + "/api/game/end";
        Debug.Log($"📡 [HTTP API] 요청 URL: {url}");
        Debug.Log($"   요청 Body: {jsonData}");

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
        bool isSuccess = request.result == UnityWebRequest.Result.Success;
#else
        bool isSuccess = !request.isNetworkError && !request.isHttpError;
#endif

        Debug.Log($"📊 [HTTP API] 응답 코드: {request.responseCode}");
        Debug.Log($"   응답 내용: {request.downloadHandler?.text}");

        if (isSuccess)
        {
            Debug.Log("✅ [HTTP API] 게임 종료 전송 성공");
            callback?.Invoke(true, "게임 종료 성공");
        }
        else
        {
            Debug.LogError($"❌ [HTTP API] 게임 종료 전송 실패");
            Debug.LogError($"   에러: {request.error}");

            // WebSocket이 성공했다면 전체적으로는 성공으로 간주
            if (wsSuccess)
            {
                callback?.Invoke(true, "게임 종료 성공 (WebSocket)");
            }
            else
            {
                callback?.Invoke(false, request.error);
            }
        }

        Debug.Log("========================================\n");
        request.Dispose();
    }

    /// <summary>
    /// 게임 점수 제출 - HTTP POST
    /// </summary>
    public void SubmitScore(int stairCount, Action<bool, ScoreData> callback = null)
    {
        StartCoroutine(PostScore(stairCount, callback));
    }

    private IEnumerator PostScore(int stairCount, Action<bool, ScoreData> callback)
    {
        ScoreSubmitRequest requestData = new ScoreSubmitRequest
        {
            stairCount = stairCount
        };

        string jsonData = JsonConvert.SerializeObject(requestData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        UnityWebRequest request = new UnityWebRequest(backendURL + "/api/game/score/submit", "POST");
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
            Debug.Log("✅ 점수 제출 성공: " + request.downloadHandler.text);

            try
            {
                GameResponse response = JsonConvert.DeserializeObject<GameResponse>(request.downloadHandler.text);

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
                Debug.LogError("❌ 응답 파싱 에러: " + e.Message);
                callback?.Invoke(false, null);
            }
        }
        else
        {
            Debug.LogError("❌ 점수 제출 실패: " + request.error);
            Debug.LogError($"   HTTP 상태 코드: {request.responseCode}");
            Debug.LogError($"   응답 내용: {request.downloadHandler.text}");
            callback?.Invoke(false, null);
        }

        request.Dispose();
    }

    public void SetBackendURL(string url)
    {
        backendURL = url;
    }

    public void SetWebSocketURL(string url)
    {
        websocketURL = url;
    }

    public void SetFrontendURL(string url)
    {
        frontendURL = url;
    }

    public void OpenDashboard()
    {
        Application.OpenURL(frontendURL);
        Debug.Log("🌐 대시보드 열기: " + frontendURL);
    }

    void OnDestroy()
    {
        if (wsConnection != null)
        {
            wsConnection.OnConnected -= OnWebSocketConnected;
            wsConnection.OnError -= OnWebSocketError;
            wsConnection.Close();
        }
    }
}