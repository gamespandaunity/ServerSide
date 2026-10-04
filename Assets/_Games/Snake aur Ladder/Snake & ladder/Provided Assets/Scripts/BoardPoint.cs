using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Snake_Ladder
{
    public class BoardPoint : MonoBehaviour
    {
        public GameController.Player player;
        public int column;
        public int row;
        public bool diagonalAllowed;

        private void OnMouseDown()
        {
            var jsonData = JsonUtility.ToJson(this);
            Debug.Log(jsonData);

            GameController.instance.ProcessClick(this);
            //NetworkManager.Instance.BroadcastBeadPositions(column, row);
        }

    }
}