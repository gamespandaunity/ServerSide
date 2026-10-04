using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

namespace NetworkManagement
{
    public delegate void DownloadHaldler(DownloadManager.DownloadParameters parameters);
    public class DownloadManager : MonoBehaviour
    {
        public enum DownloadType
        {
            AlwaysDownload = 0,
            Update,
            DownloadOrLoadFromDisc
        }
        public static event DownloadHaldler OnStartDownload;
        public static event DownloadHaldler OnEndDownload;
        public static event DownloadHaldler OnDownloading;

        public class DownloadParameters
        {
            public string URL
            {
                get;
                private set;
            }
            public string name
            {
                get;
                private set;
            }
            public string localURL
            {
                get;
                set;
            }
            public DownloadType downloadType
            {
                get;
                private set;
            }
            public bool isNull
            {
                get;
                set;
            }
            public string error
            {
                get;
                set;
            }
            public bool downloading
            {
                get;
                set;
            }
            public float progress
            {
                get;
                set;
            }
            public byte[] bytes
            {
                get;
                private set;
            }

            public string text
            {
                get;
                private set;
            }


            public Texture2D texture
            {
                get;
                private set;
            }
            public AssetBundle assetBundle
            {
                get;
                private set;
            }
            public DownloadParameters(string URL, string name, DownloadType downloadType = DownloadType.DownloadOrLoadFromDisc)
            {
                this.isNull = false;
                this.error = "";
                this.URL = URL;
                this.name = name;
                this.downloadType = downloadType;
            }

            public void SetData(byte[] bytes, string text, Texture2D texture, AssetBundle assetBundle)
            {
                this.bytes = bytes;
                this.text = text;
                this.texture = texture;
                this.assetBundle = assetBundle;
            }
        }
        public static void DeleteDocumendsFromPersistentStorage()
        {
            string path = Application.persistentDataPath + "/SavedData/Documends";
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        public static void DeleteKeyValuFromPersistentStorage()
        {
            string path = Application.persistentDataPath + "/SavedData/KeyValueStorage";

            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        public static void SaveGameDataToDocumends(Dictionary<string, DataManager.Data> gameDataDictionary)
        {
            if (gameDataDictionary == null)
            {
                return;
            }
            try
            {
                DeleteKeyValuFromPersistentStorage();
                CheckKeyValueStorageDirectory();
                foreach (KeyValuePair<string, DataManager.Data> item in gameDataDictionary)
                {
                    SaveString(item.Value.typeId, item.Key, item.Value.value);
                }
            }
            catch (Exception e) { Debug.LogException(e); }
        }
        public static Dictionary<string, DataManager.Data> LoadGameDataFromDocumends()
        {
            CheckKeyValueStorageDirectory();
            Dictionary<string, DataManager.Data> gameDataDictionary = new Dictionary<string, DataManager.Data>();

            string intPath = Application.persistentDataPath + "/SavedData/KeyValueStorage/Int/";

            DirectoryInfo intDirectoryInfo = new DirectoryInfo(intPath);
            foreach (FileInfo item in intDirectoryInfo.GetFiles())
            {
                gameDataDictionary.Add(item.Name.Replace(".dat", ""), new DataManager.Data(1, File.ReadAllText(intPath + item.Name, System.Text.Encoding.UTF8)));
            }
            string floatPath = Application.persistentDataPath + "/SavedData/KeyValueStorage/Float/";
            DirectoryInfo floatDirectoryInfo = new DirectoryInfo(floatPath);
            foreach (FileInfo item in floatDirectoryInfo.GetFiles())
            {
                gameDataDictionary.Add(item.Name.Replace(".dat", ""), new DataManager.Data(2, File.ReadAllText(floatPath + item.Name, System.Text.Encoding.UTF8)));
            }
            string stringPath = Application.persistentDataPath + "/SavedData/KeyValueStorage/String/";
            DirectoryInfo stringDirectoryInfo = new DirectoryInfo(stringPath);
            foreach (FileInfo item in stringDirectoryInfo.GetFiles())
            {
                gameDataDictionary.Add(item.Name.Replace(".dat", ""), new DataManager.Data(3, File.ReadAllText(stringPath + item.Name, System.Text.Encoding.UTF8)));
            }
            return gameDataDictionary;
        }
        private static void CheckDirectory()
        {
            if (!Directory.Exists(Application.persistentDataPath + "/SavedData"))
            {
                Directory.CreateDirectory(Application.persistentDataPath + "/SavedData");
            }
#if UNITY_IOS
            UnityEngine.iOS.Device.SetNoBackupFlag(Application.persistentDataPath + "/SavedData");
#endif
        }
        private static void CheckDocumendsDirectory()
        {
            CheckDirectory();
            if (!Directory.Exists(Application.persistentDataPath + "/SavedData/Documends"))
            {
                Directory.CreateDirectory(Application.persistentDataPath + "/SavedData/Documends");
            }
        }
        private static void CheckKeyValueStorageDirectory()
        {
            CheckDirectory();
            if (!Directory.Exists(Application.persistentDataPath + "/SavedData/KeyValueStorage/Int"))
            {
                Directory.CreateDirectory(Application.persistentDataPath + "/SavedData/KeyValueStorage/Int");
            }
            if (!Directory.Exists(Application.persistentDataPath + "/SavedData/KeyValueStorage/Float"))
            {
                Directory.CreateDirectory(Application.persistentDataPath + "/SavedData/KeyValueStorage/Float");
            }
            if (!Directory.Exists(Application.persistentDataPath + "/SavedData/KeyValueStorage/String"))
            {
                Directory.CreateDirectory(Application.persistentDataPath + "/SavedData/KeyValueStorage/String");
            }
        }
        private static string GetKeyValueStoragePath(int typeId, string key)
        {
            CheckKeyValueStorageDirectory();
            return Application.persistentDataPath + "/SavedData/KeyValueStorage/" + (typeId == 1 ? "Int" : (typeId == 2 ? "Float" : "String")).ToString() + "/" + key + ".dat";
        }
        public static string LoadString(int typeId, string key)
        {
            string path = GetKeyValueStoragePath(typeId, key);
            byte[] bytes = File.ReadAllBytes(path);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }
        public static void SaveString(int typeId, string key, string value)
        {
            string path = GetKeyValueStoragePath(typeId, key);
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(value);
            File.WriteAllBytes(path, bytes);
        }
        public static IEnumerator Download(DownloadParameters parameters, bool hasAssetBundle = false)
        {
            //Debug.Log(parameters.name +  "  " + hasAssetBundle);
            if (parameters.downloadType == DownloadType.AlwaysDownload)
            {
                if (OnStartDownload != null)
                {
                    OnStartDownload(parameters);
                }
                yield return null;
                WWW downloadData = new WWW(parameters.URL);
                while (!downloadData.isDone)
                {
                    if (OnDownloading != null)
                    {
                        parameters.downloading = true;
                        parameters.progress = downloadData.progress;
                        OnDownloading(parameters);
                    }
                    yield return null;
                }
                yield return downloadData;
                if (string.IsNullOrEmpty(downloadData.error))
                {
                    parameters.SetData(downloadData.bytes, downloadData.text, downloadData.texture,
                        hasAssetBundle ? downloadData.assetBundle : null);
                }
                else
                {
                    parameters.isNull = true;
                    parameters.error = downloadData.error;
                }
                if (OnEndDownload != null)
                {
                    OnEndDownload(parameters);
                }
            }
            else
            {
                CheckDocumendsDirectory();

                string path = Application.persistentDataPath + "/SavedData/Documends/" +
                          parameters.URL.Replace("https://www.", "").Replace(".com", "").Replace("/", "").Replace("?dl=0", "").
                    Replace("https:", "").Replace("-", "").Replace("_", "").Replace("?", "").Replace(".", "").Replace("=", "").Replace("&", "").Replace("%20", "").Replace(" ", "") + ".dat";

                if (Application.internetReachability != NetworkReachability.NotReachable && (!File.Exists(path) || parameters.downloadType == DownloadType.Update))
                {
                    if (OnStartDownload != null)
                    {
                        OnStartDownload(parameters);
                    }
                    yield return null;
                    WWW downloadData = new WWW(parameters.URL);
                    while (!downloadData.isDone)
                    {
                        if (OnDownloading != null)
                        {
                            parameters.downloading = true;
                            parameters.progress = downloadData.progress;
                            OnDownloading(parameters);
                        }
                        yield return null;
                    }

                    yield return downloadData;
                    if (string.IsNullOrEmpty(downloadData.error))
                    {
                        parameters.SetData(downloadData.bytes, downloadData.text, downloadData.texture,
                        hasAssetBundle ? downloadData.assetBundle : null);
                        File.WriteAllBytes(path, downloadData.bytes);
                    }
                    else
                    {
                        parameters.error = downloadData.error;
                        parameters.isNull = true;
                    }
                    if (OnEndDownload != null)
                    {
                        OnEndDownload(parameters);
                    }
                }
                else if (File.Exists(path))
                {
                    if (OnStartDownload != null)
                    {
                        OnStartDownload(parameters);
                    }
                    parameters.localURL = "file://" + path;
                    //                    byte[] bytes = File.ReadAllBytes(path);
                    //                    Texture2D image = new Texture2D(2, 2);
                    //                    if(image.LoadImage(bytes))
                    //                    {
                    //                        image.Apply();
                    //                    }
                    //                    string text = System.Text.Encoding.UTF8.GetString(bytes);
                    //                    AssetBundle assetBundle = AssetBundle.LoadFromMemory(bytes);
                    //                    parameters.SetData(bytes, text, image, assetBundle);


                    WWW loadData = new WWW(parameters.localURL);
                    while (!loadData.isDone)
                    {
                        if (OnDownloading != null)
                        {
                            parameters.downloading = false;
                            parameters.progress = loadData.progress;
                            OnDownloading(parameters);
                        }
                        yield return null;
                    }
                    yield return loadData;
                    if (string.IsNullOrEmpty(loadData.error))
                    {
                        parameters.SetData(loadData.bytes, loadData.text, loadData.texture,
                        hasAssetBundle ? loadData.assetBundle : null);
                    }
                    else
                    {
                        parameters.isNull = true;
                        parameters.error = loadData.error;
                    }
                    if (OnEndDownload != null)
                    {
                        OnEndDownload(parameters);
                    }

                }
                else
                {
                    parameters.isNull = true;
                    parameters.error = "Some error";
                }
            }

        }
    }
}
