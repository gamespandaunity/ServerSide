using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterJumpEffect : MonoBehaviour
{
    private GameObject WaterEffect;
    // Start is called before the first frame update
    void Start()
    {
        WaterEffect = transform.GetChild(0).gameObject;
        WaterEffect.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.transform.root.CompareTag("Player") && other.gameObject.transform.root.GetComponent<PlayerPositionController>() != null)
        {
            if (WaterEffect)
            {
                WaterEffect.SetActive(true);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.gameObject.transform.root.CompareTag("Player") && other.gameObject.transform.root.GetComponent<PlayerPositionController>() != null)
        {
            if (WaterEffect)
            {
                WaterEffect.SetActive(false);
            }
        }
    }
}
