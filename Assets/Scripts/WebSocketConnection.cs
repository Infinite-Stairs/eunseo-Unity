using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if !UNITY_WEBGL || UNITY_EDITOR
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
#endif

public class WebSocketConnection : MonoBehaviour
{
#if !UNITY_WEBGL || UNITY_EDITOR
    private ClientWebSocket websocket;
    private CancellationTokenSource cancellationTokenSource;
#endif

    private string serverUrl;
    private bool isConnected = false;

    public bool IsConnected => isConnected;

    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<string> OnMessageReceived;
    public event Action<string> OnError;

    public void Connect(string url)
    {
        serverUrl = url;
        StartCoroutine(ConnectCoroutine());
    }

    private IEnumerator ConnectCoroutine()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        // 기존 연결 정리
        if (websocket != null)
        {
            if (websocket.State == WebSocketState.Open)
            {
                var closeTask = websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                yield return new WaitUntil(() => closeTask.IsCompleted);
            }
            websocket.Dispose();
        }

        cancellationTokenSource = new CancellationTokenSource();
        websocket = new ClientWebSocket();

        Debug.Log($"WebSocket 연결 시도: {serverUrl}");

        var connectTask = websocket.ConnectAsync(new Uri(serverUrl), cancellationTokenSource.Token);

        yield return new WaitUntil(() => connectTask.IsCompleted);

        if (connectTask.IsFaulted)
        {
            string errorMsg = connectTask.Exception?.InnerException?.Message ?? "Unknown error";
            Debug.LogError($"WebSocket 연결 실패: {errorMsg}");
            OnError?.Invoke(errorMsg);
            isConnected = false;
            yield break;
        }

        Debug.Log("WebSocket 연결 성공!");
        isConnected = true;
        OnConnected?.Invoke();

        // 메시지 수신 루프 시작
        StartCoroutine(ReceiveCoroutine());
#else
        Debug.LogWarning("WebGL에서는 JavaScript WebSocket을 사용해야 합니다.");
        yield break;
#endif
    }

#if !UNITY_WEBGL || UNITY_EDITOR
    private IEnumerator ReceiveCoroutine()
    {
        var buffer = new byte[4096];

        while (websocket != null && websocket.State == WebSocketState.Open)
        {
            var receiveTask = websocket.ReceiveAsync(
                new ArraySegment<byte>(buffer),
                cancellationTokenSource.Token
            );

            yield return new WaitUntil(() => receiveTask.IsCompleted);

            if (receiveTask.IsFaulted)
            {
                if (!cancellationTokenSource.IsCancellationRequested)
                {
                    string errorMsg = receiveTask.Exception?.InnerException?.Message ?? "Unknown error";
                    Debug.LogError($"WebSocket 수신 에러: {errorMsg}");
                    OnError?.Invoke(errorMsg);
                }
                break;
            }

            var result = receiveTask.Result;

            if (result.MessageType == WebSocketMessageType.Close)
            {
                Debug.Log("WebSocket 서버에서 연결 종료");
                isConnected = false;
                OnDisconnected?.Invoke();
                break;
            }

            if (result.MessageType == WebSocketMessageType.Text)
            {
                string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                Debug.Log($"WebSocket 수신: {message}");
                OnMessageReceived?.Invoke(message);
            }
        }

        isConnected = false;
    }
#endif

    public void Send(string message)
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (websocket?.State != WebSocketState.Open)
        {
            Debug.LogError("WebSocket이 연결되지 않았습니다.");
            return;
        }

        StartCoroutine(SendCoroutine(message));
#endif
    }

#if !UNITY_WEBGL || UNITY_EDITOR
    private IEnumerator SendCoroutine(string message)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(message);
        var sendTask = websocket.SendAsync(
            new ArraySegment<byte>(bytes),
            WebSocketMessageType.Text,
            true,
            cancellationTokenSource.Token
        );

        yield return new WaitUntil(() => sendTask.IsCompleted);

        if (sendTask.IsFaulted)
        {
            string errorMsg = sendTask.Exception?.InnerException?.Message ?? "Unknown error";
            Debug.LogError($"WebSocket 전송 실패: {errorMsg}");
            OnError?.Invoke(errorMsg);
        }
        else
        {
            Debug.Log($"WebSocket 전송: {message}");
        }
    }
#endif

    public void Close()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        if (websocket == null) return;

        isConnected = false;
        cancellationTokenSource?.Cancel();

        StartCoroutine(CloseCoroutine());
#endif
    }

#if !UNITY_WEBGL || UNITY_EDITOR
    private IEnumerator CloseCoroutine()
    {
        if (websocket != null && websocket.State == WebSocketState.Open)
        {
            var closeTask = websocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            yield return new WaitUntil(() => closeTask.IsCompleted);
        }

        websocket?.Dispose();
        websocket = null;

        Debug.Log("WebSocket 연결 종료");
        OnDisconnected?.Invoke();
    }
#endif

    public void Reconnect()
    {
        Close();
        StartCoroutine(ReconnectCoroutine());
    }

    private IEnumerator ReconnectCoroutine()
    {
        yield return new WaitForSeconds(1f);
        Connect(serverUrl);
    }

    void OnDestroy()
    {
        Close();
    }
}
