using UnityEngine;
using UnityEngine.SceneManagement;

public class UISelectNetworkNameManager : MonoBehaviour
{

    public GameObject cryptoDetailinfo;
    public GameObject cryptoDetailTemplate;
    public static UISelectNetworkNameManager instance;
    // Start is called before the first frame update
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
    }
   

    public void selectNetworkBtn()
    {
        SceneLoaderUtility.LoadScene("CryptoDetailScene");
    }

    public void returnBtn()
    {
        Destroy(gameObject.transform.parent.gameObject);
    }
}
