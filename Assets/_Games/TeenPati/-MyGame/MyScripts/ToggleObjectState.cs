using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToggleObjectState : MonoBehaviour
{
    public bool SetState;
    public float TimeToChangeState = 1.5f;


    private void OnEnable()
    {
        Invoke(nameof(SetStateOfObj), TimeToChangeState);
    }


    void SetStateOfObj()
    {
        gameObject.SetActive(SetState);
    }
}
