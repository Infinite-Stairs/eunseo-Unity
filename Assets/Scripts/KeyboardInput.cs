using UnityEngine;
using UnityEngine.UI;

public class KeyboardInput : MonoBehaviour
{
    [Header("게임 플레이 버튼 연결")]
    [Tooltip("J 키에 매핑할 오르기 버튼")]
    public Button climbButton;

    [Tooltip("K 키에 매핑할 방향 전환 버튼")]
    public Button changeDirButton;

    [Header("메뉴 버튼 연결")]
    [Tooltip("컨트롤러 Start 버튼에 매핑할 재시작 버튼 (게임 오버 시)")]
    public Button restartButton;

    [Tooltip("컨트롤러 L 버튼에 매핑할 URL 버튼")]
    public Button urlButton;

    [Header("키 설정")]
    public KeyCode climbKey = KeyCode.J;
    public KeyCode changeDirKey = KeyCode.K;

    [Header("디버그")]
    public bool debugMode = true;

    void Update()
    {
        // J 키 입력 체크 (오르기)
        if (Input.GetKeyDown(climbKey))
        {
            if (debugMode)
            {
                Debug.Log($"[KeyboardInput] {climbKey} 키 눌림 | Time: {Time.time:F2}s");
            }

            if (climbButton != null)
            {
                climbButton.onClick.Invoke();
                if (debugMode)
                {
                    Debug.Log($"[KeyboardInput] 오르기 버튼 클릭 호출: {climbButton.name}");
                }
            }
            else
            {
                Debug.LogWarning("[KeyboardInput] ClimbButton이 연결되지 않았습니다!");
            }
        }

        // K 키 입력 체크 (방향 전환)
        if (Input.GetKeyDown(changeDirKey))
        {
            if (debugMode)
            {
                Debug.Log($"[KeyboardInput] {changeDirKey} 키 눌림 | Time: {Time.time:F2}s");
            }

            if (changeDirButton != null)
            {
                changeDirButton.onClick.Invoke();
                if (debugMode)
                {
                    Debug.Log($"[KeyboardInput] 방향 전환 버튼 클릭 호출: {changeDirButton.name}");
                }
            }
            else
            {
                Debug.LogWarning("[KeyboardInput] ChangeDirButton이 연결되지 않았습니다!");
            }
        }

        // 컨트롤러 L 버튼 (JoystickButton4) - URL 열기
        if (Input.GetKeyDown(KeyCode.JoystickButton4))
        {
            if (debugMode)
            {
                Debug.Log($"[KeyboardInput] L 버튼 눌림 (JoystickButton4) - URL 열기 | Time: {Time.time:F2}s");
            }

            if (urlButton != null && urlButton.gameObject.activeInHierarchy && urlButton.interactable)
            {
                urlButton.onClick.Invoke();
                if (debugMode)
                {
                    Debug.Log($"[KeyboardInput] URL 버튼 클릭 호출: {urlButton.name}");
                }
            }
            else if (debugMode)
            {
                if (urlButton == null)
                    Debug.LogWarning("[KeyboardInput] URLButton이 연결되지 않았습니다!");
                else if (!urlButton.gameObject.activeInHierarchy)
                    Debug.Log("[KeyboardInput] URLButton이 비활성화 상태입니다.");
                else if (!urlButton.interactable)
                    Debug.Log("[KeyboardInput] URLButton이 상호작용 불가능 상태입니다.");
            }
        }

        // 컨트롤러 R 버튼 (JoystickButton5) - 방향 전환
        if (Input.GetKeyDown(KeyCode.JoystickButton5))
        {
            if (debugMode)
            {
                Debug.Log($"[KeyboardInput] R 버튼 눌림 (JoystickButton5) - 방향 전환 | Time: {Time.time:F2}s");
            }

            // ChangeDirButton의 onClick 이벤트 호출 (K 키와 동일하게 처리)
            if (changeDirButton != null)
            {
                changeDirButton.onClick.Invoke();
                if (debugMode)
                {
                    Debug.Log($"[KeyboardInput] 방향 전환 버튼 클릭 호출 (R 버튼): {changeDirButton.name}");
                }
            }
            else if (debugMode)
            {
                Debug.LogWarning("[KeyboardInput] ChangeDirButton이 연결되지 않았습니다!");
            }
        }

        // 컨트롤러 Start 버튼 (JoystickButton9) - 재시작 (게임 오버 시)
        if (Input.GetKeyDown(KeyCode.JoystickButton9))
        {
            if (debugMode)
            {
                Debug.Log($"[KeyboardInput] Start 버튼 눌림 (JoystickButton9) | Time: {Time.time:F2}s");
            }

            if (restartButton != null && restartButton.gameObject.activeInHierarchy && restartButton.interactable)
            {
                restartButton.onClick.Invoke();
                if (debugMode)
                {
                    Debug.Log($"[KeyboardInput] 재시작 버튼 클릭 호출: {restartButton.name}");
                }
            }
            else if (debugMode)
            {
                if (restartButton == null)
                    Debug.LogWarning("[KeyboardInput] RestartButton이 연결되지 않았습니다!");
                else if (!restartButton.gameObject.activeInHierarchy)
                    Debug.Log("[KeyboardInput] RestartButton이 비활성화 상태입니다.");
                else if (!restartButton.interactable)
                    Debug.Log("[KeyboardInput] RestartButton이 상호작용 불가능 상태입니다.");
            }
        }

        // 디버그: 모든 조이스틱 버튼 감지
        if (debugMode)
        {
            for (int i = 0; i < 20; i++)
            {
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.JoystickButton0 + i)))
                {
                    Debug.Log($"[KeyboardInput] 🎮 Raw 입력 감지: JoystickButton{i} | Time: {Time.time:F2}s");
                }
            }
        }
    }
}
