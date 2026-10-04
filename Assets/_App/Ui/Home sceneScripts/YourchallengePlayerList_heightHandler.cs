using UnityEngine;
using UnityEngine.UI;
public class YourchallengePlayerList_heightHandler : MonoBehaviour
{
    public RectTransform contentRectTransform;
    public VerticalLayoutGroup layoutGroup;
    private void OnEnable()
    {
        contentRectTransform = GetComponent<RectTransform>();
        layoutGroup = GetComponent<VerticalLayoutGroup>();
    }
    void Start()
    {
      
    }
    private void Update()
    {
        CalculateHeight();
    }
    public void CalculateHeight()
    {
        int childCount = contentRectTransform.childCount;
        float totalHeight = 0f;

        // Calculate total height based on the size of each child element
        for (int i = 0; i < childCount; i++)
        {
            RectTransform childRectTransform = contentRectTransform.GetChild(i) as RectTransform;
            totalHeight += childRectTransform.sizeDelta.y + layoutGroup.spacing;
        }

        // Adjust the height of the content to fit all the child elements
        contentRectTransform.sizeDelta = new Vector2(contentRectTransform.sizeDelta.x, totalHeight);
        

    }

}
