using System.Collections;
using UnityEngine;
using BallPool;
using NetworkManagement;

public class LoadCueModel3D : MonoBehaviour 
{
    private Cue3DSet cueSet;
    private ProductCue3D _cachedCueProduct;
    private ProductCue3D CachedCueProduct
    {
        get
        {
            if (!_cachedCueProduct)
            {
                _cachedCueProduct = ProductCue3D.FindObjectOfType<ProductCue3D>();
            }
            return _cachedCueProduct;
        }
    }
    private GameObject opponentCuePrefab3D;
    private GameObject playerCuePrefab3D;
    private AssetBundle opponentCueBundle;

    void Awake()
    {
        if (!AightBallPoolNetworkGameAdapter.is3DGraphics || BallPoolGameLogic.playMode == BallPool.PlayMode.Replay)
        {
            enabled = false;
            return;
        }
    }
    IEnumerator Start()
    {
        while (CachedCueProduct.mainCue3DPrefab == null)
        {
            yield return null;
        }
        playerCuePrefab3D = CachedCueProduct.mainCue3DPrefab;
        if (!BallPoolGameLogic.isOnLine || !AightBallPoolNetworkGameAdapter.isSameGraphicsMode)
        {
            GenerateCueModel(playerCuePrefab3D);
        }
    }
    private void GenerateCueModel(GameObject cuePrefab)
    {
        if (cueSet)
        {
            Destroy(cueSet.gameObject);
        }
        cueSet = GameObject.Instantiate(cuePrefab).GetComponent<Cue3DSet>();
        if (cueSet)
        {
            cueSet.Set();
        }
    }
    public void InitializeOnStart()
    {
    }
    void OnDisable()
    {
        if (CachedCueProduct && opponentCueBundle)
        {
            opponentCueBundle.Unload(true);
        }
    }
    public IEnumerator AssignOpponentCuePath(string cueUrl)
    {
        ProductsManagement.ProductInfo productInfo = new ProductsManagement.ProductInfo("Cue3D");
        yield return StartCoroutine(ProductsManagement.LoadProducts(productInfo));
            
        string url = CachedCueProduct.FindUrlForCurrentPlatform(cueUrl, productInfo.sourceURLInAllPlatform);
        while (!CachedCueProduct.mainCue3DPrefab)
        {
            yield return null;
        }
        playerCuePrefab3D = CachedCueProduct.mainCue3DPrefab;
        if (!string.IsNullOrEmpty(url) && CachedCueProduct.sourcesURL != null && CachedCueProduct.sourcesURL.Length > 0 && CachedCueProduct.GetNameFromSourceURL(CachedCueProduct.sourcesURL[0]) == CachedCueProduct.GetNameFromSourceURL(cueUrl))
        {
            opponentCuePrefab3D = playerCuePrefab3D;
        }
        else
        {
            if (!string.IsNullOrEmpty(url))
            {
                DownloadManager.DownloadParameters parameter = new DownloadManager.DownloadParameters(url, "");
                yield return DownloadManager.Download(parameter, true);
                if (parameter.assetBundle)
                {
                    opponentCueBundle = parameter.assetBundle;
                    opponentCuePrefab3D = (GameObject)opponentCueBundle.LoadAsset(parameter.assetBundle.GetAllAssetNames()[0]);
                }
            }
            else 
            {
                while (!CachedCueProduct.defaultCuePrefab)
                {
                    yield return null;
                }
                opponentCuePrefab3D = CachedCueProduct.defaultCuePrefab;
            }
        }
        yield return null;
    }
    public IEnumerator ApplyCue2DTextureOnTurnChange(bool myTurn)
    {
        if (myTurn)
        {
            while (playerCuePrefab3D == null)
            {
                yield return null;
            }
            GenerateCueModel(playerCuePrefab3D);
        }
        else
        {
            while (opponentCuePrefab3D == null)
            {
                yield return null;
            }
            GenerateCueModel(opponentCuePrefab3D);
        }
        yield return null;
    }
}
