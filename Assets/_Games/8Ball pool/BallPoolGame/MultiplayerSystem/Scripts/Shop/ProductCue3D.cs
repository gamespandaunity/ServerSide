using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NetworkManagement
{
    public class ProductCue3D : ProductWithAssetBundle
    {
        private Cue3DSet cue;
        [SerializeField] private GameObject defaultCue;
        [SerializeField] private string upgradeSceneName = "Upgrade";
        public GameObject defaultCuePrefab{ get { return defaultCue; } }
        public string cue3DURL{ get; private set; }
        public GameObject mainCue3DPrefab{ get; private set; }
        public override void ResetWhenBackToEditor()
        {

        }
        public override IEnumerator ShowSavedProduct()
        {
            yield return StartCoroutine(base.ShowSavedProduct());
            if (assetBundles != null && assetBundles.Length == 1)
            {
                mainCue3DPrefab = (GameObject)assetBundles[0].LoadAsset(assetBundles[0].GetAllAssetNames()[0]);
            }
            else
            {
                mainCue3DPrefab = defaultCue;
            }

            if (SceneManager.GetActiveScene().name == upgradeSceneName)
            {
                cue = GameObject.Instantiate(mainCue3DPrefab).GetComponent<Cue3DSet>();
                cue.Set();
            }
        }
  
        protected override IEnumerator SetSources()
        {
            if (cue)
            {
                Destroy(cue.gameObject);
            }
            yield return StartCoroutine(base.SetSources());
            if (sourcesURL != null && sourcesURL.Length > 0)
            {
                cue3DURL = sourcesURL[0];
            }
            else
            {
                cue3DURL = "";

            }
            yield return StartCoroutine(ShowSavedProduct());
        }
    }
}
