//----------------------------------------------
//           	   Highway Racer
//
// Copyright © 2014 - 2021 BoneCracker Games
// http://www.bonecrackergames.com
//
//----------------------------------------------

using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using CarRace;

[CustomEditor(typeof(HR_PlayerCars))]
public class HR_PlayerCarsEditor : Editor {

    HR_PlayerCars prop;

    Vector2 scrollPos;
    List<HR_PlayerCars.Cars> playerCars = new List<HR_PlayerCars.Cars>();

    Color orgColor;

    public override void OnInspectorGUI() {

        serializedObject.Update();
        prop = (HR_PlayerCars)target;
        orgColor = GUI.color;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Player Cars Editor", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("This editor will keep update necessary .asset files in your project. Don't change directory of the ''Resources/HR_Assets''.", EditorStyles.helpBox);
        EditorGUILayout.Space();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, false, false);

        EditorGUIUtility.labelWidth = 120f;

        GUILayout.Label("Player Cars", EditorStyles.boldLabel);

        EditorGUI.indentLevel++;

        for (int i = 0; i < prop.cars.Length; i++) {

            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.Space();

            if (prop.cars[i].playerCar)
                EditorGUILayout.LabelField(prop.cars[i].playerCar.name, EditorStyles.boldLabel);

            prop.cars[i].vehicleName = EditorGUILayout.TextField("Player Car Name", prop.cars[i].vehicleName, GUILayout.MaxWidth(475f));

            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            prop.cars[i].playerCar = (GameObject)EditorGUILayout.ObjectField("Player Car Prefab", prop.cars[i].playerCar, typeof(GameObject), false, GUILayout.MaxWidth(475f));

            if (GUILayout.Button("Edit RCC"))
                Selection.activeGameObject = prop.cars[i].playerCar.gameObject;

            EditorGUILayout.EndHorizontal();

            if (prop.cars[i].playerCar && prop.cars[i].playerCar.GetComponent<RCC_CarControllerV3>()) {

                if (prop.cars[i].playerCar.GetComponent<HR_ModApplier>() == null)
                    prop.cars[i].playerCar.AddComponent<HR_ModApplier>();

                if (prop.cars[i].playerCar.GetComponent<HR_PlayerHandler>() == null)
                    prop.cars[i].playerCar.AddComponent<HR_PlayerHandler>();

                EditorGUILayout.Space();

                if (GUI.changed)
                    EditorUtility.SetDirty(prop.cars[i].playerCar);

            } else {

                EditorGUILayout.HelpBox("Select A RCC Based Car", MessageType.Error);

            }

            EditorGUILayout.Space();

            if (prop.cars[i].price <= 0)
                prop.cars[i].unlocked = true;

            if (prop.cars != null && prop.cars[i] != null && prop.cars[i].playerCar) {

                EditorGUILayout.BeginHorizontal();
                prop.cars[i].playerCar.GetComponent<RCC_CarControllerV3>().maxEngineTorque = EditorGUILayout.FloatField("Engine", prop.cars[i].playerCar.GetComponent<RCC_CarControllerV3>().maxEngineTorque, GUILayout.MaxWidth(160f));
                prop.cars[i].playerCar.GetComponent<RCC_CarControllerV3>().brakeTorque = EditorGUILayout.FloatField("Brake", prop.cars[i].playerCar.GetComponent<RCC_CarControllerV3>().brakeTorque, GUILayout.MaxWidth(160f));
                prop.cars[i].playerCar.GetComponent<RCC_CarControllerV3>().steerHelperAngularVelStrength = EditorGUILayout.FloatField("Handling", prop.cars[i].playerCar.GetComponent<RCC_CarControllerV3>().steerHelperAngularVelStrength, GUILayout.MaxWidth(160f));
                prop.cars[i].playerCar.GetComponent<RCC_CarControllerV3>().steerHelperLinearVelStrength = prop.cars[i].playerCar.GetComponent<RCC_CarControllerV3>().steerHelperAngularVelStrength;
                prop.cars[i].playerCar.GetComponent<RCC_CarControllerV3>().maxspeed = EditorGUILayout.FloatField("Speed", prop.cars[i].playerCar.GetComponent<RCC_CarControllerV3>().maxspeed, GUILayout.MaxWidth(160f));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                prop.cars[i].playerCar.GetComponent<HR_PlayerHandler>().maxEngineTorque = EditorGUILayout.FloatField("Max Engine", prop.cars[i].playerCar.GetComponent<HR_PlayerHandler>().maxEngineTorque, GUILayout.MaxWidth(160f));
                prop.cars[i].playerCar.GetComponent<HR_PlayerHandler>().maxBrakeTorque = EditorGUILayout.FloatField("Max Brake", prop.cars[i].playerCar.GetComponent<HR_PlayerHandler>().maxBrakeTorque, GUILayout.MaxWidth(160f));
                prop.cars[i].playerCar.GetComponent<HR_PlayerHandler>().maxHandlingStrength = EditorGUILayout.FloatField("Max Handling", prop.cars[i].playerCar.GetComponent<HR_PlayerHandler>().maxHandlingStrength, GUILayout.MaxWidth(160f));
                prop.cars[i].playerCar.GetComponent<HR_PlayerHandler>().maxSpeed = EditorGUILayout.FloatField("Max Speed", prop.cars[i].playerCar.GetComponent<HR_PlayerHandler>().maxSpeed, GUILayout.MaxWidth(160f));
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                prop.cars[i].price = EditorGUILayout.IntField("Price", prop.cars[i].price, GUILayout.MaxWidth(200f));
                prop.cars[i].unlocked = EditorGUILayout.ToggleLeft("Unlocked", prop.cars[i].unlocked, GUILayout.MaxWidth(122f));
                EditorGUILayout.EndHorizontal();

                if (prop.cars[i].playerCar.GetComponent<HR_PlayerHandler>().maxEngineTorque < 0f)
                    prop.cars[i].playerCar.GetComponent<HR_PlayerHandler>().maxEngineTorque = 0;

                if (prop.cars[i].playerCar.GetComponent<HR_PlayerHandler>().maxBrakeTorque < 0f)
                    prop.cars[i].playerCar.GetComponent<HR_PlayerHandler>().maxBrakeTorque = 0f;

                if (prop.cars[i].playerCar.GetComponent<HR_PlayerHandler>().maxHandlingStrength < 0f)
                    prop.cars[i].playerCar.GetComponent<HR_PlayerHandler>().maxHandlingStrength = 0f;

                if (prop.cars[i].playerCar.GetComponent<HR_PlayerHandler>().maxEngineTorque > 1000f)
                    prop.cars[i].playerCar.GetComponent<HR_PlayerHandler>().maxEngineTorque = 1000;

                if (prop.cars[i].playerCar.GetComponent<HR_PlayerHandler>().maxBrakeTorque > 5500f)
                    prop.cars[i].playerCar.GetComponent<HR_PlayerHandler>().maxBrakeTorque = 5500f;

                if (prop.cars[i].playerCar.GetComponent<HR_PlayerHandler>().maxHandlingStrength > 1f)
                    prop.cars[i].playerCar.GetComponent<HR_PlayerHandler>().maxHandlingStrength = 1f;

            }

            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("\u2191", GUILayout.MaxWidth(25f)))
                Up(i);

            if (GUILayout.Button("\u2193", GUILayout.MaxWidth(25f)))
                Down(i);

            GUI.color = Color.red;

            if (GUILayout.Button("X", GUILayout.MaxWidth(25f)))
                RemoveCar(i);

            GUI.color = orgColor;

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();

        }

        GUI.color = Color.cyan;

        if (GUILayout.Button("Create Player Car"))
            AddNewCar();

        if (GUILayout.Button("--< Return To General Settings"))
            OpenGeneralSettings();

        GUI.color = orgColor;

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Highway Racer" + HR_Version.version + "\nCreated by Buğra Özdoğanlar\nBoneCrackerGames", EditorStyles.centeredGreyMiniLabel, GUILayout.MaxHeight(50f));

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
            EditorUtility.SetDirty(prop);

    }

    void AddNewCar() {

        playerCars.Clear();
        playerCars.AddRange(prop.cars);
        HR_PlayerCars.Cars newCar = new HR_PlayerCars.Cars();
        playerCars.Add(newCar);
        prop.cars = playerCars.ToArray();
        PlayerPrefs.SetInt("SelectedPlayerCarIndex", 0);

    }

    void RemoveCar(int index) {

        playerCars.Clear();
        playerCars.AddRange(prop.cars);
        playerCars.RemoveAt(index);
        prop.cars = playerCars.ToArray();
        PlayerPrefs.SetInt("SelectedPlayerCarIndex", 0);

    }

    void Up(int index) {

        if (index <= 0)
            return;

        playerCars.Clear();
        playerCars.AddRange(prop.cars);

        HR_PlayerCars.Cars currentCar = playerCars[index];
        HR_PlayerCars.Cars previousCar = playerCars[index - 1];

        playerCars.RemoveAt(index);
        playerCars.RemoveAt(index - 1);

        playerCars.Insert(index - 1, currentCar);
        playerCars.Insert(index, previousCar);

        prop.cars = playerCars.ToArray();
        PlayerPrefs.SetInt("SelectedPlayerCarIndex", 0);

    }

    void Down(int index) {

        if (index >= prop.cars.Length - 1)
            return;

        playerCars.Clear();
        playerCars.AddRange(prop.cars);

        //		foreach(HR_PlayerCars.Cars qwe in playerCars)
        //			Debug.Log(qwe.playerCar.name);

        HR_PlayerCars.Cars currentCar = playerCars[index];
        HR_PlayerCars.Cars nextCar = playerCars[index + 1];

        playerCars.RemoveAt(index);
        playerCars.Insert(index, nextCar);

        playerCars.RemoveAt(index + 1);
        playerCars.Insert(index + 1, currentCar);

        prop.cars = playerCars.ToArray();
        PlayerPrefs.SetInt("SelectedPlayerCarIndex", 0);

    }

    void OpenGeneralSettings() {

        Selection.activeObject = HR_HighwayRacerProperties.Instance;

    }

}
