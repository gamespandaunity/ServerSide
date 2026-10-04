using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class WaitingPanelScript : MonoBehaviour
{
    [SerializeField] private Image[] circleLine;
    [SerializeField] private Sprite[] ball8Sprite , carromPieceSprite;
    [SerializeField] private Image Logo,bg;
    [SerializeField] Text name,OpponentName;
    string imageUrl;
    public RawImage rawProfileImage,rawopimage;
    public static string WaitingForroomID;
    public GameObject poolBallSprite , teenPattiLoading;
    // Start is called before the first frame update
    private void OnEnable()
    {
        //print("waiting panel api game id ===>>" + APIManager.gameid);
        SpritesManager.Instance.spritesScriptable.LogoChangerWith_GameId(Logo);
        SpritesManager.Instance.spritesScriptable.BgHandlerAccordingTo_Id(bg);

        if (staticVariables.UserProfiledata != null)
        {
            UserModel userModel = staticVariables.UserProfiledata;
            if (userModel != null && userModel.user != null)
            {
                name.text = userModel.user.first_name;
                if (staticVariables.isfromreferllinks == false)
                {
                    rawProfileImage.texture = staticVariables.ProfilePicture;
                }
            }
            else
            {
                ConstantsData_M.Log("User model or user data is null");
            }
        }
        else
        {
            ConstantsData_M.Log("User model JSON is null or empty");
        }
        switch (ApiAndRoomManager.currentGameId)
        {
            case 1:
                poolBallSprite.gameObject.SetActive(true);
                if (circleLine != null && ball8Sprite != null)
                {
                    for (int i = 0; i < circleLine.Length && i < ball8Sprite.Length; i++)
                    {
                        if (circleLine[i] != null && ball8Sprite[i] != null)
                        {
                            circleLine[i].sprite = ball8Sprite[i];
                        }
                        else
                        {
                            ConstantsData_M.Log("circleLine or ball8Sprite is null at index: " + i);
                        }
                    }
                }
                else
                {
                    ConstantsData_M.Log("circleLine or ball8Sprite is null");
                }
                break;
                case 2:
                if (circleLine != null && carromPieceSprite != null)
                {
                    for (int i = 0; i < circleLine.Length && i < carromPieceSprite.Length; i++)
                    {
                        if (circleLine[i] != null && carromPieceSprite[i] != null)
                        {
                            circleLine[i].sprite = carromPieceSprite[i];
                        }
                        else
                        {
                            ConstantsData_M.Log("circleLine or carromPieceSprite is null at index: " + i);
                        }
                    }
                }
                else
                {
                    ConstantsData_M.Log("circleLine or carromPieceSprite is null");
                }
                break; 
            case 3:
                poolBallSprite.gameObject.SetActive(false);
                teenPattiLoading.gameObject.SetActive(true);

                break;
                case 4: break;  
        }
      

        if (staticVariables.UserProfiledata !=null)
        {
            UserModel userModel = staticVariables.UserProfiledata;
            if (userModel != null && userModel.user != null)
            {
                name.text = userModel.user.first_name;
                if(staticVariables.isfromreferllinks == false)
                {
                    rawProfileImage.texture = staticVariables.ProfilePicture;
                }
            }
            else
            {
                ConstantsData_M.Log("User model or user data is null");
            }
        }
        else
        {
            ConstantsData_M.Log("User model JSON is null or empty");
        }

        if(/*staticVariables.opponentImage == null &&*/ !GameModeManager.isAI)
        {
            //print("opponent image in waiting opanel  " + staticVariables.opponentImage);

            OpponentName.text = staticVariables.OpponetProfile.userName;
            ServerConnection.DownloadSprite("/" + staticVariables.OpponetProfile.imageURL, DownloadedTexture =>
            {
                staticVariables.opponentImage = DownloadedTexture;// scriptable.nullProfileImg;
                rawopimage.texture = DownloadedTexture;// scriptable.nullProfileImg;

            }, OnFailed =>
            {


            });
        }
        else
        {
            rawopimage.texture = staticVariables.opponentImage;
        }
      //  StartCoroutine(StopRandomizer());
    }

    public void DestroyingPanel()
    {
        ApiAndRoomManager._instance.LeaveChallenge(WaitingForroomID);

        Destroy(this.gameObject);
    }
    


  

}
