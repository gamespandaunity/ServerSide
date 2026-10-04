 
using UnityEngine;
using UnityEngine.UI;
public class MiniMap : MonoBehaviour
{
    public PlayerPowerController playerPowerController;
    [SerializeField] Sprite playerMark, AiMark;

    [SerializeField] Image markImage;
    // Start is called before the first frame update
    void Start()
    {
        if(playerPowerController != null)
        {
            if (playerPowerController.isAI)
            {
                markImage.sprite = AiMark;
                markImage.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
                //Debug.Log("Yes Im AI");
            }
            else
            {
                //Photon Removal    if (playerPowerController.isPhotonViewMine())
                {
                    markImage.sprite = playerMark;
                    //Debug.Log("Its My Photon");
                    markImage.transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
                    markImage.transform.localPosition = new Vector3(markImage.transform.localPosition.x, markImage.transform.localPosition.y + 5f, markImage.transform.localPosition.z);
                }
                //Photon Removal   else
                {
                    markImage.sprite = AiMark;
                    markImage.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
                    //Debug.Log("Im Opponent");
                }
            }
        }
    }

    
}
