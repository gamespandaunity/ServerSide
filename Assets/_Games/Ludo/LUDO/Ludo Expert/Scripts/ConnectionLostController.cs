using UnityEngine;
using System.Collections;
using LudoGame;

public class ConnectionLostController : MonoBehaviour {

    // Use this for initialization
    public GameObject canvas;

    void Start() {
        DontDestroyOnLoad(transform.gameObject);
        LudoGame.GameManager.Instance.connectionLost = this;

        if (Application.internetReachability == NetworkReachability.NotReachable) {
            Debug.Log("No Internet Connection");
            //showDialog();
        }
    }

    public void destroy() {
        if (this.gameObject != null)
            DestroyImmediate(this.gameObject);
    }

    public void showDialog() {
        canvas.SetActive(true);
    }

    public void closeApp() {
        Application.Quit();
    }
}
