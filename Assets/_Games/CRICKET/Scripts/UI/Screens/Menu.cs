namespace Cricket 
{
using UnityEngine;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    public string menuName;
    public bool open;

    public void Open()
    {
        if (SceneManager.GetActiveScene().name != "MainMenu")
        {
            return;
        }
        open = true;
        gameObject.SetActive(true);
    }

    public void Close()
    {
        open = false;
        gameObject.SetActive(false);
    }
}

}