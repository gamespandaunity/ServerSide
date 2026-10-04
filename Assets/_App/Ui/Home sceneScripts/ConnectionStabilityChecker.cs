using UnityEngine;
 


public class ConnectionStabilityChecker : MonoBehaviour
{
    
    public static bool CheckLatency()
    {
        //if (PhotonNetwork.IsConnected)
        //{

        //    int latency = PhotonNetwork.GetPing() / 2;                                                                                                    //Photon Removal
        //    //Debug.Log("Network latency (ms) : " + latency);

        //    if (latency < 500) // Example threshold for a stable connection
        //    {
        //        //Debug.Log("Stable connection.");
        //        return true;
        //    }
        //    else
        //    {
        //        //Debug.Log("Unstable connection.");
        //        return false;
        //    }
        //}
        //else
        {
            ConstantsData_M.Log("Not connected to PhotonNetwork.");
            return false;
        }
           
            
        

    }
    #region Network Speeed Test
    //public void GetTestResult()
    //{
    //    StartCoroutine(SpeedTest());

    //}
    //IEnumerator SpeedTest()
    //{
    //    UnityWebRequest request = UnityWebRequest.Get("https://google.com");
    //    float startTime = Time.time; // Record the start time before sending the request
    //    yield return request.SendWebRequest();

    //    if (request.isNetworkError)
    //    {
    //        //Debug.Log("Network error!");
    //    }
    //    else if (request.isDone)
    //    {
    //        // Calculate download time
    //        float downloadTime = Time.time - startTime;

    //        if (downloadTime > 5)
    //        {
    //            //Debug.Log("Slow download speed! Network might be unstable.");
    //        }
    //    }
    //}
    #endregion

}
