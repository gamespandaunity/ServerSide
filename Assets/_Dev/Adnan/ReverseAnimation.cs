using UnityEngine;

public class ReverseAnimation : MonoBehaviour
{
    private Animator animator;

    private void Start()
    {
        // Get the Animator component attached to the GameObject
        animator = GetComponent<Animator>();
        reverseAnimation();
    }

    public void reverseAnimation()
    {
        animator.speed = -1f;
    }
    public void forwordAnimation()
    {
        animator.speed = 1f;
    }

   
}
