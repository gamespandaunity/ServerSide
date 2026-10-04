using UnityEngine;
using System.Collections;
using System.Linq;
using UnityEngine.Serialization;


public class Track_Config : MonoBehaviour
{


    //[HideInInspector]
    [FormerlySerializedAs("modulo")] public GameObject[] trackModules;

    [FormerlySerializedAs("_tipo")][HideInInspector] public int[] moduleTypes;
    [FormerlySerializedAs("_begin")][HideInInspector] public int[] segmentStartIndices;
    [FormerlySerializedAs("_end")][HideInInspector] public int[] segmentEndIndices;
    [FormerlySerializedAs("_idx")][HideInInspector] public int[] moduleIndices;

    [FormerlySerializedAs("indexMaterialGrass")][HideInInspector] public int grassMaterialIndex  = 0;
    [FormerlySerializedAs("indexMaterialRoad")][HideInInspector] public int roadMaterialIndex  = 0;
    [FormerlySerializedAs("indexMaterialFences")][HideInInspector] public int fenceMaterialIndex = 0;

  //  public void Newlist(int i)
    public void InitializeModuleList(int i)
    {
        trackModules = new GameObject[i];
        moduleTypes = new int[i];
        segmentStartIndices = new int[i];
        segmentEndIndices = new int[i];
        moduleIndices = new int[i];
    }

    private bool isMobile;

   // public void AddConfig(int index, int tipo, int begin, int end, int idx)
    public void AddModuleConfig(int index, int tipo, int begin, int end, int idx)
    {
        moduleTypes[index] = tipo;
        segmentStartIndices[index] = begin;
        segmentEndIndices[index] = end;
        moduleIndices[index] = idx;
    }

    void Awake()
    {
        isMobile = ((Application.platform == RuntimePlatform.IPhonePlayer || (Application.platform == RuntimePlatform.Android)));
        if (isMobile)
        {

            GameObject[] disabledOnMobile = GameObject.FindObjectsOfType(typeof(GameObject)).Select(g => g as GameObject).Where(g => g.name.Equals("AutoDisabledOnMobile")).ToArray();
            int n = disabledOnMobile.Length;
            if (n > 0)
                for (int i = 0; i < n; i++)
                    Destroy(disabledOnMobile[i]);

            disabledOnMobile = null;
        }
    }

   // public GameObject GetModule(int i)
    public GameObject GetTrackModule(int i)
    {
        return trackModules[i];
    }
  //  public int GetModuleLength()
    public int GetTotalModules()
    {
        return trackModules.Length;
    }
}
