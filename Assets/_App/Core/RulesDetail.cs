using UnityEngine;
using UnityEngine.Serialization;

public class RulesDetail : MonoBehaviour
{
    [TextArea(5,10)]
    [FormerlySerializedAs("rules")] public string rules;


    public void ShowRules()
    {
        PopupMessageManager.instance.ShowTextDetailPanel(rules,"HOW TO PLAY!");
    }
}
