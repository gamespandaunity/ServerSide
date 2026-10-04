using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class DiceAnimator : MonoBehaviour
{
    public float rotationSpeedY;
    public float rotationSpeedX;
    public float rotationSpeedZ;
    public Vector2 ZAxis;
    public List<Vector3> TargetRotation;
    public bool isAnimating;

    private void Update()
    {
        // Check if animation should start
        if (isAnimating)
        {
            AnimateDice(Random.Range(1, 7));
            isAnimating = false; // Reset the flag after starting the animation
        }
    }

    public void AnimateDice(int diceNumber)
    {
        // Ensure diceNumber is between 1 and 6
        if (diceNumber < 1 || diceNumber > 6)
        {
            Debug.LogError("Dice number must be between 1 and 6.");
            return;
        }

        // Reset rotation to identity
        //transform.localRotation = Quaternion.identity;

        // Move to the specified ZAxis.x position
        transform.DOLocalMoveZ(ZAxis.x, 0.2f).OnComplete(() =>
        {
            float totalRotationX = rotationSpeedX * 2f; 
            float totalRotationY = rotationSpeedY * 2f; 
            float totalRotationZ = rotationSpeedZ * 2f;
            Sequence rotationSequence = DOTween.Sequence();
            rotationSequence.Append(DOTween.To(() => transform.localEulerAngles,
                                                x => transform.localEulerAngles = x,
                                                new Vector3(transform.localEulerAngles.x + totalRotationX,
                                                transform.localEulerAngles.y + totalRotationY,
                                                transform.localEulerAngles.z+ totalRotationZ),
                                                1f).SetEase(Ease.Linear))
                            .OnComplete(() =>
                            {
                                // Set the rotation to the target position based on dice number
                                transform.localRotation = Quaternion.Euler(TargetRotation[diceNumber - 1]);

                                // Move to the specified ZAxis.y position
                                transform.DOLocalMoveZ(ZAxis.y, 0.1f);
                            });
        });
    }
}