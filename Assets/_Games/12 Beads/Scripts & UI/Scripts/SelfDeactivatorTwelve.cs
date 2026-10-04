using UnityEngine;
using System.Collections;
using UnityEngine.Serialization;
namespace Twelve
{
    public class SelfDeactivatorTwelve : MonoBehaviour
    {

        [FormerlySerializedAs("timeToDecative")] public float timeToDeactivate;

        // Use this for initialization
        void OnEnable()
        {
            Invoke("DisableObject", timeToDeactivate);
        }

        // Update is called once per frame
        void DisableObject()
        {
            gameObject.SetActive(false);
        }
    }
}
