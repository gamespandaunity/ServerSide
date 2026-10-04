namespace CarRace
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    using TMPro;
    using RuntimeInspectorNamespace;
    using Mirror;

    public class CarModification : NetworkBehaviour
    {
        [SerializeField] Material[] materials;
        MeshRenderer carBdy;
        private bool isF1;
        [SerializeField] TextMeshProUGUI PlayerNameInNumerPlate;
        
        void Awake()
        {
            //Photon Removal     var chassis = transform.Find("F1");
            //Photon Removal       if (chassis)
            // isF1 = true;
            //Photon Removal      carBdy = GetComponentInChildren<MeshCollider>().transform.gameObject.GetComponent<MeshRenderer>();
            carBdy = GetComponentInChildren<MeshCollider>().transform.gameObject.GetComponent<MeshRenderer>();
        }
       
       

        //Photon Removal   [PunRPC]
        public void ChangeColor(int colorIndex)
        {

            if (carBdy == null)
            {
                Debug.Log("carbodynull");
                carBdy = GetComponentInChildren<MeshCollider>().transform.gameObject.GetComponent<MeshRenderer>();
            }
            else

            {
                Debug.Log("carbodyNotnull");
            }
            carBdy.material = materials[colorIndex];
            //Photon Removal    if (PhotonNetwork.IsMasterClient)
            {
                //Photon Removal       if (photonView.IsMine)
                {
                    //carBdy.material = materials[0];

                }
                //Photon Removal    else



            }
            //Photon Removal    else
            {
                //Photon Removal    if (!photonView.IsNull())
                {
                    //Photon Removal       if (photonView.IsMine)
                    {
                        //    carBdy.material = materials[1];

                    }
                    //Photon Removal   else
                    {
                        //   carBdy.material = materials[0];

                    }
                }
                //Photon Removal   else
                {
                    //   carBdy.material = materials[colorIndex];
                }
            }


        }
        public Color GetMaterialColor(int materialIndex)
        {
            return materials[materialIndex].GetColor("_Color");
        }
        public int GetmaterialNumber()
        {
            return materials.Length;
        }
        public void SetPlayerNameInNumerPlate(string userName)
        {
            StartCoroutine(SetName(userName));
        }

        IEnumerator SetName(string name)
        {
            yield return new WaitForSeconds(0.3f);
            PlayerNameInNumerPlate.text = name;
        }
    }

}