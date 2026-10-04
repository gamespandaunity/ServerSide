using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
[CustomEditor(typeof(ObjectRepositioner))]
public class EditorObjectRepositioner : Editor {

	ObjectRepositioner wpScript;

	public override void  OnInspectorGUI () {

		serializedObject.Update();

		wpScript = (ObjectRepositioner)target;



		//EditorGUILayout.PropertyField(serializedObject.FindProperty("ECreator"), new GUIContent("ECreator", "ECreator"), true);

		EditorGUILayout.HelpBox("Reset Point By Shift + Left Mouse Button On Your Road", MessageType.Info);

		serializedObject.ApplyModifiedProperties();

	}

	void OnSceneGUI(){

		Event e = Event.current;
		wpScript = (ObjectRepositioner)target;

		if(e != null){

			if(e.isMouse && e.shift && e.type == EventType.MouseDown){

				Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
				RaycastHit hit = new RaycastHit();
				if (Physics.Raycast(ray, out hit, 5000.0f)) {

					Vector3 newTilePosition = hit.point;
					wpScript.transform.position = newTilePosition;



				}

			}

			if(wpScript)
				Selection.activeGameObject = wpScript.gameObject;

		}


	}
}
