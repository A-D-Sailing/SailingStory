using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class BoatController : MonoBehaviour
{
    [Header("Speed")] public float maxForwardSpeed;
    public float maxReverseSpeed;
    public float forwardAcceleration;
    public float reverseAcceleration;

    [Header("Turning")] 
    public float maxTurnRate;
    public float turnAcceleration;

    [Header("Water Drag")] 
    public float linearDrag;
    public float quadraticDrag;
    public float lateralExtraDrag;
    public float angularDrag;
    public float angularQuadraticDrag;
    public float stopThreshold;
    
    private Rigidbody rb;
    private float targetYawRateRad;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Cancel out the preset damping
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
    }

    private void FixedUpdate()
    {
        var dt = Time.fixedDeltaTime;

        #region Temp Input Check

        // TEMP CODE SECTION
        // TO-DO: Adding into input actions
        var throttle = 0f;
        var steer =  0f;
        
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed) throttle += 1f;
            if (kb.sKey.isPressed) throttle -= 1f;

            if (kb.dKey.isPressed) steer += 1f;
            if (kb.aKey.isPressed) steer -= 1f;
        }

        #endregion
        
        
        // Getting vectors for boat status
        var v = rb.linearVelocity;
        var forward = transform.forward;
        var right = transform.right;
        
        var forwardSpeed = Vector3.Dot(v, forward);
        var lateralSpeed = Vector3.Dot(v, right);
        
        ApplyThrust(throttle, forwardSpeed);
        
        ApplySteering(steer, dt);
        
        ApplyWaterDrag();
        
    }

    private void ApplyThrust(float throttle, float forwardSpeed)
    {
        if (Mathf.Approximately(throttle, 0f)) return;
        
        // Setting max speeds
        var isForward = throttle > 0f;
        var accel = isForward ? forwardAcceleration : reverseAcceleration;
        var maxSpeed = isForward ? maxForwardSpeed : maxReverseSpeed;
        
        // if reaching/near max speed, linearly slow speed down
        var speed01 = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / maxSpeed);
        var throttleScale = 1f - speed01;
        throttleScale = Mathf.Clamp(throttleScale, 0f, 1f);

        var force = transform.forward * (throttle * accel * throttleScale);
        rb.AddForce(force, ForceMode.Acceleration);
    }

    private void ApplySteering(float steer, float dt)
    {
        var desiredYawRateRad = Mathf.Deg2Rad * (steer * maxTurnRate);
        var yawAccelRad = Mathf.Deg2Rad * turnAcceleration;

        targetYawRateRad = Mathf.MoveTowards(
            targetYawRateRad, 
            desiredYawRateRad, 
            yawAccelRad * dt
            );
        
        rb.AddTorque(Vector3.up * targetYawRateRad, ForceMode.Acceleration);
    }

    private void ApplyWaterDrag()
    {
        #region LinearDrag

        var v = rb.linearVelocity;
        var speed = v.magnitude;

        if (speed > stopThreshold)
        {
            var fLinear = -v * linearDrag;

            var quadFactor = speed * quadraticDrag;
            var fQuadratic = -v * quadFactor;
            
            rb.AddForce(fLinear + fQuadratic, ForceMode.Acceleration);
        }
        else
        {
            rb.linearVelocity = Vector3.zero;
        }

        #endregion

        #region LateralDamping

        var right = transform.right;
        var lateralSpeed = Vector3.Dot(v, right);

        if (Mathf.Abs(lateralSpeed) > stopThreshold)
        {
            var lateralVel = right * lateralSpeed;
            var fLateral = -lateralVel * lateralExtraDrag;
            rb.AddForce(fLateral, ForceMode.Acceleration);
        }

        #endregion

        #region AngularDrag

        var w = rb.angularVelocity;
        var wMag = w.magnitude;

        if (wMag > stopThreshold)
        {
            var tLinear = -w * angularDrag;
            var tQuadratic = Vector3.zero;
            if (angularQuadraticDrag > 0f)
            {
                var angularQuadFactor = wMag * angularQuadraticDrag;
                tQuadratic = -w * angularQuadFactor;
            }
            rb.AddTorque(tLinear + tQuadratic, ForceMode.Acceleration);
        }
        else
        {
            rb.angularVelocity = Vector3.zero;
        }

        #endregion

    }
}    