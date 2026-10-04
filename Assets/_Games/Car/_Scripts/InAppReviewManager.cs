namespace CarRace 
{
//using System.Collections;
//using UnityEngine;
//using Google.Play.Review;
//using UnityEngine.UI;

//public class InAppReviewManager : MonoBehaviour
//{
//    // Create instance of ReviewManager
//    private ReviewManager _reviewManager;
//    private PlayReviewInfo _playReviewInfo;
//    public Button rateUsButton;
//    void Start()
//    {
//        rateUsButton.onClick.AddListener(OnRequestReviewClicked);
//    }
//     void OnRequestReviewClicked()
//    {
//        StartCoroutine(RequestReview());
//        rateUsButton.interactable = false;
//        Invoke(nameof(EnableButton), 5);
//    }

//    IEnumerator RequestReview()
//    {
//        _reviewManager = new ReviewManager();
//        var requestFlowOperation = _reviewManager.RequestReviewFlow();
//        yield return requestFlowOperation;
//        if (requestFlowOperation.Error != ReviewErrorCode.NoError)
//        {
//            // Log error. For example, using requestFlowOperation.Error.ToString().
//            yield break;
//        }
//        _playReviewInfo = requestFlowOperation.GetResult();

//        //Laucnh the In App Review

//        var launchFlowOperation = _reviewManager.LaunchReviewFlow(_playReviewInfo);
//        yield return launchFlowOperation;
//        _playReviewInfo = null; // Reset the object
//        if (launchFlowOperation.Error != ReviewErrorCode.NoError)
//        {
//            // Log error. For example, using requestFlowOperation.Error.ToString().
//            yield break;
//        }
//        // The flow has finished. The API does not indicate whether the user
//        // reviewed or not, or even whether the review dialog was shown. Thus, no
//        // matter the result, we continue our app flow.
//    }

//    void EnableButton()
//    {
//        rateUsButton.interactable = true;
//    }
//}

}