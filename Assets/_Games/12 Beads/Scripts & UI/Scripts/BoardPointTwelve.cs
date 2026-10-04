using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
namespace Twelve
{
    public class BoardPointTwelve : MonoBehaviour
{
        [FormerlySerializedAs("player")] public GameControllerTwelve.Player ownerPlayer;
        [FormerlySerializedAs("column")] public int gridColumn;
        [FormerlySerializedAs("row")] public int gridRow;
        [FormerlySerializedAs("diagonalAllowed")] public bool canMoveDiagonally;


        private void OnMouseDown()
    {
        var jsonData = JsonUtility.ToJson(this);
        //Debug.Log(jsonData);

        GameControllerTwelve.instance.ProcessClick(this);
        //NetworkManager.Instance.BroadcastBeadPositions(column, row);
    }

}
}
