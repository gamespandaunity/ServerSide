
#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace DragonArts.Common
{

    [CustomEditor(typeof(ScriptableItem))]
    public class ScriptableItemEditor : Editor {
        
        protected ScriptableItem item;
        private List<ScriptableItem> items;
        private bool showMetadata;
        private bool showDescription;

        protected virtual void OnEnable() {
            item = (ScriptableItem)target;
            items = EditorUtilities.GetAllInstances<ScriptableItem>();
            showMetadata = true;
            TryToGenerateUID();
        }

        public override void OnInspectorGUI() {
            DrawHeader("ITEM");
            DrawInspector();
            SaveInspector();
        }

        protected virtual void DrawInspector (bool drawDefaults = true) {
            if (GUILayout.Button("DUPLICATE")) {
                EditorUtilities.DuplicateScriptableObject<ScriptableItem>();
            }

            showMetadata = EditorGUILayout.Foldout(showMetadata, "Metadata");

            if (showMetadata) {
                EditorGUI.indentLevel++;
                EditorGUILayout.BeginHorizontal();
                item.uid = EditorGUILayout.TextField("UID", item.uid);
                GUIStyle style = new GUIStyle(GUI.skin.button);
                style.normal.textColor = Color.white;
                if (GUILayout.Button("COPY", style)) {
                    GUIUtility.systemCopyBuffer = item.uid;
                }
                EditorGUILayout.EndHorizontal();
                item.label = EditorGUILayout.TextField("Label", item.label);

                showDescription = EditorGUILayout.Foldout(showDescription, "Description");
                if (showDescription) {
                    EditorGUI.indentLevel++;
                    EditorStyles.textField.wordWrap = true;
                    item.description = EditorGUILayout.TextArea(item.description);
                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            if (drawDefaults)
                DrawDefaultInspector();
        }

        protected void SaveInspector () {
            // Save the changes back to the object
            if (GUI.changed) {
                TryToGenerateUID(true);
                EditorUtility.SetDirty(target);
            }
        }

        protected void DrawHeader (string header) {
            EditorUtilities.DrawHeader(header);
        }

        private void TryToGenerateUID (bool logWarning = false) {
            if (String.IsNullOrWhiteSpace(item.uid) || items.FindAll(i => i.uid == item.uid).Count > 1) {
                string wrongUid = item.uid;
                item.uid = EditorUtilities.GenerateUid("D");
                if (logWarning)
                    ConstantsData_M.LogInfo($"UID of ScriptableItem ({wrongUid}) has been changed to ({item.uid}) due to duplicity!");
            }
        }
    }
}

#endif