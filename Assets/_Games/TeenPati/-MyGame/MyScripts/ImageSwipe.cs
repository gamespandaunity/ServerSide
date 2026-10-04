using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageSwipe : MonoBehaviour
{

    public Scrollbar scrollbar;
    public GameObject dotObj;
    float scroll_pos = 0;
    float[] pos;
    int posisi = 0;
    public float lerpDelayTime = 0.1f;
    public Button nextBtn, prevBtn;
    // Start is called before the first frame update
    List<GameObject> dotList = new List<GameObject>();


    private void OnEnable()
    {

        nextBtn.onClick.AddListener(Next);
        prevBtn.onClick.AddListener(prev);
        nextBtn.gameObject.SetActive(true);
        prevBtn.gameObject.SetActive(false);
        Dots();
    }

    private void OnDisable()
    {
        scroll_pos = 0;
        posisi = 0;
        scrollbar.value = 0;
        if (pos.Length > 0)
        {
            Array.Clear(pos, 0, pos.Length);
            pos = new float[0];
        }

        foreach (var item in dotList)
        {
            Destroy(item);
        }
        dotList.Clear();
        nextBtn.onClick.RemoveListener(Next);
        prevBtn.onClick.RemoveListener(prev);
    }
    void Dots()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject dot = Instantiate(dotObj);
            LocalSettings.SetPosAndRect(dot, dotObj.GetComponentInChildren<RectTransform>(), dotObj.transform.parent);
            dotList.Add(dot);
            dotList[0].transform.GetChild(0).gameObject.SetActive(true);
            dot.SetActive(true);

        }
    }
    public void Next()
    {
        prevBtn.gameObject.SetActive(true);
        if (posisi < pos.Length - 1)
        {            
            posisi += 1;
            FilltDot(posisi);
            if (posisi >= pos.Length - 1)
            {
                nextBtn.gameObject.SetActive(false);
            }

            scroll_pos = pos[posisi];
        }
    }

    void FilltDot(int index)
    {
        // Debug.LogError("Here is your index: " + index);
        foreach (var item in dotList)
        {
            item.transform.GetChild(0).gameObject.SetActive(false);
        }
        dotList[index].transform.GetChild(0).gameObject.SetActive(true);
    }
    public void prev()
    {
        nextBtn.gameObject.SetActive(true);
        if (posisi > 0)
        {
            posisi -= 1;
            FilltDot(posisi);
            if (posisi <= 0)
            {
                prevBtn.gameObject.SetActive(false);
            }
            scroll_pos = pos[posisi];
        }

    }
    // Update is called once per frame
    void Update()
    {

        pos = new float[transform.childCount];
        float distance = 1f / (pos.Length - 1);
        for (int i = 0; i < pos.Length; i++)
        {
            pos[i] = distance * i;
        }
        if (Input.GetMouseButton(0))
        {
            //scroll_pos = scrollbar.value;
            // posisi = Mathf.RoundToInt(scroll_pos);

            //  FilltDot(posisi);
        }
        else
            for (int i = 0; i < pos.Length; i++)
            {
                if (scroll_pos < pos[i] + (distance / 2) && scroll_pos > pos[i] - (distance / 2))
                {
                    scrollbar.value = Mathf.Lerp(scrollbar.value, pos[i], lerpDelayTime);
                }
            }
    }
}
