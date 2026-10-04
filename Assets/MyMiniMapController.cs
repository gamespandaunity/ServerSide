using System.Collections;
using UnityEngine;

public class MyMiniMapController : MonoBehaviour
{
    public Transform trackStart;
    public Transform trackEnd;
    public RectTransform roadLine;
    public RectTransform playerIcon;
    public RectTransform opponentIcon;

    public Transform player;
    public Transform opponent;

    public float trackLength = 5000f;
    public float mapHeight = 300f;
    void Start()
    {
     
    }
    public void SetTarget(Transform newTarget)
    {
        player = newTarget;
    }
    public void SetOpponent(Transform target)
{
    if(opponent==null)
    {
        opponent = target;
    }
   
}

    void Update()
    {
        UpdateIcon(player, playerIcon);
    UpdateIcon(opponent, opponentIcon);
        UpdatePlayer();
    }
    void UpdatePlayer()
    {
        float progress = Mathf.InverseLerp(
            trackStart.position.z,
            trackEnd.position.z,
            player.position.z
        );
    
        progress = Mathf.Clamp01(progress);
    
        float y = Mathf.Lerp(
            -roadLine.rect.height / 2f,
             roadLine.rect.height / 2f,
             progress
        );
    
        playerIcon.anchoredPosition = new Vector2(
            playerIcon.anchoredPosition.x,
            y
        );
    }
   void UpdateIcon(Transform target, RectTransform icon)
{
    if (target == null || icon == null)
        return;

    float progress = Mathf.InverseLerp(
        trackStart.position.z,
        trackEnd.position.z,
        target.position.z
    );

    progress = Mathf.Clamp01(progress);

    float yPos = Mathf.Lerp(
        -roadLine.rect.height / 2f,
        roadLine.rect.height / 2f,
        progress
    );

    icon.anchoredPosition = new Vector2(icon.anchoredPosition.x, yPos);
}
}
