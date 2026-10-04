using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
namespace Twelve
{
    public class NodeScriptTwelve : MonoBehaviour
    {
        public int nodeNo;
        public bool isOccupied;
        public GameObject emptyNode;
        public BeadScriptTwelve currentBead;

        public int moveCount;
        public BeadScriptTwelve lastBeadSelected;
        public List<ConnectedNodesTwelve> connectedNodesList;
        public List<NodeScriptTwelve> nextNodesList;

        public void OnOccupied(bool occupied)
        {

            isOccupied = occupied;
            if (emptyNode == null)
            {
                return;
            }
            if (!isOccupied && emptyNode)
            {
                currentBead = emptyNode.GetComponent<BeadScriptTwelve>();
            }
            emptyNode.GetComponent<BeadScriptTwelve>().SetVisible(false);
            // Server updates isOccupied via RemotedMoveSelected -> OnMoveSelected.
            // Clients no longer send CmdSetNodeOccupied to avoid race conditions
            // where delayed commands from a previous turn overwrite current state.
            // emptyNode?.SetActive(false);
        }

        public bool CanMoveBead(BeadScriptTwelve beadScript)
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
        public void SetBead(BeadScriptTwelve beadScript)
        {
            currentBead = beadScript;
            if (lastBeadSelected && lastBeadSelected.Equals(beadScript))
            {
                moveCount++;
                //Debug.Log("moveCount....." + moveCount);
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
