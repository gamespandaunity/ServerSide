
#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.IO;
using System;
using DragonArts.Common;

namespace DragonArts.Collection.Countries {

    [CustomEditor(typeof(CountryGroup))]
    public class CountryGroupEditor : ScriptableItemEditor {

        private ReorderableList reorderable;
        private CountryGroup countryGroup;

        protected override void OnEnable() {
            base.OnEnable();
            countryGroup = (CountryGroup)target;

            reorderable = new ReorderableList(countryGroup.list, typeof(Country));
            reorderable.drawHeaderCallback = OnHeaderCallback;
            reorderable.drawElementCallback = OnElementCallback;
            reorderable.elementHeightCallback = (index) => {
                return OnElementHeightCallback(index);
            };
        }

        private void OnHeaderCallback(Rect rect) {
            EditorGUI.LabelField(rect, $"List ({countryGroup.list.Count})");
        }

        private int OnElementHeightCallback (int index) {
            return 130;
        }

        private void OnElementCallback(Rect rect, int index, bool isActive, bool isFocused) {
            if (countryGroup.list.Count <= 0)
                return;

            countryGroup.list[index].name = EditorGUI.TextField(new Rect(rect.x + 25, rect.y + 5, rect.width - 25, 20), "Name", countryGroup.list[index].name);
            countryGroup.list[index].isoCode2 = EditorGUI.TextField(new Rect(rect.x + 25, rect.y + 30, rect.width - 25, 20), "ISO Code", countryGroup.list[index].isoCode2);
            countryGroup.list[index].isoCode3 = EditorGUI.TextField(new Rect(rect.x + 25, rect.y + 55, rect.width - 25, 20), "ISO Code (3)", countryGroup.list[index].isoCode3);
            countryGroup.list[index].capitalCity = EditorGUI.TextField(new Rect(rect.x + 25, rect.y + 80, rect.width - 25, 20), "Capital City", countryGroup.list[index].capitalCity);
            countryGroup.list[index].dialingCode = EditorGUI.TextField(new Rect(rect.x + 25, rect.y + 105, rect.width - 25, 20), "Dialing Code", countryGroup.list[index].dialingCode);
        }

        public override void OnInspectorGUI() {
            DrawHeader("COUNTRY GROUP");
            DrawInspector();
            SaveInspector();
        }

        protected override void DrawInspector (bool drawDefaults = true) {
            base.DrawInspector(false);
            EditorGUILayout.Space();
            if (GUILayout.Button("SYNCHRONIZE")) {
                Synchronize();
            }
            EditorGUILayout.Space();
            reorderable.DoLayoutList();
            EditorGUILayout.Space();
            if (drawDefaults)
                DrawDefaultInspector();
        }

        private void Synchronize () {
            CountryGroup cg = (CountryGroup)AssetDatabase.LoadAssetAtPath("Assets/Dragon Arts/Countries Collection/Collection/Countries.asset", typeof(CountryGroup));

            foreach (Country c in countryGroup.list) {
                Country cc = null;
                if (!String.IsNullOrWhiteSpace(c.isoCode2)) {
                    cc = cg.list.FirstOrDefault(i => i.isoCode2 == c.isoCode2.ToUpper());
                } else if (!String.IsNullOrWhiteSpace(c.isoCode3)) {
                    cc = cg.list.FirstOrDefault(i => i.isoCode3 == c.isoCode3.ToUpper());
                }

                if (cc != null) {
                    c.name = cc.name;
                    c.isoCode2 = cc.isoCode2;
                    c.isoCode3 = cc.isoCode3;
                    c.capitalCity = cc.capitalCity;
                    c.dialingCode = cc.dialingCode;
                }
            }
        }
    }
}

#endif
