// ════════════════════════════════════════════════════════════════════════════════════════════
// GroundController.Fielding — KEEPER + FIELDER state machines.
// Keeper: catchAttempt gated by LockstepKeeperGateOpen (holds until the batter's input is known or
// crossing+45 ticks grace); beaten-ball collect at y<1.3. Fielders: chase/catch driven by the
// BATTER's authoritative relays (CmdSetCatchFielder catch-point, CmdSetActiveFielderSetup full
// setup, CmdConfirmOutfieldCatch early catch confirm) — the follower never recomputes chase targets.
// ════════════════════════════════════════════════════════════════════════════════════════════
using Beebyte.Obfuscator;
using CodeStage.AntiCheat.ObscuredTypes;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Cricket;
using UnityEngine.Serialization;

// Partial of GroundController — thematic split (see GroundController.cs for fields + lifecycle).
public partial class GroundController
{
    public void ShowFielder10(bool fielder10Status, bool ball10Status)
    {
        fielder10SkinRenderer.enabled = fielder10Status;
        Fielder10BallSkinRenderer.enabled = ball10Status;
        if (!fielder10Status || isBowlerActivationAllowed)
        {
            return;
        }
        isBowlerActivationAllowed = true;
        // The bowler's animation is bone-driven (no root motion), so neither the
        // root nor the model transform tracks his visible pose — only the rig does.
        // Place fielder10 at the hip bone's ground projection at this swap frame so
        // it appears exactly where the bowler is standing, for every bowler
        // type/side/hand/anim. The SetBowlerSide presets remain as the fallback.
        if (bowlerHipBoneTransform != null)
        {
            Vector3 bowlerHipPosition = bowlerHipBoneTransform.position;
            fielder10SkinTransform.position = new Vector3(bowlerHipPosition.x, 0f, bowlerHipPosition.z);
        }
        fielderAction = "idle";
        if (currentBallStatus == "bowled" && !isLineFreeHitActive)
        {
            fielder10Anim.Play("appeal");
            fielder10Anim["appeal"].speed = 0.45f;
        }
        if (hasLBWAppeal)
        {
            _stayStartTime = Time.time + 2f;
            if (!isLineFreeHitActive)
            {
                fielderAction = "lbwAppeal";
                fielder10Anim.Play("lbwAppeal");
                fielder10Anim["lbwAppeal"].speed = 0.45f;
            }
            else
            {
                hasLBWAppeal = false;
            }
        }
    }

    private void setFieldersPosition()
    {
        for (int i = 1; i <= numberOfFielders; i++)
        {
            fielders[i] = GameObject.Find("/Fielders/Fielder" + i);
            fieldersAnim[i] = fielders[i].GetComponent<Animation>();
            fielderTransforms[i] = fielders[i].transform;

            fielderBallReleasePoints[i] = GameObject.Find("/Fielders/Fielder" + i + "/ballRef");

            fielderBallReleasePointTransforms[i] = fielderBallReleasePoints[i].transform;
            fielderReferences[i] = GameObject.Find("/Fielders/Fielder" + i + "/Ref");
            fielderBalls[i] = GameObject.Find("/Fielders/Fielder" + i + "/Sphere");
            fielderTransforms[i].eulerAngles = new Vector3(fielderTransforms[i].eulerAngles.x, 0f, fielderTransforms[i].eulerAngles.z);
            ref Vector3 reference = ref fielderInitialPositions[i];
            reference = fielderTransforms[i].position;
            ref Vector3 reference2 = ref restriction1FielderPositions[i];
            reference2 = GameObject.Find("FieldRestriction1_Fielder" + i).transform.position;
            ref Vector3 reference3 = ref restriction2FielderPositions[i];
            reference3 = GameObject.Find("FieldRestriction2_Fielder" + i).transform.position;
            ref Vector3 reference4 = ref restriction3FielderPositions[i];
            reference4 = GameObject.Find("FieldRestriction3_Fielder" + i).transform.position;
            ref Vector3 reference5 = ref restriction4FielderPositions[i];
            reference5 = GameObject.Find("FieldRestriction4_Fielder" + i).transform.position;
            ref Vector3 reference6 = ref restriction5FielderPositions[i];
            reference6 = GameObject.Find("FieldRestriction5_Fielder" + i).transform.position;
            ref Vector3 reference7 = ref restriction6FielderPositions[i];
            reference7 = GameObject.Find("FieldRestriction6_Fielder" + i).transform.position;
            ref Vector3 reference8 = ref restriction7FielderPositions[i];
            reference8 = GameObject.Find("FieldRestriction7_Fielder" + i).transform.position;
            ref Vector3 reference9 = ref restriction8FielderPositions[i];
            reference9 = GameObject.Find("FieldRestriction8_Fielder" + i).transform.position;
            ref Vector3 reference10 = ref restriction9FielderPositions[i];
            reference10 = GameObject.Find("FieldRestriction9_Fielder" + i).transform.position;
            ref Vector3 reference11 = ref restriction10FielderPositions[i];
            reference11 = GameObject.Find("FieldRestriction10_Fielder" + i).transform.position;
            ref Vector3 reference12 = ref restriction11FielderPositions[i];
            reference12 = GameObject.Find("FieldRestriction11_Fielder" + i).transform.position;
            ref Vector3 reference13 = ref restriction12FielderPositions[i];
            reference13 = GameObject.Find("FieldRestriction12_Fielder" + i).transform.position;
            ref Vector3 reference14 = ref restriction13FielderPositions[i];
            reference14 = GameObject.Find("FieldRestriction13_Fielder" + i).transform.position;
            ref Vector3 reference15 = ref restriction14FielderPositions[i];
            reference15 = GameObject.Find("FieldRestriction14_Fielder" + i).transform.position;
            ref Vector3 reference16 = ref restriction15FielderPositions[i];
            reference16 = GameObject.Find("FieldRestriction15_Fielder" + i).transform.position;
            ref Vector3 reference17 = ref restriction16FielderPositions[i];
            reference17 = GameObject.Find("FieldRestriction16_Fielder" + i).transform.position;
            ref Vector3 reference18 = ref restriction17FielderPositions[i];
            reference18 = GameObject.Find("FieldRestriction17_Fielder" + i).transform.position;
            ref Vector3 reference19 = ref restriction18FielderPositions[i];
            reference19 = GameObject.Find("FieldRestriction18_Fielder" + i).transform.position;
            ref Vector3 reference20 = ref restriction19FielderPositions[i];
            reference20 = GameObject.Find("FieldRestriction19_Fielder" + i).transform.position;
            ref Vector3 reference21 = ref restriction20FielderPositions[i];
            reference21 = GameObject.Find("FieldRestriction20_Fielder" + i).transform.position;
            ref Vector3 reference22 = ref restriction21FielderPositions[i];
            reference22 = GameObject.Find("FieldRestriction21_Fielder" + i).transform.position;
            ref Vector3 reference23 = ref restriction22FielderPositions[i];
            reference23 = GameObject.Find("FieldRestriction22_Fielder" + i).transform.position;
            ref Vector3 reference24 = ref restriction23FielderPositions[i];
            reference24 = GameObject.Find("FieldRestriction23_Fielder" + i).transform.position;
            ref Vector3 reference25 = ref restriction24FielderPositions[i];
            reference25 = GameObject.Find("FieldRestriction24_Fielder" + i).transform.position;
            ref Vector3 reference26 = ref restriction25FielderPositions[i];
            reference26 = GameObject.Find("FieldRestriction25_Fielder" + i).transform.position;
            fielderChasePoints[i] = GameObject.Find("FielderChasePoint" + i);
            isFielderNearPitch.Add(item: false);
            fielderModels[i] = GameObject.Find("/Fielders/Fielder" + i + "/Fielder");
            FielderSkinRenderer[i] = fielderModels[i].GetComponent<Renderer>();
            fielderCaps[i] = GameObject.Find("/Fielders/Fielder" + i + "/Cap.001").GetComponent<Renderer>();
        }
    }

    private IEnumerator FieldersRandomWarmUpAnimation()
    {
        yield return new WaitForSeconds(0.5f);
        for (int i = 1; i <= numberOfFielders; i++)
        {
            int num = UnityEngine.Random.Range(1, 6);
            fieldersAnim[i].Play("WCCLite_FielderIdle0" + num);
        }
    }

    private void getSlipFielders()
    {
        slipFielders.Clear();
        slipFieldersWarmUpStatus.Clear();
        int num = 7;
        if (bowlerType == "spin")
        {
            num = 15;
        }
        for (int i = 1; i <= numberOfFielders; i++)
        {
            slipFieldersWarmUpStatus.Add(item: false);
            if (batsmanHand == "right" && fielderTransforms[i].position.z > 0f)
            {
                if (_wicketKeeperTransform.position.x > fielderTransforms[i].position.x && DistanceBetweenTwoVector2(wicketKeeperObject, fielders[i]) < (float)num && AngleBetweenTwoGameObjects(stumpLeft, fielders[i]) > 85f && AngleBetweenTwoGameObjects(stumpLeft, fielders[i]) < 135f)
                {
                    slipFielders.Add(fielders[i]);
                }
            }
            else if (batsmanHand == "left" && fielderTransforms[i].position.z > 0f && _wicketKeeperTransform.position.x < fielderTransforms[i].position.x && DistanceBetweenTwoVector2(wicketKeeperObject, fielders[i]) < (float)num && AngleBetweenTwoGameObjects(stumpLeft, fielders[i]) < 85f && AngleBetweenTwoGameObjects(stumpLeft, fielders[i]) > 45f)
            {
                slipFielders.Add(fielders[i]);
            }
        }
    }

    //Photon Removal [PunRPC]
    public void RPC_FielderChangeIndex(int FielderChangeIdx)
    {
        CONTROLLER.fielderChangeIndex = FielderChangeIdx;
        Singleton<PreviewScreen>.instance.SetFieldPreview();
        ResetFielders();
    }

    public void ChangeFielderIdx(int FielderChangeIdx)
    {
        if (GameConstants.isWithAI == false)
        {
            //    view.RPC("RPC_FielderChangeIndex", RpcTarget.AllBuffered, FielderChangeIdx);
            CricketNetworkManager.instance.CmdFielderChangeIndex(FielderChangeIdx);
        }



    }

    public void ResetFielders()
    {
        for (int i = 1; i <= numberOfFielders; i++)
        {
            isFielderNearPitch.Add(item: false);
            if (CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex || CONTROLLER.PlayModeSelected == 8)
            {
                if (isFieldRestrictionActive)
                {
                    if (CONTROLLER.fielderChangeIndex == 1)
                    {
                        fielderTransforms[i].position = restriction1FielderPositions[i];
                    }
                    else if (CONTROLLER.fielderChangeIndex == 2)
                    {
                        fielderTransforms[i].position = restriction2FielderPositions[i];
                    }
                    else if (CONTROLLER.fielderChangeIndex == 3)
                    {
                        fielderTransforms[i].position = restriction3FielderPositions[i];
                    }
                    else if (CONTROLLER.fielderChangeIndex == 4)
                    {
                        fielderTransforms[i].position = restriction4FielderPositions[i];
                    }
                    else if (CONTROLLER.fielderChangeIndex == 5)
                    {
                        fielderTransforms[i].position = restriction5FielderPositions[i];
                    }
                    else if (CONTROLLER.fielderChangeIndex == 6)
                    {
                        fielderTransforms[i].position = restriction6FielderPositions[i];
                    }
                    else if (CONTROLLER.fielderChangeIndex == 7)
                    {
                        fielderTransforms[i].position = restriction7FielderPositions[i];
                    }
                    else if (CONTROLLER.fielderChangeIndex == 8)
                    {
                        fielderTransforms[i].position = restriction8FielderPositions[i];
                    }
                    else if (CONTROLLER.fielderChangeIndex == 9)
                    {
                        fielderTransforms[i].position = restriction9FielderPositions[i];
                    }
                    else if (CONTROLLER.fielderChangeIndex == 10)
                    {
                        fielderTransforms[i].position = restriction10FielderPositions[i];
                    }
                    if (batsmanHand == "left")
                    {
                        if (CONTROLLER.fielderChangeIndex == 1)
                        {
                            fielderTransforms[i].position = new Vector3(restriction1FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                        else if (CONTROLLER.fielderChangeIndex == 2)
                        {
                            fielderTransforms[i].position = new Vector3(restriction2FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                        else if (CONTROLLER.fielderChangeIndex == 3)
                        {
                            fielderTransforms[i].position = new Vector3(restriction3FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                        else if (CONTROLLER.fielderChangeIndex == 4)
                        {
                            fielderTransforms[i].position = new Vector3(restriction4FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                        else if (CONTROLLER.fielderChangeIndex == 5)
                        {
                            fielderTransforms[i].position = new Vector3(restriction5FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                        else if (CONTROLLER.fielderChangeIndex == 6)
                        {
                            fielderTransforms[i].position = new Vector3(restriction6FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                        else if (CONTROLLER.fielderChangeIndex == 7)
                        {
                            fielderTransforms[i].position = new Vector3(restriction7FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                        else if (CONTROLLER.fielderChangeIndex == 8)
                        {
                            fielderTransforms[i].position = new Vector3(restriction8FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                        else if (CONTROLLER.fielderChangeIndex == 9)
                        {
                            fielderTransforms[i].position = new Vector3(restriction9FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                        else if (CONTROLLER.fielderChangeIndex == 10)
                        {
                            fielderTransforms[i].position = new Vector3(restriction10FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                    }
                }
                else if (!isFieldRestrictionActive)
                {
                    if (CONTROLLER.fielderChangeIndex == 1)
                    {
                        fielderTransforms[i].position = restriction1FielderPositions[i];
                    }
                    else if (CONTROLLER.fielderChangeIndex == 2)
                    {
                        fielderTransforms[i].position = restriction2FielderPositions[i];
                    }
                    else if (CONTROLLER.fielderChangeIndex == 3)
                    {
                        fielderTransforms[i].position = restriction3FielderPositions[i];
                    }
                    else if (CONTROLLER.fielderChangeIndex == 4)
                    {
                        fielderTransforms[i].position = restriction4FielderPositions[i];
                    }
                    else if (CONTROLLER.fielderChangeIndex == 5)
                    {
                        fielderTransforms[i].position = restriction5FielderPositions[i];
                    }
                    else if (CONTROLLER.fielderChangeIndex == 6)
                    {
                        fielderTransforms[i].position = restriction6FielderPositions[i];
                    }
                    else if (CONTROLLER.fielderChangeIndex == 7)
                    {
                        fielderTransforms[i].position = restriction7FielderPositions[i];
                    }
                    else if (CONTROLLER.fielderChangeIndex == 8)
                    {
                        fielderTransforms[i].position = restriction8FielderPositions[i];
                    }
                    else if (CONTROLLER.fielderChangeIndex == 9)
                    {
                        fielderTransforms[i].position = restriction9FielderPositions[i];
                    }
                    else if (CONTROLLER.fielderChangeIndex == 10)
                    {
                        fielderTransforms[i].position = restriction10FielderPositions[i];
                    }
                    else if (CONTROLLER.fielderChangeIndex == 11)
                    {
                        fielderTransforms[i].position = restriction11FielderPositions[i];
                    }
                    else if (CONTROLLER.fielderChangeIndex == 12)
                    {
                        fielderTransforms[i].position = restriction12FielderPositions[i];
                    }
                    else if (CONTROLLER.fielderChangeIndex == 13)
                    {
                        fielderTransforms[i].position = restriction13FielderPositions[i];
                    }
                    else if (CONTROLLER.fielderChangeIndex == 14)
                    {
                        fielderTransforms[i].position = restriction14FielderPositions[i];
                    }
                    else if (CONTROLLER.fielderChangeIndex == 15)
                    {
                        fielderTransforms[i].position = restriction15FielderPositions[i];
                    }
                    else if (CONTROLLER.fielderChangeIndex == 16)
                    {
                        fielderTransforms[i].position = restriction16FielderPositions[i];
                    }
                    else if (CONTROLLER.fielderChangeIndex == 17)
                    {
                        fielderTransforms[i].position = restriction17FielderPositions[i];
                    }
                    else if (CONTROLLER.fielderChangeIndex == 18)
                    {
                        fielderTransforms[i].position = restriction18FielderPositions[i];
                    }
                    else if (CONTROLLER.fielderChangeIndex == 19)
                    {
                        fielderTransforms[i].position = restriction19FielderPositions[i];
                    }
                    else if (CONTROLLER.fielderChangeIndex == 20)
                    {
                        fielderTransforms[i].position = restriction20FielderPositions[i];
                    }
                    else if (CONTROLLER.fielderChangeIndex == 21)
                    {
                        fielderTransforms[i].position = restriction21FielderPositions[i];
                    }
                    else if (CONTROLLER.fielderChangeIndex == 22)
                    {
                        fielderTransforms[i].position = restriction22FielderPositions[i];
                    }
                    else if (CONTROLLER.fielderChangeIndex == 23)
                    {
                        fielderTransforms[i].position = restriction23FielderPositions[i];
                    }
                    else if (CONTROLLER.fielderChangeIndex == 24)
                    {
                        fielderTransforms[i].position = restriction24FielderPositions[i];
                    }
                    else if (CONTROLLER.fielderChangeIndex == 25)
                    {
                        fielderTransforms[i].position = restriction25FielderPositions[i];
                    }
                    if (batsmanHand == "left")
                    {
                        if (CONTROLLER.fielderChangeIndex == 1)
                        {
                            fielderTransforms[i].position = new Vector3(restriction1FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                        else if (CONTROLLER.fielderChangeIndex == 2)
                        {
                            fielderTransforms[i].position = new Vector3(restriction2FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                        else if (CONTROLLER.fielderChangeIndex == 3)
                        {
                            fielderTransforms[i].position = new Vector3(restriction3FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                        else if (CONTROLLER.fielderChangeIndex == 4)
                        {
                            fielderTransforms[i].position = new Vector3(restriction4FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                        else if (CONTROLLER.fielderChangeIndex == 5)
                        {
                            fielderTransforms[i].position = new Vector3(restriction5FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                        else if (CONTROLLER.fielderChangeIndex == 6)
                        {
                            fielderTransforms[i].position = new Vector3(restriction6FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                        else if (CONTROLLER.fielderChangeIndex == 7)
                        {
                            fielderTransforms[i].position = new Vector3(restriction7FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                        else if (CONTROLLER.fielderChangeIndex == 8)
                        {
                            fielderTransforms[i].position = new Vector3(restriction8FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                        else if (CONTROLLER.fielderChangeIndex == 9)
                        {
                            fielderTransforms[i].position = new Vector3(restriction9FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                        else if (CONTROLLER.fielderChangeIndex == 10)
                        {
                            fielderTransforms[i].position = new Vector3(restriction10FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                        else if (CONTROLLER.fielderChangeIndex == 11)
                        {
                            fielderTransforms[i].position = new Vector3(restriction11FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                        else if (CONTROLLER.fielderChangeIndex == 12)
                        {
                            fielderTransforms[i].position = new Vector3(restriction12FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                        else if (CONTROLLER.fielderChangeIndex == 13)
                        {
                            fielderTransforms[i].position = new Vector3(restriction13FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                        else if (CONTROLLER.fielderChangeIndex == 14)
                        {
                            fielderTransforms[i].position = new Vector3(restriction14FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                        else if (CONTROLLER.fielderChangeIndex == 15)
                        {
                            fielderTransforms[i].position = new Vector3(restriction15FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                        else if (CONTROLLER.fielderChangeIndex == 16)
                        {
                            fielderTransforms[i].position = new Vector3(restriction16FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                        else if (CONTROLLER.fielderChangeIndex == 17)
                        {
                            fielderTransforms[i].position = new Vector3(restriction17FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                        else if (CONTROLLER.fielderChangeIndex == 18)
                        {
                            fielderTransforms[i].position = new Vector3(restriction18FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                        else if (CONTROLLER.fielderChangeIndex == 19)
                        {
                            fielderTransforms[i].position = new Vector3(restriction19FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                        else if (CONTROLLER.fielderChangeIndex == 20)
                        {
                            fielderTransforms[i].position = new Vector3(restriction20FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                        else if (CONTROLLER.fielderChangeIndex == 21)
                        {
                            fielderTransforms[i].position = new Vector3(restriction21FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                        else if (CONTROLLER.fielderChangeIndex == 22)
                        {
                            fielderTransforms[i].position = new Vector3(restriction22FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                        else if (CONTROLLER.fielderChangeIndex == 23)
                        {
                            fielderTransforms[i].position = new Vector3(restriction23FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                        else if (CONTROLLER.fielderChangeIndex == 24)
                        {
                            fielderTransforms[i].position = new Vector3(restriction24FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                        else if (CONTROLLER.fielderChangeIndex == 25)
                        {
                            fielderTransforms[i].position = new Vector3(restriction25FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                        }
                    }
                }
            }
            else if (CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex)
            {
                if (CONTROLLER.computerFielderChangeIndex == 1 || CONTROLLER.computerFielderChangeIndex == 0)
                {
                    fielderTransforms[i].position = restriction1FielderPositions[i];
                }
                else if (CONTROLLER.computerFielderChangeIndex == 2)
                {
                    fielderTransforms[i].position = restriction2FielderPositions[i];
                }
                else if (CONTROLLER.computerFielderChangeIndex == 3)
                {
                    fielderTransforms[i].position = restriction3FielderPositions[i];
                }
                else if (CONTROLLER.computerFielderChangeIndex == 4)
                {
                    fielderTransforms[i].position = restriction4FielderPositions[i];
                }
                else if (CONTROLLER.computerFielderChangeIndex == 5)
                {
                    fielderTransforms[i].position = restriction5FielderPositions[i];
                }
                else if (CONTROLLER.computerFielderChangeIndex == 6)
                {
                    fielderTransforms[i].position = restriction6FielderPositions[i];
                }
                else if (CONTROLLER.computerFielderChangeIndex == 7)
                {
                    fielderTransforms[i].position = restriction7FielderPositions[i];
                }
                else if (CONTROLLER.computerFielderChangeIndex == 8)
                {
                    fielderTransforms[i].position = restriction8FielderPositions[i];
                }
                else if (CONTROLLER.computerFielderChangeIndex == 9)
                {
                    fielderTransforms[i].position = restriction9FielderPositions[i];
                }
                else if (CONTROLLER.computerFielderChangeIndex == 10)
                {
                    fielderTransforms[i].position = restriction10FielderPositions[i];
                }
                else if (CONTROLLER.computerFielderChangeIndex == 11)
                {
                    fielderTransforms[i].position = restriction11FielderPositions[i];
                }
                else if (CONTROLLER.computerFielderChangeIndex == 12)
                {
                    fielderTransforms[i].position = restriction12FielderPositions[i];
                }
                else if (CONTROLLER.computerFielderChangeIndex == 13)
                {
                    fielderTransforms[i].position = restriction13FielderPositions[i];
                }
                else if (CONTROLLER.computerFielderChangeIndex == 14)
                {
                    fielderTransforms[i].position = restriction14FielderPositions[i];
                }
                else if (CONTROLLER.computerFielderChangeIndex == 15)
                {
                    fielderTransforms[i].position = restriction15FielderPositions[i];
                }
                else if (CONTROLLER.computerFielderChangeIndex == 16)
                {
                    fielderTransforms[i].position = restriction16FielderPositions[i];
                }
                else if (CONTROLLER.computerFielderChangeIndex == 17)
                {
                    fielderTransforms[i].position = restriction17FielderPositions[i];
                }
                else if (CONTROLLER.computerFielderChangeIndex == 18)
                {
                    fielderTransforms[i].position = restriction18FielderPositions[i];
                }
                else if (CONTROLLER.computerFielderChangeIndex == 19)
                {
                    fielderTransforms[i].position = restriction19FielderPositions[i];
                }
                else if (CONTROLLER.computerFielderChangeIndex == 20)
                {
                    fielderTransforms[i].position = restriction20FielderPositions[i];
                }
                else if (CONTROLLER.computerFielderChangeIndex == 21)
                {
                    fielderTransforms[i].position = restriction21FielderPositions[i];
                }
                else if (CONTROLLER.computerFielderChangeIndex == 22)
                {
                    fielderTransforms[i].position = restriction22FielderPositions[i];
                }
                else if (CONTROLLER.computerFielderChangeIndex == 23)
                {
                    fielderTransforms[i].position = restriction23FielderPositions[i];
                }
                else if (CONTROLLER.computerFielderChangeIndex == 24)
                {
                    fielderTransforms[i].position = restriction24FielderPositions[i];
                }
                else if (CONTROLLER.computerFielderChangeIndex == 25)
                {
                    fielderTransforms[i].position = restriction25FielderPositions[i];
                }
                if (batsmanHand == "left")
                {
                    if (CONTROLLER.computerFielderChangeIndex == 1 || CONTROLLER.computerFielderChangeIndex == 0)
                    {
                        fielderTransforms[i].position = new Vector3(restriction1FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                    }
                    else if (CONTROLLER.computerFielderChangeIndex == 2)
                    {
                        fielderTransforms[i].position = new Vector3(restriction2FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                    }
                    else if (CONTROLLER.computerFielderChangeIndex == 3)
                    {
                        fielderTransforms[i].position = new Vector3(restriction3FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                    }
                    else if (CONTROLLER.computerFielderChangeIndex == 4)
                    {
                        fielderTransforms[i].position = new Vector3(restriction4FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                    }
                    else if (CONTROLLER.computerFielderChangeIndex == 5)
                    {
                        fielderTransforms[i].position = new Vector3(restriction5FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                    }
                    else if (CONTROLLER.computerFielderChangeIndex == 6)
                    {
                        fielderTransforms[i].position = new Vector3(restriction6FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                    }
                    else if (CONTROLLER.computerFielderChangeIndex == 7)
                    {
                        fielderTransforms[i].position = new Vector3(restriction7FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                    }
                    else if (CONTROLLER.computerFielderChangeIndex == 8)
                    {
                        fielderTransforms[i].position = new Vector3(restriction8FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                    }
                    else if (CONTROLLER.computerFielderChangeIndex == 9)
                    {
                        fielderTransforms[i].position = new Vector3(restriction9FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                    }
                    else if (CONTROLLER.computerFielderChangeIndex == 10)
                    {
                        fielderTransforms[i].position = new Vector3(restriction10FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                    }
                    else if (CONTROLLER.computerFielderChangeIndex == 11)
                    {
                        fielderTransforms[i].position = new Vector3(restriction11FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                    }
                    else if (CONTROLLER.computerFielderChangeIndex == 12)
                    {
                        fielderTransforms[i].position = new Vector3(restriction12FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                    }
                    else if (CONTROLLER.computerFielderChangeIndex == 13)
                    {
                        fielderTransforms[i].position = new Vector3(restriction13FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                    }
                    else if (CONTROLLER.computerFielderChangeIndex == 14)
                    {
                        fielderTransforms[i].position = new Vector3(restriction14FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                    }
                    else if (CONTROLLER.computerFielderChangeIndex == 15)
                    {
                        fielderTransforms[i].position = new Vector3(restriction15FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                    }
                    else if (CONTROLLER.computerFielderChangeIndex == 16)
                    {
                        fielderTransforms[i].position = new Vector3(restriction16FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                    }
                    else if (CONTROLLER.computerFielderChangeIndex == 17)
                    {
                        fielderTransforms[i].position = new Vector3(restriction17FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                    }
                    else if (CONTROLLER.computerFielderChangeIndex == 18)
                    {
                        fielderTransforms[i].position = new Vector3(restriction18FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                    }
                    else if (CONTROLLER.computerFielderChangeIndex == 19)
                    {
                        fielderTransforms[i].position = new Vector3(restriction19FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                    }
                    else if (CONTROLLER.computerFielderChangeIndex == 20)
                    {
                        fielderTransforms[i].position = new Vector3(restriction20FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                    }
                    else if (CONTROLLER.computerFielderChangeIndex == 21)
                    {
                        fielderTransforms[i].position = new Vector3(restriction21FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                    }
                    else if (CONTROLLER.computerFielderChangeIndex == 22)
                    {
                        fielderTransforms[i].position = new Vector3(restriction22FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                    }
                    else if (CONTROLLER.computerFielderChangeIndex == 23)
                    {
                        fielderTransforms[i].position = new Vector3(restriction23FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                    }
                    else if (CONTROLLER.computerFielderChangeIndex == 24)
                    {
                        fielderTransforms[i].position = new Vector3(restriction24FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                    }
                    else if (CONTROLLER.computerFielderChangeIndex == 25)
                    {
                        fielderTransforms[i].position = new Vector3(restriction25FielderPositions[i].x * -1f, fielderTransforms[i].position.y, fielderTransforms[i].position.z);
                    }
                }
            }
            fieldersAnim[i].Play("idle");
            GameObject gameObject = fielderBalls[i];
            gameObject.GetComponent<Renderer>().enabled = false;
            fielderTransforms[i].LookAt(stumpLeftCrease.transform);
        }
    }

    private void FielderExtraActions()
    {
        getSlipFielders();
        List<int> list = new List<int>(new int[11]
        {
            -5, -4, -3, -2, -1, 0, 1, 2, 3, 4,
            5
        });
        List<int> list2 = new List<int>(new int[3] { 1, 2, 3 });
        for (int i = 0; i < slipFielders.Count; i++)
        {
            if (!isReplayModeActive && slipFielders[i] != null && !slipFieldersWarmUpStatus[i])
            {
                slipFieldersExtraActions[i] = 0f;
                int index = UnityEngine.Random.Range(0, list.Count);
                slipFieldersExtraActions[i] = list[index];
                list.RemoveAt(index);
                slipFieldersWarmUpAnimationSpeeds[i] = UnityEngine.Random.Range(1, 3);
                int num = (int)slipFieldersExtraActions[i];
                if (num >= 1)
                {
                    GameObject gameObject = slipFielders[i];
                    gameObject.GetComponent<Animation>().CrossFade("warmUp" + num);
                }
            }
        }
    }

    private void GetFieldersAngle()
    {
        for (int i = 1; i <= numberOfFielders; i++)
        {
            float y = ballStartPoint.position.x - fielderTransforms[i].position.x;
            float x = ballStartPoint.position.z - fielderTransforms[i].position.z;
            float num = (Mathf.Atan2(y, x) * radToDeg + 360f) % 360f;
            num = (270f - num + 360f) % 360f;
            fielderAngles[i] = num;
        }
    }

    // Keeps a moving fielder inside the rope. Every chase step below advances the fielder along the angle to
    // the BALL with no distance limit of its own, and once the ball has travelled past the chase point the
    // pure-chase branch follows it directly. The ball never truly stops — its speed is floored at 0.4 and it
    // creeps on — so a chaser had nothing to stop it running straight over the boundary (tester: "fielder ball
    // ko chase karte karte boundary se bahar nikal jate hain"). The one existing guard, > 70f, sits BEYOND the
    // 68.5 rope and only runs while isBallOnBoundaryLine/shouldStopFielders is already set, so it fires late
    // or not at all. Clamping the position costs nothing and cannot deadlock anything: it only ever pulls a
    // fielder back onto the rope, which is a legal place to stand.
    private void ClampFielderInsidePlayingArea(Transform t)
    {
        if (t == null || groundCenterMarkerTransform == null) return;
        Vector3 c = groundCenterMarkerTransform.position;
        float dx = t.position.x - c.x;
        float dz = t.position.z - c.z;
        float d = Mathf.Sqrt(dx * dx + dz * dz);
        if (d <= playingAreaRadius || d <= 0.0001f) return;
        float k = playingAreaRadius / d;
        t.position = new Vector3(c.x + dx * k, t.position.y, c.z + dz * k);
    }

    private void GetFieldersDistance()
    {
        for (int i = 1; i <= numberOfFielders; i++)
        {
            fielderDistances[i] = DistanceBetweenTwoVector2(ballTimingStartGO, fielders[i]);
        }
    }

    // Restores the fielder run speed for a fresh delivery. defaultFielderSpeed is BOTH the per-delivery
    // constant this file divides by when picking chase points AND the value that actually moves the fielders
    // (position += dir * defaultFielderSpeed * Time.deltaTime). During a delivery it gets driven DOWN: the
    // boundary/stop paths decay it every frame (speed -= speed * dt * 0.5f) and four sites set it to 0
    // outright on a pickup/catch. ResetAll() restores it — but ResetAll is skipped on some paths (notably the
    // first post-reconnect ball, which arrives via RpcAutomaticBall directly), and SetActiveFielders' own
    // restore sits BELOW two early returns, so a delivery could begin with a stale 0.
    // At 0 the damage is double: `num12 / defaultFielderSpeed` is Infinity, so every fielder looks unable to
    // reach any intercept, the push-out loop runs to the playing-area cap and the friction clamp then pulls
    // them all back to the SAME point — which is exactly what the a461f541 log shows, two chasers 32u apart
    // handed the identical cp(0.6,-48.7) — and the fielders cannot move anyway, which is the tester's
    // "fielders ball ko chase hi nahi karte". Restore before any early return so every delivery starts sane.
    private void RestoreFielderSpeedForDelivery()
    {
        float _prev = defaultFielderSpeed;
        if (CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex && CONTROLLER.PlayModeSelected != 8)
            defaultFielderSpeed = 7f * (1f + agilityMultiplier);
        else
            defaultFielderSpeed = 7f;
        if (CONTROLLER.difficultyMode == "hard" && BattingBy == "user")
            defaultFielderSpeed = 8f;
        if (_prev < 1f)
            ConstantsData_M.MpLog($"[ChaseDiag] Fielder speed was DEGRADED to {_prev:F2} at delivery setup — restored to {defaultFielderSpeed:F1}. At that value the chase-point maths divides by ~0 and every chaser collapses onto the same clamped point.");
    }

    public void SetActiveFielders()
    {
        // MUST run before the early returns below — see RestoreFielderSpeedForDelivery.
        RestoreFielderSpeedForDelivery();
        if (canKeeperCatchBall)
        {
            return;
        }
        // Bowling follower: once the batting authority's full fielder setup has been relayed+applied
        // (RPC_SetActiveFielderSetup), never recompute locally — the local num9 chase radius uses a
        // per-client random firstBounceDistance/horizontalVelocity and would re-diverge (chaser to the
        // wrong distance). The relayed setup is the single source of truth. Reset per delivery in ResetAll.
        if (_hasAuthoritativeFielderSetup && CONTROLLER.PlayModeSelected == 8
            && CONTROLLER.myTeamIndex == CONTROLLER.BowlingTeamIndex)
        {
            return;
        }
        float num = 25f;
        float num2 = 100f;
        if (CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex && CONTROLLER.PlayModeSelected != 8)
        {
            defaultFielderSpeed = 7f * (1f + agilityMultiplier);
            num = 25f * (1f + agilityMultiplier);
        }
        else
        {
            defaultFielderSpeed = 7f;
        }
        if (CONTROLLER.difficultyMode == "hard" && BattingBy == "user")
        {
            num = 20f;
            defaultFielderSpeed = 8f;
        }
        targetFielderForCatch = null;
        activeFielders.Clear();
        activeFieldersActions.Clear();
        fielderChasePointsSet.Clear();
        for (int i = 1; i <= numberOfFielders; i++)
        {
            float num3 = fielderAngles[i];
            float num4 = fielderDistances[i];
            float num5 = Mathf.Abs(_ballAngle - num3);
            if (num5 > 180f)
            {
                num5 = ((!(_ballAngle > num3)) ? (360f - num3 + _ballAngle) : (360f + num3 - _ballAngle));
            }
            fielderChasePointsSet.Add(item: true);
            fielderAngleDifferencesToBall[i] = num5;
            if (!(num5 < num))
            {
                continue;
            }
            bool flag = false;
            if (deactivateSlipFielders(fielders[i]))
            {
                if (batsmanHand == "left")
                {
                    if (_ballAngle > 50f && _ballAngle < 75f && AngleBetweenTwoGameObjects(fielders[i], matchBall) <= 255f)
                    {
                        flag = true;
                        activeFielders.Add(i);
                    }
                    else if (_ballAngle >= 75f && AngleBetweenTwoGameObjects(fielders[i], matchBall) > 255f)
                    {
                        flag = true;
                        activeFielders.Add(i);
                    }
                }
                else if (_ballAngle < 110f && AngleBetweenTwoGameObjects(fielders[i], matchBall) < 285f)
                {
                    flag = true;
                    activeFielders.Add(i);
                }
                else if (_ballAngle >= 110f && AngleBetweenTwoGameObjects(fielders[i], matchBall) >= 285f)
                {
                    flag = true;
                    activeFielders.Add(i);
                }
            }
            else
            {
                flag = true;
                activeFielders.Add(i);
            }
            if (flag && DistanceBetweenTwoGameObjects(fielders[i], ballTimingStartGO) > 8f)
            {
                fieldersAnim[i].Play("run");
            }
            if (flag && DistanceBetweenTwoGameObjects(fielders[i], ballTimingStartGO) > 8f)
            {
                fieldersAnim[i]["run"].speed = 1f;
            }
            if (!flag)
            {
                continue;
            }
            float length = fieldersAnim[i]["run"].length;
            fieldersAnim[i]["run"].time = UnityEngine.Random.Range(0f, length);
            float num6 = Vector3.Distance(fielderTransforms[i].position, ballCatchingPointTransform.position);
            float num7 = num6 / defaultFielderSpeed;
            float num8 = (firstBounceDistance - preCatchDistance) / horizontalVelocity;
            if (num7 < num8)
            {
                activeFieldersActions.Add("goForCatch");
                if (num7 < num2)
                {
                    num2 = num7;
                    targetFielderForCatch = fielders[i];
                    minPickupDistance = firstBounceDistance;
                    // Catch-decision sync: the BATTING authority broadcasts the chosen catcher's index so
                    // the BOWLING follower marks the SAME fielder as the catcher (goForCatch) instead of
                    // independently deciding catch-vs-chase — which diverged (one screen catches, the
                    // other chases to the boundary). Reliable+ordered, so the last (closest) wins.
                    if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
                        && CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex
                        && CricketNetworkManager.instance != null)
                    {
                        CricketNetworkManager.instance.CmdSetCatchFielder(i, ballCatchingPointTransform.position);
                    }
                }
                continue;
            }
            activeFieldersActions.Add("goForChase");
            float f = Mathf.Sin(num5 * degToRad) * num4;
            float num9 = Mathf.Sqrt(Mathf.Pow(fielderDistances[i], 2f) - Mathf.Pow(f, 2f));
            float x = ballStartPoint.position.x + num9 * Mathf.Cos(_ballAngle * degToRad);
            float z = ballStartPoint.position.z + num9 * Mathf.Sin(_ballAngle * degToRad);
            GameObject gameObject = fielderChasePoints[i];
            gameObject.transform.position = new Vector3(x, 0f, z);
            bool flag2 = false;
            if (num9 < firstBounceDistance)
            {
                num9 = firstBounceDistance;
                x = ballStartPoint.position.x + num9 * Mathf.Cos(_ballAngle * degToRad);
                z = ballStartPoint.position.z + num9 * Mathf.Sin(_ballAngle * degToRad);
                gameObject.transform.position = new Vector3(x, gameObject.transform.position.y, z);
                GameObject go = fielderChasePoints[i];
                while (DistanceBetweenTwoVector2(groundCenterMarker, go) > playingAreaRadius)
                {
                    num9 -= 2f;
                    x = ballStartPoint.position.x + num9 * Mathf.Cos(_ballAngle * degToRad);
                    z = ballStartPoint.position.z + num9 * Mathf.Sin(_ballAngle * degToRad);
                    gameObject.transform.position = new Vector3(x, gameObject.transform.position.y, z);
                }
                flag2 = true;
            }
            float num10 = 9f;
            float num11 = num10 * animationFrameInterval * horizontalVelocity;
            float num12 = Vector3.Distance(fielderTransforms[i].position, gameObject.transform.position);
            // Never divide by a zeroed speed: Infinity here silently pushes EVERY chaser to the cap and the
            // friction clamp then collapses them onto one point. The speed is restored at setup, so this is
            // belt-and-braces for any path that zeroes it mid-setup.
            float num13 = num12 / Mathf.Max(defaultFielderSpeed, 0.001f);
            float num14 = (num9 - num11) / horizontalVelocity;
            if (!flag2)
            {
                if (num13 < num14)
                {
                    while (num13 < num14)
                    {
                        num9 -= 1f;
                        x = ballStartPoint.position.x + num9 * Mathf.Cos(_ballAngle * degToRad);
                        z = ballStartPoint.position.z + num9 * Mathf.Sin(_ballAngle * degToRad);
                        gameObject.transform.position = new Vector3(x, gameObject.transform.position.y, z);
                        num12 = Vector3.Distance(fielderTransforms[i].position, gameObject.transform.position);
                        num13 = num12 / defaultFielderSpeed;
                        num14 = (num9 - num11) / horizontalVelocity;
                    }
                    num9 += 1f;
                    x = ballStartPoint.position.x + num9 * Mathf.Cos(_ballAngle * degToRad);
                    z = ballStartPoint.position.z + num9 * Mathf.Sin(_ballAngle * degToRad);
                    gameObject.transform.position = new Vector3(x, gameObject.transform.position.y, z);
                }
                else
                {
                    GameObject go2 = fielderChasePoints[i];
                    while (num13 > num14 && DistanceBetweenTwoVector2(groundCenterMarker, go2) < playingAreaRadius - 3f)
                    {
                        num9 += 1f;
                        x = ballStartPoint.position.x + num9 * Mathf.Cos(_ballAngle * degToRad);
                        z = ballStartPoint.position.z + num9 * Mathf.Sin(_ballAngle * degToRad);
                        gameObject.transform.position = new Vector3(x, gameObject.transform.position.y, z);
                        num12 = Vector3.Distance(fielderTransforms[i].position, gameObject.transform.position);
                        num13 = num12 / defaultFielderSpeed;
                        num14 = (num9 - num11) / horizontalVelocity;
                    }
                }
            }
            // FRICTION-REACH CLAMP (13-07 "slow-motion 4": both chasers parked at z=-66 — BEYOND the rope —
            // waiting for a ball that friction physically stops near ~57u, which then crept to the boundary
            // at the 0.4 floor for ~a minute). The interception estimate above assumes CONSTANT ball speed;
            // the ground-friction raise 10→30 broke that. Ground travel after the first bounce decays at
            // velocityDampingFactor %/s (30 non-power / 20 power) → asymptotic reach ≈ fbd + hVel/k. Park the
            // chaser at 75% of the post-bounce travel so the ball always ARRIVES at him with speed.
            // Deterministic: pure math from relayed values — identical on both clients.
            float frictionK = (isPowerShotActive || isPowerShotActiveSaved) ? 0.20f : 0.30f;
            float maxFrictionReach = Mathf.Max(firstBounceDistance, 0f) + (horizontalVelocity / frictionK) * 0.75f;
            float _num9BeforeClamp = num9;
            bool _clamped = num9 > maxFrictionReach;
            if (_clamped)
            {
                num9 = maxFrictionReach;
                x = ballStartPoint.position.x + num9 * Mathf.Cos(_ballAngle * degToRad);
                z = ballStartPoint.position.z + num9 * Mathf.Sin(_ballAngle * degToRad);
                gameObject.transform.position = new Vector3(x, gameObject.transform.position.y, z);
            }
            // CHASE DIAGNOSTIC (tester: "fielders ball ko chase hi nahi karte, power ka issue lagta hai").
            // In the a461f541 freeze BOTH chasers were handed the IDENTICAL point cp(0.6,-48.7) despite
            // standing 32u apart — the signature of both being cut down to maxFrictionReach. That clamp is
            // driven purely by horizontalVelocity, which is exactly the "power" the tester suspects, and it
            // runs AFTER the num13/num14 push-out loop without re-checking that the fielder can still make
            // the shortened point. Print the whole decision per fielder so the next log settles whether the
            // chaser is parked short, parked long, or simply never arrives.
            if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false)
            {
                float _fielderTime = num12 / Mathf.Max(defaultFielderSpeed, 0.001f);
                float _ballTimeConst = (num9 - num11) / Mathf.Max(horizontalVelocity, 0.001f);
                ConstantsData_M.MpLog($"[ChaseDiag] f{i} pos=({fielderTransforms[i].position.x:F1},{fielderTransforms[i].position.z:F1}) reach={num9:F1} (pre-clamp {_num9BeforeClamp:F1}, clamped={_clamped}, maxFrictionReach={maxFrictionReach:F1}) hVel={horizontalVelocity:F1} fbd={firstBounceDistance:F1} power={(isPowerShotActive || isPowerShotActiveSaved)} k={frictionK:F2} fielderT={_fielderTime:F2}s ballT(const)={_ballTimeConst:F2}s dist={num12:F1} speed={defaultFielderSpeed:F1}");
            }
            if (num9 > firstBounceDistance && num9 < minPickupDistance)
            {
                minPickupDistance = num9;
            }
        }
        // NO-CHASER FALLBACK. A fielder is only considered when it sits within `num` (25 degrees) of the
        // ball's angle, so a shot straight into the GAP between two fielders selects NOBODY: the ball rolls
        // out, nothing goes after it, and the delivery never completes — the tester's "straight shot, ball
        // stuck on ground, no fielder approaches to pick it" (and the earlier "ball fielders ke center se
        // guzri, unhone nahi pakri"). In the 26-07 log three of twelve deliveries print no FielderSetup line
        // at all, i.e. zero active fielders. Send the CLOSEST fielder by angle instead, so somebody always
        // collects. Deterministic: it reads the same relayed contact values and angle table on both clients,
        // so each side picks the same man; the chase point uses the same friction-reach formula as above.
        if (activeFielders.Count == 0 && numberOfFielders > 0 && !canKeeperCatchBall)
        {
            int nearestFielder = -1;
            float nearestAngleDiff = float.MaxValue;
            for (int i = 1; i <= numberOfFielders; i++)
            {
                if (fielders == null || i >= fielders.Length || fielders[i] == null) continue;
                if (fielderAngleDifferencesToBall == null || i >= fielderAngleDifferencesToBall.Length) continue;
                float diff = fielderAngleDifferencesToBall[i];
                if (diff < nearestAngleDiff)
                {
                    nearestAngleDiff = diff;
                    nearestFielder = i;
                }
            }
            if (nearestFielder > 0)
            {
                activeFielders.Add(nearestFielder);
                activeFieldersActions.Add("goForChase");
                float fallbackFrictionK = (isPowerShotActive || isPowerShotActiveSaved) ? 0.20f : 0.30f;
                float fallbackReach = Mathf.Max(firstBounceDistance, 0f) + (horizontalVelocity / fallbackFrictionK) * 0.75f;
                if (fielderChasePoints != null && nearestFielder < fielderChasePoints.Length && fielderChasePoints[nearestFielder] != null)
                {
                    GameObject fallbackPoint = fielderChasePoints[nearestFielder];
                    fallbackPoint.transform.position = new Vector3(
                        ballStartPoint.position.x + fallbackReach * Mathf.Cos(_ballAngle * degToRad),
                        fallbackPoint.transform.position.y,
                        ballStartPoint.position.z + fallbackReach * Mathf.Sin(_ballAngle * degToRad));
                }
                if (fallbackReach > firstBounceDistance && fallbackReach < minPickupDistance)
                {
                    minPickupDistance = fallbackReach;
                }
                ConstantsData_M.MpLog($"[Fielding] No fielder was within the {num:F0}-degree gate for ballAngle={_ballAngle:F1} — falling back to nearest f{nearestFielder} (off by {nearestAngleDiff:F1} degrees) so the ball is collected.");
            }
        }
        if (minPickupDistance > 80f)
        {
            minPickupDistance = 80f;
        }
        fielderFocusObjectToCollectBall.transform.position = new Vector3(ballStartPoint.position.x + Mathf.Cos(_ballAngle * degToRad) * minPickupDistance, fielderFocusObjectToCollectBall.transform.position.y, ballStartPoint.position.z + Mathf.Sin(_ballAngle * degToRad) * minPickupDistance);
        // Catch-decision sync (bowling follower): if the batting authority already told us which fielder
        // catches, override our local catch-vs-chase guess above so both screens agree on the catcher.
        if (_authoritativeCatchFielder >= 0)
        {
            ApplyAuthoritativeCatchFielder();
        }

        // ONE CHASER PER CHASE POINT — the dead-straight shot.
        //
        // A shot straight down the ground sits inside the 25-degree selection band of BOTH straight fielders,
        // so both are picked, and the friction clamp then cuts both to the same reach — they are handed the
        // IDENTICAL point and converge on it from opposite sides. Every log of this stall shows it:
        //   f6=goForChase@cp(0.6,-39.4)pos(-14.6,-31.6)   f7=goForChase@cp(0.6,-39.4)pos(17.1,-31.6)
        // Two men arriving at one spot is also what feeds the de-dup, which parks one of them, and between
        // the two of them the ball is never actually collected.
        //
        // Real fielders do not do this: one man goes, gathers, and throws. So when chasers share a chase
        // point, keep exactly ONE — the one who gets there soonest (ties broken by fielder index so both
        // clients pick the same man). Catchers are never touched. This runs BEFORE the relay below, so the
        // follower receives the single chaser and cannot re-add the other.
        if (activeFielders.Count > 1)
        {
            for (int a = 0; a < activeFielders.Count; a++)
            {
                if (a >= activeFieldersActions.Count || activeFieldersActions[a] != "goForChase") continue;
                GameObject pa = fielderChasePoints[activeFielders[a]];
                if (pa == null) continue;
                for (int b = activeFielders.Count - 1; b > a; b--)
                {
                    if (b >= activeFieldersActions.Count || activeFieldersActions[b] != "goForChase") continue;
                    GameObject pb = fielderChasePoints[activeFielders[b]];
                    if (pb == null) continue;
                    if (DistanceBetweenTwoVector2(pa, pb) > 3f) continue;   // genuinely different targets
                    // Same target: keep whichever reaches it first.
                    float ta = Vector3.Distance(fielderTransforms[activeFielders[a]].position, pa.transform.position);
                    float tb = Vector3.Distance(fielderTransforms[activeFielders[b]].position, pb.transform.position);
                    int drop = (tb < ta || (tb == ta && activeFielders[b] < activeFielders[a])) ? a : b;
                    int keep = (drop == a) ? b : a;
                    ConstantsData_M.MpLog($"[Fielding] Straight shot — f{activeFielders[drop]} and f{activeFielders[keep]} were sent to the same chase point; f{activeFielders[keep]} takes it alone.");
                    // STAND THE DROPPED MAN DOWN. Removing him from activeFielders means nothing updates him
                    // again this delivery — including anything that would stop him. If a run animation had
                    // already been started on him he keeps playing it where he stands, sprinting on the spot
                    // for the rest of the ball (tester: "dusra fielder apni place py hi running krta"). Put
                    // him back to idle and face him at the ball, which is what a fielder not involved in the
                    // chase should look like.
                    int _droppedIdx = activeFielders[drop];
                    if (_droppedIdx >= 0 && _droppedIdx < fieldersAnim.Length && fieldersAnim[_droppedIdx] != null)
                        fieldersAnim[_droppedIdx].Play("idle");
                    if (_droppedIdx >= 0 && _droppedIdx < fielderTransforms.Length && fielderTransforms[_droppedIdx] != null && matchBall != null)
                        fielderTransforms[_droppedIdx].LookAt(new Vector3(matchBall.transform.position.x, fielderTransforms[_droppedIdx].position.y, matchBall.transform.position.z));
                    activeFielders.RemoveAt(drop);
                    activeFieldersActions.RemoveAt(drop);
                    if (drop <= a) { a--; break; }   // indices shifted — restart this outer step
                }
            }
        }

        // Full fielder-setup relay (batting authority → bowling follower): the chase RADIUS each fielder runs
        // to (num9 above) is derived from a per-client random firstBounceDistance/horizontalVelocity, so the
        // follower's local SetActiveFielders sends chasers to the wrong distance ("idhar udhar nikal jata"),
        // especially on the RPC_SyncBallShot adopt path (no authoritative distance there). Relay the FINAL
        // active indices + actions + chase points so the follower applies them directly with NO local
        // recompute (a superset of the catch-point relay). Gated to the batting authority.
        if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
            && CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex
            && CricketNetworkManager.instance != null && activeFielders.Count > 0)
        {
            int n = activeFielders.Count;
            int[] idx = new int[n];
            byte[] act = new byte[n];
            Vector3[] pts = new Vector3[n];
            for (int a = 0; a < n; a++)
            {
                idx[a] = activeFielders[a];
                act[a] = (a < activeFieldersActions.Count && activeFieldersActions[a] == "goForCatch") ? (byte)1 : (byte)0;
                pts[a] = (fielderChasePoints[activeFielders[a]] != null)
                    ? fielderChasePoints[activeFielders[a]].transform.position : Vector3.zero;
            }
            CricketNetworkManager.instance.CmdSetActiveFielderSetup(idx, act, pts);
        }
    }

    // Applies the batting authority's chosen catcher on the bowling follower: marks that fielder as the
    // catcher (goForCatch) and demotes any other fielder our local prediction wrongly chose to chase.
    private void ApplyAuthoritativeCatchFielder()
    {
        if (_authoritativeCatchFielder < 0 || fielders == null
            || _authoritativeCatchFielder >= fielders.Length) return;
        targetFielderForCatch = fielders[_authoritativeCatchFielder];
        bool catcherInList = false;
        for (int k = 0; k < activeFielders.Count; k++)
        {
            if (activeFielders[k] == _authoritativeCatchFielder)
            {
                catcherInList = true;
                if (k < activeFieldersActions.Count) activeFieldersActions[k] = "goForCatch";
                PlayRunOnAdoptedFielder(activeFielders[k]); // animation-only: adopted catcher must run, not slide
            }
            else if (k < activeFieldersActions.Count && activeFieldersActions[k] == "goForCatch")
            {
                // a different fielder was locally (wrongly) chosen as catcher — send it to chase instead
                activeFieldersActions[k] = "goForChase";
                PlayRunOnAdoptedFielder(activeFielders[k]); // animation-only: demoted-to-chase fielder must run, not slide
            }
        }
        if (!catcherInList)
        {
            activeFielders.Add(_authoritativeCatchFielder);
            activeFieldersActions.Add("goForCatch");
            PlayRunOnAdoptedFielder(_authoritativeCatchFielder); // animation-only: appended catcher must run, not slide
        }
    }

    // Catch-decision sync receiver (bowling follower only). The batting authority sends the index of the
    // fielder that catches a lofted shot; adopt it so our fielders don't diverge (one screen catches,
    // the other chases to the boundary). Stored so a later SetActiveFielders re-applies it too.
    public void RPC_SetCatchFielder(int fielderIndex, Vector3 catchPoint)
    {
        if (CONTROLLER.PlayModeSelected != 8) return;
        if (CONTROLLER.myTeamIndex != CONTROLLER.BowlingTeamIndex) return; // batting authority ignores its own send
        // Lockstep: this side computes the IDENTICAL catch setup locally at its own (delayed) resolve — the
        // batter's exact contact values are applied first (value relay), so this real-time relay is redundant
        // AND early: it started the catcher ~0.45s ahead of this side's delayed ball (13-07 logs: fielder
        // stopped a ball the batter saw reach the rope). The confirm relay (OnConfirmedOutfieldCatch) still
        // covers genuine divergence tails.
        if (LockstepActive)
        {
            ConstantsData_M.MpLog("[Lockstep] CatchSync relay ignored — local resolve owns the fielder setup.");
            return;
        }
        // Stale-relay guard (reconnect phantom catch, tester #4): a catch relay for a CANCELLED ball (the
        // opponent disconnected mid-flight; the server delivers the buffered Cmd to this rebuilt connection
        // AFTER the restore) must not latch catch state with no ball in flight — it replayed a full catch-out
        // scene on the NEXT ball. Only accept while a delivery is actually live.
        if (!isBallReleased || isReplayModeActive) return;
        if (fielders == null || fielderIndex < 0 || fielderIndex >= fielders.Length) return;
        _authoritativeCatchFielder = fielderIndex;
        // Adopt the batting authority's EXACT catch point so the catcher runs to the SAME spot (the local
        // point is derived from a per-client random firstBounceDistance and diverged). Lock it so the local
        // FixBallCatchingSpot can't overwrite it (see the guard there). Reset per delivery in ResetAll.
        if (catchPoint != Vector3.zero && ballCatchingPointTransform != null)
        {
            ballCatchingPointTransform.position = new Vector3(catchPoint.x, ballCatchingPointTransform.position.y, catchPoint.z);
            _hasAuthoritativeCatchPoint = true;
        }
        ApplyAuthoritativeCatchFielder();
        ConstantsData_M.MpLog($"[CatchSync] Bowling follower adopted authoritative catcher = fielder {fielderIndex}, catchPoint={catchPoint}.");
    }

    // Lockstep follower: the batting authority's relayed fielder setup, held until THIS side resolves its
    // own (delayed) contact. See RPC_SetActiveFielderSetup for why it is stashed rather than applied live.
    private int[] _stashedFielderIndices;
    private byte[] _stashedFielderActions;
    private Vector3[] _stashedFielderChasePoints;
    private bool _hasStashedFielderSetup;

    /// <summary>
    /// Follower only: replace the locally computed chase points with the batting authority's relayed ones.
    /// Called right after SetActiveFielders() inside the lockstep resolve, so the chase STARTS on this
    /// side's own timeline (no 0.45s-early interception) while the destinations match the batter's exactly.
    /// The local pass already decided who chases; this only corrects WHERE they run, which is the part that
    /// diverged (per-client friction/power state feeding the chase-radius clamp).
    /// </summary>
    private void ApplyStashedFielderSetupIfAny()
    {
        if (!LockstepActive || !_hasStashedFielderSetup) return;
        if (CONTROLLER.PlayModeSelected != 8) return;
        if (CONTROLLER.myTeamIndex != CONTROLLER.BowlingTeamIndex) return;
        if (_stashedFielderIndices == null || _stashedFielderChasePoints == null
            || _stashedFielderIndices.Length != _stashedFielderChasePoints.Length) return;
        if (fielders == null || fielderChasePoints == null) return;

        activeFielders.Clear();
        activeFieldersActions.Clear();
        for (int a = 0; a < _stashedFielderIndices.Length; a++)
        {
            int fi = _stashedFielderIndices[a];
            if (fi < 1 || fi > numberOfFielders || fi >= fielderChasePoints.Length || fielderChasePoints[fi] == null) continue;
            GameObject cp = fielderChasePoints[fi];
            cp.transform.position = new Vector3(_stashedFielderChasePoints[a].x, cp.transform.position.y, _stashedFielderChasePoints[a].z);
            activeFielders.Add(fi);
            bool isCatch = (_stashedFielderActions != null && a < _stashedFielderActions.Length && _stashedFielderActions[a] == 1);
            activeFieldersActions.Add(isCatch ? "goForCatch" : "goForChase");
        }
        ConstantsData_M.MpLog($"[Lockstep] Applied the batter's relayed fielder setup ({activeFielders.Count} fielders) — chase points now match the authority.");
        _hasStashedFielderSetup = false;
    }

    // Full fielder-setup receiver (bowling follower). The batting authority relays its FINAL active-fielder
    // indices + actions (0=goForChase, 1=goForCatch) + each chase point, so the follower's chasers run to the
    // SAME spots instead of recomputing a divergent chase radius from per-client random firstBounceDistance/
    // horizontalVelocity. Applies directly (no local recompute) + locks SetActiveFielders. Reset in ResetAll.
    public void RPC_SetActiveFielderSetup(int[] indices, byte[] actions, Vector3[] chasePoints)
    {
        if (CONTROLLER.PlayModeSelected != 8) return;
        if (CONTROLLER.myTeamIndex != CONTROLLER.BowlingTeamIndex) return; // batting authority ignores its own send
        // Lockstep: identical setup is computed locally at this side's own (delayed) resolve from the relayed
        // exact contact values — adopting the batter's REAL-TIME setup started the chase ~0.45s early relative
        // to this side's delayed ball (fielders intercepted balls that were 4s on the batter's screen).
        if (LockstepActive)
        {
            // Do NOT adopt the batter's setup right now — its chase would start ~0.45s early against this
            // side's delayed ball (that was the original reason this relay was dropped). But dropping it
            // entirely let the two screens compute DIFFERENT chase points from identical contact values:
            // 26-07 ball 0.1 resolved with byte-identical contact numbers on both clients, yet the batter
            // parked its chasers at z=-35.2 (friction clamp 41.8) while the follower sent f7 to z=-55.4
            // (clamp 62.0) — i.e. the follower's clamp used the POWER friction constant. The ball then
            // ended up in a fielder's hands on one screen and past him on the other. Stash the authority's
            // points and apply them at OUR OWN resolve instead: authoritative positions, local timing.
            _stashedFielderIndices = indices;
            _stashedFielderActions = actions;
            _stashedFielderChasePoints = chasePoints;
            _hasStashedFielderSetup = true;
            ConstantsData_M.MpLog($"[Lockstep] FielderSetupSync relay STASHED ({(indices != null ? indices.Length : 0)} fielders) — applies at this side's own resolve.");
            return;
        }
        // Stale-relay guard (reconnect phantom catch, tester #4): a catch relay for a CANCELLED ball (the
        // opponent disconnected mid-flight; the server delivers the buffered Cmd to this rebuilt connection
        // AFTER the restore) must not latch catch state with no ball in flight — it replayed a full catch-out
        // scene on the NEXT ball. Only accept while a delivery is actually live.
        if (!isBallReleased || isReplayModeActive) return;
        if (indices == null || chasePoints == null || indices.Length != chasePoints.Length) return;
        if (fielders == null || fielderChasePoints == null) return;
        activeFielders.Clear();
        activeFieldersActions.Clear();
        for (int a = 0; a < indices.Length; a++)
        {
            int fi = indices[a];
            if (fi < 1 || fi > numberOfFielders || fielderChasePoints[fi] == null) continue;
            GameObject cp = fielderChasePoints[fi];
            cp.transform.position = new Vector3(chasePoints[a].x, cp.transform.position.y, chasePoints[a].z);
            activeFielders.Add(fi);
            bool isCatch = (actions != null && a < actions.Length && actions[a] == 1);
            activeFieldersActions.Add(isCatch ? "goForCatch" : "goForChase");
            if (isCatch && fi < fielders.Length) targetFielderForCatch = fielders[fi];
            PlayRunOnAdoptedFielder(fi); // animation-only: adopted chaser/catcher must run, not slide
        }
        _hasAuthoritativeFielderSetup = true;
        ConstantsData_M.MpLog($"[FielderSetupSync] Bowling follower adopted authoritative fielder setup, count={indices.Length}.");
    }

    // Animation-only fix (bowling follower): fielders adopted via the relays (RPC_SetActiveFielderSetup /
    // ApplyAuthoritativeCatchFielder) get their activeFielders/Actions populated directly but never start the
    // looping "run" clip that SetActiveFielders plays (~4129-4142), so the per-frame chase loop translates them
    // while still in the "idle" pose (~3992) => they slide. Start the SAME run clip here, mirroring that gating.
    // Writes NO ball position (temporaryPosition/matchBallTransform untouched) and only animates — it does NOT
    // overwrite a fielder already mid-dive/catch (IsFielderPlayingAnAnimation), so a synced catcher's catch pose
    // is preserved. Randomised ["run"].time desyncs the loops without affecting determinism (visual only).
    private void PlayRunOnAdoptedFielder(int fi)
    {
        if (CONTROLLER.PlayModeSelected != 8 || GameConstants.isWithAI) return; // MP-only, mirrors relay gating
        if (fielders == null || fieldersAnim == null) return;
        if (fi < 1 || fi > numberOfFielders || fi >= fieldersAnim.Length) return;
        if (fielders[fi] == null || fieldersAnim[fi] == null || ballTimingStartGO == null) return;
        if (IsFielderPlayingAnAnimation(fi)) return; // never stomp an in-progress dive/catch pose
        if (DistanceBetweenTwoGameObjects(fielders[fi], ballTimingStartGO) > 8f) // same gate as SetActiveFielders
        {
            fieldersAnim[fi].Play("run");
            fieldersAnim[fi]["run"].speed = 1f;
            float length = fieldersAnim[fi]["run"].length;
            fieldersAnim[fi]["run"].time = UnityEngine.Random.Range(0f, length); // desync loops (visual only)
        }
    }

    public void DelayMakeFieldersToCelebrate()
    {
        makeFieldersToCelebrate(null);
    }

    public void makeFieldersToCelebrate(GameObject fielderToAvoid)
    {
        if (isRunOutSaved)
        {
            if (throwTargetSaved == "Fielder10")
            {
                keeperAnim.Play("appealFast");
            }
            else if (throwTargetSaved == "WicketKeeper")
            {
                fielder10Anim.Play("appeal");
            }
        }
        for (int i = 1; i <= numberOfFielders; i++)
        {
            if (fielders[i] != fielderToAvoid && (isOversteppedDelivery || !isLineFreeHitActive))
            {
                fieldersAnim[i].Play("appeal");
                fieldersAnim[i]["appeal"].speed = 1.3f + UnityEngine.Random.Range(0f, 1f);
                if (currentBallStatus != "bowled" && !hasLBWAppeal)
                {
                    Vector3 position = matchBallTransform.position;
                    position = new Vector3(position.x, 0f, position.z);
                    fielderTransforms[i].LookAt(position);
                }
            }
        }
    }

    [Skip]
    private void EnableFielder10ToCollectBall()
    {
        // The bowler's walk to the collect spot is an iTween, and THIS is its completion callback. The
        // decision that starts it is already known to be identical on both clients (BowlerCollectDiag: same
        // angles, same gate, 21/21), so the divergence behind "ek side bowler batsman ke paas chala gaya, or
        // doosri side apni jagah pe raha" has to be here or later. If this line appears on ONE side only, the
        // tween never completed there — it was stopped or replaced — and that is the bug; if it appears on
        // both with the same position, the move is fine and the difference is purely visual timing.
        if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false)
            ConstantsData_M.MpLog($"[BowlerCollectDiag] Move COMPLETED — bowler at {(fielder10Object != null ? fielder10Object.transform.position.ToString("F2") : "<null>")} fielderAction '{fielderAction}' -> 'waitForBall' amBatting={(CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex)}.");
        fielder10Anim.Play("idle");
        fielder10SkinTransform.LookAt(fielderFocusObjectToCollectBall.transform);
        fielderAction = "waitForBall";
    }

    private bool deactivateSlipFielders(GameObject fielderGO)
    {
        for (int i = 0; i < slipFielders.Count; i++)
        {
            if (fielderGO == slipFielders[i])
            {
                return true;
            }
        }
        return false;
    }

    private void ActivateFielders()
    {
        // Bowling-follower forced-catch poll: the batting authority confirmed a clean outfield catch this delivery
        // (OnConfirmedOutfieldCatch). Force our local ball to resolve as that catch the instant it would diverge
        // into a ground-field/throw-back. ForceFollowerCatchVisualOnDivergence self-gates (outfield catch only,
        // only when genuinely diverged, no-op once resolved), so good local catches are never pre-empted.
        if (_forceCatchConfirmed && outcomeOfBall != "wicket")
        {
            ForceFollowerCatchVisualOnDivergence();
        }

        int num = 0;

        if (CONTROLLER.BowlingTeamIndex == CONTROLLER.myTeamIndex || CONTROLLER.PlayModeSelected == 8)
        {
            num = 1;
        }
        Vector3 vector = default(Vector3);
        int num2 = 180;
        if (activeFielders.Count <= 0 && !isPowerShotActive)
        {
            velocityDampingFactor = 10f;
        }
        for (int i = 0; i < activeFielders.Count; i++)
        {
            GameObject gameObject = fielders[activeFielders[i]];
            Animation animation = fieldersAnim[activeFielders[i]];
            GameObject gameObject2 = fielderModels[activeFielders[i]];
            GameObject gameObject3 = fielderReferences[activeFielders[i]];
            GameObject gameObject4 = fielderBalls[activeFielders[i]];
            GameObject gameObject5 = fielderChasePoints[activeFielders[i]];
            int num3 = activeFielders[i];
            // The de-dup below exists to stop two men converging on a MOVING ball — one takes it, the other
            // stands by. Once the ball is AT REST that reasoning does not apply, and leaving it on creates a
            // ping-pong with the stand-by resume: resume sets goForChase, this demotes it again next frame,
            // and the fielder oscillates without ever reaching the ball. The 04-08 local run shows it —
            // "[Fielding] f6 resuming the chase" seven times in a row, then the 20s watchdog. Skip it for a
            // stationary ball and let whoever is nearest actually go and collect it.
            bool _ballAtRest = horizontalVelocity <= 0.01f;
            if (!_ballAtRest && activeFielders.Count > 1 && (activeFieldersActions[i] == "goForCatch" || activeFieldersActions[i] == "goForChase" || activeFieldersActions[i] == "waitToCatch"))
            {
                ////ConstantsData_M.MpLog("Picked");

                for (int j = 0; j < activeFielders.Count - 1; j++)
                {
                    GameObject gameObject6 = fielders[activeFielders[j]];
                    for (int k = j + 1; k < activeFielders.Count; k++)
                    {
                        GameObject gameObject7 = fielders[activeFielders[k]];
                        if (!(DistanceBetweenTwoVector2(gameObject6, gameObject7) < 3f))
                        {
                            continue;
                        }
                        if (DistanceBetweenTwoGameObjects(gameObject6, matchBall) < DistanceBetweenTwoGameObjects(gameObject7, matchBall))
                        {
                            if (activeFieldersActions[k] == "goForCatch" || activeFieldersActions[k] == "goForChase")
                            {
                                activeFieldersActions[k] = "waitAndSeeTheCatch";
                                gameObject7.GetComponent<Animation>().Play("idle");
                                gameObject = gameObject6;
                            }
                        }
                        else if (activeFieldersActions[j] == "goForCatch" || activeFieldersActions[j] == "goForChase")
                        {
                            activeFieldersActions[j] = "waitAndSeeTheCatch";
                            gameObject6.GetComponent<Animation>().Play("idle");
                            gameObject = gameObject7;
                        }
                    }
                }
            }
            if (activeFieldersActions[i] == "goForCatch")
            {
                ////ConstantsData_M.MpLog("Picked");

                if ((isBallOnBoundaryLine || shouldStopFielders) && outcomeOfBall != "wicket")
                {
                    if (!animation.IsPlaying("runComplete") && DistanceBetweenTwoGameObjects(gameObject, ballTimingStartGO) > 8f)
                    {
                        animation.Play("runComplete");
                        defaultFielderSpeed -= defaultFielderSpeed * Time.deltaTime * 0.5f;
                        float num4 = AngleBetweenTwoGameObjects(gameObject, matchBall);
                        fielderTransforms[activeFielders[i]].transform.position += new Vector3(Mathf.Cos(num4 * degToRad) * defaultFielderSpeed * Time.deltaTime, 0f, Mathf.Sin(num4 * degToRad) * defaultFielderSpeed * Time.deltaTime);
                        ClampFielderInsidePlayingArea(fielderTransforms[activeFielders[i]]);
                    }
                    if (isBallOnBoundaryLine && DistanceBetweenTwoGameObjects(gameObject, groundCenterMarker) > 8f)
                    {
                        defaultFielderSpeed -= defaultFielderSpeed * Time.deltaTime * 0.5f;
                        float num4 = AngleBetweenTwoGameObjects(gameObject, matchBall);
                        fielderTransforms[activeFielders[i]].position += new Vector3(Mathf.Cos(num4 * degToRad) * defaultFielderSpeed * Time.deltaTime, 0f, Mathf.Sin(num4 * degToRad) * defaultFielderSpeed * Time.deltaTime);
                        ClampFielderInsidePlayingArea(fielderTransforms[activeFielders[i]]);
                        activeFieldersActions[i] = "stopChasing";
                    }
                    if (shouldStopFielders)
                    {
                        animation.Play("runComplete");
                        defaultFielderSpeed -= defaultFielderSpeed * Time.deltaTime * 0.5f;
                        float num4 = AngleBetweenTwoGameObjects(gameObject, matchBall);
                        fielderTransforms[activeFielders[i]].position += new Vector3(Mathf.Cos(num4 * degToRad) * defaultFielderSpeed * Time.deltaTime, 0f, Mathf.Sin(num4 * degToRad) * defaultFielderSpeed * Time.deltaTime);
                        ClampFielderInsidePlayingArea(fielderTransforms[activeFielders[i]]);
                        //fielderTransform[activeFielderNumber[i]].LookAt(new Vector3(ballTransform.position.x, 0f, ballTransform.position.z));
                        fielderTransforms[activeFielders[i]].LookAt(new Vector3(temporaryPosition.x, 0f, temporaryPosition.z));
                    }
                }
                if (DistanceBetweenTwoGameObjects(ballCatchingPoint, ballFirstBounce) < 3f && hasTopEdge)
                {
                    ballCatchingPointTransform.position = new Vector3(ballFirstBounce.transform.position.x, ballCatchingPointTransform.position.y, ballFirstBounce.transform.position.z);
                }
                if (DistanceBetweenTwoGameObjects(gameObject, ballTimingStartGO) < 8f)
                {
                    vector = fielderTransforms[activeFielders[i]].InverseTransformPoint(ballStartPoint.position);
                    num2 = 180;
                    if (vector.x > 0f)
                    {
                        num2 = -180;
                    }
                    if (!isFielderNearPitch[num3])
                    {
                        isFielderNearPitch[num3] = true;
                        iTween.RotateTo(gameObject, iTween.Hash("y", fielderTransforms[activeFielders[i]].eulerAngles.y + (float)num2, "time", 0.4, "oncomplete", "stopITween", "oncompletetarget", base.gameObject, "oncompleteparams", gameObject));
                    }
                }
                else if ((double)DistanceBetweenTwoGameObjects(gameObject, ballCatchingPoint) > 0.6)
                {
                    if (targetFielderForCatch == gameObject || (targetFielderForCatch != gameObject && DistanceBetweenTwoGameObjects(gameObject, ballCatchingPoint) > 3f))
                    {
                        float num4 = AngleBetweenTwoGameObjects(gameObject, ballCatchingPoint);
                        fielderTransforms[activeFielders[i]].position += new Vector3(Mathf.Cos(num4 * degToRad) * defaultFielderSpeed * Time.deltaTime, 0f, Mathf.Sin(num4 * degToRad) * defaultFielderSpeed * Time.deltaTime);
                        ClampFielderInsidePlayingArea(fielderTransforms[activeFielders[i]]);
                        fielderTransforms[activeFielders[i]].LookAt(ballCatchingPointTransform);
                    }
                    else
                    {
                        activeFieldersActions[i] = "waitAndSeeTheCatch";
                        fielderTransforms[activeFielders[i]].LookAt(ballCatchingPointTransform);
                        animation.Play("idle");
                    }
                }
                else
                {
                    activeFieldersActions[i] = "waitToCatch";
                    fielderTransforms[activeFielders[i]].position = new Vector3(ballCatchingPointTransform.position.x, fielderTransforms[activeFielders[i]].position.y, ballCatchingPointTransform.position.z);
                    float y = fielderTransforms[activeFielders[i]].eulerAngles.y;
                    fielderTransforms[activeFielders[i]].LookAt(ballStartPoint);
                    float y2 = fielderTransforms[activeFielders[i]].eulerAngles.y;
                    fielderTransforms[activeFielders[i]].eulerAngles = new Vector3(fielderTransforms[activeFielders[i]].eulerAngles.x, y, fielderTransforms[activeFielders[i]].eulerAngles.z);
                    iTween.RotateTo(gameObject, iTween.Hash("y", y2, "time", 0.1));
                    animation.Play("idle");
                }
            }
            else if (activeFieldersActions[i] == "waitToCatch")
            {
                ////ConstantsData_M.MpLog("Picked");

                float num5 = 4f;
                float num6 = num5 * animationFrameInterval * horizontalVelocity;
                if (DistanceBetweenTwoVector2(gameObject, matchBall) < num6)
                {
                    float num7 = preCatchDistance / horizontalVelocity;
                    float num8 = num7 * angleChangeRate;
                    float num9 = 360f - num8;
                    float num10 = Mathf.Abs(arcHeight * Mathf.Sin(num9 * degToRad));
                    if (num10 < 0.2f)
                    {
                        animation.Play("lowCatch");
                    }
                    else if (num10 < 1.33f)
                    {
                        animation.Play("hipCatch");
                        animation["hipCatch"].speed = 2.5f;
                    }
                    else if (num10 < 2f)
                    {
                        animation.Play("sideCatch");
                    }
                    else if (num10 < 2.5f)
                    {
                        animation.Play("highCatch");
                    }
                    if (Singleton<GameData>.instance != null && !isReplayModeActive)
                    {
                        Singleton<GameData>.instance.PlayGameSound("Cheer");
                    }
                    activeFieldersActions[i] = "catchAttempt";
                    shouldStopFielders = true;
                    defaultFielderSpeed = 0f;
                }
            }
            else if (activeFieldersActions[i] == "catchAttempt")
            {
                ////ConstantsData_M.MpLog("Picked");

                if (DistanceBetweenTwoVector2(gameObject, matchBall) < 0.5f || DistanceBetweenTwoVector2(groundCenterMarker, matchBall) > DistanceBetweenTwoVector2(groundCenterMarker, gameObject))
                {
                    if (!isReplayModeActive)
                    {
                        restrictReplayCameraHeight = true;
                    }
                    //if ((ballTransform.position.y < 2.5f && !replayMode) || (savedSummary == "catch" && replayMode) || (savedSummary == "picked" && savedSummary == "picked" && isFreeHit) || (savedSummary == "picked" && (overStepBall || lineFreeHit || !CONTROLLER.isLineFreeHitBallCompleted)))
                    if ((temporaryPosition.y < 2.5f && !isReplayModeActive) || (summarySaved == "catch" && isReplayModeActive) || (summarySaved == "picked" && summarySaved == "picked" && isFreeHitActive) || (summarySaved == "picked" && (isOversteppedDelivery || isLineFreeHitActive || !CONTROLLER.isLineFreeHitBallCompleted)))
                    {
                        ShowBall(status: false);
                        gameObject4.GetComponent<Renderer>().enabled = true;
                        if (isOversteppedDelivery || isLineFreeHitActive || !CONTROLLER.isLineFreeHitBallCompleted)
                        {
                            animation.Play("throw");
                            if (isEnhancedModeActive)
                            {
                                animation["throw"].speed = 7f;
                            }
                            else
                            {
                                animation["throw"].speed = 1f;
                            }
                            if (isLineFreeHitActive)
                            {
                                isLineFreeHitActive = false;
                            }
                            //ConstantsData_M.MpLog("!!!=>>>" + overStepBall + "+ " + lineFreeHit + "+ " + CONTROLLER.isLineFreeHitBallCompleted);
                            activeFieldersActions[i] = "pickedup";
                            pickupAnimationToPlaySaved = "highCatch";
                            animation.CrossFade(pickupAnimationToPlaySaved, 0.3f);
                        }
                        else if (noBall || freeHit || bounceCount >= 1)
                        {
                            animation.Play("throw");
                            animation["throw"].speed = 7f / (7f * (1f - controlMultiplier * (float)num));

                            activeFieldersActions[i] = "pickedup";
                            pickupAnimationToPlaySaved = "highCatch";
                            animation.CrossFade(pickupAnimationToPlaySaved, 0.3f);
                            if (!noBall)
                            {
                                ////ConstantsData_M.MpLog("!!!=>>>");

                                isJokerFreeHitActive = true;
                            }
                        }
                        else if (isJokerFreeHitActive && isReplayModeActive && summarySaved == "picked")
                        {
                            animation.Play("throw");
                            animation["throw"].speed = 7f / (7f * (1f - controlMultiplier * (float)num));

                            activeFieldersActions[i] = "pickedup";
                            if (!noBall)
                            {
                                ////ConstantsData_M.MpLog("!!!=>>>");

                                isJokerFreeHitActive = false;
                            }
                        }
                        else
                        {
                            //ConstantsData_M.MpLog("!!!=>>>");

                            int num11;
                            if (!isReplayModeActive)
                            {
                                summarySaved = "catch";
                                num11 = (celebrationAnimationIndexSaved = UnityEngine.Random.Range(0, 3));
                            }
                            else
                            {
                                num11 = celebrationAnimationIndexSaved;
                            }
                            switch (num11)
                            {
                                case 0:
                                    animation.PlayQueued("celebration", QueueMode.CompleteOthers);
                                    break;
                                case 1:
                                    animation.PlayQueued("celebration2", QueueMode.CompleteOthers);
                                    break;
                                default:
                                    animation.PlayQueued("appeal", QueueMode.CompleteOthers);
                                    break;
                            }
                            animation.PlayQueued("celebrationRun", QueueMode.CompleteOthers);
                            fielderAction = string.Empty;
                            fielder10Anim.Play("appeal");
                            activeFieldersActions[i] = "catched";
                            outcomeOfBall = "wicket";
                            makeFieldersToCelebrate(gameObject);
                            // Confirmed-catch relay (batting authority → bowling follower): a CLEAN outfield catch
                            // just completed. Tell the follower to resolve its (possibly-diverged) local ball as a
                            // CATCH now, instead of waiting for the slower RpcBallOutcome (by then the follower's
                            // ball already fell + the camera chased the throw-back = the bug). Sent once per
                            // delivery and only here (a real catch), so no dropped-catch false positive.
                            if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
                                && CONTROLLER.myTeamIndex == CONTROLLER.BattingTeamIndex
                                && CricketNetworkManager.instance != null && !_sentConfirmCatch
                                && activeFielders != null && i < activeFielders.Count)
                            {
                                _sentConfirmCatch = true;
                                CricketNetworkManager.instance.CmdConfirmOutfieldCatch(activeFielders[i]);
                            }
                        }
                        isBallPaused = true;
                        if (!isOversteppedDelivery && !isLineFreeHitActive && CONTROLLER.isLineFreeHitBallCompleted)
                        {
                            canRun = false;
                            disableRunCancelBtn();
                        }
                        else
                        {
                            canRun = true;
                        }
                    }
                    else
                    {
                        //if (ballTransform.position.y < 4f)
                        if (temporaryPosition.y < 4f)
                        {
                            animation.Play("highCatch");
                            animation["highCatch"].speed = 2f;
                        }
                        iTween.RotateTo(gameObject, iTween.Hash("y", fielderTransforms[activeFielders[i]].eulerAngles.y + 135f, "time", 2));
                        activeFieldersActions[i] = "waitingTooHighBall";
                        if (hasTopEdge)
                        {
                            activeFieldersActions[i] = "goForChase";
                            animation.Play("run");
                            iTween.Stop();
                            isBallOnBoundaryLine = false;
                            shouldStopFielders = false;
                        }
                    }
                }
            }
            else if (activeFieldersActions[i] == "catched")
            {
                ////ConstantsData_M.MpLog("Picked");    

                if (animation.IsPlaying("celebrationRun"))
                {
                    _stayStartTime = Time.time;
                    activeFieldersActions[i] = "celebrationRun";
                }
            }
            else if (activeFieldersActions[i] == "celebrationRun")
            {
                ////ConstantsData_M.MpLog("Picked");

                if (isReplayModeActive)
                {
                    ResetFielders();
                    activeFieldersActions[i] = string.Empty;
                    HideReplay();
                    break;
                }
                float num4 = AngleBetweenTwoGameObjects(gameObject, ballTimingStartGO);
                fielderTransforms[activeFielders[i]].position += new Vector3(Mathf.Cos(num4 * degToRad) * defaultFielderSpeed / 1.5f * Time.deltaTime, 0f, Mathf.Sin(num4 * degToRad) * defaultFielderSpeed / 1.5f * Time.deltaTime);
                ClampFielderInsidePlayingArea(fielderTransforms[activeFielders[i]]);
                if (_stayStartTime - 0.5f + delayBetweenDeliveries < Time.time)
                {
                    ResetFielders();
                    _stayStartTime = Time.time;
                    activeFieldersActions[i] = "waitForResult";
                    _mainUmpireTransform.position = mainUmpireInitialPosition;
                    _mainUmpireTransform.eulerAngles = new Vector3(_mainUmpireTransform.eulerAngles.x, 0f, _mainUmpireTransform.eulerAngles.z);
                    //mainUmpireTransform.localScale = new Vector3(1f, mainUmpireTransform.localScale.y, mainUmpireTransform.localScale.z);
                    rightFieldCamera.enabled = false;
                    leftFieldCamera.enabled = false;
                    gameplayCamera.enabled = false;
                    showPreviewCamera(status: false);
                    umpireViewCamera.enabled = true;
                    umpireCamTransform.position = mainUmpireInitialPosition;
                    umpireCamTransform.position = new Vector3(umpireCamTransform.position.x, 2f, umpireCamTransform.position.z);
                    umpireCamTransform.position += new Vector3(0f, 0f, 3f);
                    umpireCamTransform.eulerAngles = new Vector3(umpireCamTransform.eulerAngles.x, 180f, umpireCamTransform.eulerAngles.z);
                    umpireCamTransform.eulerAngles = new Vector3(5f, umpireCamTransform.eulerAngles.y, umpireCamTransform.eulerAngles.z);
                    iTween.MoveTo(umpireViewCamera.gameObject, iTween.Hash("position", new Vector3(mainUmpireInitialPosition.x - UnityEngine.Random.Range(6f, 10f), UnityEngine.Random.Range(1.4f, 4f), UnityEngine.Random.Range(-6f, -8f)), "time", 4.5));
                    iTween.RotateTo(umpireViewCamera.gameObject, iTween.Hash("y", 140, "time", 4.5));
                    if (noBall || freeHit)
                    {
                        mainUmpireAnim.Play("NotOut");
                    }
                    if (!noBall && !freeHit)
                    {
                        mainUmpireAnim.Play("Out");
                    }
                    if (Singleton<GameData>.instance != null && !isReplayModeActive)
                    {
                        Singleton<GameData>.instance.PlayGameSound("Cheer");
                    }
                }
            }
            else if (activeFieldersActions[i] == "waitForResult")
            {
                ////ConstantsData_M.MpLog("Picked");

                if (_stayStartTime + 2f + delayBetweenDeliveries < Time.time)
                {
                    activeFieldersActions[i] = string.Empty;
                    if (Singleton<GameData>.instance != null && !isReplayModeActive)
                    {
                        if (noBall)
                        {
                            CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
                            Singleton<GameData>.instance.UpdateCurrentBall(0, 1, 0, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, activeFielders[i], CONTROLLER.StrikerIndex, isBoundary: false);
                            CONTROLLER.isJokerCall = false;
                            if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
                            {
                            }
                        }
                        else if (freeHit)
                        {
                            Singleton<GameData>.instance.UpdateCurrentBall(1, 1, 0, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, activeFielders[i], CONTROLLER.StrikerIndex, isBoundary: false);
                            freeHit = false;
                        }
                        else if (!noBall && !freeHit)
                        {
                            Singleton<GameData>.instance.UpdateCurrentBall(1, 1, 0, 0, CONTROLLER.StrikerIndex, 1, 3, CONTROLLER.CurrentBowlerIndex, activeFielders[i], CONTROLLER.StrikerIndex, isBoundary: false);
                        }
                    }
                }
            }
            else if (activeFieldersActions[i] == "BadCallCaughtResult")
            {
                ////ConstantsData_M.MpLog("Picked");

                if (_stayStartTime + 2f + delayBetweenDeliveries < Time.time)
                {
                    activeFieldersActions[i] = string.Empty;
                    if (Singleton<GameData>.instance != null && !isReplayModeActive)
                    {
                        Singleton<GameData>.instance.UpdateCurrentBall(1, 1, runsScoredThisBall, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                    }
                }
            }
            else if (activeFieldersActions[i] == "goForChase")
            {
                ////ConstantsData_M.MpLog("Picked");

                bool flag = false;
                if (isBallOnBoundaryLine || shouldStopFielders)
                {
                    if (outcomeOfBall != "wicket")
                    {
                        if (!animation.IsPlaying("runComplete") && DistanceBetweenTwoGameObjects(gameObject, ballTimingStartGO) > 8f)
                        {
                            animation.Play("runComplete");
                            defaultFielderSpeed -= defaultFielderSpeed * Time.deltaTime * 0.5f;
                            float num4 = AngleBetweenTwoGameObjects(gameObject, matchBall);
                            fielderTransforms[activeFielders[i]].position += new Vector3(Mathf.Cos(num4 * degToRad) * defaultFielderSpeed * Time.deltaTime, 0f, Mathf.Sin(num4 * degToRad) * defaultFielderSpeed * Time.deltaTime);
                            ClampFielderInsidePlayingArea(fielderTransforms[activeFielders[i]]);
                            activeFieldersActions[i] = "stopChasing";
                        }
                        if (isBallOnBoundaryLine && DistanceBetweenTwoGameObjects(gameObject, groundCenterMarker) > 8f)
                        {
                            defaultFielderSpeed -= defaultFielderSpeed * Time.deltaTime * 0.5f;
                            float num4 = AngleBetweenTwoGameObjects(gameObject, matchBall);
                            fielderTransforms[activeFielders[i]].position += new Vector3(Mathf.Cos(num4 * degToRad) * defaultFielderSpeed * Time.deltaTime, 0f, Mathf.Sin(num4 * degToRad) * defaultFielderSpeed * Time.deltaTime);
                            ClampFielderInsidePlayingArea(fielderTransforms[activeFielders[i]]);
                            activeFieldersActions[i] = "stopChasing";
                        }
                        if (shouldStopFielders)
                        {
                            animation.Play("runComplete");
                            defaultFielderSpeed -= defaultFielderSpeed * Time.deltaTime * 0.5f;
                            float num4 = AngleBetweenTwoGameObjects(gameObject, matchBall);
                            fielderTransforms[activeFielders[i]].position += new Vector3(Mathf.Cos(num4 * degToRad) * defaultFielderSpeed * Time.deltaTime, 0f, Mathf.Sin(num4 * degToRad) * defaultFielderSpeed * Time.deltaTime);
                            ClampFielderInsidePlayingArea(fielderTransforms[activeFielders[i]]);
                            //fielderTransform[activeFielderNumber[i]].LookAt(new Vector3(ballTransform.position.x, 0f, ballTransform.position.z));
                            fielderTransforms[activeFielders[i]].LookAt(new Vector3(temporaryPosition.x, 0f, temporaryPosition.z));
                        }
                        float time = animation["runComplete"].time;
                        float num12 = time * animationFramesPerSecond;
                        // 70f sat BEYOND the 68.5 rope, so this only conceded the chase after the fielder was already
                        // outside it. Stop at the rope instead; the clamp above keeps him there either way.
                        if (num12 > 50f || DistanceBetweenTwoVector2(groundCenterMarker, gameObject) >= playingAreaRadius)
                        {
                            activeFieldersActions[i] = "stopChasing";
                        }
                    }
                }
                else if (DistanceBetweenTwoGameObjects(gameObject, ballTimingStartGO) < 8f)
                {
                    vector = fielderTransforms[activeFielders[i]].InverseTransformPoint(ballStartPoint.position);
                    num2 = 180;
                    if (vector.x > 0f)
                    {
                        num2 = -180;
                    }
                    if (!isFielderNearPitch[num3])
                    {
                        isFielderNearPitch[num3] = true;
                        iTween.RotateTo(gameObject, iTween.Hash("y", fielderTransforms[activeFielders[i]].eulerAngles.y + (float)num2, "time", 0.4, "oncomplete", "stopITween", "oncompletetarget", base.gameObject, "oncompleteparams", gameObject));
                    }
                }
                else if (DistanceBetweenTwoGameObjects(ballTimingStartGO, matchBall) > DistanceBetweenTwoGameObjects(ballTimingStartGO, gameObject5))
                {
                    // This is the branch that goes after a ball which has passed the chase point — the one a
                    // resumed fielder needs. Floor the speed so a shared defaultFielderSpeed that some other
                    // path zeroed cannot silently freeze the pursuit; without this the fielder is "chasing"
                    // at 0 u/s and the ball is never collected.
                    float _chaseSpeed = Mathf.Max(defaultFielderSpeed, 3f);
                    float num4 = AngleBetweenTwoGameObjects(gameObject, matchBall);
                    fielderTransforms[activeFielders[i]].position += new Vector3(Mathf.Cos(num4 * degToRad) * _chaseSpeed * Time.deltaTime, 0f, Mathf.Sin(num4 * degToRad) * _chaseSpeed * Time.deltaTime);
                    ClampFielderInsidePlayingArea(fielderTransforms[activeFielders[i]]);
                    //fielderTransform[activeFielderNumber[i]].LookAt(new Vector3(ballTransform.position.x, 0f, ballTransform.position.z));
                    fielderTransforms[activeFielders[i]].LookAt(new Vector3(temporaryPosition.x, 0f, temporaryPosition.z));
                }
                else if (DistanceBetweenTwoGameObjects(gameObject, gameObject5) > 0.17f)
                {
                    float num13 = DistanceBetweenTwoGameObjects(gameObject, gameObject5);
                    float num14 = num13 / defaultFielderSpeed;
                    float num15 = DistanceBetweenTwoGameObjects(matchBall, gameObject5) / horizontalVelocity;
                    float num16 = DistanceBetweenTwoVector2(groundCenterMarker, gameObject);
                    if (targetFielderForCatch != gameObject && num13 <= 5f && num13 >= 2f && DistanceBetweenTwoGameObjects(ballTimingStartGO, matchBall) < DistanceBetweenTwoGameObjects(ballTimingStartGO, gameObject5) && num15 < num14 && matchBallTransform.position.y < 0.5f && num16 > playingAreaRadius - 5f)
                    {
                        activeFieldersActions[i] = "diveToField";
                        animation.Play("diveStraight");
                    }
                    else if (targetFielderForCatch != gameObject && DistanceBetweenTwoGameObjects(gameObject, ballCatchingPoint) > 3f)
                    {
                        float num17 = DistanceBetweenTwoGameObjects(gameObject, gameObject5);
                        float num4 = AngleBetweenTwoGameObjects(gameObject, gameObject5);
                        fielderTransforms[activeFielders[i]].position += new Vector3(Mathf.Cos(num4 * degToRad) * defaultFielderSpeed * Time.deltaTime, 0f, Mathf.Sin(num4 * degToRad) * defaultFielderSpeed * Time.deltaTime);
                        ClampFielderInsidePlayingArea(fielderTransforms[activeFielders[i]]);
                        fielderTransforms[activeFielders[i]].LookAt(gameObject5.transform);
                        float num18 = DistanceBetweenTwoGameObjects(gameObject, gameObject5);
                        if (num17 < num18)
                        {
                            fielderTransforms[activeFielders[i]].position = new Vector3(gameObject5.transform.position.x, fielderTransforms[activeFielders[i]].position.y, gameObject5.transform.position.z);
                        }
                    }
                    else
                    {
                        activeFieldersActions[i] = "waitAndSeeTheCatch";
                        animation.Play("idle");
                    }
                }
                else
                {
                    animation.Play("idle");
                    fielderTransforms[activeFielders[i]].LookAt(ballStartPoint);
                    activeFieldersActions[i] = "waitToPick";
                }
            }
            else if (activeFieldersActions[i] == "stopChasing")
            {
                ////ConstantsData_M.MpLog("Picked");

                if (defaultFielderSpeed > 1f)
                {
                    defaultFielderSpeed -= 2.8f * Time.deltaTime;
                    float num4 = AngleBetweenTwoGameObjects(gameObject, matchBall);
                    fielderTransforms[activeFielders[i]].position += new Vector3(Mathf.Cos(num4 * degToRad) * defaultFielderSpeed * Time.deltaTime, 0f, Mathf.Sin(num4 * degToRad) * defaultFielderSpeed * Time.deltaTime);
                    ClampFielderInsidePlayingArea(fielderTransforms[activeFielders[i]]);
                }
                if (defaultFielderSpeed <= 1f && !animation.IsPlaying("WCCLite_FielderIdle0"))
                {
                    defaultFielderSpeed = 0f;
                    animation.CrossFade("WCCLite_FielderIdle05", 0.1f);
                }
            }
            else if (activeFieldersActions[i] == "waitAndSeeTheCatch")
            {
                // "waitAndSeeTheCatch" means STAND BY, someone else is taking this. It is assigned in four
                // places in this file and was handled in NONE of them — so a fielder who entered it played
                // idle and never moved again for the rest of the delivery, whatever happened to the ball.
                //
                // That turns into a hung delivery on the straight drive. Both chasers get sent to the SAME
                // point (03-08 log: cp(-7.5,-43.9) with f5 at x=10.5 and f6 at x=-17.7, converging from
                // opposite sides), the de-dup above parks the farther one here the moment they come within 3u
                // of each other, and the ball then rolls to rest with nobody left to collect it. Nothing
                // resolves the ball, so nothing ends the delivery — the tester's "straight shot without loft,
                // fielders don't catch the ball, game stuck".
                //
                // Standing by is only correct while somebody IS handling it. Once the ball is at rest and
                // still uncollected, resume the chase. The walk-to-a-resting-ball path in waitToPick then
                // completes the pickup.
                if (outcomeOfBall != "wicket" && !isBallOnBoundaryLine && !shouldStopFielders
                    && horizontalVelocity <= 0.01f
                    && DistanceBetweenTwoVector2(gameObject, matchBall) > 1.5f)
                {
                    // Restore the run speed too. defaultFielderSpeed is ONE SHARED field for every fielder, and
                    // several paths in this file drive it to 0 during a delivery (the pickup attempt, and
                    // stopChasing's decay). Handing the fielder goForChase without it left him in the chase
                    // state at speed 0 — the 04-08 09:09 run shows exactly that: f6 and f7 each resume ONCE
                    // (the ping-pong is gone) and then neither moves for the full 20s until the watchdog.
                    RestoreFielderSpeedForDelivery();
                    ConstantsData_M.MpLog($"[Fielding] f{activeFielders[i]} resuming the chase — the ball came to rest uncollected while this fielder was standing by (speed restored to {defaultFielderSpeed:F1}).");
                    activeFieldersActions[i] = "goForChase";
                    animation.Play("run");
                }
            }
            else if (activeFieldersActions[i] == "diveToField")
            {
                ////ConstantsData_M.MpLog("Picked");

                float num19 = 15f;
                if (animation.IsPlaying("diveStraight") && animation["diveStraight"].time < num19 * animationFrameInterval)
                {
                    float num4 = AngleBetweenTwoGameObjects(gameObject, gameObject5);
                    fielderTransforms[activeFielders[i]].position += new Vector3(Mathf.Cos(num4 * degToRad) * defaultFielderSpeed / 1.5f * Time.deltaTime, 0f, Mathf.Sin(num4 * degToRad) * defaultFielderSpeed / 1.5f * Time.deltaTime);
                    ClampFielderInsidePlayingArea(fielderTransforms[activeFielders[i]]);
                }
                else
                {
                    activeFieldersActions[i] = "diveEnd";
                }
            }
            else if (activeFieldersActions[i] == "waitToPick")
            {
                ////ConstantsData_M.MpLog("Picked");

                if (_ballAngle > 90f && _ballAngle < 270f)
                {
                    if (Singleton<RightSmoothFov>.instance != null)
                    {
                        Singleton<RightSmoothFov>.instance.setCameraPosition(gameObject.transform.position, gameObject);
                    }
                }
                else if (Singleton<LeftFovLerp>.instance != null)
                {
                    Singleton<LeftFovLerp>.instance.setCameraPosition(gameObject.transform.position, gameObject);
                }
                if (CONTROLLER.cameraType == 0)
                {
                    if (_ballAngle > 90f && _ballAngle < 270f)
                    {
                        if (Singleton<RightSmoothFov>.instance != null)
                        {
                            Singleton<RightSmoothFov>.instance.setCameraPosition(gameObject.transform.position, gameObject);
                        }
                    }
                    else if (Singleton<LeftFovLerp>.instance != null)
                    {
                        Singleton<LeftFovLerp>.instance.setCameraPosition(gameObject.transform.position, gameObject);
                    }
                }
                if (shouldStopFielders)
                {
                    activeFieldersActions[i] = "stopChasing";
                }
                // The ball has come to REST short of this fielder. The pickup test below sizes its reach from
                // the BALL's speed (num21 = 7 * animationFrameInterval * horizontalVelocity), so a stationary
                // ball has a reach of ZERO and can only be collected by someone already within ~1u of it.
                // Waiting in idle for a ball that will never move again is the stall behind "dono players ny
                // nhi pkri or game stuck". Walk to it instead. Uses a floor on the speed because the pickup
                // path zeroes defaultFielderSpeed, and clamps to the rope like every other chase step.
                if (horizontalVelocity <= 0.01f && !shouldStopFielders && !isBallOnBoundaryLine
                    && DistanceBetweenTwoVector2(gameObject, matchBall) > 1.5f)
                {
                    if (!animation.IsPlaying("run")) animation.Play("run");
                    float _toBall = AngleBetweenTwoGameObjects(gameObject, matchBall);
                    float _walk = Mathf.Max(defaultFielderSpeed, 3f);
                    fielderTransforms[activeFielders[i]].position += new Vector3(Mathf.Cos(_toBall * degToRad) * _walk * Time.deltaTime, 0f, Mathf.Sin(_toBall * degToRad) * _walk * Time.deltaTime);
                    ClampFielderInsidePlayingArea(fielderTransforms[activeFielders[i]]);
                    fielderTransforms[activeFielders[i]].LookAt(new Vector3(temporaryPosition.x, 0f, temporaryPosition.z));
                }
                float num20 = 7f;
                // Floor the reach so a STOPPED ball is still collectable once the fielder is on top of it —
                // otherwise num21 is 0 and the test becomes "within exactly 1u", which the walk above can
                // overshoot between frames.
                float num21 = Mathf.Max(num20 * animationFrameInterval * horizontalVelocity, 0.35f);
                if (DistanceBetweenTwoVector2(gameObject, matchBall) - 1f < num21)
                {
                    float num22 = Mathf.Abs(distanceToNextPitch - DistanceBetweenTwoVector2(ballTimingStartGO, gameObject));
                    //if (ballTransform.position.y < 0.7f || num22 < 2f)
                    if (temporaryPosition.y < 0.7f || num22 < 2f)
                    {
                        pickupAnimationToPlaySaved = "lowCatch";
                    }
                    //else if (ballTransform.position.y < 1.33f)
                    else if (temporaryPosition.y < 1.33f)
                    {
                        pickupAnimationToPlaySaved = "hipCatch";
                    }
                    //else if (ballTransform.position.y < 2f)
                    else if (temporaryPosition.y < 2f)
                    {
                        pickupAnimationToPlaySaved = "sideCatch";
                    }
                    else
                    {
                        pickupAnimationToPlaySaved = "highCatch";
                    }
                    animation.Play(pickupAnimationToPlaySaved);
                    animation[pickupAnimationToPlaySaved].speed = 1f;
                    fielderTransforms[activeFielders[i]].LookAt(ballStartPoint);
                    activeFieldersActions[i] = "pickupAttempt";
                    shouldStopFielders = true;
                    defaultFielderSpeed = 0f;
                }
            }
            else if (activeFieldersActions[i] == "pickupAttempt")
            {
                ////ConstantsData_M.MpLog("Picked");

                if (_ballAngle > 90f && _ballAngle < 270f)
                {
                    if (Singleton<RightSmoothFov>.instance != null)
                    {
                        Singleton<RightSmoothFov>.instance.setCameraPosition(gameObject.transform.position, gameObject);
                    }
                }
                else if (Singleton<LeftFovLerp>.instance != null)
                {
                    Singleton<LeftFovLerp>.instance.setCameraPosition(gameObject.transform.position, gameObject);
                }
                if (CONTROLLER.cameraType == 0)
                {
                    if (_ballAngle > 90f && _ballAngle < 270f)
                    {
                        if (Singleton<RightSmoothFov>.instance != null)
                        {
                            Singleton<RightSmoothFov>.instance.setCameraPosition(gameObject.transform.position, gameObject);
                        }
                    }
                    else if (Singleton<LeftFovLerp>.instance != null)
                    {
                        Singleton<LeftFovLerp>.instance.setCameraPosition(gameObject.transform.position, gameObject);
                    }
                }
                isBallPickedByFielder = true;
                stopOtherFielders(i, gameObject);
                if (!isReplayModeActive || (isReplayModeActive && pickedUpFielderIndexSaved == i))
                {
                    //if (ballTransform.position.y < 2.5f)
                    if (temporaryPosition.y < 2.5f)
                    {
                        if (!isReplayModeActive)
                        {
                            pickedUpFielderIndexSaved = i;
                        }
                        if ((!(DistanceBetweenTwoVector2(groundCenterMarker, gameObject) > 20f) && !isRunBeingTaken) || !animation.IsPlaying("run") || !(DistanceBetweenTwoVector2(groundCenterMarker, gameObject) > 52f) || !isReplayModeActive)
                        {
                        }
                        float time2 = animation[pickupAnimationToPlaySaved].time;
                        float num23 = time2 * animationFramesPerSecond;
                        //if (num23 >= 5f && (pickingupAnimationToPlay == "hipCatch" || pickingupAnimationToPlay == "highCatch") && ballTransform.position.y < 0.4f)
                        if (num23 >= 5f && (pickupAnimationToPlaySaved == "hipCatch" || pickupAnimationToPlaySaved == "highCatch") && temporaryPosition.y < 0.4f)
                        {
                            pickupAnimationToPlaySaved = "lowCatch";
                            animation.CrossFade(pickupAnimationToPlaySaved, 0.3f);
                            animation[pickupAnimationToPlaySaved].speed = 2f;
                        }
                        //else if (num23 >= 5f && (pickingupAnimationToPlay == "lowCatch" || pickingupAnimationToPlay == "highCatch") && ballTransform.position.y > 0.5f && ballTransform.position.y < 1.3f)
                        else if (num23 >= 5f && (pickupAnimationToPlaySaved == "lowCatch" || pickupAnimationToPlaySaved == "highCatch") && temporaryPosition.y > 0.5f && temporaryPosition.y < 1.3f)
                        {
                            pickupAnimationToPlaySaved = "hipCatch";
                            animation.CrossFade(pickupAnimationToPlaySaved, 0.3f);
                            animation[pickupAnimationToPlaySaved].speed = 2f;
                        }
                        //else if (num23 >= 5f && (pickingupAnimationToPlay == "lowCatch" || pickingupAnimationToPlay == "hipCatch") && ballTransform.position.y >= 1.3f)
                        else if (num23 >= 5f && (pickupAnimationToPlaySaved == "lowCatch" || pickupAnimationToPlaySaved == "hipCatch") && temporaryPosition.y >= 1.3f)
                        {
                            pickupAnimationToPlaySaved = "highCatch";
                            animation.CrossFade(pickupAnimationToPlaySaved, 0.3f);
                            animation[pickupAnimationToPlaySaved].speed = 2f;
                        }
                        if (num23 >= 7f || DistanceBetweenTwoVector2(ballTimingStartGO, gameObject) - 1f < DistanceBetweenTwoVector2(ballTimingStartGO, matchBall))
                        {
                            gameObject4.GetComponent<Renderer>().enabled = true;
                            ShowBall(status: false);
                            isBallPaused = true;
                            activeFieldersActions[i] = "pickedup";
                            if (_batsmanTransform.position.z < 8f)
                            {
                                animation[pickupAnimationToPlaySaved].speed = 2f;
                            }
                        }
                        fielderThrowTimeElapsed = 0f;
                    }
                }
                else if (DistanceBetweenTwoVector2(gameObject, matchBall) > 10f)
                {
                    IEnumerator enumerator = animation.GetEnumerator();
                    try
                    {
                        while (enumerator.MoveNext())
                        {
                            AnimationState animationState = (AnimationState)enumerator.Current;
                            animationState.speed = 1f;
                        }
                    }
                    finally
                    {
                        IDisposable disposable;
                        if ((disposable = enumerator as IDisposable) != null)
                        {
                            disposable.Dispose();
                        }
                    }
                    activeFieldersActions[i] = "goForChase";
                    animation.Play("run");
                }
            }
            else if (activeFieldersActions[i] == "pickedup")
            {

                if (pickupAnimationToPlaySaved != string.Empty)
                {
                    ////ConstantsData_M.MpLog("Picked");
                    float time3 = animation[pickupAnimationToPlaySaved].time;
                    float num24 = time3 * animationFramesPerSecond;
                    if (num24 > 23f || num24 == 0f)
                    {
                        animation.Play("throw");
                        ////ConstantsData_M.MpLog("Picked2");

                        animation["throw"].speed = 7f / (7f * (1f - controlMultiplier * (float)num));

                        pickupAnimationToPlaySaved = string.Empty;
                    }
                    break;
                }
                isBallPickedByFielder = true;
                float num25 = ((CONTROLLER.BowlingTeamIndex != CONTROLLER.opponentTeamIndex) ? (7f * (1f - controlMultiplier * (float)num)) : 7f);
                if (CONTROLLER.PlayModeSelected == 8)
                {
                    num25 = 7f;
                }
                float num26 = num25 * animationFrameInterval;
                if (animation.IsPlaying("throw"))
                {
                    ////ConstantsData_M.MpLog("Picked");

                    if (!isReplayModeActive && throwTargetSaved == string.Empty)
                    {
                        ////ConstantsData_M.MpLog("Picked");

                        // The batting authority has already ruled — adopt it instead of deciding from
                        // local state. Both inputs below (which end this fielder is nearer to, and
                        // fielderAction) are local-simulation only, so an independent decision here is
                        // what put the ball in the bowler's hands on one screen and the keeper's on the
                        // other. See OnThrowTargetRelayed.
                        if (!string.IsNullOrEmpty(_relayedThrowTarget))
                        {
                            throwTargetSaved = _relayedThrowTarget;
                            throwTarget = (_relayedThrowTarget == "Fielder10") ? fielder10Object : wicketKeeperObject;
                        }
                        else
                        {
                            if (DistanceBetweenTwoVector2(gameObject, fielder10Object) < DistanceBetweenTwoVector2(gameObject, wicketKeeperObject))
                            {
                                ////ConstantsData_M.MpLog("Picked");

                                if (fielderAction == "waitForBall")
                                {
                                    throwTargetSaved = "Fielder10";
                                    throwTarget = fielder10Object;
                                }
                                else
                                {
                                    throwTargetSaved = "WicketKeeper";
                                    throwTarget = wicketKeeperObject;
                                }
                            }
                            else
                            {
                                throwTargetSaved = "WicketKeeper";
                                throwTarget = wicketKeeperObject;
                            }

                            // Rule once, from the batting side, the moment the throw starts — the same
                            // authority and the same timing the 4-vs-6 boundary verdict uses. If the
                            // relay never lands the other side keeps its own guess, i.e. today's
                            // behaviour, so this can only reduce divergence, never stall a throw.
                            if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false
                                && CONTROLLER.BattingTeamIndex == CONTROLLER.myTeamIndex
                                && CricketNetworkManager.ReadyToSend)
                            {
                                ConstantsData_M.MpLog($"[GroundController][ThrowTarget] Authority ruling: '{throwTargetSaved}' (fielderAction={fielderAction}).");
                                CricketNetworkManager.instance.CmdThrowTarget(staticVariables.UserProfiledata.user._id, throwTargetSaved);
                            }
                        }
                    }
                    fielderHasThrown = true;
                    if (throwTargetSaved == "Fielder10")
                    {
                        fielderTransforms[activeFielders[i]].LookAt(fielder10SkinTransform);
                    }
                    fielderThrowTimeElapsed += Time.deltaTime;
                    if (fielderThrowTimeElapsed >= num26)
                    {
                        ////ConstantsData_M.MpLog("Picked");

                        float num27 = 2f;
                        string text = gameObject.name;
                        int num28 = int.Parse(text.Substring(text.Length - 1));
                        fielderBallReleasePointTransforms[num28].position = new Vector3(fielderBallReleasePointTransforms[num28].position.x, num27, fielderBallReleasePointTransforms[num28].position.z);
                        gameObject4.GetComponent<Renderer>().enabled = false;
                        ShowBall(status: true);
                        ////ConstantsData_M.MpLog("* position change");
                        matchBallTransform.position = fielderBallReleasePointTransforms[num28].position;
                        temporaryPosition = matchBallTransform.position;
                        GameObject gameObject8 = null;
                        float num29 = 0f;
                        if (throwTargetSaved == "Fielder10")
                        {
                            if (postBattingStumpingFielderDirection == "straight")
                            {
                                gameObject8 = fielderStraightStumpingPosition;
                            }
                            else if (postBattingStumpingFielderDirection == "straightDown")
                            {
                                gameObject8 = stumpRightCrease;
                            }
                            else if (postBattingStumpingFielderDirection == "offSide")
                            {
                                gameObject8 = fielderOffSideStumpingPosition;
                            }
                            else if (postBattingStumpingFielderDirection == "legSide")
                            {
                                gameObject8 = fielderLegSideStumpingPosition;
                            }
                            fielderAction = "waitToCollect";
                            fielder10SkinTransform.LookAt(fielderTransforms[activeFielders[i]]);
                            keeperAnim.CrossFade("idle");
                            num29 = DistanceBetweenTwoGameObjects(gameObject, gameObject8) + 1f;
                            // Draw through the per-delivery SEEDED stream, not UnityEngine.Random. Whether a long
                            // throw bounces, and where, was decided by an independent coin flip on each client
                            // (tester #8: "ik side keeper ke paas ball fielder ki taraf se HAWA mein aayi aur ik
                            // side ball ground pe lag kar pohnchi"). Nothing relayed either value, so the two
                            // screens showed different throws for the same delivery. These are exactly the
                            // "later seeded roll" the post-contact re-seed exists to keep aligned; the throw
                            // path was simply never converted with the rest of them.
                            if (num29 > 40f && DetRange(1, 10) <= 5)
                            {
                                throwFirstBounceDistance = DetRange(5f, 8f);
                                num29 -= throwFirstBounceDistance;
                            }
                            if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false)
                                ConstantsData_M.MpLog($"[ThrowArc] target='{throwTargetSaved}' len={num29:F1} bounceDist={throwFirstBounceDistance:F1} bounced={(throwFirstBounceDistance > 0f)}.");
                            if (!isReplayModeActive)
                            {
                                throwingFirstBounceDistanceSaved = throwFirstBounceDistance;
                                throwLengthSaved = num29;
                                summarySaved = "picked";
                            }
                            else if (isReplayModeActive)
                            {
                                throwFirstBounceDistance = throwingFirstBounceDistanceSaved;
                                num29 = throwLengthSaved;
                            }
                        }
                        else if (isWicketKeeperAtStump)
                        {
                            if (wicketKeeperDirectionAfterBatting == "straight")
                            {
                                gameObject8 = wicketKeeperStraightStumpingPosition;
                            }
                            else if (wicketKeeperDirectionAfterBatting == "offSide")
                            {
                                gameObject8 = wicketKeeperOffSideStumpingPosition;
                            }
                            else if (wicketKeeperDirectionAfterBatting == "legSide")
                            {
                                gameObject8 = wicketKeeperLegSideStumpingPosition;
                            }
                            currentWicketKeeperStatus = "waitToCollect";
                            _wicketKeeperTransform.LookAt(fielderTransforms[activeFielders[i]]);
                            fielder10Anim.CrossFade("idle");
                            num29 = DistanceBetweenTwoGameObjects(gameObject, gameObject8);
                            // Same unsynced coin flip as the Fielder10 branch above — see the note there.
                            if (num29 > 40f && DetRange(1, 10) <= 5)
                            {
                                throwFirstBounceDistance = DetRange(5f, num29 / 2f);
                                num29 -= throwFirstBounceDistance;
                            }
                            if (CONTROLLER.PlayModeSelected == 8 && GameConstants.isWithAI == false)
                                ConstantsData_M.MpLog($"[ThrowArc] target='{throwTargetSaved}' len={num29:F1} bounceDist={throwFirstBounceDistance:F1} bounced={(throwFirstBounceDistance > 0f)}.");
                            if (!isReplayModeActive)
                            {
                                throwingFirstBounceDistanceSaved = throwFirstBounceDistance;
                                throwLengthSaved = num29;
                                summarySaved = "picked";
                            }
                            else if (isReplayModeActive)
                            {
                                throwFirstBounceDistance = throwingFirstBounceDistanceSaved;
                                num29 = throwLengthSaved;
                            }
                        }
                        if (gameObject8 != null)
                        {
                            ////ConstantsData_M.MpLog("Picked");

                            _ballAngle = AngleBetweenTwoGameObjects(fielderBallReleasePoints[num28], gameObject8);
                            horizontalVelocity = 24f;
                            arcHeight = num29 / 6f;
                            float num30 = Mathf.Asin(num27 / arcHeight) * radToDeg;
                            if (num27 >= arcHeight)
                            {
                                num30 = 90f;
                                arcHeight = num27;
                            }
                            launchAngle = 180f + num30;
                            angleChangeRate = (180f - num30) / num29 * horizontalVelocity;
                            isBallPaused = false;
                            currentBallStatus = "throw";
                            activeFieldersActions[i] = "throw";
                        }
                    }
                }
            }
            else if (!(activeFieldersActions[i] == "throw"))
            {
                ////ConstantsData_M.MpLog("Picked");

                if (activeFieldersActions[i] == "end")
                {
                    ////ConstantsData_M.MpLog("ENDDDDDD THROWW");
                    //if (CONTROLLER.PlayModeSelected == 6 && (shotPlayed == "bt6Defense" || shotPlayed == "frontFootOffSideDefense" || shotPlayed == "backFootDefenseHighBall"))
                    //{
                    //	break;
                    //}


                    if (_stayStartTime + 1f + delayBetweenDeliveries < Time.time)
                    {
                        activeFieldersActions[i] = string.Empty;
                        if (Singleton<GameData>.instance != null)
                        {
                            if (noBall)
                            {
                                CONTROLLER.TeamList[CONTROLLER.BattingTeamIndex].currentMatchNoball++;
                                Singleton<GameData>.instance.UpdateCurrentBall(0, 1, runsScoredThisBall, 1, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                                CONTROLLER.isJokerCall = false;
                                if (CONTROLLER.BowlingTeamIndex != CONTROLLER.myTeamIndex)
                                {
                                }
                            }
                            else if (freeHit)
                            {
                                Singleton<GameData>.instance.UpdateCurrentBall(1, 1, runsScoredThisBall, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                                freeHit = false;
                                isFreeHitActive = false;
                            }
                            else if (!freeHit && !noBall)
                            {
                                Singleton<GameData>.instance.UpdateCurrentBall(1, 1, runsScoredThisBall, 0, CONTROLLER.StrikerIndex, 0, 0, CONTROLLER.CurrentBowlerIndex, 0, 0, isBoundary: false);
                            }
                        }
                    }
                }
                else if (activeFieldersActions[i] == "goToBoundary")
                {
                    MoveToCollectBallAfterBoundary(activeFielders[i], stop: false, first: true);
                    activeFieldersActions[i] = "goingToBoundary";
                }
                else if (activeFieldersActions[i] == "goingToBoundary")
                {
                    if (DistanceBetweenTwoVector2(fielders[num3], matchBall) < 1.25f)
                    {
                        if (!isBallPaused)
                        {
                            if (boundaryAnimationName.Equals(string.Empty))
                            {
                                //float y3 = ballTransform.position.y;
                                float y3 = temporaryPosition.y;
                                if (y3 >= 1.75f)
                                {
                                    boundaryAnimationName = "highCatch";
                                }
                                else if (y3 >= 1.25f)
                                {
                                    boundaryAnimationName = "chestCatch";
                                }
                                else if (y3 >= 1f)
                                {
                                    boundaryAnimationName = "hipCatch";
                                }
                                else
                                {
                                    boundaryAnimationName = "runAndFieldForward";
                                }
                            }
                            float time4 = animation[boundaryAnimationName].time;
                            float num31 = time4 * animationFramesPerSecond;
                            if (!(num31 >= 26f))
                            {
                                if (num31 >= 8f)
                                {
                                    animation.Play(boundaryAnimationName);
                                    fielderBalls[activeFielders[i]].GetComponent<Renderer>().enabled = true;
                                    ShowBall(status: false);
                                    isBallPaused = true;
                                }
                                else if (!animation.IsPlaying(boundaryAnimationName))
                                {
                                    animation.Play(boundaryAnimationName);
                                    horizontalVelocity = Mathf.Min(horizontalVelocity, 0.5f);
                                }
                            }
                        }
                    }
                    else
                    {
                        MoveToCollectBallAfterBoundary(activeFielders[i]);
                    }
                }
            }
            //else if ( wicketKeeperStatus == "end" && activeFielderAction[i] == "throw")
            //{
            //	activeFielderAction[i] = "end";
            //}


            if (isBallOnBoundaryLine && (isBallReflectedFromBoundary || horizontalVelocity < boundarySpeedThreshold) && closestFielderIndex == -1 && !isBallOverTheFence)
            {
                //nearestFielderIndex = GetNearestFielderIndex(ballTransform.position, 20f, requireIdle: true);
                closestFielderIndex = GetNearestFielderIndex(temporaryPosition, 20f, requireIdle: true);
                if (closestFielderIndex != -1 && !IsFielderPlayingAnAnimation(activeFielders[closestFielderIndex]))
                {
                    activeFieldersActions[closestFielderIndex] = "goToBoundary";
                }
            }
        }
    }

    private void stopOtherFielders(int val, GameObject thisFielder)
    {
        for (int i = 0; i < activeFielders.Count; i++)
        {
            GameObject gameObject = fielders[activeFielders[i]];
            if (gameObject != thisFielder && activeFielders.Count > 1 && activeFieldersActions[i] == "goForChase")
            {
                activeFieldersActions[i] = "stopChasing";
            }
        }
    }

    private void turnOnFielders(bool boolean)
    {
        for (int i = 0; i < fielders.Length; i++)
        {
            if (fielders[i] != null)
            {
                FielderSkinRenderer[i].enabled = boolean;
                fielderCaps[i].enabled = boolean;
            }
        }
        batsmanSkinRenderer.enabled = true;
        RunnerSkinRenderer.enabled = boolean;
        BatsmanCricketKitSkinRenderer.enabled = true;
        BatsmanBatSkinRenderer.enabled = true;
        RunnerCricketKitSkinRenderer.enabled = boolean;
        RunnerBatSkinRenderer.enabled = boolean;
        mainUmpireTransform.gameObject.SetActive(boolean);
        sideUmpireObject.SetActive(boolean);
        wicketKeeperObject.SetActive(boolean);
    }

    private void distanceBetweenUmpireAndFielder(bool boolean)
    {
        for (int i = 1; i <= numberOfFielders; i++)
        {
            if (!boolean)
            {
                if (DistanceBetweenTwoVector2(sideUmpireObject, fielders[i]) < 4f)
                {
                    FielderSkinRenderer[i].enabled = boolean;
                    fielderCaps[i].enabled = boolean;

                }
            }
            else
            {
                FielderSkinRenderer[i].enabled = boolean;
                fielderCaps[i].enabled = boolean;

            }
        }
    }

    private int runOutScenario(string runOutBy)
    {
        int result = 0;
        if (runOutBy == "w")
        {
            result = ((!(_batsmanTransform.position.z < 0f)) ? CONTROLLER.StrikerIndex : CONTROLLER.NonStrikerIndex);
        }
        else if (runOutBy == "b")
        {
            result = ((!(_batsmanTransform.position.z < 0f)) ? CONTROLLER.NonStrikerIndex : CONTROLLER.StrikerIndex);
        }
        return result;
    }

    [Skip]
    private void ThrowingBallMovement()
    {
        ////ConstantsData_M.MpLog("* ThrowingBallMovement");
        if (!isBallPaused && currentBallStatus == "throw")
        {
            ballTrailRenderer.time = 0.03f;
            BallMovement();
            if (launchAngle >= 360f)
            {
                launchAngle = 180f;
                horizontalVelocity *= 0.8f;
                angleChangeRate = 90f / throwFirstBounceDistance * horizontalVelocity;
                arcHeight = 0.75f;
            }
        }
    }

    private bool isBatsmanRunOut()
    {
        if (leftLegEdgeObject.transform.position.z < creaseLineLimit && leftLegEdgeObject.transform.position.z > creaseLineLimit * -1f && rightLegEdgeObject.transform.position.z < creaseLineLimit && rightLegEdgeObject.transform.position.z > creaseLineLimit * -1f && batEdgeObject.transform.position.z < creaseLineLimit && batEdgeObject.transform.position.z > creaseLineLimit * -1f)
        {
            return true;
        }
        return false;
    }

    public void EnableFielders(bool boolean)
    {
        for (int i = 0; i < fielders.Length; i++)
        {
            if (fielders[i] != null)
            {
                FielderSkinRenderer[i].enabled = boolean;

                fielderCaps[i].enabled = boolean;

            }
        }
        RunnerSkinRenderer.enabled = boolean;
        RunnerCricketKitSkinRenderer.enabled = boolean;
        RunnerBatSkinRenderer.enabled = boolean;
    }

    private void SetFieldersSizeBackToNormal()
    {
        for (int i = 1; i <= numberOfFielders; i++)
        {
            fielderTransforms[i].localScale = new Vector3(1f, 1f, 1f);
        }
    }

    private void gatherFielders()
    {
        for (int i = 1; i <= numberOfFielders; i++)
        {
            fielderTransforms[i].position = new Vector3(UnityEngine.Random.Range(15, 25), 0f, UnityEngine.Random.Range(-15, 0));
            fielderTransforms[i].eulerAngles = new Vector3(0f, 0f, 0f);
            fieldersAnim[i].Play("walk");
            fieldersAnim[i]["walk"].time = UnityEngine.Random.Range(1, 5);
            fieldersAnim[i]["walk"].speed = UnityEngine.Random.Range(1f, 1.5f);
        }
        hideBatsmenAndUmpires();
    }

    private void moveFielders()
    {
        for (int i = 1; i <= numberOfFielders; i++)
        {
            if (fielderTransforms[i].position.z < 64f)
            {
                fielderTransforms[i].position += new Vector3(0f, 0f, 0.009f);
            }
        }
    }

    private bool IsTightRunoutCall()
    {
        bool result = false;
        if (Mathf.Abs(batEdgeObject.transform.position.z) > 7.5f && Mathf.Abs(batEdgeObject.transform.position.z) < 9f)
        {
            result = true;
        }
        if (Mathf.Abs(batEdgeObject.transform.position.z) > 8.4f && Mathf.Abs(batEdgeObject.transform.position.z) < 9f)
        {
            isVeryTightRunoutCall = true;
        }
        return result;
    }

    public void StartIntroFielderAnimation()
    {
        InvokeRepeating("SetFielderAnimation", 0f, 3f);
    }

    public void StopIntroFielderAnimation()
    {
        CancelInvoke("SetFielderAnimation");
    }

    public void SetFielderAnimation()
    {
        for (int i = 1; i < fielders.Length - 1; i++)
        {
            switch (UnityEngine.Random.Range(1, 3))
            {
                case 1:
                    fieldersAnim[i].CrossFade("warmUp" + UnityEngine.Random.Range(1, 6));
                    break;
                case 2:
                    fieldersAnim[i].CrossFade("fielderArrangingPlayers");
                    break;
                case 3:
                    fieldersAnim[i].CrossFade("getReady");
                    break;
                case 4:
                    fieldersAnim[i].CrossFade("celebration1");
                    break;
            }
        }
    }

    private int GetNearestFielderIndex(Vector3 initialPosition, float fielderScanDistance, bool requireIdle = false)
    {
        float num = float.PositiveInfinity;
        for (int i = 0; i < activeFielders.Count; i++)
        {
            float num2 = DistanceBetweenTwoVector2(initialPosition, fielderTransforms[activeFielders[i]].position);
            if (num2 < num)
            {
                num = num2;
                closestFielderIndex = i;
            }
        }
        return (!(num <= fielderScanDistance)) ? (-1) : closestFielderIndex;
    }

    private bool IsFielderPlayingAnAnimation(int fielderIndex)
    {
        bool flag = false;
        flag |= fieldersAnim[fielderIndex].IsPlaying("diveStraight");
        flag |= fieldersAnim[fielderIndex].IsPlaying("highCatch");
        flag |= fieldersAnim[fielderIndex].IsPlaying("hipCatch");
        flag |= fieldersAnim[fielderIndex].IsPlaying("lowCatch");
        flag |= fieldersAnim[fielderIndex].IsPlaying("sideCatch");
        return flag | fieldersAnim[fielderIndex].IsPlaying("slideAndField");
    }

    private void ScanForUserFielders()
    {
        _scanForUserFielders.Clear();
        foreach (GameObject item in aiFielderScanList)
        {
            _scanForUserFielders.Add(AngleBetweenTwoGameObjects(batsmanObject, item));
        }
        _scanForUserFielders.Sort();
        if (CONTROLLER.PlayModeSelected != 7)
        {
            MakeAIHitInGap();
        }
        else
        {
            MakeAIHitToFielder();
        }
    }

    private void MakeAIHitToFielder()
    {
        int index = UnityEngine.Random.Range(0, _scanForUserFielders.Count);
        aiBallAngle = _scanForUserFielders[index];
        if (aiBallAngle > 360f)
        {
            aiBallAngle %= 360f;
        }
        if (CONTROLLER.StrikerHand == "left")
        {
            aiBallAngle = 180f - aiBallAngle + 360f;
            aiBallAngle %= 360f;
        }
    }

}
