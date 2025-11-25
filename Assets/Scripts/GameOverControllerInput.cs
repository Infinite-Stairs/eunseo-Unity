using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GameStart 씬(게임오버 화면)에서 컨트롤러 입력을 처리하는 스크립트
/// RestartBtn을 Select 버튼으로 매핑
/// </summary>
public class GameOverControllerInput : MonoBehaviour
{
    [Header("UI 버튼 참조")]
    public Button restartButton;  // Select 버튼 (JoystickButton8)에 매핑

    [Header("디버그 설정")]
    public bool debugMode = true;

    void Start()
    {
        // 버튼 참조 확인
        if (debugMode)
        {
            if (restartButton == null)
                Debug.LogWarning("[GameOverControllerInput] RestartButton이 연결되지 않았습니다!");
            else
                Debug.Log($"[GameOverControllerInput] RestartButton 연결됨: {restartButton.name}");
        }
    }

    void Update()
    {
        // Select 버튼 (JoystickButton8) → RestartButton 클릭
        if (Input.GetKeyDown(KeyCode.JoystickButton8))
        {
            if (restartButton != null && restartButton.interactable)
            {
                restartButton.onClick.Invoke();
                if (debugMode)
                {
                    Debug.Log($"[GameOverControllerInput] Select 버튼으로 RestartButton 클릭 | Time: {Time.time:F2}s");
                }
            }
        }

        // 디버그: 모든 조이스틱 버튼 감지
        if (debugMode)
        {
            for (int i = 0; i < 20; i++)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.JoystickButton0 + i)))
                {
                    Debug.Log($"[GameOverControllerInput] 🎮 Raw 입력 감지: JoystickButton{i} | Time: {Time.time:F2}s");
                }
            }
        }
    }
}
