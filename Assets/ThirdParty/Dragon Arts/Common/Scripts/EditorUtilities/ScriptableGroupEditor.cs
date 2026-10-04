
#if UNITY_EDITOR

using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;

namespace DragonArts.Common
{

    [CustomEditor(typeof(ScriptableGroup))]
    public class ScriptableGroupEditor : ScriptableItemEditor {

        private ReorderableList reorderable;
        private ScriptableGroup group;
        private List<string> itemUids = new List<string>();

        protected override void OnEnable() {
            base.OnEnable();
            group = (ScriptableGroup)target;

            reorderable = new ReorderableList(group.list, typeof(ScriptableGroupItem));
            reorderable.drawHeaderCallback = OnHeaderCallback;
            reorderable.drawElementCallback = OnElementCallback;
            reorderable.onAddCallback = OnAddCallback;
            reorderable.elementHeightCallback = (index) => {
                return OnElementHeightCallback(index);
            };

            itemUids.Clear();
            foreach (ScriptableGroupItem item in group.list) {
                if (String.IsNullOrWhiteSpace(item.uid)) item.uid = GenerateItemUid();
                itemUids.Add(item.uid);
            }
        }

        private string GenerateItemUid () {
            System.Random random = new System.Random();
            string chars = "AaBbCcDdEeFfGgHhIiJjKkLlMmNnOoPpQqRrSsTtUuVvWwXxYyZz0123456789";

            string newUid = DragonArts.Common.Utilities.GenerateUid(chars, random, 5);

            while (itemUids.Contains(newUid)) {
                newUid = DragonArts.Common.Utilities.GenerateUid(chars, random, 5);
            }

            return newUid;
        }

        private void OnHeaderCallback(Rect rect) {
            EditorGUI.LabelField(rect, $"List ({group.list.Count})");
        }

        private int OnElementHeightCallback (int index) {
            return group.quantified ? 80 : 55;
        }

        private void OnElementCallback(Rect rect, int index, bool isActive, bool isFocused) {
            if (group.list.Count <= 0)
                return;

            EditorGUILayout.BeginHorizontal();
            group.list[index].uid = EditorGUI.TextField(new Rect(rect.x + 25, rect.y + 5, rect.width - 25, 20), "UID", group.list[index].uid);
            if (GUI.Button(new Rect(rect.x + 50, rect.y + 5 , 75, 20), DragonArts.Common.EditorUtilities.GetCopyIcon(), new GUIStyle())) {
                GUIUtility.systemCopyBuffer = group.list[index].uid;
            }
            EditorGUILayout.EndHorizontal();

            group.list[index].item = EditorGUI.ObjectField(new Rect(rect.x + 25, rect.y + 30, rect.width - 25, 20), "Item", group.list[index].item, typeof(ScriptableItem), false) as ScriptableItem;
            if (group.quantified) {
                int quantity = group.list[index].quantity;
                group.list[index].quantity = EditorGUI.IntField(new Rect(rect.x + 25, rect.y + 55, rect.width - 25, 20), "Quantity", quantity < 1 ? 1 : quantity);
            }   
        }
        
        private void OnAddCallback (ReorderableList list) {
            ReorderableList.defaultBehaviours.DoAddButton(list);
            group.list[group.list.Count-1].uid = GenerateItemUid();
        }

        public override void OnInspectorGUI() {
            DrawHeader("GROUP");
            DrawInspector();
            SaveInspector();
        }

        protected override void DrawInspector (bool drawDefaults = true) {
            base.DrawInspector(false);
            group.quantified = EditorGUILayout.Toggle("Quantified", group.quantified);
            EditorGUILayout.Space();
            reorderable.DoLayoutList();
            EditorGUILayout.Space();
            if (drawDefaults)
                DrawDefaultInspector();
        }
    }
}

#endif
