using UnityEngine;
using UnityEngine.UI;

public class URLButton : MonoBehaviour
{
    public string url = "https://hanseong-front-s1gv.vercel.app";
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
        Application.OpenURL(url);
    }
}
