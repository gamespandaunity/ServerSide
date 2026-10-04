using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using NetworkManagement;
using BallPool;

public class LoadTableScene3D : MonoBehaviour
{
    private ProductTable3D _cachedTable3D;

    private ProductTable3D CachedTable3D
    {
        get
        {
            if (!_cachedTable3D)
            {
                _cachedTable3D = ProductTable3D.FindObjectOfType<ProductTable3D>();
            }
            return _cachedTable3D;
        }
    }

    private string opponentTableSceneName;
    private AssetBundle opponentTable;
    private AssetBundle opponentTableScene;
    private bool isStarted;

    void Awake()
    {
        if (!AightBallPoolNetworkGameAdapter.is3DGraphics)
        {
            enabled = false;
            return;
        }
        isStarted = false;
    }

    void Start()
    {
       // string tableSceneName = ProductTable3D.GetTable3DSceneName();
      //  SceneLoaderUtility.LoadScene(tableSceneName, LoadSceneMode.Additive);
        isStarted = true;
    }

    public void HandleStart()
    {
    }

    void OnDisable()
    {
        if (CachedTable3D && opponentTable && opponentTableScene)
        {
            opponentTable.Unload(true);
            opponentTableScene.Unload(true);
        }
    }

    public IEnumerator AssignOpponentTableAssets(string table3DURL, string table3DSceneURL, string tableSceneName)
    {
        ConstantsData_M.LogInfo("SetOpponentTableURLs " + tableSceneName);
        while (!isStarted)
        {
            yield return null;
        }
        ProductsManagement.ProductInfo productInfo = new ProductsManagement.ProductInfo("Table3D");
        yield return StartCoroutine(ProductsManagement.LoadProducts(productInfo));


        string tableURL = CachedTable3D.FindUrlForCurrentPlatform(table3DURL, productInfo.sourceURLInAllPlatform);
        string tableSceneURL = CachedTable3D.FindUrlForCurrentPlatform(table3DSceneURL, productInfo.sourceURLInAllPlatform);

        if (tableSceneName != ProductTable3D.GetTable3DSceneName())
        {
            if (!string.IsNullOrEmpty(tableURL) && !string.IsNullOrEmpty(tableSceneURL))
            {
                DownloadManager.DownloadParameters parameter1 = new DownloadManager.DownloadParameters(tableURL, "");
                yield return DownloadManager.Download(parameter1, true);
                if (parameter1.assetBundle)
                {
                    opponentTable = parameter1.assetBundle;
                }
                DownloadManager.DownloadParameters parameter2 = new DownloadManager.DownloadParameters(tableSceneURL, "");
                yield return DownloadManager.Download(parameter2, true);
                if (parameter2.assetBundle)
                {
                    opponentTableScene = parameter2.assetBundle;
                }
                opponentTableSceneName = tableSceneName;
                ConstantsData_M.LogInfo("opponentTableSceneName " + opponentTableSceneName);
            }
            else
            {
                opponentTableSceneName = CachedTable3D.defaultTableSceneName;
            }
        }
        yield return null;

        int number = (AightBallPoolPlayer.mainPlayer.coins == AightBallPoolPlayer.otherPlayer.coins) ? 0 : (AightBallPoolPlayer.mainPlayer.coins > AightBallPoolPlayer.otherPlayer.coins ? 1 : 2);
        StartCoroutine(ApplyTableTextureAtGameStart(number));
    }

    public IEnumerator ApplyTableTextureAtGameStart(int number)
    {
        ConstantsData_M.LogInfo("SetTable3DTextureOnStartGame1");
        while (!isStarted)
        {
            yield return null;
        }
        yield return null;
        ConstantsData_M.LogInfo("SetTable3DTextureOnStartGame2");
        if (opponentTableSceneName != ProductTable3D.GetTable3DSceneName())
        {
            while (!CachedTable3D.isSetSources)
            {
                yield return null;
            }
            ConstantsData_M.LogInfo("SetTable3DTextureOnStartGame2");
            if (number != 0)
            {
                if (number == 1)
                {
                    BallPoolGameManager.instance.SetGameInfo("You have more coins than your opponent \n You played with your table");
                }
                else
                {
                    while (string.IsNullOrEmpty(opponentTableSceneName) || (opponentTableSceneName != CachedTable3D.defaultTableSceneName && (opponentTable == null || opponentTableScene == null)))
                    {
                        yield return null;
                    }
                   
                   // SceneLoaderUtility.LoadScene(opponentTableSceneName, LoadSceneMode.Additive);
                    string tableSceneName = ProductTable3D.GetTable3DSceneName();
                    BallPoolGameManager.instance.SetGameInfo("You have less coins than your opponent \n You played with your opponent table");
                    yield return  SceneManager.UnloadSceneAsync(tableSceneName);
                }
            }
        }
        isStarted = false;
      
    }
}
