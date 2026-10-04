using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NitroFill : MonoBehaviour
{
    public Image NitroImage;

    void Update()
    {
        if (Tutorials.isTutorialActive)
        {
            if (HorseMobileButton.Instance.powerBoostController.NoS < 1)
            {
                Tutorials.instance.HideNitroTutorial();
            }
        }
    }
}
