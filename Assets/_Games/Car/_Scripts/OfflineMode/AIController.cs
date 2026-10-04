namespace CarRace
{
    using System.Collections;
    using System.Collections.Generic;
    using TMPro;
    using UnityEngine;

    /// <summary>
    /// Script To Handle AI Cars
    /// </summary>
    public class AIController : CarController
    {
        private RCC_CarControllerV3 rCC_CarController;
        private int lap;
        protected override void Start()
        {
            base.Start();
            playerPositionUI.SetActive(false);
            CarModification carModification = GetComponent<CarModification>();
            carModification.ChangeColor(Random.Range(0, carModification.GetmaterialNumber()));
            lap = 1;
        }
        private void OnTriggerExit(Collider collider)
        {
            if (collider.CompareTag("DirectionPoints"))
            {
                playerPositionUI.SetActive(true);
                positionNameText.text = playerName;
                if (!collider.GetComponent<WayPoint>().IsCarPassed(playerID))
                {
                    ////Debug.Log("<color=orange> Wapoint Crossed </color>" + collider.transform.name);
                    PlayerPositionSystem._Instance.PlayerCrossedWaypoint(playerID);
                }

            }
            else if (collider.CompareTag("Middle"))
            {
                hasCrossedMiddleCheckPoint = true;
            }
            else if (collider.CompareTag("Finish"))
            {
                if (hasCrossedMiddleCheckPoint)
                {
                    "Ai Collision".Show();
                    hasCrossedMiddleCheckPoint = false;
                    LapFinished();
                    lap++;
                }
            }

        }
        public void SetPlayerNameandDifficulty(string name, int difficultyLevel)
        {
            playerName = name;
            GetComponentInChildren<TextMeshProUGUI>().text = playerName;
            rCC_CarController = GetComponent<RCC_CarControllerV3>();
            rCC_CarController.useDamage = System.Convert.ToBoolean(PlayerPrefs.GetInt(PPConst.Damage));
            difficultyLevel = 1;
            SetDifficulty(difficultyLevel);
            Debug.Log("difficultyLevel: " + difficultyLevel);
        }
        void SetDifficulty(int difficultyLevel) //Change Max Speed, Brake Torgue , Max Engine Torgue
        {
            switch (difficultyLevel)
            {
                case 1:
                    Easy();
                    break;
                case 2:
                    Medium();
                    break;
                case 3:
                    Hard();
                    break;
                default:
                    Randomm();
                    break;
            }
            // Max Torgue , Normal 560
            // Max Speed , Normal 350
            // Brakes , Normal 4000
            void Easy()
            {
                rCC_CarController.maxEngineTorque = Random.Range(380, 400);
                rCC_CarController.maxspeed = Random.Range(160, 180);
                rCC_CarController.brakeTorque = Random.Range(3300, 3700);
            }
            void Medium()
            {
                rCC_CarController.maxEngineTorque = Random.Range(450, 500);
                rCC_CarController.maxspeed = Random.Range(190, 230);
                rCC_CarController.brakeTorque = Random.Range(4000, 4300);
            }
            void Hard()
            {
                rCC_CarController.maxEngineTorque = Random.Range(670, 750);
                rCC_CarController.maxspeed = Random.Range(390, 420);
                rCC_CarController.brakeTorque = Random.Range(5300, 5500);
            }
            void Randomm()
            {
                rCC_CarController.maxEngineTorque = Random.Range(430, 750);
                rCC_CarController.maxspeed = Random.Range(240, 420);
                rCC_CarController.brakeTorque = Random.Range(3300, 5500);
            }

        }

        protected override void OnGameStart(int totalLaps, int totalPlayers)
        {
            PlayerPositionSystem._Instance.RegisterPlayer(new PlayerDataForPositionSystem(playerID, transform, playerName), this);
            base.OnGameStart(totalLaps, totalPlayers);
        }

        protected override void LapFinished()
        {
            base.LapFinished();
            lapCount.Show("lap count");
            totalLaps.Show("total Laps");
            EventManager.OnAILapCompleted.Invoke(playerID);
            if (lapCount == totalLaps)
            {
                ("finish").Show();
                GetComponent<RCC_CarControllerV3>().SetCanControl(false);
                GetComponent<RCC_AICarController>().Stop();
                InGameMenuController.Instance.raceFinishPanel.SetActive(true);
                // ResultManagerForCar.instance.HandleGameResultAlt(false, "ai");
                if (ResultManager.GameSpawnedFinished == false)
                {
                    ResultManager.GameSpawnedFinished = true;
                    GameObject enemyPrefab = Resources.Load<GameObject>("WinLoseGameManager");

                    if (enemyPrefab != null)
                    {
                        "1".Show();
                        // Spawn at position (0,0,0)
                        var gameObject = Instantiate(enemyPrefab, Vector3.zero, Quaternion.identity);
                        gameObject.GetComponent<ResultManager>().HandleGameResultAltAI(false, staticVariables.UserProfiledata.user._id.ToString());
                    }
                    else
                    {
                        Debug.LogError("WinLose GameManager prefab not found!");
                    }
                }
            }

        }
    }

}