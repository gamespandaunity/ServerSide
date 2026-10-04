using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class MyEvent : UnityEvent
{
}

public class EnableDisableEvent : MonoBehaviour
{

    public MyEvent OnEnableEvent;
    public MyEvent OnDisableEvent;

    private void OnEnable()
    {
        OnEnableEvent?.Invoke();
    }

    private void OnDisable()
    {
        OnDisableEvent?.Invoke();
    }
}
