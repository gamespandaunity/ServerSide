#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace DragonArts.Common
{

    public static class EditorUtilities {

        public static void CreateScriptableObject<T> (T asset, string name) where T : ScriptableObject {
            string path = GetSelectionPath();
            string assetPathAndName = AssetDatabase.GenerateUniqueAssetPath($"{path}/{name}.asset");
    
            AssetDatabase.CreateAsset(asset, assetPathAndName);
    
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }

        public static void DuplicateScriptableObject<T> () where T : ScriptableObject {
            string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            string path = GetSelectionPath();
            List<T> assets = AssetDatabase.FindAssets($"t: {typeof(T).Name}", new string[] { path }).ToList()
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .ToList();
            
            if(!AssetDatabase.CopyAsset(assetPath, $"{path}/{Selection.activeObject.name} {assets.FindAll(so => ((ScriptableObject) so).name.Contains(Selection.activeObject.name)).Count}.asset"))
                ConstantsData_M.LogInfo($"Failed to duplicate {assetPath}");
        }

        public static void InstantiatePrefab (MenuCommand menuCommand, string enginePath, string prefabName, string prefabDisplayName) {
            // Create a custom game object
            UnityEngine.Object prefab = AssetDatabase.LoadAssetAtPath($"{enginePath}/Engine/Prefabs/{prefabName}.prefab", typeof(GameObject));
            if (prefab == null)
                return;
            GameObject go = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            go.name = prefabDisplayName;
            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);
            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);
            Selection.activeObject = go;
        }

        public static string GetSelectionPath () {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (path == "") {
                path = "Assets";
            } 
            else if (Path.GetExtension(path) != "")  {
                path = path.Replace(Path.GetFileName(AssetDatabase.GetAssetPath(Selection.activeObject)), "");
            }
            return path;
        }

        public static Texture GetCopyIcon () {
            return EditorGUIUtility.Load("Assets/Dragon Arts/Common/CopyIcon.png") as Texture;
        }

        public static void DrawHeader (string header) {
            GUILayout.BeginHorizontal("box");
            GUILayout.FlexibleSpace();
            GUILayout.Label(header);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        public static string GenerateUid (string prefix = null) {
            System.Random random = new System.Random();
            string chars = "AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz0123456789";

            List<ScriptableItem> items = GetAllInstances<ScriptableItem>();

            string newUid = DragonArts.Common.Utilities.GenerateUid(chars, random, 10, prefix);

            while (items.FirstOrDefault(i => i.uid == newUid) != null) {
                string[] arr = newUid.Split('-');
                newUid = DragonArts.Common.Utilities.GenerateUid(chars, random, 10, arr.Length > 1 ? arr[0] : null);
            }

            return newUid;
        }

        public static List<T> GetAllInstances<T>() where T : ScriptableObject {
            return AssetDatabase.FindAssets($"t: {typeof(T).Name}").ToList()
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .ToList();
        }
    }
}

#endif