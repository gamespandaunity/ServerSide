using UnityEngine;

public class InputFieldData : MonoBehaviour
{
    [SerializeField]
    private RectTransform myRectTransform;

    public RectTransform MyRectTransform
    {
        get { return myRectTransform; }
    }

    private void Awake()
    {
        if (myRectTransform == null)
        {
            myRectTransform = GetComponent<RectTransform>();
        }
    }
}