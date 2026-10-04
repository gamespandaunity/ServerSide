using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SpriteAnimator : MonoBehaviour
{
    public Sprite[] sprites; // Assign 20 sprites in the inspector
    public Image image;
    public float frameRate = 0.1f; // 10 FPS (1 / 10)

    void OnEnable()
    {
        StopAllCoroutines();
        StartCoroutine(AnimateSprites());
    }

    IEnumerator AnimateSprites()
    {
        // Guard: an unassigned/empty sprites array (or missing image) must not be indexed.
        // Three instances in the 8-Ball-pool Home.unity ship with an empty array; on a cricket
        // reconnect the lobby UI re-activates and this coroutine threw IndexOutOfRange (and a
        // latent DivideByZero on the % below) once per frame. Short-circuit the empty case.
        if (image == null || sprites == null || sprites.Length == 0)
            yield break;
        int index = 0;
        while (true)
        {
            image.sprite = sprites[index]; // Change sprite
            index = (index + 1) % sprites.Length; // Loop animation
            yield return new WaitForSeconds(frameRate);
        }
    }
}
