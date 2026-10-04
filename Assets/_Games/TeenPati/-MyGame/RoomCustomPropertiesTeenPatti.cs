using System.Linq;
using System.Numerics;
using Mirror;
using TeenPattiGame;
using UnityEngine;

public class RoomCustomPropertiesTeenPatti : NetworkBehaviour
{
    public readonly SyncDictionary<string, bool> customRoomBoolData = new SyncDictionary<string, bool>();
    public readonly SyncDictionary<string, int> customRoomData = new SyncDictionary<string, int>();



    // ===== BOOL METHODS =====
    [Command(requiresAuthority = false)]
    public void CmdSetCustomRoomBoolData(string datakey, bool newValue)
    {
        // Only server can modify room properties
        if (!NetworkServer.active) return;

        customRoomBoolData[datakey] = newValue;
    }

    // Server-only method (for direct server control)
    [Server]
    public void SetCustomRoomBoolData(string datakey, bool newValue)
    {
        customRoomBoolData[datakey] = newValue;
    }

    public bool GetCustomRoomBoolData(string datakey)
    {
        if (customRoomBoolData.TryGetValue(datakey, out bool value))
        {
            return value;
        }
        return false;
    }

    // ===== INT METHODS =====
    [Command(requiresAuthority = false)]
    public void CmdSetCustomRoomData(string datakey, int newValue)
    {
        // Only server can modify room properties
        if (!NetworkServer.active) return;

        customRoomData[datakey] = newValue;
    }

    // Server-only method (for direct server control)
    [Server]
    public void SetCustomRoomData(string datakey, int newValue)
    {
        customRoomData[datakey] = newValue;
    }

    public int GetCustomRoomData(string datakey)
    {
        if (customRoomData.TryGetValue(datakey, out int value))
        {
            return value;
        }
        return 0;
    }

    public readonly SyncDictionary<string, float> customFloatRoomData = new SyncDictionary<string, float>();

    // ===== FLOAT METHODS =====
    [Command(requiresAuthority = false)]
    public void CmdSetCustomFloatRoomData(string datakey, float newValue)
    {
        // Only server can modify room properties
        if (!NetworkServer.active) return;

        customFloatRoomData[datakey] = newValue;
    }

    // Server-only method (for direct server control)
    [Server]
    public void SetCustomFloatRoomData(string datakey, float newValue)
    {
        customFloatRoomData[datakey] = newValue;
    }

    public float GetCustomFloatRoomData(string datakey)
    {
        if (customFloatRoomData.TryGetValue(datakey, out float value))
        {
            return value;
        }
        return 0;
    }

    public readonly SyncDictionary<string, string> customBigIntegerRoomData = new SyncDictionary<string, string>();

    // SyncDictionary for int arrays (stored as string)
    public readonly SyncDictionary<string, string> customArrayRoomData = new SyncDictionary<string, string>();

    // ===== BIGINTEGER METHODS =====
    [Command(requiresAuthority = false)]
    public void CmdSetTableCollectedCash(string datakey, string newValue)
    {
        if (!NetworkServer.active) return;

        customBigIntegerRoomData[datakey] = newValue;
    }

    [Server]
    public void SetTableCollectedCash(string datakey, BigInteger newValueInt)
    {
        customBigIntegerRoomData[datakey] = newValueInt.ToString();
    }

    // Helper method for clients
    public void SetTableCollectedCashClient(string datakey, BigInteger newValueInt)
    {
        CmdSetTableCollectedCash(datakey, newValueInt.ToString());
    }

    public BigInteger GetTableCollectedCash(string datakey)
    {
        if (customBigIntegerRoomData.TryGetValue(datakey, out string value))
        {
            return BigInteger.Parse(value);
        }
        return 0;
    }

    // ===== INT ARRAY METHODS =====
    [Command(requiresAuthority = false)]
    public void CmdSetLastRecord(string datakey, int[] newValue)
    {
        if (!NetworkServer.active) return;

        string arrayString = string.Join(",", newValue);
        customArrayRoomData[datakey] = arrayString;
    }

    [Server]
    public void SetLastRecord(string datakey, int[] newValue)
    {
        string arrayString = string.Join(",", newValue);
        customArrayRoomData[datakey] = arrayString;
    }

    public int[] GetLastRecord(string datakey)
    {
        if (customArrayRoomData.TryGetValue(datakey, out string arrayString))
        {
            if (string.IsNullOrEmpty(arrayString))
                return new int[0];

            return arrayString.Split(',').Select(int.Parse).ToArray();
        }
        return null;
    }



    public readonly SyncDictionary<string, int> customRoomStateData = new SyncDictionary<string, int>();

    // SyncDictionary for playing list array (stored as string)
    public readonly SyncDictionary<string, string> customPlayingListData = new SyncDictionary<string, string>();

    // ===== ROOM STATE METHODS =====
    [Command(requiresAuthority = false)]
    public void CmdSetRoomStateProperty(string datakey, int stateValue)
    {
        if (!NetworkServer.active) return;
        customRoomStateData[datakey] = stateValue;
    }

    [Server]
    public void SetRoomStateProperty(string datakey, int newValue)
    {
        customRoomStateData[datakey] = newValue;
    }

    public RoomState.STATE GetRoomStateProperty(string datakey)
    {
        if (customRoomStateData.TryGetValue(datakey, out int value))
        {
            return (RoomState.STATE)value;
        }
        return 0;
    }

    // ===== PLAYING LIST METHODS =====
    [Command(requiresAuthority = false)]
    public void CmdSetPlayingList(string datakey, int[] newValue)
    {
        if (!NetworkServer.active) return;
        string arrayString = string.Join(",", newValue);
        customPlayingListData[datakey] = arrayString;
    }

    [Server]
    public void SetPlayingList(string datakey, int[] newValue)
    {
        string arrayString = string.Join(",", newValue);
        customPlayingListData[datakey] = arrayString;
    }

    public int[] GetPlayingList(string datakey)
    {
        if (customPlayingListData.TryGetValue(datakey, out string arrayString))
        {
            if (string.IsNullOrEmpty(arrayString))
                return new int[0];

            return arrayString.Split(',').Select(int.Parse).ToArray();
        }
        return null;
    }

    public readonly SyncDictionary<string, string> customSeatingData = new SyncDictionary<string, string>();

    // ===== SEATING RECORD METHODS =====
    [Command(requiresAuthority = false)]
    public void CmdSetRoomSeatingRecord(string datakey, bool[] allseats)
    {
        if (!NetworkServer.active) return;
        string arrayString = string.Join(",", allseats.Select(b => b ? "1" : "0"));
        customSeatingData[datakey] = arrayString;
    }

    [Server]
    public void SetRoomSeatingRecord(string datakey, bool[] allseats)
    {
        string arrayString = string.Join(",", allseats.Select(b => b ? "1" : "0"));
        customSeatingData[datakey] = arrayString;
    }

    public bool[] GetRoomSeatingRecord(string datakey)
    {
        if (customSeatingData.TryGetValue(datakey, out string arrayString))
        {
            if (string.IsNullOrEmpty(arrayString))
                return new bool[5];

            return arrayString.Split(',').Select(s => s == "1").ToArray();
        }
        bool[] noSeating = new bool[5];
        return noSeating;
    }
}
