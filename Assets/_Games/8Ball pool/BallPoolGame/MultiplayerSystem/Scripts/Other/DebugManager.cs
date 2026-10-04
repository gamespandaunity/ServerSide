using UnityEngine;
using UnityEngine.UI;

namespace NetworkManagement
{
    public class DebugManager : MonoBehaviour
    {
        [SerializeField] private Text text;
        private static DebugManager instance;
        [SerializeField] private int linesMaxCount;
        private int linesCount;

        public static void DebugLog(string value)
        {
            #if UNITY_EDITOR
            //Debug.Log(value);
            #else
            if(instance)
            {
                instance.DebugValue(value);
            }
            #endif
        }

        private void DebugValue(string value)
        {
            linesCount++;
            if (linesCount > linesMaxCount)
            {
                linesCount = 0;
                text.text = "";
            }
            text.text += value + "\n";
        }

        void Awake()
        {
            #if UNITY_EDITOR
            text.gameObject.SetActive(false);
            gameObject.SetActive(false);
            #else
            if (instance)
            {
                Destroy(gameObject);
            }
            else
            {
                DontDestroyOnLoad(gameObject);
                instance = this;
            }
            #endif
        }
    }
}
