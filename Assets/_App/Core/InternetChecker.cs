using UnityEngine;
using DG.Tweening;
public class InternetChecker : MonoBehaviour
{
    private void OnEnable()
    {
        this.gameObject.transform.localScale = Vector3.zero; 
        this.gameObject.transform.DOScale(Vector3.one, 1).SetEase(Ease.OutBack);
       
    }
    private void OnDisable()
    {
        this.gameObject.transform.localScale = Vector3.zero;
        this.gameObject.transform.DOScale(Vector3.zero, 1).SetEase(Ease.OutBack);
       
    }
}
