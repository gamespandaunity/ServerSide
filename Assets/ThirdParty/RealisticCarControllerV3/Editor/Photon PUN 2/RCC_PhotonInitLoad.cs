namespace CarRace 
{
//----------------------------------------------
//            Realistic Car Controller
//
// Copyright © 2014 - 2022 BoneCracker Games
// http://www.bonecrackergames.com
// Buğra Özdoğanlar
//
//----------------------------------------------

using UnityEngine;
using UnityEditor;

public class RCC_PhotonInitLoad : MonoBehaviour {

	[InitializeOnLoad]
	public class InitOnLoad {

		static InitOnLoad(){

			RCC_SetScriptingSymbol.SetEnabled("RCC_PHOTON", true);

			if(!EditorPrefs.HasKey("RCC_Photon" + RCC_Version.version.ToString())){
				
				EditorPrefs.SetInt("RCC_Photon" + RCC_Version.version.ToString(), 1);
				EditorUtility.DisplayDialog("Photon PUN 2 For Realistic Car Controller", "Be sure you have imported latest Photon PUN 2 to your project. Pass in your AppID to Photon, and run the RCC City Photon 2 demo scene. You can find more detailed info in documentation.", "Close");

			}

		}

	}

}

}