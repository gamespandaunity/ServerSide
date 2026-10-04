using System.Collections;
using UnityEngine;

namespace NetworkManagement
{
    public class ProductWithTexturs2D : NetworkManagement.Product
    {
        [SerializeField] protected Material[] materials;
        [SerializeField] protected Texture2D[] firstTextures;

        public override void ResetWhenBackToEditor()
        {
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i].mainTexture = firstTextures[i];
            }
        }

        public override IEnumerator InitializeProduct(ProductProfile productProfile)
        {
            yield return StartCoroutine(base.InitializeProduct(productProfile));
            yield return StartCoroutine(SetSources());
        }
        protected override IEnumerator SetSources()
        {
            if (sourcesURL != null && sourcesURL.Length > 0)
            {
                parameters = new DownloadManager.DownloadParameters[sourcesURL.Length];
                for (int i = 0; i < sourcesURL.Length; i++)
                {
					parameters[i] = new DownloadManager.DownloadParameters(sourcesURL[i], names[i], updateThis? DownloadManager.DownloadType.Update : DownloadManager.DownloadType.DownloadOrLoadFromDisc);
                    yield return DownloadManager.Download(parameters[i]);
                }
                for (int i = 0; i < sourcesURL.Length; i++)
                {
                    if (!parameters[i].isNull)
                    {
                        materials[i].mainTexture = parameters[i].texture;
                        SaveSourceURL(i, parameters[i].URL);
                    }
                }
            }
            else if(productProfile != null)
            {
                for (int i = 0; i < productProfile.data.defaultProductProfile.sources.Length; i++)
                {
                    materials[i].mainTexture = (Texture2D)productProfile.data.defaultProductProfile.sources[i];
                }
                if (parameters != null)
                {
                    for (int i = 0; i < parameters.Length; i++)
                    {
                        SaveSourceURL(i, "");
                    }
                    parameters = null;
                }
            }
			updateThis = false;
            yield return null;
        }
    }
}
