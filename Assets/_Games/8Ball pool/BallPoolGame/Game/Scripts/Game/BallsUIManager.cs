using UnityEngine;
using UnityEngine.Serialization;

public class BallsUIManager : MonoBehaviour
{
    [FormerlySerializedAs("defaultBall")] public Sprite ballSpriteDefault;
    [FormerlySerializedAs("defaultColor")] public Color ballColorDefault;
    [FormerlySerializedAs("solidsBall")] public Sprite ballSpriteSolids;
    [FormerlySerializedAs("stripesBall")] public Sprite ballSpriteStripes;
    [FormerlySerializedAs("ballsColors")] public Color[] ballColorsByType; //RAR
    [FormerlySerializedAs("allBallsSprites")] public Sprite[] allBallSprites; //RAR
}
