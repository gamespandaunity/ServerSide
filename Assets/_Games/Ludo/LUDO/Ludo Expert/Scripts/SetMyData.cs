using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using LudoGame;

public class SetMyData : MonoBehaviour
{

    public GameObject avatar;
    public GameObject name;
    public GameObject matchCanvas;
    public GameObject controlAvatars;
    public GameObject backButton;


    // Use this for initialization


    public void MatchPlayer()
    {

        //name.GetComponent<Text>().text = LudoGame.GameManager.Instance.nameMy;
        if (LudoGame.GameManager.Instance.avatarMy != null)
            avatar.GetComponent<Image>().sprite = LudoGame.GameManager.Instance.avatarMy;


        controlAvatars.GetComponent<ControlAvatars>().reset();

    }

    public void setBackButton(bool active)
    {
        backButton.SetActive(active);
    }
}
