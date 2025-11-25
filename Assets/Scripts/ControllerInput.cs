using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MainMenu 씬에서 컨트롤러 입력을 처리하는 스크립트
/// URLButton을 Select 버튼으로, StartBtn을 Start 버튼으로 매핑
/// </summary>
/// 
public class ControllerInput : MonoBehaviour
{
    [Header("UI 버튼 참조")]
    public Button urlButton;      // Select 버튼 (JoystickButton8)에 매핑
    public Button startButton;    // Start 버튼 (JoystickButton9)에 매핑
    public Button restartButton;  // 재시작 버튼 (필요 시 사용 가능)

    [Header("디버그 설정")]
    public bool debugMode = true;

    void Start()
    {
        // 버튼 참조 확인
        if (debugMode)
        {
            if (urlButton == null)
                Debug.LogWarning("[ControllerInput] URLButton이 연결되지 않았습니다!");
            else
                Debug.Log($"[ControllerInput] URLButton 연결됨: {urlButton.name}");

            if (startButton == null)
                Debug.LogWarning("[ControllerInput] StartButton이 연결되지 않았습니다!");
            else
                Debug.Log($"[ControllerInput] StartButton 연결됨: {startButton.name}");
            if (restartButton == null)
                Debug.LogWarning("[ControllerInput] RestartButton이 연결되지 않았습니다!");
            else
                Debug.Log($"[ControllerInput] RestartButton 연결됨: {restartButton.name}");
        }
    }

    void Update()
    {
        // Select 버튼 (JoystickButton8) → URLButton 클릭
        if (Input.GetKeyDown(KeyCode.JoystickButton8))
        {
            if (urlButton != null && urlButton.interactable)
            {
                urlButton.onClick.Invoke();
                if (debugMode)
                {
                    Debug.Log($"[ControllerInput] Select 버튼으로 URLButton 클릭 | Time: {Time.time:F2}s");
                }
            }
        }

        // Start 버튼 (JoystickButton9) → StartButton 클릭
        if (Input.GetKeyDown(KeyCode.JoystickButton9))
        {
            if (startButton != null && startButton.interactable)
            {
                startButton.onClick.Invoke();
                if (debugMode)
                {
                    Debug.Log($"[ControllerInput] Start 버튼으로 StartButton 클릭 | Time: {Time.time:F2}s");
                }
            }
            // if (restartButton != null && restartButton.interactable)
            // {
            //     restartButton.onClick.Invoke();
            //     if (debugMode)
            //     {
            //         Debug.Log($"[ControllerInput] Start 버튼으로 RestartButton 클릭 | Time: {Time.time:F2}s");
            //     }
            // }
        }

        // 디버그: 모든 조이스틱 버튼 감지
        if (debugMode)
        {
            for (int i = 0; i < 20; i++)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.JoystickButton0 + i)))
                {
                    Debug.Log($"[ControllerInput] 🎮 Raw 입력 감지: JoystickButton{i} | Time: {Time.time:F2}s");
                }
            }
        }
    }
}
