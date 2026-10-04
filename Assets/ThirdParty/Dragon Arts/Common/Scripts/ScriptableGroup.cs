using System.Collections.Generic;
using UnityEngine;
using System;

namespace DragonArts.Common
{

    [Serializable]
    public class ScriptableGroupItem {
        public string uid;
        public ScriptableItem item;
        public int quantity;
    }

    public class ScriptableGroup : ScriptableItem {
        [HideInInspector]
        public bool quantified;

        [HideInInspector]
        public List<ScriptableGroupItem> list;

        public ScriptableGroup () {
            list = new List<ScriptableGroupItem>();
        }

        public List<T> GetItems<T> () where T: ScriptableItem {
            List<T> items = new List<T>();
            foreach (ScriptableGroupItem i in list) {
                if (i.item is T) items.Add((T)i.item);
            }
            return items;
        }
    }
}
