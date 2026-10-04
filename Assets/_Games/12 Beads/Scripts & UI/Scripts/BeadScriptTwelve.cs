using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

namespace Twelve
{
    public class BeadScriptTwelve : NetworkBehaviour
    {
        public PLAYERS playerID;
        public int beadId;
        public NodeScriptTwelve currentNode;
        public Sprite selectedSprite;
        public BeadScriptTwelve enmyBead;

        public bool isDead { get; set; }

        public Sprite Greenbead, OrangeBead;
        public Sprite GreenbeadSelected, OrangeBeadSelected;
        private readonly SyncList<int> moveHistorySync = new SyncList<int>();
        private readonly List<int> moveHistoryLocal = new List<int>();
        private bool IsNetworkActive => NetworkServer.active || NetworkClient.active;
        public IReadOnlyList<int> MoveHistory => IsNetworkActive ? (IReadOnlyList<int>)moveHistorySync : moveHistoryLocal;

        [SyncVar(hook = nameof(OnNodeChanged))]
        public int currentNodeId = -1;
        [SyncVar(hook = nameof(OnBeadIdChanged))]
        public int currentBeadId = -1;
        private void Start()
        {
            transform.parent = GamePlayControllerTwelve.instance.beadsParentTransform;

        }
        void OnBeadIdChanged(int oldId, int newId)
        {
            beadId = newId;
        }
        void OnNodeChanged(int oldId, int newId)
        {
            Debug.Log("NEW id :" + newId + playerID.ToString());
            if (newId < 0) return;

            var allNodes = GamePlayControllerTwelve.instance.allNodes;
            // Defensive: SyncVar carries a nodeNo. We treat it as an index into allNodes,
            // which only works if every node's serialized nodeNo equals its list index.
            // A typo in the scene/prefab (e.g. nodeNo=1415 instead of 15) would otherwise
            // throw IndexOutOfRange here and leave transform.position un-updated, making
            // the bead visually stay on its old square after a kill in multiplayer.
            NodeScriptTwelve resolved = (newId >= 0 && newId < allNodes.Count) ? allNodes[newId] : null;
            if (resolved == null || resolved.nodeNo != newId)
            {
                for (int i = 0; i < allNodes.Count; i++)
                {
                    if (allNodes[i] != null && allNodes[i].nodeNo == newId) { resolved = allNodes[i]; break; }
                }
            }
            if (resolved == null)
            {
                Debug.LogError($"[BeadScriptTwelve] OnNodeChanged: nodeNo {newId} not found in allNodes (count={allNodes.Count})");
                return;
            }

            if (playerID == PLAYERS.EMPTY)
            {
                currentNode = resolved;
                currentNode.emptyNode = gameObject;
            }
            else if (playerID == PLAYERS.PLAYER1 || playerID == PLAYERS.PLAYER2)
            {
                currentNode = resolved;
                currentNode.OnOccupied(true);
                currentNode.currentBead = this;
            }

            transform.position = currentNode.transform.position + new Vector3(0, 0, -2);
        }
        [SyncVar(hook = nameof(OnVisibilityChanged))]
        bool isVisible = true;
        // CmdSetNodeValue(int) was removed: it let any client teleport any bead by writing
        // its node SyncVar. The validated move transaction writes currentNodeId on the server.

        [Server]
        public void ServerSetNodeValue(int value)
        {
            currentNodeId = value;
        }

        void OnVisibilityChanged(bool oldVal, bool newVal)
        {
            if (!newVal && playerID != PLAYERS.EMPTY && currentNode != null && currentNode.currentBead == this)
            {
                currentNode.OnOccupied(false);
            }

            gameObject.SetActive(newVal);
        }

        public void SetVisible(bool value)
        {
            if (NetworkServer.active)
                isVisible = value;
            else gameObject.SetActive(value);
        }
        public override void OnStartClient()
        {
            base.OnStartClient();
            transform.parent = GamePlayControllerTwelve.instance.beadsParentTransform;

            if (playerID == PLAYERS.EMPTY)
            {
                GamePlayControllerTwelve.instance.emptyBeads.Add(this);
                Debug.Log("Empty Beads Count: " + GamePlayControllerTwelve.instance.emptyBeads.Count);
                if (GamePlayControllerTwelve.instance.emptyBeads.Count == 25)
                {
                    // Map by beadId (not list order) so rematch/reconnect scene reloads stay correct
                    foreach (var bead in GamePlayControllerTwelve.instance.emptyBeads)
                    {
                        Debug.Log("Setting Bead: " + bead.beadId);
                        GamePlayControllerTwelve.instance.allNodes[bead.beadId].emptyNode = bead.gameObject;
                    }
                }
            }
            else if (playerID == PLAYERS.PLAYER1)
            {
                GamePlayControllerTwelve.instance.team1Beads.Add(this);
            }
            else if (playerID == PLAYERS.PLAYER2)
            {
                GamePlayControllerTwelve.instance.team2Beads.Add(this);
            }
        }
        public void SetColors()
        {

            Image img = GetComponent<Image>();
            img.sprite = img.sprite == Greenbead ? OrangeBead : Greenbead;
            selectedSprite = selectedSprite == GreenbeadSelected ? OrangeBeadSelected : GreenbeadSelected;

        }

        public void RecordMoveNode(NodeScriptTwelve nodeScript)
        {
            if (!IsNetworkActive)
            {
                moveHistoryLocal.Add(nodeScript.nodeNo);
                while (moveHistoryLocal.Count > 4)
                    moveHistoryLocal.RemoveAt(0);
            }
            else if (NetworkServer.active)
            {
                moveHistorySync.Add(nodeScript.nodeNo);
                while (moveHistorySync.Count > 4)
                    moveHistorySync.RemoveAt(0);
            }
        }

        public void ClearMoveHistory()
        {
            if (!IsNetworkActive)
                moveHistoryLocal.Clear();
            else if (NetworkServer.active)
                moveHistorySync.Clear();
        }

        public bool isCanMove(NodeScriptTwelve nodeScript)
        {
            if (MoveHistory.Count >= 4)
            {
                if (MoveHistory[0] == MoveHistory[2] &&
                    MoveHistory[1] == MoveHistory[3] &&
                    MoveHistory[0] == nodeScript.nodeNo)
                {
                    return false;
                }
            }
            return true;
        }


        public void OnBeadSelectedRpc()
        {
            OnBeadSelected();
        }

        public void OnBeadSelected()
        {
            // Add your bead selection logic here

            // Only the player with input authority can select this bead
            OnBeadSelectedRpc();

        }

        // Optional: Add network tick method for any per-tick updates

    }
}
