using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CarRace;
public class MenuVehicleSkidMarks : MonoBehaviour
{

    private RCC_Skidmarks skidmarks;        // Main Skidmarks Manager class.
    private float startSlipValue = .25f;        // Draw skidmarks when forward or sideways slip is bigger than this value.
    private int lastSkidmark = -1;

    private float wheelSlipAmountForward = 0f;      // Forward slip.
    private float wheelSlipAmountSideways = 0f; // Sideways slip.
    internal float totalSlip = 0f;
    //WheelFriction Curves and Stiffness.
    public WheelFrictionCurve forwardFrictionCurve;
    public WheelFrictionCurve sidewaysFrictionCurve;

    private WheelCollider _wheelCollider;
    public WheelCollider wheelCollider
    {
        get
        {
            if (_wheelCollider == null)
                _wheelCollider = GetComponent<WheelCollider>();
            return _wheelCollider;
        }
    }
    // Use this for initialization
    void Start()
    {

        if (GameObject.FindObjectOfType(typeof(RCC_Skidmarks)))
            skidmarks = GameObject.FindObjectOfType(typeof(RCC_Skidmarks)) as RCC_Skidmarks;

        forwardFrictionCurve = SetFrictionCurves(forwardFrictionCurve, .3f, 1f, .8f, 1f);
        sidewaysFrictionCurve = SetFrictionCurves(sidewaysFrictionCurve, .3f, 1f, .5f, 1f);
    }
    // Setting a new friction to WheelCollider.
    public WheelFrictionCurve SetFrictionCurves(WheelFrictionCurve curve, float extremumSlip, float extremumValue, float asymptoteSlip, float asymptoteValue)
    {

        WheelFrictionCurve newCurve = curve;

        newCurve.extremumSlip = extremumSlip;
        newCurve.extremumValue = extremumValue;
        newCurve.asymptoteSlip = asymptoteSlip;
        newCurve.asymptoteValue = asymptoteValue;

        return newCurve;

    }
    // Update is called once per frame
    void FixedUpdate()
    {
        SkidMarks();
    }

    // Creating skidmarks.
    void SkidMarks()
    {

        // First, we are getting groundhit data.
        WheelHit GroundHit;
        wheelCollider.GetGroundHit(out GroundHit);

        // Forward, sideways, and total slips.
        wheelSlipAmountForward = Mathf.Abs(GroundHit.forwardSlip);
        wheelSlipAmountSideways = Mathf.Abs(GroundHit.sidewaysSlip);

        totalSlip = 1;//Mathf.Lerp(totalSlip, (wheelSlipAmountSideways + wheelSlipAmountForward), Time.fixedDeltaTime * 3f) / 1f;

        // If scene has skidmarks manager...
        if (skidmarks)
        {


            // If slips are bigger than target value...
            if (totalSlip > startSlipValue)
            {

                Vector3 skidPoint = transform.position;
                //Debug.Log("skidmarks");

               // lastSkidmark = skidmarks.AddSkidMark(skidPoint, skidPoint, totalSlip, lastSkidmark);


            }
            else
            {

                lastSkidmark = -1;

            }

        }

    }
}
