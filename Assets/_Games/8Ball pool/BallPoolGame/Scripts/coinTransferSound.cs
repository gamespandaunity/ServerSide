using System.Collections;
using UnityEngine;

public class coinTransferSound : MonoBehaviour
{
    public AudioSource coinTransfer;
    public void PlaySound()
    {

    
            StartCoroutine(PlayCoinTransferSound());
        
    }
    IEnumerator PlayCoinTransferSound()
    {

        yield return new WaitForSeconds(1f);

        coinTransfer.Play();



    }

}
