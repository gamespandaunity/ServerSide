//----------------------------------------------
//                  Smart AI Car
//
// Copyright © 2015 BoneCracker Games
// http://www.bonecrackergames.com
//
//----------------------------------------------

using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(SAICSmartAICar)), CanEditMultipleObjects]
public class SAICEditor : Editor {

	SAICSmartAICar aiScript;

	[MenuItem("Tools/BoneCracker Games/Smart AI Car/Add Smart AI Car To Vehicle")]
	static void CreateAIBehavior(){
		
		if(!Selection.activeGameObject.GetComponent<SAICSmartAICar>()){
			Selection.activeGameObject.AddComponent<SAICSmartAICar>();
		}else if(Selection.activeGameObject.GetComponent<SAICSmartAICar>()){	
			EditorUtility.DisplayDialog("Your Vehicle Already Has Smart AI Car Controller", "Your Vehicle Already Has Smart AI Car Controller", "Ok");
		}
		
	}
	
	[MenuItem("Tools/BoneCracker Games/Smart AI Car/Add Waypoints Container To Scene")]
	static void CreateWaypointsContainer(){
		
		if(GameObject.FindObjectOfType<SAICWaypointsContainer>() == null){
			
			GameObject wp = new GameObject("Waypoints Container");
			wp.transform.position = Vector3.zero;
			wp.transform.rotation = Quaternion.identity;
			wp.AddComponent<SAICWaypointsContainer>();
			Selection.activeGameObject = wp;
			
		}else{
			EditorUtility.DisplayDialog("Your Scene Already Has Waypoints Container", "Your Scene Already Has Waypoints Container", "Ok");
		}
		
	}
	
	[MenuItem("Tools/BoneCracker Games/Smart AI Car/Add BrakeZones Container To Scene")]
	static void CreateBrakeZonesContainer(){
		
		if(GameObject.FindObjectOfType<SAICBrakeZonesContainer>() == null){
			
			GameObject bz = new GameObject("Brake Zones Container");
			bz.transform.position = Vector3.zero;
			bz.transform.rotation = Quaternion.identity;
			bz.AddComponent<SAICBrakeZonesContainer>();
			Selection.activeGameObject = bz;
			
		}else{
			EditorUtility.DisplayDialog("Your Scene Already Has Brake Zones Container", "Your Scene Already Has Brake Zones", "Ok");
		}
		
	}

	public override void OnInspectorGUI () {

		serializedObject.Update();

		aiScript = (SAICSmartAICar)target;

		if(GUILayout.Button("Create Wheel Colliders")){
			
			WheelCollider[] wheelColliders = aiScript.gameObject.GetComponentsInChildren<WheelCollider>();
			
			if(wheelColliders.Length >= 1)
				ConstantsData_M.Log("Your Vehicle has Wheel Colliders already!");
			else
				aiScript.CreateWheelColliders();
			
		}

		DrawDefaultInspector();
	
		if(GUI.changed && !EditorApplication.isPlaying){
			EngineCurveInit();
		}

		serializedObject.ApplyModifiedProperties();

	}

	void EngineCurveInit (){
		
		if(aiScript.totalGears <= 0){
			ConstantsData_M.Log("You are trying to set your vehicle gear to 0 or below! Why you trying to do this???");
			return;
		}
		
		aiScript.gearSpeed = new float[aiScript.totalGears];
		aiScript.engineTorqueCurve = new AnimationCurve[aiScript.totalGears];
		aiScript.currentGear = 0;
		
		for(int i = 0; i < aiScript.engineTorqueCurve.Length; i ++){
			aiScript.engineTorqueCurve[i] = new AnimationCurve(new Keyframe(0, 1));
		}
		
		for(int i = 0; i < aiScript.totalGears; i ++){
			
			aiScript.gearSpeed[i] = Mathf.Lerp(0, aiScript.maximumSpeed / 1.5f, ((float)(i+1)/(float)(aiScript.totalGears))) * Mathf.Lerp(1f, 1f, (float)i / aiScript.totalGears);
			
			if(i != 0){
				aiScript.engineTorqueCurve[i].MoveKey(0, new Keyframe(0, Mathf.Lerp (.5f, 0, (float)(i+1) / (float)aiScript.totalGears)));
				aiScript.engineTorqueCurve[i].AddKey(Mathf.Lerp(0, aiScript.maximumSpeed / 1.5f, ((float)(i)/(float)(aiScript.totalGears))), Mathf.Lerp(1f, .75f, ((float)(i)/(float)(aiScript.totalGears))));
				aiScript.engineTorqueCurve[i].AddKey(Mathf.Lerp (0, aiScript.maximumSpeed, (float)(i+1) / (float)aiScript.totalGears), 0f);
				aiScript.engineTorqueCurve[i].postWrapMode = WrapMode.Clamp;
			}else{
				aiScript.engineTorqueCurve[i].MoveKey(0, new Keyframe(0, 1));
				aiScript.engineTorqueCurve[i].AddKey(Mathf.Lerp (0, aiScript.maximumSpeed / 1.5f, (float)(i+1) / (float)aiScript.totalGears), .75f);
				aiScript.engineTorqueCurve[i].AddKey(Mathf.Lerp(25, aiScript.maximumSpeed / 1f, ((float)(i+1) / (float)(aiScript.totalGears))), 0f);
				aiScript.engineTorqueCurve[i].postWrapMode = WrapMode.Clamp;
			}
			
		}
		
	}

}
