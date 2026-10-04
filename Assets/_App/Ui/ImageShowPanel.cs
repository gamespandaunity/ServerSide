using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageShowPanel : MonoBehaviour
{
    // Start is called before the first frame update
    public static ImageShowPanel instance;
    public GameObject ImagePanel;
    public Image ImageComponent;
    private void Awake()
    {
        instance = this;
    }
    void Start()
    {

    }
    public void ShowImage(Image image)
    {
        ImagePanel.SetActive(true);
        ImageComponent.sprite=image.sprite;
    }
    public void ShowImage(Sprite sprite)
    {
        ImagePanel.SetActive(true);
        ImageComponent.sprite = sprite;
    }
   public void Close()
    {
        ImagePanel.SetActive(false);
    }
}
