 
using System.Collections;
using System.Collections.Generic;
using CarRace;
using UnityEngine;
using UnityEngine.UI;
using UnityExtensions;

public class HealthUIKit : MonoBehaviour
{
    public GameObject healthPanel;
    public Text enemyName;
    public Slider healthSlider;
    public Image countryFlagImg;


    private void Start()
    {
        setPlayerName();
    }

    void Update()
    {
        if (Camera.main != null) healthPanel.transform.LookAt(Camera.main.transform.position, Vector3.up);
         if (MultiPlayerGame.isSinglePlayer)
        {
            if(countryFlagImg.sprite==null)
            {
                Debug.Log("flagChange");
                var playerController = gameObject.GetComponentInParent<PlayerPositionController>();
                countryFlagImg.sprite = CountriesFlags.LoadFlag(playerController.playerCountry);
            }
            
        }
    }

    public void setPlayerName()
    {
       
         
        
        // For human or AI, use local data

        if (MultiPlayerGame.isSinglePlayer)
        {
            var playerController = gameObject.GetComponentInParent<PlayerPositionController>();
             if (playerController == null) return;

            this.DelayUntil(() => playerController.PlayerName != "", () =>
               {
                   Debug.Log("Setting player name on health UI.");
                   enemyName.text = playerController.PlayerName;
               });
            this.DelayUntil(() => playerController.playerCountry != "", () =>
            {
                if (countryFlagImg)
                {
                    countryFlagImg.sprite = CountriesFlags.LoadFlag(playerController.playerCountry);//playerController.playerCountry);
                }
            });
        }
        else
        {
             var HorseAnimationSync = gameObject.GetComponentInParent<HorseAnimationSync>();
            this.DelayUntil(() => HorseAnimationSync.PlayerName != "", () =>
       {
           Debug.Log("Setting player name on health UI.");
           enemyName.text = HorseAnimationSync.PlayerName;
       });
            this.DelayUntil(() => HorseAnimationSync.playerCountry != "", () =>
          {
              if (countryFlagImg)
              {
                  countryFlagImg.sprite = CountriesFlags.LoadFlag(HorseAnimationSync.playerCountry);//playerController.playerCountry);
              }
          });

        }        //playerController.PlayerName;



            // Debug logs
          //  Debug.Log($"SetPlayerName: Player = {playerController.PlayerName}, Country = {playerController.playerCountry}");

    //     if (GetComponentInParent<PhotonView>() && !gameObject.GetComponentInParent<PlayerPositionController>().isAI)
    //     {
    //        gameObject.SetActive(!GetComponentInParent<PhotonView>().IsMine);
    //        enemyName.text = GetComponentInParent<PhotonView>().Owner.NickName;
    //        if (gameObject.GetComponentInParent<PlayerPositionController>())
    //        {
    //            gameObject.GetComponentInParent<PlayerPositionController>().PlayerName =
    //                GetComponentInParent<PhotonView>().Owner.NickName;                                                                                                 //Photon Removal
    //        }

    //        object countryflag;
    //        if (countryFlagImg && GetComponentInParent<PhotonView>().Owner.CustomProperties
    //                .TryGetValue(MultiPlayerGame.PLAYER_COUNTRY, out countryflag))
    //        {
    //            countryFlagImg.sprite = CountriesFlags.LoadFlag((string) countryflag);
    //        }
    //     }
    //     else if (gameObject.GetComponentInParent<PlayerPositionController>())
    //     {
    //        enemyName.text = gameObject.GetComponentInParent<PlayerPositionController>().PlayerName;
    //        if (countryFlagImg)
    //        {
    //            countryFlagImg.sprite =
    //                CountriesFlags.LoadFlag(gameObject.GetComponentInParent<PlayerPositionController>().playerCountry);
    //        }
    //     }
    }

    public void setPlayerUI(bool isActive)
    {
        gameObject.SetActive(isActive);
    }
}