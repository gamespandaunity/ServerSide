namespace CarRace 
{
using UnityEngine;
using UnityEngine.EventSystems;
public class LookBackCamera : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    RCC_Camera _Camera;
    private void Start()
    {
        _Camera = RCC_SceneManager.Instance.activePlayerCamera;
    }
    public void OnPointerDown(PointerEventData eventData)
    {
        _Camera.lookBack = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _Camera.lookBack = false;
       
    }


}

}