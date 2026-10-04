using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Snake_Ladder
{
    public class NodeScript : MonoBehaviour
    {
        public int nodeNo;
        public bool isOccupied;
        public GameObject emptyNode;
        public BeadScript currentBead;

        public int moveCount;
        public BeadScript lastBeadSelected;
        public List<ConnectedNodes> connectedNodesList;
        public List<NodeScript> nextNodesList;

        public void OnOccupied(bool occupied)
        {
            isOccupied = occupied;
            if (!isOccupied && emptyNode)
            {
                currentBead = emptyNode.GetComponent<BeadScript>();
            }
            emptyNode.SetActive(false);
        }

        public bool CanMoveBead(BeadScript beadScript)
        {
            if (lastBeadSelected && lastBeadSelected.Equals(beadScript) && moveCount >= 1)
            {
                return false;
            }
            else
            {
                return true;
            }

        }
        public void SetBead(BeadScript beadScript)
        {
            currentBead = beadScript;
            if (lastBeadSelected && lastBeadSelected.Equals(beadScript))
            {
                moveCount++;
                Debug.Log("moveCount....." + moveCount);
            }
            else
            {
                moveCount = 0;
            }
            lastBeadSelected = beadScript;
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            // Display the explosion radius when selected
            Gizmos.color = new Color(1, 1, 0, 0.75F);
            Gizmos.DrawSphere(transform.position, 0.1f);
            Gizmos.color = Color.red;

            for (int i = 0; i < connectedNodesList.Count; i++)
            {
                if (connectedNodesList[i].nextNextNode)
                {
                    Vector3 startPosition = transform.position;
                    Vector3 endPosition = connectedNodesList[i].nextNode.transform.position;
                    UnityEditor.Handles.DrawBezier(startPosition, endPosition, startPosition,
                        endPosition, Color.red, null, 6);

                    startPosition = connectedNodesList[i].nextNode.transform.position;
                    endPosition = connectedNodesList[i].nextNextNode.transform.position;
                    UnityEditor.Handles.DrawBezier(startPosition, endPosition, startPosition,
                        endPosition, Color.green, null, 6);

                    //  Gizmos.DrawLine(transform.position, connectedNodesList[i].nextNextNode.transform.position);

                }
                else
                {
                    UnityEditor.Handles.DrawBezier(transform.position, connectedNodesList[i].nextNode.transform.position, transform.position,
                       connectedNodesList[i].nextNode.transform.position, Color.red, null, 6);
                    //  Gizmos.DrawLine(transform.position, connectedNodesList[i].nextNode.transform.position);

                }
            }

        }
        void OnDrawGizmos()
        {
#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.red;
            UnityEditor.Handles.Label(transform.position, transform.name);
#endif
        }

#endif
    }
}