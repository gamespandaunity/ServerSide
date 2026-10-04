//----------------------------------------------
//           	   Highway Racer
//
// Copyright © 2014 - 2021 BoneCracker Games
// http://www.bonecrackergames.com
//
//----------------------------------------------

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using CarRace;

#if PHOTON_UNITY_NETWORKING
//using Photon;
 
 
#endif

#if PHOTON_UNITY_NETWORKING && BCG_HR_PHOTON

/// <summary>
/// Modification applier for vehicles. Needs to be attached to the vehicle.
/// 7 Upgrade managers for paint, wheel, upgrade, neon, decal, spoiler, and siren.
/// </summary>
[AddComponentMenu("BoneCracker Games/Highway Racer/Player/HR Mod Applier")]
public class HR_ModApplier : MonoBehaviour{

    #region All upgrade managers

    private HR_VehicleUpgrade_UpgradeManager _upgradeManager;
    public HR_VehicleUpgrade_UpgradeManager upgradeManager {

        get {

            if (_upgradeManager == null)
                _upgradeManager = GetComponentInChildren<HR_VehicleUpgrade_UpgradeManager>();

            return _upgradeManager;

        }

    }

    private HR_VehicleUpgrade_PaintManager _paintManager;
    public HR_VehicleUpgrade_PaintManager paintManager {

        get {

            if (_paintManager == null)
                _paintManager = GetComponentInChildren<HR_VehicleUpgrade_PaintManager>();

            return _paintManager;

        }

    }

    private HR_VehicleUpgrade_WheelManager _wheelManager;
    public HR_VehicleUpgrade_WheelManager wheelManager {

        get {

            if (_wheelManager == null)
                _wheelManager = GetComponentInChildren<HR_VehicleUpgrade_WheelManager>();

            return _wheelManager;

        }

    }

    private HR_VehicleUpgrade_DecalManager _decalManager;
    public HR_VehicleUpgrade_DecalManager decalManager {

        get {

            if (_decalManager == null)
                _decalManager = GetComponentInChildren<HR_VehicleUpgrade_DecalManager>();

            return _decalManager;

        }

    }

    private HR_VehicleUpgrade_SpoilerManager _spoilerManager;
    public HR_VehicleUpgrade_SpoilerManager spoilerManager {

        get {

            if (_spoilerManager == null)
                _spoilerManager = GetComponentInChildren<HR_VehicleUpgrade_SpoilerManager>();

            return _spoilerManager;

        }

    }

    private HR_VehicleUpgrade_NeonManager _neonManager;
    public HR_VehicleUpgrade_NeonManager neonManager {

        get {

            if (_neonManager == null)
                _neonManager = GetComponentInChildren<HR_VehicleUpgrade_NeonManager>();

            return _neonManager;

        }

    }

    private HR_VehicleUpgrade_SirenManager _sirenManager;
    public HR_VehicleUpgrade_SirenManager sirenManager {

        get {

            if (_sirenManager == null)
                _sirenManager = GetComponentInChildren<HR_VehicleUpgrade_SirenManager>();

            return _sirenManager;

        }

    }

    #endregion

    private RCC_CarControllerV3 _carController;
    public RCC_CarControllerV3 carController {

        get {

            if (!_carController)
                _carController = GetComponent<RCC_CarControllerV3>();

            return _carController;

        }

    }

    //  Local and networked indexes.
    private int spoilerIndex = -1;
    private int N_spoilerIndex = -1;
    private int neonIndex = -1;
    private int N_neonIndex = -1;
    private int sirenIndex = -1;
    private int N_sirenIndex = -1;
    private Color paint = new Color(1f, 1f, 1f, 1f);
    private Color N_paint = new Color(1f, 1f, 1f, 0f);
    private int wheelIndex = -1;
    private int N_wheelIndex = -1;

    private int decal_FIndex = -1;
    private int decal_BIndex = -1;
    private int decal_LIndex = -1;
    private int decal_RIndex = -1;

    private int N_decal_FIndex = -1;
    private int N_decal_BIndex = -1;
    private int N_decal_LIndex = -1;
    private int N_decal_RIndex = -1;

    private bool initialized = false;

    private void Awake() {

        //  Make sure all visual parts are disabled at start.

        if (spoilerManager)
            spoilerManager.DisableAll();

        if (neonManager)
            neonManager.DisableAll();

        if (sirenManager)
            sirenManager.DisableAll();

        if (decalManager)
            decalManager.DisableAll();

    }

    public  void OnEnable() {

        //  Initialize all upgrade managers if we own this vehicle and stream corresponding indexes.
        //  If we are not owner of this vehicle, receive all indexes and enable corresponding indexes.

        bool networkVehicle = false;
        bool networkVehicleMine = false;

        //if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
        //    networkVehicle = true;

        //if (networkVehicle && photonView.IsMine)
            networkVehicleMine = true;

        if (networkVehicle && !networkVehicleMine)
            return;

        if (spoilerManager)
            spoilerManager.Initialize();

        if (sirenManager)
            sirenManager.Initialize();

        if (neonManager)
            neonManager.Initialize();

        if (wheelManager)
            wheelManager.Initialize();

        if (paintManager)
            paintManager.Initialize();

        if (upgradeManager)
            upgradeManager.Initialize();

        if (decalManager)
            decalManager.Initialize();

        if (spoilerManager) {

            foreach (Transform item in spoilerManager.transform) {

                if (item != spoilerManager.transform && item.gameObject.activeSelf)
                    spoilerIndex = item.GetSiblingIndex();

            }

        }

        if (neonManager) {

            foreach (Transform item in neonManager.transform) {

                if (item != neonManager.transform && item.gameObject.activeSelf)
                    neonIndex = item.GetSiblingIndex();

            }

        }

        if (sirenManager) {

            foreach (Transform item in sirenManager.transform) {

                if (item != sirenManager.transform && item.gameObject.activeSelf)
                    sirenIndex = item.GetSiblingIndex();

            }

        }

        if (paintManager) {

            HR_VehicleUpgrade_Paint[] paints = paintManager.transform.GetComponentsInChildren<HR_VehicleUpgrade_Paint>();

            foreach (HR_VehicleUpgrade_Paint item in paints) {

                if (item.bodyRenderer)
                    paint = item.bodyRenderer.materials[item.index].color;

            }

        }

        if (wheelManager)
            wheelIndex = PlayerPrefs.GetInt(transform.root.name + "SelectedWheel", -1);

        if (decalManager) {

            decal_FIndex = decalManager.decal[0].lastSelected;
            decal_BIndex = decalManager.decal[1].lastSelected;
            decal_LIndex = decalManager.decal[2].lastSelected;
            decal_RIndex = decalManager.decal[3].lastSelected;

        }

    }

    private void FixedUpdate() {

        //if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom && photonView && !photonView.IsMine) {

        //    if (initialized)
        //        return;

        //    if (spoilerManager && N_spoilerIndex != -1)
        //        spoilerManager.transform.GetChild(N_spoilerIndex).gameObject.SetActive(true);

        //    if (neonManager && N_neonIndex != -1)
        //        neonManager.transform.GetChild(N_neonIndex).gameObject.SetActive(true);

        //    if (sirenManager && N_sirenIndex != -1)
        //        sirenManager.transform.GetChild(N_sirenIndex).gameObject.SetActive(true);

        //    if (paintManager) {

        //        HR_VehicleUpgrade_Paint[] paints = paintManager.transform.GetComponentsInChildren<HR_VehicleUpgrade_Paint>();

        //        foreach (HR_VehicleUpgrade_Paint item in paints) {

        //            if (item.bodyRenderer && N_paint != new Color(1f, 1f, 1f, 0f))
        //                item.bodyRenderer.materials[item.index].color = N_paint;

        //        }

        //    }

        //    if (wheelManager) {

        //        if (N_wheelIndex != -1)
        //            RCC_Customization.ChangeWheels(GetComponent<RCC_CarControllerV3>(), HR_Wheels.Instance.wheels[N_wheelIndex].wheel, true);

        //    }

        //    if (decalManager) {

        //        if (N_decal_FIndex != -1) {

        //            decalManager.selectedIndex = 0;
        //            decalManager.SetDecalMaterial(N_decal_FIndex);

        //        }

        //        if (N_decal_BIndex != -1) {

        //            decalManager.selectedIndex = 1;
        //            decalManager.SetDecalMaterial(N_decal_BIndex);

        //        }

        //        if (N_decal_LIndex != -1) {

        //            decalManager.selectedIndex = 2;
        //            decalManager.SetDecalMaterial(N_decal_LIndex);

        //        }

        //        if (N_decal_RIndex != -1) {

        //            decalManager.selectedIndex = 3;
        //            decalManager.SetDecalMaterial(N_decal_RIndex);

        //        }

        //    }

        //    initialized = true;

        //}

    }

  

    //public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    //{
    //    if (stream.IsWriting)
    //    {

    //        stream.SendNext(spoilerIndex);
    //        stream.SendNext(neonIndex);
    //        stream.SendNext(sirenIndex);
    //        stream.SendNext(paint.r);
    //        stream.SendNext(paint.g);
    //        stream.SendNext(paint.b);
    //        stream.SendNext(wheelIndex);
    //        stream.SendNext(decal_FIndex);
    //        stream.SendNext(decal_BIndex);
    //        stream.SendNext(decal_LIndex);
    //        stream.SendNext(decal_RIndex);

    //    }
    //    else if (stream.IsReading)
    //    {

    //        N_spoilerIndex = (int)stream.ReceiveNext();
    //        N_neonIndex = (int)stream.ReceiveNext();
    //        N_sirenIndex = (int)stream.ReceiveNext();
    //        N_paint.r = (float)stream.ReceiveNext();
    //        N_paint.g = (float)stream.ReceiveNext();
    //        N_paint.b = (float)stream.ReceiveNext();
    //        N_wheelIndex = (int)stream.ReceiveNext();
    //        N_decal_FIndex = (int)stream.ReceiveNext();
    //        N_decal_BIndex = (int)stream.ReceiveNext();
    //        N_decal_LIndex = (int)stream.ReceiveNext();
    //        N_decal_RIndex = (int)stream.ReceiveNext();

    //    }
    //}
}

#else

/// <summary>
/// Modification applier for vehicles. Needs to be attached to the vehicle.
/// 7 Upgrade managers for paint, wheel, upgrade, neon, decal, spoiler, and siren.
/// </summary>
[AddComponentMenu("BoneCracker Games/Highway Racer/Player/HR Mod Applier")]
public class HR_ModApplier : MonoBehaviour {

#region All upgrade managers

    private HR_VehicleUpgrade_UpgradeManager _upgradeManager;
    public HR_VehicleUpgrade_UpgradeManager upgradeManager {

        get {

            if (_upgradeManager == null)
                _upgradeManager = GetComponentInChildren<HR_VehicleUpgrade_UpgradeManager>();

            return _upgradeManager;

        }

    }

    private HR_VehicleUpgrade_PaintManager _paintManager;
    public HR_VehicleUpgrade_PaintManager paintManager {

        get {

            if (_paintManager == null)
                _paintManager = GetComponentInChildren<HR_VehicleUpgrade_PaintManager>();

            return _paintManager;

        }

    }

    private HR_VehicleUpgrade_WheelManager _wheelManager;
    public HR_VehicleUpgrade_WheelManager wheelManager {

        get {

            if (_wheelManager == null)
                _wheelManager = GetComponentInChildren<HR_VehicleUpgrade_WheelManager>();

            return _wheelManager;

        }

    }

    private HR_VehicleUpgrade_DecalManager _decalManager;
    public HR_VehicleUpgrade_DecalManager decalManager {

        get {

            if (_decalManager == null)
                _decalManager = GetComponentInChildren<HR_VehicleUpgrade_DecalManager>();

            return _decalManager;

        }

    }

    private HR_VehicleUpgrade_SpoilerManager _spoilerManager;
    public HR_VehicleUpgrade_SpoilerManager spoilerManager {

        get {

            if (_spoilerManager == null)
                _spoilerManager = GetComponentInChildren<HR_VehicleUpgrade_SpoilerManager>();

            return _spoilerManager;

        }

    }

    private HR_VehicleUpgrade_NeonManager _neonManager;
    public HR_VehicleUpgrade_NeonManager neonManager {

        get {

            if (_neonManager == null)
                _neonManager = GetComponentInChildren<HR_VehicleUpgrade_NeonManager>();

            return _neonManager;

        }

    }

    private HR_VehicleUpgrade_SirenManager _sirenManager;
    public HR_VehicleUpgrade_SirenManager sirenManager {

        get {

            if (_sirenManager == null)
                _sirenManager = GetComponentInChildren<HR_VehicleUpgrade_SirenManager>();

            return _sirenManager;

        }

    }

#endregion

    private RCC_CarControllerV3 _carController;
    public RCC_CarControllerV3 carController {

        get {

            if (!_carController)
                _carController = GetComponent<RCC_CarControllerV3>();

            return _carController;

        }

    }

    private void Awake() {

        if (spoilerManager)
            spoilerManager.DisableAll();

        if (neonManager)
            neonManager.DisableAll();

        if (sirenManager)
            sirenManager.DisableAll();

    }

    private void OnEnable() {

        if (spoilerManager)
            spoilerManager.Initialize();

        if (sirenManager)
            sirenManager.Initialize();

        if (neonManager)
            neonManager.Initialize();

        if (wheelManager)
            wheelManager.Initialize();

        if (paintManager)
            paintManager.Initialize();

        if (upgradeManager)
            upgradeManager.Initialize();

        if (decalManager)
            decalManager.Initialize();

    }

}

#endif