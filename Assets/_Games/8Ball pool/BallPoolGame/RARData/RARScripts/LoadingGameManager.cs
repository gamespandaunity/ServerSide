using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LoadingGameManager : MonoBehaviour
{
    public float loadingTime;
    public Image fillImage;
    void Start()
    {
        StartCoroutine("Timer");
    }

    void Update()
    {
        fillImage.fillAmount += Time.deltaTime / loadingTime;
    }

    IEnumerator Timer()
    {
        yield return new WaitForSeconds(loadingTime);
        //print("loaded");
    }
}
