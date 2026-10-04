//----------------------------------------------
//           	   Highway Racer
//
// Copyright © 2014 - 2021 BoneCracker Games
// http://www.bonecrackergames.com
//
//----------------------------------------------

using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class HR_InitOnLoadPhoton {

    [InitializeOnLoadMethod]
    static void InitOnLoad() {

        EditorApplication.delayCall += EditorUpdate;

    }

    public static void EditorUpdate() {

        bool hasKey = false;

#if BCG_HR_PHOTON
        hasKey = true;
#endif

        if (!hasKey) {

        //    EditorUtility.DisplayDialog("Highway Racer Photon PUN2 Installation", "Please read the documentation about Photon installation first.", "Ok");

        }

        CarRace .RCC_SetScriptingSymbol.SetEnabled("BCG_HR_PHOTON", true);

    }

}