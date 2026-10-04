//
using NetworkManagement;
using System.Collections;
using System.Diagnostics;
using System.Numerics;

namespace POKER
{
    public static class AllCustomProperties
    {
        public static void SetCustomString(this Player player, string datakey, string newValue)
        {
            Hashtable custom = new Hashtable();  // using PUN's implementation of Hashtable
            custom[datakey] = newValue;

            player.SetCustomProperties(custom);  // this locally sets the score and will sync it in-game asap.
        }


        public static string GetCustomString(this Player player, string datakey)
        {
            object custom;
            if (player.CustomProperties.ContainsKey(datakey))
            {
                return (string)player.CustomProperties[datakey];
            }

            return "null";
        }
        public static void SetCustomData(this Player player, string datakey, int newValue)
        {
            Hashtable custom = new Hashtable();  // using PUN's implementation of Hashtable
            custom[datakey] = newValue;

            player.SetCustomProperties(custom);  // this locally sets the score and will sync it in-game asap.
        }

        public static int GetCustomData(this Player player, string datakey)
        {
            object custom;
            if (player.CustomProperties.ContainsKey(datakey))
            {
                return (int)player.CustomProperties[datakey];
            }

            return 0;
        }

        public static void SetCustomBigIntegerData(this Player player, string datakey, BigInteger newValueBigInt)
        {
            string newValue = newValueBigInt.ToString();
            Hashtable custom = new Hashtable();  // using PUN's implementation of Hashtable
            custom[datakey] = newValue;
            //Constants_M.Log(player);
            player.SetCustomProperties(custom);  // this locally sets the score and will sync it in-game asap.
        }

        public static BigInteger GetCustomBigIntegerData(this Player player, string datakey)
        {
            object custom;
            if (player != null)
                if (player.CustomProperties.ContainsKey(datakey))
                {
                    string returnedValue = (string)player.CustomProperties[datakey];
                    return BigInteger.Parse(returnedValue);
                }
            return 0;
        }


        public static void SetCustomArray(this Player player, string datakey, int[] newValue)
        {
            Hashtable custom = new Hashtable();  // using PUN's implementation of Hashtable
            custom[datakey] = newValue;

            player.SetCustomProperties(custom);  // this locally sets the score and will sync it in-game asap.
        }

        public static int[] GetCustomArray(this Player player, string datakey)
        {
            object custom;
            if (player.CustomProperties.ContainsKey(datakey))
            {
                return (int[])player.CustomProperties[datakey];
            }
            return null;
        }

        public static void SetCustomBoolData(this Player player, string datakey, bool newValue)
        {
            Hashtable custom = new Hashtable();  // using PUN's implementation of Hashtable
            custom[datakey] = newValue;

            player.SetCustomProperties(custom);  // this locally sets the score and will sync it in-game asap.
        }

        public static bool GetCustomBoolData(this Player player, string datakey)
        {
            object custom;
            if (player.CustomProperties.ContainsKey(datakey))//(datakey, out custom))
            {
                return (bool)player.CustomProperties[datakey];
            }

            return false;
        }
       

        public static void SetPlayerStateProperty(this Player player, string datakey, PlayerState.STATE newValue)
        {
            Hashtable custom = new Hashtable();  // using PUN's implementation of Hashtable
            custom[datakey] = newValue;

            player.SetCustomProperties(custom);  // this locally sets the score and will sync it in-game asap.
        }

        public static PlayerState.STATE GetPlayerStateProperty(this Player player, string datakey)
        {
            object custom;
            if (player.CustomProperties.ContainsKey(datakey))
            {
                return (PlayerState.STATE)player.CustomProperties[datakey];
            }
            return 0;
        }
    }
}