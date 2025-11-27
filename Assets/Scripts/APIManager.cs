using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

// WebSocket용 요청 (state 필드 수정: int -> bool)
[Serializable]
public class GameStateRequest
{
    // 서버 요구사항에 맞춰 int(0, 1)에서 bool(false, true)로 변경
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
        // 프론트엔드에서 유니티로 메시지를 보내려면 "ReceiverObject"라는 이름의 GameObject가 필요함
        // JavaScript에서 SendMessage("ReceiverObject", "MethodName", "data") 형태로 호출함
        gameObject.name = "ReceiverObject";
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 프론트엔드(JavaScript)에서 유니티로 돌아올 때 호출되는 메서드
    /// JavaScript에서 호출: unityInstance.SendMessage("ReceiverObject", "OnReturnFromDashboard", "data");
    /// </summary>
    public void OnReturnFromDashboard(string data)
    {
        Debug.Log("대시보드에서 돌아옴: " + data);
        // 필요한 경우 여기서 추가 처리 (예: 게임 재시작, UI 업데이트 등)
    }

    /// <summary>
    /// 프론트엔드에서 게임 시작을 요청할 때 호출
    /// JavaScript에서 호출: unityInstance.SendMessage("ReceiverObject", "OnStartGameFromWeb", "");
    /// </summary>
    public void OnStartGameFromWeb(string data)
    {
        Debug.Log("웹에서 게임 시작 요청: " + data);
        // 게임 시작 로직 호출
        SendGameStart();
    }

    /// <summary>
    /// 프론트엔드에서 전달하는 일반 메시지 수신
    /// JavaScript에서 호출: unityInstance.SendMessage("ReceiverObject", "ReceiveMessage", "your message");
    /// </summary>
    public void ReceiveMessage(string message)
    {
        Debug.Log("웹에서 메시지 수신: " + message);
    }

    void Start()
    {
        // WebSocket 연결 시작
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

    /// <summary>
    /// WebSocket 연결 성공 callback
    /// </summary>
    private void OnWebSocketConnected()
    {
        Debug.Log("WebSocket 연결됨!");
    }

    private void OnWebSocketError(string error)
    {
        Debug.LogError($"WebSocket 에러: {error}");
        // 3초 후 재연결 시도 (오브젝트가 활성화되어 있을 때만)
        if (gameObject != null && gameObject.activeInHierarchy)
        {
            StartCoroutine(ReconnectAfterDelay(3f));
        }
    }

    private IEnumerator ReconnectAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log("WebSocket 재연결 시도...");
        ConnectWebSocket();
    }

    /// <summary>
    /// 게임 시작 시 호출 - HTTP API + WebSocket 둘 다 호출
    /// </summary>
    public void SendGameStart(Action<bool, string> callback = null)
    {
        StartCoroutine(SendGameStartCoroutine(callback));
    }

    private IEnumerator SendGameStartCoroutine(Action<bool, string> callback)
    {
        // 1. HTTP API 호출 (백엔드 game_handler.is_playing = True 설정)
        string url = backendURL + "/api/game/start";
        Debug.Log($"[API 호출 시작] URL: {url}");
        Debug.Log($"[API 호출 시작] backendURL: {backendURL}");

        // 빈 JSON 객체를 body로 전송
        string jsonData = "{}";
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        Debug.Log($"[API 호출 전] 요청 정보 - Method: POST, Headers: Content-Type=application/json, Body: {jsonData}");

        yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
        bool isSuccess = request.result == UnityWebRequest.Result.Success;
#else
        bool isSuccess = !request.isNetworkError && !request.isHttpError;
#endif

        Debug.Log($"[API 호출 완료] HTTP 상태 코드: {request.responseCode}");
        Debug.Log($"[API 호출 완료] Result: {request.result}");
        Debug.Log($"[API 호출 완료] Error: {request.error}");
        Debug.Log($"[API 호출 완료] Response: {request.downloadHandler?.text}");

        if (isSuccess)
        {
            Debug.Log("✅ 게임 시작 API 호출 성공: " + request.downloadHandler.text);

            // 2. WebSocket으로도 전송 (라즈베리파이에 알림)
            if (wsConnection != null && wsConnection.IsConnected)
            {
                // 수정됨: state = 1 대신 state = true 전송
                var wsRequest = new GameStateRequest { state = true };
                string wsJsonData = JsonConvert.SerializeObject(wsRequest);
                wsConnection.Send(wsJsonData);
                Debug.Log("게임 시작 WebSocket 전송: " + wsJsonData);
            }

            callback?.Invoke(true, "게임 시작 성공");
        }
        else
        {
            Debug.LogError($"❌ 게임 시작 API 호출 실패");
            Debug.LogError($"    - URL: {backendURL + "/api/game/start"}");
            Debug.LogError($"    - HTTP 상태 코드: {request.responseCode}");
            Debug.LogError($"    - 에러 메시지: {request.error}");
            Debug.LogError($"    - 응답 내용: {request.downloadHandler?.text}");
            callback?.Invoke(false, request.error);
        }

        request.Dispose();
    }

    /// <summary>
    /// 게임 종료 시 호출 - HTTP API + WebSocket 둘 다 호출
    /// </summary>
    public void SendGameEnd(int stairCount, Action<bool, string> callback = null)
    {
        StartCoroutine(SendGameEndCoroutine(stairCount, callback));
    }

    private IEnumerator SendGameEndCoroutine(int stairCount, Action<bool, string> callback)
    {
        // 1. HTTP API 호출 (백엔드 game_handler.is_playing = False 설정)
        // stairCount를 JSON body로 전송
        ScoreSubmitRequest requestData = new ScoreSubmitRequest
        {
            stairCount = stairCount
        };

        string jsonData = JsonConvert.SerializeObject(requestData);
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        string url = backendURL + "/api/game/end";
        Debug.Log($"[API 호출 시작] URL: {url}");
        Debug.Log($"[API 호출 시작] backendURL: {backendURL}");
        Debug.Log($"[API 호출 시작] Request Body: {jsonData}");

        UnityWebRequest request = new UnityWebRequest(url, "POST");
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        Debug.Log($"[API 호출 전] 요청 정보 - Method: POST, Headers: Content-Type=application/json");

        yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
        bool isSuccess = request.result == UnityWebRequest.Result.Success;
#else
        bool isSuccess = !request.isNetworkError && !request.isHttpError;
#endif

        Debug.Log($"[API 호출 완료] HTTP 상태 코드: {request.responseCode}");
        Debug.Log($"[API 호출 완료] Result: {request.result}");
        Debug.Log($"[API 호출 완료] Error: {request.error}");
        Debug.Log($"[API 호출 완료] Response: {request.downloadHandler?.text}");

        if (isSuccess)
        {
            Debug.Log("✅ 게임 종료 API 호출 성공: " + request.downloadHandler.text);

            // 2. WebSocket으로도 전송 (라즈베리파이에 알림)
            if (wsConnection != null && wsConnection.IsConnected)
            {
                // 수정됨: state = 0 대신 state = false 전송
                var wsRequest = new GameStateRequest { state = false };
                string wsJsonData = JsonConvert.SerializeObject(wsRequest);
                wsConnection.Send(wsJsonData);
                Debug.Log("게임 종료 WebSocket 전송: " + wsJsonData);
            }

            callback?.Invoke(true, "게임 종료 성공");
        }
        else
        {
            Debug.LogError($"❌ 게임 종료 API 호출 실패");
            Debug.LogError($"    - URL: {backendURL + "/api/game/end"}");
            Debug.LogError($"    - Request Body: {jsonData}");
            Debug.LogError($"    - HTTP 상태 코드: {request.responseCode}");
            Debug.LogError($"    - 에러 메시지: {request.error}");
            Debug.LogError($"    - 응답 내용: {request.downloadHandler?.text}");
            callback?.Invoke(false, request.error);
        }

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
            Debug.Log("점수 제출 성공: " + request.downloadHandler.text);

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
                Debug.LogError("응답 파싱 에러: " + e.Message);
                callback?.Invoke(false, null);
            }
        }
        else
        {
            Debug.LogError("점수 제출 실패: " + request.error);
            Debug.LogError("HTTP 상태 코드: " + request.responseCode);
            Debug.LogError("응답 내용: " + request.downloadHandler.text);
            callback?.Invoke(false, null);
        }

        request.Dispose();
    }

    /// <summary>
    /// 백엔드 URL 설정
    /// </summary>
    public void SetBackendURL(string url)
    {
        backendURL = url;
    }

    /// <summary>
    /// WebSocket URL 설정
    /// </summary>
    public void SetWebSocketURL(string url)
    {
        websocketURL = url;
    }

    /// <summary>
    /// 프론트엔드 URL 설정
    /// </summary>
    public void SetFrontendURL(string url)
    {
        frontendURL = url;
    }

    /// <summary>
    /// 대시보드(프론트엔드)를 브라우저에서 열기
    /// </summary>
    public void OpenDashboard()
    {
        Application.OpenURL(frontendURL);
        Debug.Log("대시보드 열기: " + frontendURL);
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