using TeenPattiGame;
using System.Numerics;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public static class AllCustomProperties
{
    // public static void SetCustomString(this Player player, string datakey, string newValue)
    // {
    //     Hashtable custom = new Hashtable();  // using PUN's implementation of Hashtable
    //     custom[datakey] = newValue;

    //     player.SetCustomProperties(custom);  // this locally sets the score and will sync it in-game asap.
    // }


    // public static string GetCustomString(this Player player, string datakey)
    // {
    //     object custom;
    //     if (player.CustomProperties.TryGetValue(datakey, out custom))
    //     {
    //         return (string)custom;
    //     }

    //     return "null";
    // }
    // public static void SetCustomData(this Player player, string datakey, int newValue)
    // {
    //     Hashtable custom = new Hashtable();  // using PUN's implementation of Hashtable
    //     custom[datakey] = newValue;

    //     player.SetCustomProperties(custom);  // this locally sets the score and will sync it in-game asap.
    // }

    // public static int GetCustomData(this Player player, string datakey)
    // {
    //     object custom;
    //     if (player.CustomProperties.TryGetValue(datakey, out custom))
    //     {
    //         return (int)custom;
    //     }

    //     return 0;
    // }

    // public static void SetCustomBigIntegerData(this Player player, string datakey, BigInteger newValueBigInt)
    // {
    //     string newValue = newValueBigInt.ToString();
    //     Hashtable custom = new Hashtable();  // using PUN's implementation of Hashtable
    //     custom[datakey] = newValue;
    //     player.SetCustomProperties(custom);  // this locally sets the score and will sync it in-game asap.
    // }

    // public static BigInteger GetCustomBigIntegerData(this Player player, string datakey)
    // {
    //     object custom;
    //     if (player.CustomProperties.TryGetValue(datakey, out custom))
    //     {
    //         string returnedValue = (string)custom;
    //         return BigInteger.Parse(returnedValue);
    //     }
    //     return 0;
    // }


    // public static void SetCustomArray(this Player player, string datakey, int[] newValue)
    // {
    //     Hashtable custom = new Hashtable();  // using PUN's implementation of Hashtable
    //     custom[datakey] = newValue;

    //     player.SetCustomProperties(custom);  // this locally sets the score and will sync it in-game asap.
    // }

    // public static int[] GetCustomArray(this Player player, string datakey)
    // {
    //     object custom;
    //     if (player.CustomProperties.TryGetValue(datakey, out custom))
    //     {
    //         return (int[])custom;
    //     }
    //     return null;
    // }

    // public static void SetCustomBoolData(this Player player, string datakey, bool newValue)
    // {
    //     Hashtable custom = new Hashtable();  // using PUN's implementation of Hashtable
    //     custom[datakey] = newValue;

    //     player.SetCustomProperties(custom);  // this locally sets the score and will sync it in-game asap.
    // }

    // public static bool GetCustomBoolData(this Player player, string datakey)
    // {
    //     object custom;
    //     if (player.CustomProperties.TryGetValue(datakey, out custom))
    //     {
    //         return (bool)custom;
    //     }

    //     return false;
    // }
    // public static void SetCustomRoomBoolData(this Room room, string datakey, bool newValue)
    // {
    //     Hashtable custom = new Hashtable();  // using PUN's implementation of Hashtable
    //     custom[datakey] = newValue;

    //     room.SetCustomProperties(custom);  // this locally sets the score and will sync it in-game asap.
    // }

    // public static bool GetCustomRoomBoolData(this Room room, string datakey)
    // {
    //     object custom;
    //     if (room.CustomProperties.TryGetValue(datakey, out custom))
    //     {
    //         return (bool)custom;
    //     }

    //     return false;
    // }


    // public static void SetCustomRoomData(this Room room, string datakey, int newValue)
    // {
    //     Hashtable custom = new Hashtable();  // using PUN's implementation of Hashtable
    //     custom[datakey] = newValue;

    //     room.SetCustomProperties(custom);  // this locally sets the score and will sync it in-game asap.
    // }

    // public static int GetCustomRoomData(this Room room, string datakey)
    // {
    //     object custom;
    //     if (room.CustomProperties.TryGetValue(datakey, out custom))
    //     {
    //         return (int)custom;
    //     }
    //     return 0;
    // }

    // public static void SetCustomFloatRoomData(this Room room, string datakey, float newValue)
    // {
    //     Hashtable custom = new Hashtable();  // using PUN's implementation of Hashtable
    //     custom[datakey] = newValue;

    //     room.SetCustomProperties(custom);  // this locally sets the score and will sync it in-game asap.
    // }

    // public static float GetCustomFloatRoomData(this Room room, string datakey)
    // {
    //     object custom;
    //     if (room.CustomProperties.TryGetValue(datakey, out custom))
    //     {
    //         return (float)custom;
    //     }
    //     return 0;
    // }

    // public static void SetTableCollectedCash(this Room room, string datakey, BigInteger newValueInt)
    // {
    //     string newValue = newValueInt.ToString();
    //     Hashtable custom = new Hashtable();  // using PUN's implementation of Hashtable
    //     custom[datakey] = newValue;

    //     room.SetCustomProperties(custom);  // this locally sets the score and will sync it in-game asap
    // }

    // public static void SetLastRecord(this Room room, string datakey, int[] newValue)
    // {
    //     Hashtable custom = new Hashtable();  // using PUN's implementation of Hashtable

    //     custom[datakey] = newValue;

    //     room.SetCustomProperties(custom);  // this locally sets the score and will sync it in-game asap.
    // }

    // public static int[] GetLastRecord(this Room room, string datakey)
    // {
    //     object custom;
    //     if (room.CustomProperties.TryGetValue(datakey, out custom))
    //     {
    //         return (int[])custom;
    //     }
    //     return null;
    // }


    // public static BigInteger GetTableCollectedCash(this Room room, string datakey)
    // {
    //     object custom;
    //     if (room.CustomProperties.TryGetValue(datakey, out custom))
    //     {
    //         string customString = custom.ToString();
    //         return BigInteger.Parse(customString);
    //         //return (BigInteger)custom;
    //     }
    //     return 0;
    // }


    // public static void SetPlayerStateProperty(this Player player, string datakey, PlayerState.STATE newValue)
    // {
    //     Hashtable custom = new Hashtable();  // using PUN's implementation of Hashtable
    //     custom[datakey] = newValue;

    //     player.SetCustomProperties(custom);  // this locally sets the score and will sync it in-game asap.
    // }


    // public static PlayerState.STATE GetPlayerStateProperty(this Player player, string datakey)
    // {
    //     object custom;
    //     if (player.CustomProperties.TryGetValue(datakey, out custom))
    //     {
    //         return (PlayerState.STATE)custom;
    //     }
    //     return 0;
    // }

    // public static void SetRoomStateProperty(this Room room, string datakey, RoomState.STATE newValue)
    // {
    //     Hashtable custom = new Hashtable();  // using PUN's implementation of Hashtable
    //     custom[datakey] = newValue;

    //     room.SetCustomProperties(custom);  // this locally sets the score and will sync it in-game asap.
    // }

    // public static RoomState.STATE GetRoomStateProperty(this Room room, string datakey)
    // {
    //     object custom;
    //     if (room.CustomProperties.TryGetValue(datakey, out custom))
    //     {
    //         return (RoomState.STATE)custom;
    //     }
    //     return 0;
    // }

    // public static void SetPlayingList(this Room room, string datakey, int[] newValue)
    // {
    //     Hashtable custom = new Hashtable();  // using PUN's implementation of Hashtable

    //     custom[datakey] = newValue;

    //     room.SetCustomProperties(custom);  // this locally sets the score and will sync it in-game asap.
    // }

    // public static int[] GetPlayingList(this Room room, string datakey)
    // {
    //     object custom;
    //     if (room.CustomProperties.TryGetValue(datakey, out custom))
    //     {
    //         return (int[])custom;
    //     }
    //     return null;
    // }



    // public static void SetCardsList(this Room room, string datakey, int[] newValue)
    // {
    //     Hashtable custom = new Hashtable();  // using PUN's implementation of Hashtable

    //     custom[datakey] = newValue;

    //     room.SetCustomProperties(custom);  // this locally sets the score and will sync it in-game asap.
    // }

    // public static int[] GetCardsList(this Room room, string datakey)
    // {
    //     object custom;
    //     if (room.CustomProperties.TryGetValue(datakey, out custom))
    //     {
    //         return (int[])custom;
    //     }
    //     return null;
    // }

    // public static void SetStringList(this Room room, string datakey, string[] newValue)
    // {
    //     Hashtable custom = new Hashtable();  // using PUN's implementation of Hashtable

    //     custom[datakey] = newValue;

    //     room.SetCustomProperties(custom);  // this locally sets the score and will sync it in-game asap.
    // }

    // public static string[] GetStringList(this Room room, string datakey)
    // {
    //     object custom;
    //     if (room.CustomProperties.TryGetValue(datakey, out custom))
    //     {
    //         return (string[])custom;
    //     }
    //     return null;
    // }


    // public static void SetRoomSeatingRecord(this Room room, string datakey, bool[] allseats)
    // {
    //     Hashtable custom = new Hashtable();  // using PUN's implementation of Hashtable
    //     custom[datakey] = allseats;

    //     room.SetCustomProperties(custom);  // this locally sets the score and will sync it in-game asap.
    // }

    // public static bool[] GetRoomSeatingRecord(this Room room, string datakey)
    // {
    //     object custom;
    //     if (room.CustomProperties.TryGetValue(datakey, out custom))
    //     {
    //         return (bool[])custom;
    //     }
    //     bool[] noSeating = new bool[5];
    //     return noSeating;
    // }



}
