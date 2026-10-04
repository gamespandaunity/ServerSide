
using Mirror;
using UnityEngine;

public class rotateBall : MonoBehaviour
{
    private Transform thisTransform;
    private Rigidbody thisRigidbody;
    private Transform innerMeshTransform;
    private mainScript mainScriptComp;
    public float smoothTime = 0.1f;

    private void Start()
    {
        thisTransform = transform;
        thisRigidbody = GetComponent<Rigidbody>();
        innerMeshTransform = thisTransform.Find("ballMesh");
        mainScriptComp = GameObject.Find("cueBall").GetComponent<mainScript>();

        innerMeshTransform.eulerAngles = new Vector3(
            Random.Range(0f, 360f),
            Random.Range(0f, 360f),
            Random.Range(0f, 360f)
        );
    }
    private void OnEnable()
    {
        if (SnokerNetwork.IsMultiplayer && NetworkServer.active)
        {
            (int.Parse(gameObject.name) - 1).Show("Enable ball index");
            if (StickManager.instance != null)
            {
                StickManager.instance.ballsActiveStatus[int.Parse(gameObject.name) - 1] = true;
            }
        }

    }

    private void OnDisable()
    {
        if (SnokerNetwork.IsMultiplayer && NetworkServer.active)
        {
            //(int.Parse(gameObject.name) - 1).Show("Disable ball index");
            if (StickManager.instance != null)
            {
                StickManager.instance.ballsActiveStatus[int.Parse(gameObject.name) - 1] = false;
            }
        }
    }
    private void FixedUpdate()
    {
        // Run physics only if we own the object or PhotonView is not present (i.e., AI or offline)
        if (!NetworkServer.active && SnokerNetwork.IsMultiplayer) thisRigidbody.isKinematic = true;
        if (thisRigidbody.isKinematic)
            return;

        thisRigidbody.AddForce(0f, -9f, 0f, ForceMode.Force);

        // Velocity damping
        float speed = thisRigidbody.linearVelocity.magnitude;

        if (speed > 0.22f)
        {
            thisRigidbody.linearVelocity *= 0.993f; // was 0.993f
        }
        else if (speed > 0.11f)
        {
            thisRigidbody.linearVelocity *= 0.99f; // was 0.99f
        }
        else if (speed > 0f)
        {
            thisRigidbody.linearVelocity *= 0.96f;
            if (thisRigidbody.linearVelocity.magnitude < 0.0055f)
            {
                thisRigidbody.linearVelocity = Vector3.zero;
            }
        }

        // Visual rotation
        if (thisRigidbody.linearVelocity.magnitude > 0.0055f) // match the cutoff
        {
            innerMeshTransform.Rotate(
                thisRigidbody.linearVelocity.z * 0.11f,
                0f,
                -thisRigidbody.linearVelocity.x * 0.11f,
                Space.World
            );
        }

    }


    private void OnCollisionEnter(Collision collision)
    {
        if (NetworkServer.active || !SnokerNetwork.IsMultiplayer)
        {
            float mySpeed = thisRigidbody.linearVelocity.magnitude;

            if (mySpeed > 0.05f && collision.collider.CompareTag("ballTag"))
            {
                Rigidbody otherRb = collision.collider.attachedRigidbody;

                if (Vector3.Angle(thisRigidbody.linearVelocity, collision.contacts[0].normal) < 4.5f &&
                    otherRb != null &&
                    otherRb.linearVelocity.magnitude < mySpeed)
                {
                    mainScriptComp.playBallHitSound(Mathf.Clamp(mySpeed / 3f, 0.04f, 1f));
                }
            }

            if (collision.collider.CompareTag("colSideTag") || collision.collider.CompareTag("colSideEndTag") || collision.collider.CompareTag("colHoleTag"))
            {
                int ballIndex = int.Parse(gameObject.name) - 1;

                if (mainScript.railHitBallArray[ballIndex] != 1)
                {
                    mainScript.railHitCountInThisShot++;
                    mainScript.railHitBallArray[ballIndex] = 1;
                }

                if (Vector3.Angle(thisRigidbody.linearVelocity, collision.contacts[0].normal) < 4f)
                {
                    mainScriptComp.playRailHitSound(thisTransform.position, Mathf.Clamp(mySpeed / 4f, 0.06f, 1f));
                }
            }
        }
    }


}
