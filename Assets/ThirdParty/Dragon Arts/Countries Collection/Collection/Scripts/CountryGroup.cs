using System.Collections.Generic;
using UnityEngine;
using DragonArts.Common;
using System;
using System.Linq;

namespace DragonArts.Collection.Countries {

    public class CountryGroup : ScriptableItem {
        [HideInInspector]
        public List<Country> list;

        public CountryGroup () {
            list = new List<Country>();
        }

        public Country GetCountryByISOCode (string isoCode2) {
            if (String.IsNullOrWhiteSpace(isoCode2)) return null;
            return list.FirstOrDefault(c => c.isoCode2 == isoCode2.ToUpper());
        }

        public Country GetCountryByISOCode3 (string isoCode3) {
            if (String.IsNullOrWhiteSpace(isoCode3)) return null;
            return list.FirstOrDefault(c => c.isoCode3 == isoCode3.ToUpper());
        }
    }
}
