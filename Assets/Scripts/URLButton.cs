using UnityEngine;
using UnityEngine.UI;

public class URLButton : MonoBehaviour
{
    public string url = "https://dowhile001.vercel.app";
    public Sprite buttonImage;

    private void Start()
    {
        // 버튼 이미지 변경
        if (buttonImage != null)
        {
            Image image = GetComponent<Image>();
            if (image != null)
            {
                image.sprite = buttonImage;
            }
        }
    }

    public void OpenURL()
    {
        Debug.Log($"[URLButton] 버튼 클릭됨 - GameObject: {gameObject.name}, URL: {url}");
        Application.OpenURL(url);
        Debug.Log($"[URLButton] URL 열기 완료 - {url}");
    }

    /// <summary>
    /// 버튼 클릭 테스트용 - URL 열기 없이 로그만 출력
    /// </summary>
    public void TestClick()
    {
        Debug.Log($"[URLButton] 테스트 클릭 - GameObject: {gameObject.name}, Time: {Time.time}");
    }
}
