using UnityEngine;
using UnityEditor;
using System.Collections;
using NetworkManagement;

public class GameEditorManagers
{
    [MenuItem ("Window/Game Data Management/Clear Preferences")]
    static void ClearPreferences () 
    {
        PlayerPrefs.DeleteAll();
    }
    [MenuItem ("Window/Game Data Management/Clear Key Value Data From Persistent Directory")]
    static void ClearPersistentKeyValueData () 
    {
        DownloadManager.DeleteKeyValuFromPersistentStorage();
    }
    [MenuItem ("Window/Game Data Management/Clear Documends From Persistent Directory")]
    static void ClearPersistentDocumends () 
    {
        DownloadManager.DeleteDocumendsFromPersistentStorage();
    }
}
