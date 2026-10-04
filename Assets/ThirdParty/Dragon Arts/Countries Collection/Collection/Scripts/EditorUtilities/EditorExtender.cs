#if UNITY_EDITOR

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

namespace DragonArts.Collection.Countries {

    public class EditorExtender : EditorWindow {

        [MenuItem("Assets/Create/Dragon Arts/Collection/Country Group", false, 1)]
        private static void CreateSet() {
            CountryGroup asset = ScriptableObject.CreateInstance<CountryGroup> ();
            asset.uid = DragonArts.Common.EditorUtilities.GenerateUid("CG");
            DragonArts.Common.EditorUtilities.CreateScriptableObject<CountryGroup>(asset, "New Country Group");
        }
    }
}

#endif