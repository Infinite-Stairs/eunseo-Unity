using UnityEngine;

public class InputMapper : MonoBehaviour
{
    // 컨트롤러 버튼 12개 (실제 컨트롤러 버튼 번호는 테스트 후 수정 필요)
    private KeyCode[] controllerButtons = new KeyCode[12] {
        KeyCode.JoystickButton4,  // L 버튼
        KeyCode.JoystickButton5,  // R 버튼
        KeyCode.JoystickButton2,  // X 버튼
        KeyCode.JoystickButton3,  // Y 버튼
        KeyCode.JoystickButton0,  // A 버튼
        KeyCode.JoystickButton1,  // B 버튼
        KeyCode.JoystickButton8,  // Select 버튼
        KeyCode.JoystickButton9,  // Start 버튼
        KeyCode.JoystickButton11, // Up 버튼
        KeyCode.JoystickButton12, // Down 버튼
        KeyCode.JoystickButton13, // Left 버튼
        KeyCode.JoystickButton14  // Right 버튼
    };

    // 매핑될 키보드 키들
    public enum ControllerButton
    {
        L = 0,
        R = 1,
        X = 2,
        Y = 3,
        A = 4,
        B = 5,
        Select = 6,
        Start = 7,
        Up = 8,
        Down = 9,
        Left = 10,
        Right = 11
    }

    // 현재 프레임의 입력 상태
    private bool[] currentButtonState = new bool[12];
    private bool[] previousButtonState = new bool[12];

    // 디버그 모드: true로 설정하면 컨트롤러 버튼 눌림을 콘솔에 출력
    [SerializeField] private bool debugMode = true;

    // 버튼 이름 매핑 (더 식별하기 쉽게)
    private readonly string[] buttonNames = new string[12] {
        "L (왼쪽 상단)",
        "R (오른쪽 상단)",
        "X (왼쪽)",
        "Y (위쪽)",
        "A (아래쪽)",
        "B (오른쪽)",
        "Select (선택)",
        "Start (시작)",
        "Up (D-Pad 위)",
        "Down (D-Pad 아래)",
        "Left (D-Pad 왼쪽)",
        "Right (D-Pad 오른쪽)"
    };

    void Update()
    {
        // 모든 컨트롤러 버튼 체크
        for (int i = 0; i < 12; i++)
        {
            previousButtonState[i] = currentButtonState[i];
            currentButtonState[i] = Input.GetKey(controllerButtons[i]);

            // 디버그 모드일 때 버튼 눌림 출력
            if (debugMode && currentButtonState[i] && !previousButtonState[i])
            {
                Debug.Log($"[InputMapper] ▶ 버튼 눌림: {buttonNames[i]} | KeyCode: JoystickButton{controllerButtons[i] - KeyCode.JoystickButton0} | Time: {Time.time:F2}s");
            }

            // 버튼 뗌 로그
            if (debugMode && !currentButtonState[i] && previousButtonState[i])
            {
                Debug.Log($"[InputMapper] ◀ 버튼 뗌: {buttonNames[i]} | Time: {Time.time:F2}s");
            }
        }

        // 디버그: 모든 조이스틱 버튼 감지 (어떤 버튼이 몇 번인지 확인용)
        if (debugMode)
        {
            for (int i = 0; i < 20; i++)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.JoystickButton0 + i)))
                {
                    Debug.Log($"[InputMapper] 🎮 Raw 입력 감지: JoystickButton{i} | Time: {Time.time:F2}s");
                }
            }
        }
    }

    /// <summary>
    /// 컨트롤러 버튼이 눌려있는지 체크
    /// </summary>
    public bool GetButton(ControllerButton button)
    {
        int index = (int)button;
        if (index >= 0 && index < 12)
        {
            return currentButtonState[index];
        }
        return false;
    }

    /// <summary>
    /// 컨트롤러 버튼이 눌린 순간 체크
    /// </summary>
    public bool GetButtonDown(ControllerButton button)
    {
        int index = (int)button;
        if (index >= 0 && index < 12)
        {
            return currentButtonState[index] && !previousButtonState[index];
        }
        return false;
    }

    /// <summary>
    /// 컨트롤러 버튼을 뗀 순간 체크
    /// </summary>
    public bool GetButtonUp(ControllerButton button)
    {
        int index = (int)button;
        if (index >= 0 && index < 12)
        {
            return !currentButtonState[index] && previousButtonState[index];
        }
        return false;
    }
}
