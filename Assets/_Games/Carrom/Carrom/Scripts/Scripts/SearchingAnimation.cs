using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
namespace BEKStudio
{
    public class SearchingAnimation : MonoBehaviour
{
    public Image avatarImage; // Reference to the Image component where avatars will be displayed
    private List<Sprite> avatars; // List of avatar sprites
    private LTDescr searchTween; // Reference to the LeanTween description

    private void OnEnable()
    {
        // Get the list of all avatars from the GameManager
        avatars = new List<Sprite>(GameManager.Instance.GetAllAvatars());
        StartSearching();
    }

    private void StartSearching()
    {
        // Shuffle the avatars list
        avatars.Shuffle();

        // Start the looping animation
        searchTween = LeanTween.value(gameObject, 0f, 1f, 0.2f)
            .setOnUpdate((float val) => ChangeAvatar())
            .setLoopClamp();
    }

    private void ChangeAvatar()
    {
        if (avatars != null && avatars.Count > 0)
        {
            // Get a random avatar from the list
            int randomIndex = Random.Range(0, avatars.Count);
            avatarImage.sprite = avatars[randomIndex];
        }
    }

    private void OnDisable()
    {
        // Stop the animation when the object is disabled
        if (searchTween != null)
        {
            LeanTween.cancel(searchTween.id);
        }
    }
}

public static class Extensions
{
    // Extension method to shuffle a list
    public static void Shuffle<T>(this IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}
}
