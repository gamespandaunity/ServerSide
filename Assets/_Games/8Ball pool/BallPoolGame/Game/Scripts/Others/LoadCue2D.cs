using System.Collections;
using UnityEngine;
using NetworkManagement;

public class LoadCue2D : MonoBehaviour 
{
    private ProductCue2D _cachedCue2D;
    private ProductCue2D CachedCue2D
    {
        get
        {
            if (!_cachedCue2D)
            {
                _cachedCue2D = ProductCue2D.FindObjectOfType<ProductCue2D>();
            }
            return _cachedCue2D;
        }
    }
    private Texture opponentCueTexture2D;
    private Texture playerCueTexture2D;

    public void OnStart()
    {
    }
    void OnDisable()
    {
        if (playerCueTexture2D != null &&  CachedCue2D != null && CachedCue2D.cue2dMaterial != null)
        {
            CachedCue2D.cue2dMaterial.mainTexture = playerCueTexture2D;
        }
    }
    public IEnumerator AssignOpponentCuePath(string url)
    {
        if (!string.IsNullOrEmpty(url))
        {
            DownloadManager.DownloadParameters parameter = new DownloadManager.DownloadParameters(url, "");
            yield return DownloadManager.Download(parameter);
            if (parameter.texture)
            {
                opponentCueTexture2D = parameter.texture;
                staticVariables.cueOpponentTex = opponentCueTexture2D;
            }
        }
        else
        {
            while (!CachedCue2D.cueDefault2DTexture)
            {
                yield return null;
            }
            opponentCueTexture2D = CachedCue2D.cueDefault2DTexture;
        }
        while (!CachedCue2D.mainCue2DTexture)
        {
            yield return null;
        }
        playerCueTexture2D = CachedCue2D.mainCue2DTexture;
        staticVariables.cueMainTex = playerCueTexture2D;
        yield return null;
    }
    public IEnumerator ApplyCue2DTextureOnTurnChange(bool myTurn)
    {
        if (myTurn)
        {
            while (playerCueTexture2D == null)
            {
                yield return null;
            }
            CachedCue2D.cue2dMaterial.mainTexture = playerCueTexture2D;
            staticVariables.cueMainTex = CachedCue2D.cue2dMaterial.mainTexture;

        }
        else
        {
            while (opponentCueTexture2D == null)
            {
                yield return null;
            }
            CachedCue2D.cue2dMaterial.mainTexture = opponentCueTexture2D;
            staticVariables.cueOpponentTex = CachedCue2D.cue2dMaterial.mainTexture;
        }
    }
}
