using UnityEngine;

namespace DragonArts.Common
{

    public class ScriptableItem : ScriptableObject {
        [HideInInspector]
        public string uid;
        [HideInInspector]
        public string label;
        [HideInInspector]
        public string description;
    }
}
