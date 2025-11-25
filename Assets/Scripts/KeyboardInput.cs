using UnityEngine;
using UnityEngine.UI;

public class KeyboardInput : MonoBehaviour
{
    [Header("버튼 연결")]
    [Tooltip("J 키에 매핑할 오르기 버튼")]
    public Button climbButton;

    [Tooltip("스위치에 매핑할 방향 전환 버튼")]
    public Button changeDirButton;

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
    }
}
