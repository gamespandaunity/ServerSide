using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cricket;

public class MultiplayerPanel : MonoBehaviour
{
    public GameObject Holder;
    public GameObject CreateRoomPanel;
    public GameObject CreatedRoomPanel;
    public GameObject JoinRoomPanel;
    [SerializeField] Cricket.Menu[] menus;


    public static MultiplayerPanel Instance;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void Awake()
    {
        Instance = this;
    }

    public void OnMultiplayer()
    {
        //if (CONTROLLER.PlayModeSelected == 6)
        //{
        //    Holder.SetActive(true);
        //}
    }

    public void EnableCreateRoomPanel()
    {
        Holder.SetActive(false);
        CreateRoomPanel.SetActive(true);
    }

    public void OpenRoomPanel()
    {
        Holder.SetActive(false);
        CreateRoomPanel.SetActive(false);
        JoinRoomPanel.SetActive(false);
        CreatedRoomPanel.SetActive(true);
    }

    public void CloseRoomPanel()
    {
        CreateRoomPanel.SetActive(false);
        JoinRoomPanel.SetActive(false);
        CreatedRoomPanel.SetActive(false);
        Holder.SetActive(false);
    }

    public void JoinRoom()
    {
        CreateRoomPanel.SetActive(false);
        CreatedRoomPanel.SetActive(false);
        Holder.SetActive(false);
        JoinRoomPanel.SetActive(true);
    }


    public void OpenMenu(string menuName)
    {
        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            return;
        }
        for (int i = 0; i < menus.Length; i++)
        {
            // The menus array holds scene objects; across a scene reload an entry can be a
            // DESTROYED component. Field reads (menuName/open) still work on those, but
            // Open()/Close() touch .gameObject and throw — the Launcher.Start ->
            // MultiplayerPanel.CloseMenu NRE present in every 2026-07-20/21 cricket report.
            if (menus[i] == null)
            {
                continue;
            }
            if (menus[i].menuName == menuName)
            {
                menus[i].Open();
                if (menuName == "Main")
                {
                    //Debug.Log("******p****");
                    //Launcher.Instance.ConnectToServer();
                }
            }
            else if (menus[i].open)
            {
                CloseMenu(menus[i]);
            }
        }
    }

    public void OpenMenu(Cricket.Menu menu)
    {
        if (menu == null)
        {
            return;
        }
        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            return;
        }
        for (int i = 0; i < menus.Length; i++)
        {
            if (menus[i] == null)
            {
                continue;
            }
            if (menus[i].open)
            {
                CloseMenu(menus[i]);
            }
        }
        if(menu.menuName == "Main")
        {
            //Debug.Log("HELLOWWW1");
            //Launcher.Instance.ConnectToServer();
        }
        menu.Open();
    }

    public void CloseMenu(Cricket.Menu menu)
    {
        if (menu == null)
        {
            return;
        }
        menu.Close();
    }

    public void CloseMenu(string MenuName)
    {
        MenuName.Show("Menu Name");
        for (int i = 0; i < menus.Length; i++)
        {
            if (menus[i] == null)
            {
                continue;
            }
            if (menus[i].menuName == MenuName)
            {
                menus[i].Close();
            }
        }
    }

    public bool IsMenuActive(string MenuName)
    {
        for (int i = 0; i < menus.Length; i++)
        {
            if (menus[i].menuName == MenuName)
            {
                if (menus[i].gameObject.activeInHierarchy)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        return false;
    }
}
