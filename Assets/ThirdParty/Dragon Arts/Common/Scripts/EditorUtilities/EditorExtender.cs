#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;

namespace DragonArts.Common
{

    public class EditorExtender : EditorWindow {

        [MenuItem("Assets/Create/Dragon Arts/Common/Scriptable Item", false, 0)]
        private static void CreateScriptableItem() {
            ScriptableItem asset = ScriptableObject.CreateInstance<ScriptableItem> ();
            asset.uid = EditorUtilities.GenerateUid("I");
            EditorUtilities.CreateScriptableObject(asset, "New Scriptable Item");
        }

        [MenuItem("Assets/Create/Dragon Arts/Common/Scriptable Group", false, 1)]
        private static void CreateScriptableGroup() {
            ScriptableGroup asset = ScriptableObject.CreateInstance<ScriptableGroup> ();
            asset.uid = EditorUtilities.GenerateUid("G");
            EditorUtilities.CreateScriptableObject(asset, "New Scriptable Group");
        }
    }
}

#endif