using MalbersAnimations;
using System.Collections;
using UnityEngine;

public class HorseHurdleCollisionDetection : MonoBehaviour
{
    private static readonly int FallFromHurdleStateHash = Animator.StringToHash("FallFromHurdle");

    private const float EnterStateTimeout = 3f;
    private const float LandingTimeout = 6f;
    private const float GroundSnapDistance = 50f;

    public Animal animal;

    /// <summary>
    /// True while the horse is playing the hurdle fall and has not landed yet.
    /// Player controls (run / boost / steer) are ignored during this window so the
    /// horse drops straight down and only responds again once it touches the ground.
    /// </summary>
    public bool IsFallingFromHurdle { get; private set; }

    private Rigidbody horseRigidbody;
    //private enum ColliderType
    //{
    //    Front,
    //    Back
    //}
    //[SerializeField] private ColliderType type;
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("HurdleFall") || animal == null) return;

        PlayerPositionController positionController = animal.GetComponent<PlayerPositionController>();
        if (positionController == null || positionController.isAI || animal.AnimState != AnimTag.Jump) return;

        // Several child colliders on a horse use this component. Route all of them to
        // one active detector so the same fence contact cannot start parallel falls.
        HorseHurdleCollisionDetection coordinator =
            animal.GetComponentInChildren<HorseHurdleCollisionDetection>();

        if (coordinator != null && coordinator != this)
        {
            coordinator.BeginHurdleFall();
            return;
        }

        BeginHurdleFall();
    }

    private void BeginHurdleFall()
    {
        if (IsFallingFromHurdle || animal == null) return;

        IsFallingFromHurdle = true;
        animal.SetAnimatorFallFromHurdle();
        StartCoroutine(HandleHurdleFall());
    }

    private IEnumerator HandleHurdleFall()
    {
        Animator anim = animal.Anim;
        horseRigidbody = animal._RigidBody;

        // The Jump -> FallFromHurdle transition has an exit time, so it is not taken on
        // the same frame. Keep the flag set until the animator has actually entered (or is
        // transitioning into) the FallFromHurdle state, then clear it. Clearing the bool
        // does not cancel an in-progress transition, it only stops future re-entries.
        float timeout = EnterStateTimeout;
        while (timeout > 0f && !IsInFallFromHurdle(anim))
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (animal == null)
        {
            IsFallingFromHurdle = false;
            yield break;
        }

        animal.ResetFallFromHurdle();

        if (!IsInFallFromHurdle(anim))
        {
            IsFallingFromHurdle = false;
            yield break;
        }

        // JumpBehaviour freezes Y when it exits to an untagged state. Explicitly put the
        // horse back in physics-driven air mode so it drops instead of hovering on the fence.
        PrepareForVerticalFall();

        // Keep falling until the one-shot animation is finished. Gravity normally reaches
        // the track first; the final ground snap handles a horse resting on top of a fence.
        float landTimeout = LandingTimeout;
        while (landTimeout > 0f && animal != null && IsInFallFromHurdle(anim))
        {
            landTimeout -= Time.deltaTime;
            PrepareForVerticalFall();
            yield return new WaitForFixedUpdate();
        }

        if (animal != null)
        {
            SnapToGround();
            FinishLanding();
        }

        IsFallingFromHurdle = false;
    }

    private bool IsInFallFromHurdle(Animator anim)
    {
        if (anim == null) return false;

        for (int layer = 0; layer < anim.layerCount; layer++)
        {
            // IsName requires the full nested state-machine path. shortNameHash works for
            // all four FallFromHurdle states used by this controller.
            if (anim.GetCurrentAnimatorStateInfo(layer).shortNameHash == FallFromHurdleStateHash ||
                (anim.IsInTransition(layer) &&
                 anim.GetNextAnimatorStateInfo(layer).shortNameHash == FallFromHurdleStateHash))
                return true;
        }
        return false;
    }

    private void PrepareForVerticalFall()
    {
        if (animal == null || horseRigidbody == null) return;

        animal.IsInAir = true;
        animal.RootMotion = false;
        horseRigidbody.useGravity = true;
        horseRigidbody.constraints = RigidbodyConstraints.FreezeRotation;

        Vector3 velocity = horseRigidbody.linearVelocity;
        velocity.x = 0f;
        velocity.z = 0f;
        velocity.y = Mathf.Min(velocity.y, 0f);
        horseRigidbody.linearVelocity = velocity;
    }

    private void SnapToGround()
    {
        if (animal == null || horseRigidbody == null) return;

        Vector3 up = animal.transform.up;
        RaycastHit groundHit;
        if (!Physics.Raycast(animal.Main_Pivot_Point, -up, out groundHit,
                GroundSnapDistance * animal.ScaleFactor, animal.GroundLayer,
                QueryTriggerInteraction.Ignore))
            return;

        float standingPivotHeight = animal.height * animal.ScaleFactor;
        float verticalCorrection = groundHit.distance - standingPivotHeight;
        horseRigidbody.position -= up * verticalCorrection;
    }

    private void FinishLanding()
    {
        if (animal == null || horseRigidbody == null) return;

        horseRigidbody.linearVelocity = Vector3.zero;
        animal.IsInAir = false;
        animal.RootMotion = true;
    }

    private void OnDisable()
    {
        if (!IsFallingFromHurdle) return;

        if (animal != null) animal.ResetFallFromHurdle();
        IsFallingFromHurdle = false;
    }

    public void SetYMovementConstraint()
    {
        // Add the Y-axis movement constraint
        animal.GetComponent<Rigidbody>().constraints |= RigidbodyConstraints.FreezePositionY;
    }
    //private void OnTriggerExit(Collider other
    //{
    //    if (other.transform.CompareTag("HurdleFall") && animal.AnimState != AnimTag.Jump)
    //        Invoke("ResetTrigger", 1.5f);
    //}
    //void ResetTrigger()
    //{
    //    //Debug.Log("reset Trigger");
    //    animal.ResetFallFromHurdle();
    //}
}
