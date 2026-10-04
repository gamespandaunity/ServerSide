using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace Snake_Ladder
{
    public class BeadScript : MonoBehaviour
    {
        public PLAYERS playerID;
        public int beadId;
        public NodeScript currentNode;
        public Sprite selectedSprite;
        public BeadScript enmyBead;
        public bool isDead;
        public List<NodeScript> nodeScripts = new List<NodeScript>();


        public void AdNode(NodeScript nodeScript)
        {
            nodeScripts.Add(nodeScript);
        }

        public bool isCanMove(NodeScript nodeScript)
        {

            if (nodeScripts.Count >= 4)
            {
                if (nodeScripts[0].Equals(nodeScripts[2]) && nodeScripts[1].Equals(nodeScripts[3]) && nodeScripts[0].Equals(nodeScript))
                {
                    return false;
                }
                else
                {

                    nodeScripts.Clear();
                }
            }
            return true;
        }


        public void OnBeadSelected()
        {

        }

    }
}