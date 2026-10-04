using CarRace;
using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.UI;

public class MiniMapCar : MonoBehaviour
{


    public RCC_AICarController _AIController;
    [SerializeField] Sprite playerMark, AiMark;

    [SerializeField] Image markImage;
    RCC_MirrorNetwork _MirrorNetwork;
    // Start is called before the first frame update
    void Start()
    {

        if (_AIController != null)
        {
            if (_AIController)
            {
                markImage.sprite = AiMark;
                markImage.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
                Debug.Log("Yes Im AI");
            }
            else
            {

            }
        }
        else
        {
            Debug.Log("Yes Im Player");
            _MirrorNetwork = gameObject.GetComponent<RCC_MirrorNetwork>();
            if (_MirrorNetwork.isOwned)
            //Photon Removal    if (playerPowerController.isPhotonViewMine())
            {
                markImage.sprite = playerMark;
                Debug.Log("Its My Player");
                markImage.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                markImage.transform.localPosition = new Vector3(markImage.transform.localPosition.x, markImage.transform.localPosition.y + 5f, markImage.transform.localPosition.z);
            }
            //Photon Removal
            else
            {
                Debug.Log("Its other Player");
                markImage.sprite = AiMark;
                markImage.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
                //Debug.Log("Im Opponent");
            }
        }
    }


}


