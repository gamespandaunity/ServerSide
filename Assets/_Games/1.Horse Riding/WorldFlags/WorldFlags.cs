using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

public static class CountriesFlags
{
    public enum CountriesNames
    {
        ///<sumary>
        /// Andorra
        ///</sumary>
        AD,
        ///<sumary>
        /// United Arab Emirates
        ///</sumary>
        AE,
        ///<sumary>
        /// Afghanistan
        ///</sumary>
        AF,
        ///<sumary>
        /// Antigua and Barbuda
        ///</sumary>
        AG,
        ///<sumary>
        /// Anguilla
        ///</sumary>
        AI,
        ///<sumary>
        /// Albania
        ///</sumary>
        AL,
        ///<sumary>
        /// Armenia
        ///</sumary>
        AM,
        ///<sumary>
        /// Angola
        ///</sumary>
        AO,
        ///<sumary>
        /// Antarctica
        ///</sumary>
        AQ,
        ///<sumary>
        /// Argentina
        ///</sumary>
        AR,
        ///<sumary>
        /// American Samoa
        ///</sumary>
        AS,
        ///<sumary>
        /// Austria
        ///</sumary>
        AT,
        ///<sumary>
        /// Australia
        ///</sumary>
        AU,
        ///<sumary>
        /// Aruba
        ///</sumary>
        AW,
        ///<sumary>
        /// Åland Islands
        ///</sumary>
        AX,
        ///<sumary>
        /// Azerbaijan
        ///</sumary>
        AZ,
        ///<sumary>
        /// Bosnia and Herzegovina
        ///</sumary>
        BA,
        ///<sumary>
        /// Barbados
        ///</sumary>
        BB,
        ///<sumary>
        /// Bangladesh
        ///</sumary>
        BD,
        ///<sumary>
        /// Belgium
        ///</sumary>
        BE,
        ///<sumary>
        /// Burkina Faso
        ///</sumary>
        BF,
        ///<sumary>
        /// Bulgaria
        ///</sumary>
        BG,
        ///<sumary>
        /// Bahrain
        ///</sumary>
        BH,
        ///<sumary>
        /// Burundi
        ///</sumary>
        BI,
        ///<sumary>
        /// Benin
        ///</sumary>
        BJ,
        ///<sumary>
        /// Saint Barthélemy
        ///</sumary>
        BL,
        ///<sumary>
        /// Bermuda
        ///</sumary>
        BM,
        ///<sumary>
        /// Brunei Darussalam
        ///</sumary>
        BN,
        ///<sumary>
        /// Bolivia
        ///</sumary>
        BO,
        ///<sumary>
        /// Bonaire
        ///</sumary>
        BQ,
        ///<sumary>
        /// Brazil
        ///</sumary>
        BR,
        ///<sumary>
        /// Bahamas
        ///</sumary>
        BS,
        ///<sumary>
        /// Bhutan
        ///</sumary>
        BT,
        ///<sumary>
        /// Bouvet Island
        ///</sumary>
        BV,
        ///<sumary>
        /// Botswana
        ///</sumary>
        BW,
        ///<sumary>
        /// Belarus
        ///</sumary>
        BY,
        ///<sumary>
        /// Belize
        ///</sumary>
        BZ,
        ///<sumary>
        /// Canada
        ///</sumary>
        CA,
        ///<sumary>
        /// Cocos (Keeling) Islands
        ///</sumary>
        CC,
        ///<sumary>
        /// Congo
        ///</sumary>
        CD,
        ///<sumary>
        /// Central African Republic
        ///</sumary>
        CF,
        ///<sumary>
        /// Congo
        ///</sumary>
        CG,
        ///<sumary>
        /// Switzerland
        ///</sumary>
        CH,
        ///<sumary>
        /// Côte d'Ivoire
        ///</sumary>
        CI,
        ///<sumary>
        /// Cook Islands
        ///</sumary>
        CK,
        ///<sumary>
        /// Chile
        ///</sumary>
        CL,
        ///<sumary>
        /// Cameroon
        ///</sumary>
        CM,
        ///<sumary>
        /// China
        ///</sumary>
        CN,
        ///<sumary>
        /// Colombia
        ///</sumary>
        CO,
        ///<sumary>
        /// Costa Rica
        ///</sumary>
        CR,
        ///<sumary>
        /// Cuba
        ///</sumary>
        CU,
        ///<sumary>
        /// Cabo Verde
        ///</sumary>
        CV,
        ///<sumary>
        /// Curaçao
        ///</sumary>
        CW,
        ///<sumary>
        /// Christmas Island
        ///</sumary>
        CX,
        ///<sumary>
        /// Cyprus
        ///</sumary>
        CY,
        ///<sumary>
        /// Czech Republic
        ///</sumary>
        CZ,
        ///<sumary>
        /// Germany
        ///</sumary>
        DE,
        ///<sumary>
        /// Djibouti
        ///</sumary>
        DJ,
        ///<sumary>
        /// Denmark
        ///</sumary>
        DK,
        ///<sumary>
        /// Dominica
        ///</sumary>
        DM,
        ///<sumary>
        /// Dominican Republic
        ///</sumary>
        DO,
        ///<sumary>
        /// Algeria
        ///</sumary>
        DZ,
        ///<sumary>
        /// Ecuador
        ///</sumary>
        EC,
        ///<sumary>
        /// Estonia
        ///</sumary>
        EE,
        ///<sumary>
        /// Egypt
        ///</sumary>
        EG,
        ///<sumary>
        /// Western Sahara
        ///</sumary>
        EH,
        ///<sumary>
        /// Eritrea
        ///</sumary>
        ER,
        ///<sumary>
        /// Spain
        ///</sumary>
        ES,
        ///<sumary>
        /// Ethiopia
        ///</sumary>
        ET,
        ///<sumary>
        /// Finland
        ///</sumary>
        FI,
        ///<sumary>
        /// Fiji
        ///</sumary>
        FJ,
        ///<sumary>
        /// Falkland Islands (Malvinas)
        ///</sumary>
        FK,
        ///<sumary>
        /// Micronesia
        ///</sumary>
        FM,
        ///<sumary>
        /// Faroe Islands
        ///</sumary>
        FO,
        ///<sumary>
        /// France
        ///</sumary>
        FR,
        ///<sumary>
        /// Gabon
        ///</sumary>
        GA,
        ///<sumary>
        /// United Kingdom of Great Britain
        ///</sumary>
        GB,
        ///<sumary>
        /// Grenada
        ///</sumary>
        GD,
        ///<sumary>
        /// Georgia
        ///</sumary>
        GE,
        ///<sumary>
        /// French Guiana
        ///</sumary>
        GF,
        ///<sumary>
        /// Guernsey
        ///</sumary>
        GG,
        ///<sumary>
        /// Ghana
        ///</sumary>
        GH,
        ///<sumary>
        /// Gibraltar
        ///</sumary>
        GI,
        ///<sumary>
        /// Greenland
        ///</sumary>
        GL,
        ///<sumary>
        /// Gambia
        ///</sumary>
        GM,
        ///<sumary>
        /// Guinea
        ///</sumary>
        GN,
        ///<sumary>
        /// Guadeloupe
        ///</sumary>
        GP,
        ///<sumary>
        /// Equatorial Guinea
        ///</sumary>
        GQ,
        ///<sumary>
        /// Greece
        ///</sumary>
        GR,
        ///<sumary>
        /// South Georgia and the South Sandwich Islands
        ///</sumary>
        GS,
        ///<sumary>
        /// Guatemala
        ///</sumary>
        GT,
        ///<sumary>
        /// Guam
        ///</sumary>
        GU,
        ///<sumary>
        /// Guinea-Bissau
        ///</sumary>
        GW,
        ///<sumary>
        /// Guyana
        ///</sumary>
        GY,
        ///<sumary>
        /// Hong Kong
        ///</sumary>
        HK,
        ///<sumary>
        /// Heard Island and McDonald Islands
        ///</sumary>
        HM,
        ///<sumary>
        /// Honduras
        ///</sumary>
        HN,
        ///<sumary>
        /// Croatia
        ///</sumary>
        HR,
        ///<sumary>
        /// Haiti
        ///</sumary>
        HT,
        ///<sumary>
        /// Hungary
        ///</sumary>
        HU,
        ///<sumary>
        /// Indonesia
        ///</sumary>
        ID,
        ///<sumary>
        /// Ireland
        ///</sumary>
        IE,
        ///<sumary>
        /// Israel
        ///</sumary>
        IL,
        ///<sumary>
        /// Isle of Man
        ///</sumary>
        IM,
        ///<sumary>
        /// India
        ///</sumary>
        IN,
        ///<sumary>
        /// British Indian Ocean Territory
        ///</sumary>
        IO,
        ///<sumary>
        /// Iraq
        ///</sumary>
        IQ,
        ///<sumary>
        /// Iran
        ///</sumary>
        IR,
        ///<sumary>
        /// Iceland
        ///</sumary>
        IS,
        ///<sumary>
        /// Italy
        ///</sumary>
        IT,
        ///<sumary>
        /// Jersey
        ///</sumary>
        JE,
        ///<sumary>
        /// Jamaica
        ///</sumary>
        JM,
        ///<sumary>
        /// Jordan
        ///</sumary>
        JO,
        ///<sumary>
        /// Japan
        ///</sumary>
        JP,
        ///<sumary>
        /// Kenya
        ///</sumary>
        KE,
        ///<sumary>
        /// Kyrgyzstan
        ///</sumary>
        KG,
        ///<sumary>
        /// Cambodia
        ///</sumary>
        KH,
        ///<sumary>
        /// Kiribati
        ///</sumary>
        KI,
        ///<sumary>
        /// Comoros
        ///</sumary>
        KM,
        ///<sumary>
        /// Saint Kitts and Nevis
        ///</sumary>
        KN,
        ///<sumary>
        /// Korea
        ///</sumary>
        KP,
        ///<sumary>
        /// Korea
        ///</sumary>
        KR,
        ///<sumary>
        /// Kuwait
        ///</sumary>
        KW,
        ///<sumary>
        /// Cayman Islands
        ///</sumary>
        KY,
        ///<sumary>
        /// Kazakhstan
        ///</sumary>
        KZ,
        ///<sumary>
        /// Lao People's Democratic Republic
        ///</sumary>
        LA,
        ///<sumary>
        /// Lebanon
        ///</sumary>
        LB,
        ///<sumary>
        /// Saint Lucia
        ///</sumary>
        LC,
        ///<sumary>
        /// Liechtenstein
        ///</sumary>
        LI,
        ///<sumary>
        /// Sri Lanka
        ///</sumary>
        LK,
        ///<sumary>
        /// Liberia
        ///</sumary>
        LR,
        ///<sumary>
        /// Lesotho
        ///</sumary>
        LS,
        ///<sumary>
        /// Lithuania
        ///</sumary>
        LT,
        ///<sumary>
        /// Luxembourg
        ///</sumary>
        LU,
        ///<sumary>
        /// Latvia
        ///</sumary>
        LV,
        ///<sumary>
        /// Libya
        ///</sumary>
        LY,
        ///<sumary>
        /// Morocco
        ///</sumary>
        MA,
        ///<sumary>
        /// Monaco
        ///</sumary>
        MC,
        ///<sumary>
        /// Moldova
        ///</sumary>
        MD,
        ///<sumary>
        /// Montenegro
        ///</sumary>
        ME,
        ///<sumary>
        /// Saint Martin (French part)
        ///</sumary>
        MF,
        ///<sumary>
        /// Madagascar
        ///</sumary>
        MG,
        ///<sumary>
        /// Marshall Islands
        ///</sumary>
        MH,
        ///<sumary>
        /// Macedonia
        ///</sumary>
        MK,
        ///<sumary>
        /// Mali
        ///</sumary>
        ML,
        ///<sumary>
        /// Myanmar
        ///</sumary>
        MM,
        ///<sumary>
        /// Mongolia
        ///</sumary>
        MN,
        ///<sumary>
        /// Macao
        ///</sumary>
        MO,
        ///<sumary>
        /// Northern Mariana Islands
        ///</sumary>
        MP,
        ///<sumary>
        /// Martinique
        ///</sumary>
        MQ,
        ///<sumary>
        /// Mauritania
        ///</sumary>
        MR,
        ///<sumary>
        /// Montserrat
        ///</sumary>
        MS,
        ///<sumary>
        /// Malta
        ///</sumary>
        MT,
        ///<sumary>
        /// Mauritius
        ///</sumary>
        MU,
        ///<sumary>
        /// Maldives
        ///</sumary>
        MV,
        ///<sumary>
        /// Malawi
        ///</sumary>
        MW,
        ///<sumary>
        /// Mexico
        ///</sumary>
        MX,
        ///<sumary>
        /// Malaysia
        ///</sumary>
        MY,
        ///<sumary>
        /// Mozambique
        ///</sumary>
        MZ,
        ///<sumary>
        /// Namibia
        ///</sumary>
        NA,
        ///<sumary>
        /// New Caledonia
        ///</sumary>
        NC,
        ///<sumary>
        /// Niger
        ///</sumary>
        NE,
        ///<sumary>
        /// Norfolk Island
        ///</sumary>
        NF,
        ///<sumary>
        /// Nigeria
        ///</sumary>
        NG,
        ///<sumary>
        /// Nicaragua
        ///</sumary>
        NI,
        ///<sumary>
        /// Netherlands
        ///</sumary>
        NL,
        ///<sumary>
        /// Norway
        ///</sumary>
        NO,
        ///<sumary>
        /// Nepal
        ///</sumary>
        NP,
        ///<sumary>
        /// Nauru
        ///</sumary>
        NR,
        ///<sumary>
        /// Niue
        ///</sumary>
        NU,
        ///<sumary>
        /// New Zealand
        ///</sumary>
        NZ,
        ///<sumary>
        /// Oman
        ///</sumary>
        OM,
        ///<sumary>
        /// Panama
        ///</sumary>
        PA,
        ///<sumary>
        /// Peru
        ///</sumary>
        PE,
        ///<sumary>
        /// French Polynesia
        ///</sumary>
        PF,
        ///<sumary>
        /// Papua New Guinea
        ///</sumary>
        PG,
        ///<sumary>
        /// Philippines
        ///</sumary>
        PH,
        ///<sumary>
        /// Pakistan
        ///</sumary>
        PK,
        ///<sumary>
        /// Poland
        ///</sumary>
        PL,
        ///<sumary>
        /// Saint Pierre and Miquelon
        ///</sumary>
        PM,
        ///<sumary>
        /// Pitcairn
        ///</sumary>
        PN,
        ///<sumary>
        /// Puerto Rico
        ///</sumary>
        PR,
        ///<sumary>
        /// Palestine
        ///</sumary>
        PS,
        ///<sumary>
        /// Portugal
        ///</sumary>
        PT,
        ///<sumary>
        /// Palau
        ///</sumary>
        PW,
        ///<sumary>
        /// Paraguay
        ///</sumary>
        PY,
        ///<sumary>
        /// Qatar
        ///</sumary>
        QA,
        ///<sumary>
        /// Réunion
        ///</sumary>
        RE,
        ///<sumary>
        /// Romania
        ///</sumary>
        RO,
        ///<sumary>
        /// Serbia
        ///</sumary>
        RS,
        ///<sumary>
        /// Russian Federation
        ///</sumary>
        RU,
        ///<sumary>
        /// Rwanda
        ///</sumary>
        RW,
        ///<sumary>
        /// Saudi Arabia
        ///</sumary>
        SA,
        ///<sumary>
        /// Solomon Islands
        ///</sumary>
        SB,
        ///<sumary>
        /// Seychelles
        ///</sumary>
        SC,
        ///<sumary>
        /// Sudan
        ///</sumary>
        SD,
        ///<sumary>
        /// Sweden
        ///</sumary>
        SE,
        ///<sumary>
        /// Singapore
        ///</sumary>
        SG,
        ///<sumary>
        /// Saint Helena
        ///</sumary>
        SH,
        ///<sumary>
        /// Slovenia
        ///</sumary>
        SI,
        ///<sumary>
        /// Svalbard and Jan Mayen
        ///</sumary>
        SJ,
        ///<sumary>
        /// Slovakia
        ///</sumary>
        SK,
        ///<sumary>
        /// Sierra Leone
        ///</sumary>
        SL,
        ///<sumary>
        /// San Marino
        ///</sumary>
        SM,
        ///<sumary>
        /// Senegal
        ///</sumary>
        SN,
        ///<sumary>
        /// Somalia
        ///</sumary>
        SO,
        ///<sumary>
        /// Suriname
        ///</sumary>
        SR,
        ///<sumary>
        /// South Sudan
        ///</sumary>
        SS,
        ///<sumary>
        /// Sao Tome and Principe
        ///</sumary>
        ST,
        ///<sumary>
        /// El Salvador
        ///</sumary>
        SV,
        ///<sumary>
        /// Sint Maarten (Dutch part)
        ///</sumary>
        SX,
        ///<sumary>
        /// Syrian Arab Republic
        ///</sumary>
        SY,
        ///<sumary>
        /// Swaziland
        ///</sumary>
        SZ,
        ///<sumary>
        /// Turks and Caicos Islands
        ///</sumary>
        TC,
        ///<sumary>
        /// Chad
        ///</sumary>
        TD,
        ///<sumary>
        /// French Southern Territories
        ///</sumary>
        TF,
        ///<sumary>
        /// Togo
        ///</sumary>
        TG,
        ///<sumary>
        /// Thailand
        ///</sumary>
        TH,
        ///<sumary>
        /// Tajikistan
        ///</sumary>
        TJ,
        ///<sumary>
        /// Tokelau
        ///</sumary>
        TK,
        ///<sumary>
        /// Timor-Leste
        ///</sumary>
        TL,
        ///<sumary>
        /// Turkmenistan
        ///</sumary>
        TM,
        ///<sumary>
        /// Tunisia
        ///</sumary>
        TN,
        ///<sumary>
        /// Tonga
        ///</sumary>
        TO,
        ///<sumary>
        /// Turkey
        ///</sumary>
        TR,
        ///<sumary>
        /// Trinidad and Tobago
        ///</sumary>
        TT,
        ///<sumary>
        /// Tuvalu
        ///</sumary>
        TV,
        ///<sumary>
        /// Taiwan
        ///</sumary>
        TW,
        ///<sumary>
        /// Tanzania
        ///</sumary>
        TZ,
        ///<sumary>
        /// Ukraine
        ///</sumary>
        UA,
        ///<sumary>
        /// Uganda
        ///</sumary>
        UG,
        ///<sumary>
        /// United States Minor Outlying Islands
        ///</sumary>
        UM,
        ///<sumary>
        /// United States of America
        ///</sumary>
        US,
        ///<sumary>
        /// Uruguay
        ///</sumary>
        UY,
        ///<sumary>
        /// Uzbekistan
        ///</sumary>
        UZ,
        ///<sumary>
        /// Holy See
        ///</sumary>
        VA,
        ///<sumary>
        /// Saint Vincent and the Grenadines
        ///</sumary>
        VC,
        ///<sumary>
        /// Venezuela
        ///</sumary>
        VE,
        ///<sumary>
        /// Virgin Islands British
        ///</sumary>
        VG,
        ///<sumary>
        /// Virgin Islands U.S.
        ///</sumary>
        VI,
        ///<sumary>
        /// Viet Nam
        ///</sumary>
        VN,
        ///<sumary>
        /// Vanuatu
        ///</sumary>
        VU,
        ///<sumary>
        /// Wallis and Futuna
        ///</sumary>
        WF,
        ///<sumary>
        /// Samoa
        ///</sumary>
        WS,
        ///<sumary>
        /// Yemen
        ///</sumary>
        YE,
        ///<sumary>
        /// Mayotte
        ///</sumary>
        YT,
        ///<sumary>
        /// South Africa
        ///</sumary>
        ZA,
        ///<sumary>
        /// Zambia
        ///</sumary>
        ZM,
        ///<sumary>
        /// Zimbabwe
        ///</sumary>
        ZW,
        EW
    }
    public static Dictionary<string, string> countryCodes_NamesMapping = new Dictionary<string, string>() {
        {"AD" , " Andorra"},
        {"AE" , " United Arab Emirates"},
        {"AF" , " Afghanistan"},
        {"AG" , " Antigua and Barbuda"},
        {"AI" , " Anguilla"},
        {"AL" , " Albania"},
        {"AM" , " Armenia"},
        {"AO" , " Angola"},
        {"AQ" , " Antarctica"},
        {"AR" , " Argentina"},
        {"AS" , " American Samoa"},
        {"AT" , " Austria"},
        {"AU" , " Australia"},
        {"AW" , " Aruba"},
        {"AX" , " Åland Islands"},
        {"AZ" , " Azerbaijan"},
        {"BA" , " Bosnia and Herzegovina"},
        {"BB" , " Barbados"},
        {"BD" , " Bangladesh"},
        {"BE" , " Belgium"},
        {"BF" , " Burkina Faso"},
        {"BG" , " Bulgaria"},
        {"BH" , " Bahrain"},
        {"BI" , " Burundi"},
        {"BJ" , " Benin"},
        {"BL" , " Saint Barthélemy"},
        {"BM" , " Bermuda"},
        {"BN" , " Brunei Darussalam"},
        {"BO" , " Bolivia"},
        {"BQ" , " Bonaire"},
        {"BR" , " Brazil"},
        {"BS" , " Bahamas"},
        {"BT" , " Bhutan"},
        {"BV" , " Bouvet Island"},
        {"BW" , " Botswana"},
        {"BY" , " Belarus"},
        {"BZ" , " Belize"},
        {"CA" , " Canada"},
        {"CC" , " Cocos (Keeling) Islands"},
        {"CD" , " Congo"},
        {"CF" , " Central African Republic"},
        {"CG" , " Congo"},
        {"CH" , " Switzerland"},
        {"CI" , " Côte d'Ivoire"},
        {"CK" , " Cook Islands"},
        {"CL" , " Chile"},
        {"CM" , " Cameroon"},
        {"CN" , " China"},
        {"CO" , " Colombia"},
        {"CR" , " Costa Rica"},
        {"CU" , " Cuba"},
        {"CV" , " Cabo Verde"},
        {"CW" , " Curaçao"},
        {"CX" , " Christmas Island"},
        {"CY" , " Cyprus"},
        {"CZ" , " Czech Republic"},
        {"DE" , " Germany"},
        {"DJ" , " Djibouti"},
        {"DK" , " Denmark"},
        {"DM" , " Dominica"},
        {"DO" , " Dominican Republic"},
        {"DZ" , " Algeria"},
        {"EC" , " Ecuador"},
        {"EE" , " Estonia"},
        {"EG" , " Egypt"},
        {"EH" , " Western Sahara"},
        {"ER" , " Eritrea"},
        {"ES" , " Spain"},
        {"ET" , " Ethiopia"},
        {"FI" , " Finland"},
        {"FJ" , " Fiji"},
        {"FK" , " Falkland Islands (Malvinas)"},
        {"FM" , " Micronesia"},
        {"FO" , " Faroe Islands"},
        {"FR" , " France"},
        {"GA" , " Gabon"},
        {"GB" , " United Kingdom of Great Britain"},
        {"GD" , " Grenada"},
        {"GE" , " Georgia"},
        {"GF" , " French Guiana"},
        {"GG" , " Guernsey"},
        {"GH" , " Ghana"},
        {"GI" , " Gibraltar"},
        {"GL" , " Greenland"},
        {"GM" , " Gambia"},
        {"GN" , " Guinea"},
        {"GP" , " Guadeloupe"},
        {"GQ" , " Equatorial Guinea"},
        {"GR" , " Greece"},
        {"GS" , " South Georgia and the South Sandwich Islands"},
        {"GT" , " Guatemala"},
        {"GU" , " Guam"},
        {"GW" , " Guinea-Bissau"},
        {"GY" , " Guyana"},
        {"HK" , " Hong Kong"},
        {"HM" , " Heard Island and McDonald Islands"},
        {"HN" , " Honduras"},
        {"HR" , " Croatia"},
        {"HT" , " Haiti"},
        {"HU" , " Hungary"},
        {"ID" , " Indonesia"},
        {"IE" , " Ireland"},
        {"IL" , " Israel"},
        {"IM" , " Isle of Man"},
        {"IN" , " India"},
        {"IO" , " British Indian Ocean Territory"},
        {"IQ" , " Iraq"},
        {"IR" , " Iran"},
        {"IS" , " Iceland"},
        {"IT" , " Italy"},
        {"JE" , " Jersey"},
        {"JM" , " Jamaica"},
        {"JO" , " Jordan"},
        {"JP" , " Japan"},
        {"KE" , " Kenya"},
        {"KG" , " Kyrgyzstan"},
        {"KH" , " Cambodia"},
        {"KI" , " Kiribati"},
        {"KM" , " Comoros"},
        {"KN" , " Saint Kitts and Nevis"},
        {"KP" , " Korea"},
        {"KR" , " Korea"},
        {"KW" , " Kuwait"},
        {"KY" , " Cayman Islands"},
        {"KZ" , " Kazakhstan"},
        {"LA" , " Lao People's Democratic Republic"},
        {"LB" , " Lebanon"},
        {"LC" , " Saint Lucia"},
        {"LI" , " Liechtenstein"},
        {"LK" , " Sri Lanka"},
        {"LR" , " Liberia"},
        {"LS" , " Lesotho"},
        {"LT" , " Lithuania"},
        {"LU" , " Luxembourg"},
        {"LV" , " Latvia"},
        {"LY" , " Libya"},
        {"MA" , " Morocco"},
        {"MC" , " Monaco"},
        {"MD" , " Moldova"},
        {"ME" , " Montenegro"},
        {"MF" , " Saint Martin (French part)"},
        {"MG" , " Madagascar"},
        {"MH" , " Marshall Islands"},
        {"MK" , " Macedonia"},
        {"ML" , " Mali"},
        {"MM" , " Myanmar"},
        {"MN" , " Mongolia"},
        {"MO" , " Macao"},
        {"MP" , " Northern Mariana Islands"},
        {"MQ" , " Martinique"},
        {"MR" , " Mauritania"},
        {"MS" , " Montserrat"},
        {"MT" , " Malta"},
        {"MU" , " Mauritius"},
        {"MV" , " Maldives"},
        {"MW" , " Malawi"},
        {"MX" , " Mexico"},
        {"MY" , " Malaysia"},
        {"MZ" , " Mozambique"},
        {"NA" , " Namibia"},
        {"NC" , " New Caledonia"},
        {"NE" , " Niger"},
        {"NF" , " Norfolk Island"},
        {"NG" , " Nigeria"},
        {"NI" , " Nicaragua"},
        {"NL" , " Netherlands"},
        {"NO" , " Norway"},
        {"NP" , " Nepal"},
        {"NR" , " Nauru"},
        {"NU" , " Niue"},
        {"NZ" , " New Zealand"},
        {"OM" , " Oman"},
        {"PA" , " Panama"},
        {"PE" , " Peru"},
        {"PF" , " French Polynesia"},
        {"PG" , " Papua New Guinea"},
        {"PH" , " Philippines"},
        {"PK" , " Pakistan"},
        {"PL" , " Poland"},
        {"PM" , " Saint Pierre and Miquelon"},
        {"PN" , " Pitcairn"},
        {"PR" , " Puerto Rico"},
        {"PS" , " Palestine"},
        {"PT" , " Portugal"},
        {"PW" , " Palau"},
        {"PY" , " Paraguay"},
        {"QA" , " Qatar"},
        {"RE" , " Réunion"},
        {"RO" , " Romania"},
        {"RS" , " Serbia"},
        {"RU" , " Russian Federation"},
        {"RW" , " Rwanda"},
        {"SA" , " Saudi Arabia"},
        {"SB" , " Solomon Islands"},
        {"SC" , " Seychelles"},
        {"SD" , " Sudan"},
        {"SE" , " Sweden"},
        {"SG" , " Singapore"},
        {"SH" , " Saint Helena"},
        {"SI" , " Slovenia"},
        {"SJ" , " Svalbard and Jan Mayen"},
        {"SK" , " Slovakia"},
        {"SL" , " Sierra Leone"},
        {"SM" , " San Marino"},
        {"SN" , " Senegal"},
        {"SO" , " Somalia"},
        {"SR" , " Suriname"},
        {"SS" , " South Sudan"},
        {"ST" , " Sao Tome and Principe"},
        {"SV" , " El Salvador"},
        {"SX" , " Sint Maarten (Dutch part)"},
        {"SY" , " Syrian Arab Republic"},
        {"SZ" , " Swaziland"},
        {"TC" , " Turks and Caicos Islands"},
        {"TD" , " Chad"},
        {"TF" , " French Southern Territories"},
        {"TG" , " Togo"},
        {"TH" , " Thailand"},
        {"TJ" , " Tajikistan"},
        {"TK" , " Tokelau"},
        {"TL" , " Timor-Leste"},
        {"TM" , " Turkmenistan"},
        {"TN" , " Tunisia"},
        {"TO" , " Tonga"},
        {"TR" , " Turkey"},
        {"TT" , " Trinidad and Tobago"},
        {"TV" , " Tuvalu"},
        {"TW" , " Taiwan"},
        {"TZ" , " Tanzania"},
        {"UA" , " Ukraine"},
        {"UG" , " Uganda"},
        {"UM" , " United States Minor Outlying Islands"},
        {"US" , " United States of America"},
        {"UY" , " Uruguay"},
        {"UZ" , " Uzbekistan"},
        {"VA" , " Holy See"},
        {"VC" , " Saint Vincent and the Grenadines"},
        {"VE" , " Venezuela"},
        {"VG" , " Virgin Islands British"},
        {"VI" , " Virgin Islands U.S."},
        {"VN" , " Viet Nam"},
        {"VU" , " Vanuatu"},
        {"WF" , " Wallis and Futuna"},
        {"WS" , " Samoa"},
        {"YE" , " Yemen"},
        {"YT" , " Mayotte"},
        {"ZA" , " South Africa"},
        {"ZM" , " Zambia"},
        {"ZW" , " Zimbabwe"},
        {"EW" , " World"},
    };
    public static Sprite LoadFlag(string CountryShortCode)
    {
        if (CountryShortCode == null || (CountryShortCode != null && CountryShortCode.Equals("")))
        {
            CountryShortCode = "ew";
        }
        //Sprite flag = Resources.Load<Sprite>(CountryShortCode);
        //if (flag == null)
        //{
        //    flag = Resources.Load<Sprite>("us");
        //}

        return Resources.Load<Sprite>(CountryShortCode);
    }

    public static string GetNameOfCountry(string CountryShortCode)
    {
        if (countryCodes_NamesMapping.ContainsKey(CountryShortCode))
        {
            return countryCodes_NamesMapping[CountryShortCode];

        }
        else
        {
            //Debug.Log("CountryShortCode " + CountryShortCode);
            return "United States";
        }
    }
    public static Sprite LoadFlag(CountriesNames name)
    {
        return Resources.Load<Sprite>(name.ToString());
    }
    /// <summary>
    /// The Geo data for a user.
    /// You are free to use ip-api.com for non-commercial use. We do not allow commercial use without prior approval.
    /// So if you want to use it in commercial use -> https://signup.ip-api.com/
    /// 
    /// http://ip-api.com/docs/api:json
    /// 
    /// <code>
    /// {
    /// 	"status": "success",
    /// 	"country": "COUNTRY",
    /// 	"countryCode": "COUNTRY CODE",
    /// 	"region": "REGION CODE",
    /// 	"regionName": "REGION NAME",
    /// 	"city": "CITY",
    /// 	"zip": "ZIP CODE",
    /// 	"lat": LATITUDE,
    /// 	"lon": LONGITUDE,
    /// 	"timezone": "TIME ZONE",
    /// 	"isp": "ISP NAME",
    /// 	"org": "ORGANIZATION NAME",
    /// 	"as": "AS NUMBER / NAME",
    /// 	"query": "IP ADDRESS USED FOR QUERY"
    /// }
    /// </code>
    /// 
    /// </summary>
    public class LocationData
    {
        /// <summary>
        /// The status that is returned if the response was successful.
        /// </summary>
        public string status;
        public string country;
        public string countryCode;
        public string region;
        public string regionName;
        public string city;
        public string zip;
        public string lat;
        public string lon;
        public string timezone;
        public string isp;
        public string org;
        public string query;
    }

    //Check http://ip-api.com/docs/api:json for commercial and usage limits

    /// <summary>
    /// Method to get your current location with IP and return the results in Done Action
    /// </summary>
    /// <param name="MonoThread">MonoBehaviour class to run a Coroutine</param>
    /// <param name="Done">Action will be called after Web Request return</param>
    public static void GetLocation(MonoBehaviour MonoThread, UnityAction<LocationData, bool> Done)
    {
        MonoThread.StartCoroutine(GetLocationCoroutine(Done));
    }
    public static IEnumerator GetLocationCoroutine(UnityAction<LocationData, bool> Done)
    {
        UnityWebRequest www = UnityWebRequest.Get("http://ip-api.com/json");
        www.SendWebRequest();
        while (!www.isDone)
        {
            yield return new WaitForSeconds(0.1f);
        }
        if (www.isNetworkError)
        {
            ConstantsData_M.LogInfo("Error While Sending: " + www.error);
            Done(null, true);
        }
        else
        {
            LocationData data = JsonUtility.FromJson<LocationData>(www.downloadHandler.text);
            ////Debug.Log("Received: " + www.downloadHandler.text);
            ////Debug.Log(JsonUtility.ToJson(data));
            Done(data, false);
        }
    }

    public static Sprite getRandomConuntryFlag()
    {
        List<string> CountriesNames = new List<string>(System.Enum.GetNames(typeof(CountriesFlags.CountriesNames)));

        int flagIndex = Random.Range(0, CountriesNames.Count);
        Sprite sprite = CountriesFlags.LoadFlag(CountriesNames[flagIndex]);



        return sprite;

    }

    public static string GetRandomCountryName()
    {
        List<string> CountriesNames = new List<string>(System.Enum.GetNames(typeof(CountriesFlags.CountriesNames)));

        int flagIndex = Random.Range(0, CountriesNames.Count);
        string countryName = CountriesNames[flagIndex];

        return countryName;

    }

    public static int GetIndexByName( Dropdown dropDown, string name)
    {
        if (dropDown == null) { return -1; } // or exception
        if (string.IsNullOrEmpty(name) == true) { return -1; }
        List<Dropdown.OptionData> list = dropDown.options;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].text.Equals(name)) { return i; }
        }
        return -1;
    }

}
