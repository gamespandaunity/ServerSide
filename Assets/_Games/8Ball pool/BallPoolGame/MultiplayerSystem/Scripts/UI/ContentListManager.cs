using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NetworkManagement
{
    public abstract class ContentListManager : MonoBehaviour
    {
        public GameObject Challenge_browse,MyChallengeItem;
        //[SerializeField] protected Button contentButton;
        public Transform BrowseYourRoomsContent, BrowseOthersRoomsContent,ChallengeRequestContent;
        public  List<GameObject> buttons = new List<GameObject>();
        public List<GameObject> buttonsBrowse =new List<GameObject>();
        [HideInInspector]
        public GameObject obj;

      

       

        public void RessetOldButtons(bool hideContentButton = true) //RAR
        {           
           if (buttons != null)
            {
                foreach (GameObject playerUI in buttons)
                {
                    if (playerUI)
                    {
                        ////print(playerUI.name);
                       // Destroy(playerUI);
                    }
                }
            }

            if (buttonsBrowse != null)
            {
                foreach (GameObject playerUI in buttonsBrowse)
                {
                    if (playerUI)
                    {
                       // //print(playerUI.name);
                        Destroy(playerUI);
                    }
                }
            }


        }

/*
        public void ButtonsDestroy()
        {
            if (buttons != null)
            {
                foreach (Button playerUI in buttons)
                {
                    Destroy(playerUI.gameObject);
                }
            }

            if (buttonsBrowse != null)
            {
                //print("browse button");
                foreach (Button playerUI in buttonsBrowse)
                {
                    Destroy(playerUI.gameObject);
                }
            }
        }*/
       

        public void AddButton(GameObject button)
        {
            buttons.Add(button);
       

        }


        public void AddButtonBrowse(GameObject button)
        {
            buttonsBrowse.Add(button);

        }


    }
}
