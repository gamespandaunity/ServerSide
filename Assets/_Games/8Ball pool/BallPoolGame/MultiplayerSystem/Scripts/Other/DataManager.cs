using System.Collections.Generic;
using UnityEngine;
using BallPool.Mechanics;

namespace NetworkManagement
{
    public delegate void gameStateChangedHandle(string game_state);

    public static class DataExtansion
    {
        public static string ToString4(this float value)
        {
            return ((int)(value * 10000f)).ToString();
        }

        public static float ToFloat4(this string value)
        {
            return ((float)int.Parse(value)) / 10000f;
        }
    }

    public class DataManager : MonoBehaviour
    {
        private static DataManager instance;
        private static bool gameDataSaved = false;

        void Awake()
        {
            if (instance)
            {
                Destroy(gameObject);
            }
            else
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }
        void OnDisable()
        {
            if (gameDataSaved && instance == this)
            {
                SaveGameData();
            }
        }
        void OnDestroy()
        {
            if (!gameDataSaved && instance == this)
            {
                SaveGameData();
            }
        }
        void OnApplicationQuit()
        {
            if (!gameDataSaved )
            {
                SaveGameData();
            }
        }
        private static Dictionary<string, Data> gameStateDictionary;
        private static Dictionary<string, Data> gameDataDictionary;
        public class Data
        {
            public int typeId{ get; private set; }
            public string value{ get; private set; }
            public Data(int typeId, string value)
            {
                this.typeId = typeId;
                this.value = value;
            }
        }
        public static string gameState;
        private static string _gameState;
        public static event gameStateChangedHandle OngameStateChanged;

        private static void SavegameState()
        {
            if(OngameStateChanged != null)
            {
                OngameStateChanged(gameState);
            }
            PlayerPrefs.Save();
        }
        public static void SaveGameData()
        {
            gameDataSaved = true;
            DownloadManager.SaveGameDataToDocumends(gameDataDictionary);
        }
        private static void LoadGameData()
        {
            gameDataDictionary = DownloadManager.LoadGameDataFromDocumends();
        }
        /// <summary>
        /// Gets from game data (typeId: 1-int, 2-float, 3-string).
        /// </summary>
        private static string GetFromGameData(int typeId, string key)
        {
            if (gameDataDictionary == null)
            {
                LoadGameData();
            }
            if (gameDataDictionary == null)
            {
                return "";
            }
            foreach (KeyValuePair<string, Data> item in gameDataDictionary)
            {
                if (item.Key == key && item.Value.typeId == typeId)
                {
                    return gameDataDictionary[key].value;
                }
            }
            return "";
        }
        /// <summary>
        /// Adds to game data (typeId: 1-int, 2-float, 3-string).
        /// </summary>
        private static void AddToGameData(int typeId, string key, string value)
        {
            if (gameDataDictionary == null)
            {
                gameDataDictionary = new Dictionary<string, Data>();
            }
            if (gameDataDictionary.ContainsKey(key))
            {
                gameDataDictionary[key] = new Data(typeId, value);
            }
            else
            {
                gameDataDictionary.Add(key, new Data(typeId, value));
            }
        }
        /// <summary>
        /// Adds to game state (typeId: 1-int, 2-float, 3-string).
        /// </summary>
        private static bool AddToGameState(int typeId, string key, string value)
        {
            if (value.Contains("[") || value.Contains("]") || value.Contains(";") || value.Contains("{") || value.Contains("}"))
            {
                ConstantsData_M.Log(value + ": the value can not have characters such as [ ; { ");
                return false;
            }

            if (gameStateDictionary == null)
            {
                gameStateDictionary = new Dictionary<string, Data>();
            }
            if (gameStateDictionary.ContainsKey(key))
            {
                gameStateDictionary[key] = new Data(typeId, value);
            }
            else
            {
                gameStateDictionary.Add(key, new Data(typeId, value));
            }

            _gameState = "";
            foreach (KeyValuePair<string, Data> item in gameStateDictionary)
            {
                _gameState += "[" + item.Value.typeId + ";" + item.Key + ";" + item.Value.value + "]";
            }

            if(gameState != _gameState)
            {
                gameState = _gameState;
                SavegameState();
            }
            return true;
        }
        #region Key-Vakue using
        public static void SetInt(string key, int value)
        {
            if(AddToGameState(1, key, value.ToString()))
            {
                PlayerPrefs.SetInt(key, value);
            }
        }

        public static int GetInt(string key)
        {
            return  PlayerPrefs.GetInt(key);
        }

        public static void SetFloat(string key, float value)
        {
            if (AddToGameState(2, key, value.ToString()))
            {
                PlayerPrefs.SetFloat(key, value);
            }
        }

        public static float GetFloat(string key)
        {
            return  PlayerPrefs.GetFloat(key);
        }

        public static void SetString(string key, string value)
        {
            if (AddToGameState(3, key, value))
            {
                PlayerPrefs.SetString(key, value);
            }
        }

        public static string GetString(string key)
        {
            return  PlayerPrefs.GetString(key);
        }
        public static void DeleteKey(string key)
        {
            if (gameStateDictionary.ContainsKey(key))
            {
                gameStateDictionary.Remove(key);
            }
            PlayerPrefs.DeleteKey(key);
        }
        #endregion
            
        #region Data using
        public static void SetIntData(string key, int value)
        {
            AddToGameData(1, key, value.ToString());
        }

        public static int GetIntData(string key)
        {
            int value = 0;
            if (int.TryParse(GetFromGameData(1, key), out value))
            {
                return value;
            }
            return value;
        }

        public static void SetFloatData(string key, float value)
        {
            AddToGameData(2, key, value.ToString());
        }

        public static float GetFloatData(string key)
        {
            float value = 0.0f;
            if (float.TryParse(GetFromGameData(2, key), out value))
            {
                return value;
            }
            return value;
        }

        public static void SetStringData(string key, string value)
        {
            AddToGameData(3, key, value.ToString());
        }

        public static string GetStringData(string key)
        {
            return  GetFromGameData(3, key);
        }
       
        public static void SetColorData(string key, Color value)
        {
            SetStringData(key, ColorToString(value));
        }

        public static bool GetColorData(string key, out Color value)
        {
            string colorStr = GetStringData(key);

            if (!string.IsNullOrEmpty(colorStr))
            {
                value = ColorFromString(colorStr);
                return true;
            }
            else
            {
                value = new Color();
                return false;
            }
        }
        public static void DeleteKeyData(string key)
        {
            if (gameDataDictionary.ContainsKey(key))
            {
                gameDataDictionary.Remove(key);
            }
        }
        #endregion
        public static float CutValue(float value, int count = 3)
        {
            return value;
//            float pow = Mathf.Pow(10.0f, (float)count);
//            return  (float)((int)(pow * value)) / pow;
        }
        /// <summary>
        /// Create string froms the Vector3.
        /// </summary>
        public static string Vector3ToString(Vector3 v)
        {
            float x = CutValue(v.x);
            float y = CutValue(v.y);
            float z = CutValue(v.z);
            if (x == 0.0f && y == 0.0f && z == 0.0f)
            {
                return "z";
            } else if((x == 1.0f && y == 0.0f && z == 0.0f))
            {
                return "r";
            } else if((x == 0.0f && y == 1.0f && z == 0.0f))
            {
                return "u";
            } else if((x == 0.0f && y == 0.0f && z == 1.0f))
            {
                return "f";
            } else if((x == -1.0f && y == 0.0f && z == 0.0f))
            {
                return "l";
            } else if((x == 0.0f && y == -1.0f && z == 0.0f))
            {
                return "d";
            } else if((x == 0.0f && y == 0.0f && z == -1.0f))
            {
                return "b";
            }else if((x == 1.0f && y == 1.0f && z == 1.0f))
            {
                return "o";
            } else
            {
                return "(" + x.ToString4() + ", " + y.ToString4() + ", " + z.ToString4() + ")";
            }
        }

  
        /// <summary>
        /// Create Vector3 froms the string.
        /// </summary>
        public static Vector3 Vector3FromString(string s)
        {
            if (s == "" || s == "z")
            {
                return Vector3.zero;
            } else if (s == "r")
            {
                return Vector3.right;
            } else if (s == "u")
            {
                return Vector3.up;
            } else if (s == "f")
            {
                return Vector3.forward;
            } else if (s == "l")
            {
                return Vector3.left;
            } else if (s == "d")
            {
                return Vector3.down;
            } else if (s == "b")
            {
                return Vector3.back;
            } else if (s == "o")
            {
                return Vector3.one;
            }
            string strX = "";
            string strY = "";
            string strZ = "";

            int step = 1;
            foreach (char c in s)
            {
                if(c == ',')
                {
                    step++;
                    continue;
                } else if(c == '(' || c == ' ' || c == ')')
                {
                    continue;
                }
                if(step == 1)
                {
                    strX += c.ToString();
                } else if(step == 2)
                {
                    strY += c.ToString();
                } else if(step == 3)
                {
                    strZ += c.ToString();
                }
            }
            float x = strX.ToFloat4();
            float y = strY.ToFloat4();
            float z = strZ.ToFloat4();

      
            return new Vector3(x, y, z);
        } 

        public static Color ColorFromString(string data)
        {
            string[] list = ConvertDataToStringArray(data);
            float r = list[0].ToFloat4();
            float g = list[1].ToFloat4();
            float b = list[2].ToFloat4();
            float a = list[3].ToFloat4();
            return new Color(r, g, b, a);
        }
   
        public static string ColorToString(Color value)
        {
            return "[" + value.r.ToString4() + ";" + value.g.ToString4() + ";" + value.b.ToString4() + ";" + value.a.ToString4() + "]";
        }

        public static string ImpulseToString(Impulse impulse)
        {
            return "[" + Vector3ToString(impulse.point) + ";" + Vector3ToString(impulse.impulse) + "]";
        }
        public static Impulse ImpulseFromString(string data)
        {
            string[] list = ConvertDataToStringArray(data);
            Vector3 point = Vector3FromString(list[0]);
            Vector3 impulse = Vector3FromString(list[1]);
            return new Impulse(point, impulse);
        }
        /// <summary>
        /// [ value0; value2; value3;... ].
        /// </summary>
        public static string[] ConvertDataToStringArray (string data)
        {
            List<string> list = new List<string>(0);
            string value = "";
            foreach (char c in data)
            {
                if (c == ']')
                {
                    list.Add(value);
                    break;
                }
                if(c == ';')
                {
                    list.Add(value);
                    value = "";
                    continue;
                } 
                if(c == '[' || c == ' ')
                {
                    continue;
                }
                value += c.ToString();
            }
            return list.ToArray();
        }
        /// <summary>
        /// [ value0; value2; value3;... ] [ value0; value2; value3;... ][ value0; value2; value3;... ].
        /// </summary>
        public static string[] ConvertArrayDataToStringArray (string data)
        {
            List<string> list = new List<string>(0);
            string value = "";
            foreach (char c in data)
            {
                if(c == ']')
                {
                    value += c;
                    list.Add(value);
                    value = "";
                    continue;
                } 
                if(c == ' ')
                {
                    continue;
                }
                value += c;
            }
            return list.ToArray();
        }
    }
}
