using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NetworkManagement
{
    public class ProductTable3D : ProductWithAssetBundle
    {
        public string defaultTableSceneName;
        [SerializeField] private string upgradeSceneName = "Upgrade";
        public string tableSceneName{ get; private set; }
        public string table3DURL{ get; private set; }
        public string table3DSceneURL{ get; private set; }

        public bool isSetSources{ get; private set; }

        public override void ResetWhenBackToEditor()
        {

        }
        public override IEnumerator ShowSavedProduct()
        {
            yield return StartCoroutine(base.ShowSavedProduct());
            if (assetBundles != null && assetBundles.Length == 2)
            {
                tableSceneName = assetBundles[1].GetAllScenePaths()[0];
            }
            else
            {
                tableSceneName = defaultTableSceneName;
            }
            SaveTable3DSceneName(tableSceneName);
            if (SceneManager.GetActiveScene().name == upgradeSceneName)
            {
                SceneLoaderUtility.LoadScene(tableSceneName, LoadSceneMode.Additive);
            }
        }
  
        public static void SaveTable3DSceneName(string tableSceneName)
        {
            DataManager.SetStringData("Table3DSceneName", tableSceneName);
        }
        public static string GetTable3DSceneName()
        {
            return DataManager.GetStringData("Table3DSceneName");
        }
        protected override IEnumerator SetSources()
        {
            isSetSources = false;
            if (!string.IsNullOrEmpty(tableSceneName))
            {
                yield return SceneManager.UnloadSceneAsync(tableSceneName);
            }
            yield return StartCoroutine(base.SetSources());
            yield return StartCoroutine(ShowSavedProduct());
            if (sourcesURL != null && sourcesURL.Length > 1)
            {
                table3DURL = sourcesURL[0];
                table3DSceneURL = sourcesURL[1];
            }
            else
            {
                table3DURL = "";
                table3DSceneURL = "";
            }
            isSetSources = true;
        }
    }
}
