using System.Linq;
using System.Numerics;
using Mirror;
using TeenPattiGame;
using UnityEngine;

public class PlayerCustomProperties : NetworkBehaviour
{
    public readonly SyncDictionary<string, string> customProperties = new SyncDictionary<string, string>();

    [Command(requiresAuthority = false)]
    public void CmdSetCustomString(string datakey, string newValue)
    {
        customProperties[datakey] = newValue;
    }
    public void SetCustomString(string dataKey, string newValue)
    {
        if (NetworkServer.active)
            customProperties[dataKey] = newValue;
        else
            CmdSetCustomString(dataKey, newValue);
    }
    public string GetCustomString(string datakey)
    {
        if (customProperties.TryGetValue(datakey, out string value))
        {
            return value;
        }
        return "null";
    }
    public readonly SyncDictionary<string, int> customData = new SyncDictionary<string, int>();

    [Command(requiresAuthority = false)]
    public void CmdSetCustomData(string datakey, int newValue)
    {
        customData[datakey] = newValue;
    }
    public void SetCustomData(string datakey, int newValue)
    {
        if (NetworkServer.active)
            customData[datakey] = newValue;
        else
            CmdSetCustomData(datakey, newValue);
    }
    // Get integer data (can be called from anywhere)
    public int GetCustomData(string datakey)
    {
        if (customData.TryGetValue(datakey, out int value))
        {
            return value;
        }
        return 0;
    }

    public readonly SyncDictionary<string, string> customBigIntegerData = new SyncDictionary<string, string>();

    [Command(requiresAuthority = false)]
    public void CmdSetCustomBigIntegerData(string datakey, string newValue)
    {
        customBigIntegerData[datakey] = newValue;
    }

    public void SetCustomBigIntegerData(string datakey, BigInteger newValueBigInt)
    {
        if (NetworkServer.active)
            customBigIntegerData[datakey] = newValueBigInt.ToString();
        else
            CmdSetCustomBigIntegerData(datakey, newValueBigInt.ToString());
    }

    // Get BigInteger data
    public BigInteger GetCustomBigIntegerData(string datakey)
    {
        if (customBigIntegerData.TryGetValue(datakey, out string value))
        {
            return BigInteger.Parse(value);
        }
        return 0;
    }

    public readonly SyncDictionary<string, bool> customBoolData = new SyncDictionary<string, bool>();

    // For int arrays (stored as comma-separated string)
    public readonly SyncDictionary<string, string> customArrayData = new SyncDictionary<string, string>();

    // ===== BOOL METHODS =====
    [Command(requiresAuthority = false)]
    public void CmdSetCustomBoolData(string datakey, bool newValue)
    {
        customBoolData[datakey] = newValue;
    }
    public void SetCustomBoolData(string datakey, bool newValue)
    {
        if (NetworkServer.active)
            customBoolData[datakey] = newValue;
        else
            CmdSetCustomBoolData(datakey, newValue);
    }
    public bool GetCustomBoolData(string datakey)
    {
        if (customBoolData.TryGetValue(datakey, out bool value))
        {
            return value;
        }
        return false;
    }

    // ===== ARRAY METHODS =====
    [Command(requiresAuthority = false)]
    public void CmdSetCustomArray(string datakey, int[] newValue)
    {
        // Convert array to comma-separated string
        string arrayString = string.Join(",", newValue);
        customArrayData[datakey] = arrayString;
    }
    public void SetCustomArray(string datakey, int[] newValue)
    {
        string arrayString = string.Join(",", newValue);
        if (NetworkServer.active)
            customArrayData[datakey] = arrayString;
        else
            CmdSetCustomArray(datakey, newValue);
    }
    public int[] GetCustomArray(string datakey)
    {
        if (customArrayData.TryGetValue(datakey, out string arrayString))
        {
            // Convert string back to int array
            if (string.IsNullOrEmpty(arrayString))
                return new int[0];

            return arrayString.Split(',').Select(int.Parse).ToArray();
        }
        return null;
    }
    public readonly SyncDictionary<string, int> customStateData = new SyncDictionary<string, int>();

    // ===== ENUM STATE METHODS =====
    [Command(requiresAuthority = false)]
    public void CmdSetPlayerStateProperty(string datakey, int stateValue)
    {
        "Tillu".Show();
        customStateData[datakey] = stateValue;
    }
    public void SetPlayerStateProperty(string datakey, int stateValue)
    {
        if (NetworkServer.active)
            customStateData[datakey] = stateValue;
        else
            CmdSetPlayerStateProperty(datakey, stateValue);
    }

    public PlayerState.STATE GetPlayerStateProperty(string datakey)
    {
        if (customStateData.TryGetValue(datakey, out int value))
        {
            return (PlayerState.STATE)value;
        }
        return 0; // Or use a default enum value like PlayerState.STATE.None
    }

}
