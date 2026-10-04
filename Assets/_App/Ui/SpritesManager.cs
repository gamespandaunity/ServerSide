using UnityEngine;

public class SpritesManager : MonoBehaviour
{
    public SpritesHOlder spritesScriptable;
    public static SpritesManager Instance;

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
