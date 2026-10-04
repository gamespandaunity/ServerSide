using System.Collections;
using System.Collections.Generic;
using DG.Tweening.Core;
using UnityEngine;

public class TutorialType : MonoBehaviour
{
   public GameObject infoText;
   void OnEnable()
   {
      if (!Tutorials.tutorialType.Contains(gameObject.name))
      {
         gameObject.SetActive(false);
         infoText.SetActive(false);
      }
   }
}
