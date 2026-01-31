using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Normal boat control state - handles regular sailing movement
/// Player can freely control throttle and steering
/// </summary>
public class BoatNormalControlState : BoatBaseState
{
    private float targetYawRateRad;
    
    // Dock detection
    private const float DOCK_CHECK_INTERVAL = 0.2f; // Check every 0.2 seconds for performance
    private float lastDockCheckTime;
    
    private CameraShakeFeedbacks _cameraShakePlayer;

    // Add Audio Value
    private AkAmbient _boatMoveSound;  
    private bool _isMoving = false;
    private const float MOVE_THRESHOLD = 0.1f;

    /// <summary>
    /// Enter the normal control state
    /// </summary>
    /// <param name="owner">The boat controller</param>
    /// <param name="args">
    /// args[0]: (required) CameraShakeFeedbacks - Camera shake feedback player for collision effects
    /// </param>
    public override void EnterState(BoatController owner, params object[] args)
    {
        targetYawRateRad = 0f;
        lastDockCheckTime = 0f;
        
        // args[0]: (Required) CameraShakeFeedbacks - camera shake feedback
        if (args.Length > 0 && args[0] is CameraShakeFeedbacks cameraShake)
        {
            _cameraShakePlayer = cameraShake;
        }

        // Get Boat Audio Component
        _boatMoveSound = owner.GetComponent<AkAmbient>();
        if (_boatMoveSound == null)
        {
            Debug.LogWarning("[BoatSound] AkAmbient component not found on boat!");
        }

        Debug.Log("[BoatState] Entered Normal Control State");
    }
    
    public override void HandleUpdate(BoatController owner)
    {
        // Check for nearby docks periodically
        if (Time.time - lastDockCheckTime > DOCK_CHECK_INTERVAL)
        {
            lastDockCheckTime = Time.time;
            CheckForNearbyDock(owner);
        }
    }

    public override void HandleFixedUpdate(BoatController owner)
    {
        var dt = Time.fixedDeltaTime;

        // Get input
        var (throttle, steer) = GetInput();

        // Getting vectors for boat status
        var rb = owner.Rigidbody;
        var v = rb.linearVelocity;
        var forward = owner.transform.forward;

        var forwardSpeed = Vector3.Dot(v, forward);

        ApplyThrust(owner, throttle, forwardSpeed);
        ApplySteering(owner, steer, dt);
        ApplyWaterDrag(owner);

        CheckMovementAndPlaySound(rb);
    }

    private void CheckMovementAndPlaySound(Rigidbody rb)
    {
        if (_boatMoveSound == null) return;

        float linearSpeed = rb.linearVelocity.magnitude;
        float angularSpeed = rb.angularVelocity.magnitude;

        bool isCurrentlyMoving = (linearSpeed > MOVE_THRESHOLD) || (angularSpeed > 0.1f);

        if (isCurrentlyMoving && !_isMoving)
        {
            AkUnitySoundEngine.PostEvent("Play_Boat_Sailing_Slow", _boatMoveSound.gameObject);
            Debug.Log("[BoatSound] Started moving - Playing Play_Boat_Sailing_Slow");
        }
        else if (!isCurrentlyMoving && _isMoving)
        {
            AkUnitySoundEngine.PostEvent("Stop_Boat_Sailing_Slow", _boatMoveSound.gameObject);
            Debug.Log("[BoatSound] Stopped moving - Stopping Play_Boat_Sailing_Slow");
        }

        _isMoving = isCurrentlyMoving;
    }
    public override void ExitState(BoatController owner)
    {
        if (_boatMoveSound != null && _isMoving)
        {
            AkUnitySoundEngine.PostEvent("Stop_Boat_Sailing_Slow", _boatMoveSound.gameObject);
            _isMoving = false;
        }

        Debug.Log("[BoatState] Exited Normal Control State");
        _cameraShakePlayer = null;
        _boatMoveSound = null;
    }
    
    public override void HandleCollisionEnter(BoatController owner, Collision collision)
    {
        if (_cameraShakePlayer && collision.gameObject.CompareTag("Obstacle"))
        {
            _cameraShakePlayer.PlayFeedbacks(true);
        }
    }
    
    private void CheckForNearbyDock(BoatController owner)
    {
        // Find all objects with Dock tag
        var docks = GameObject.FindGameObjectsWithTag("Dock");
        
        foreach (var dock in docks)
        {
            float distance = Vector3.Distance(owner.transform.position, dock.transform.position);
            if (distance <= owner.autoDockDistance)
            {
                Debug.Log($"[BoatState] Dock detected within range: {dock.name}");
                owner.TransitionToDocking(dock.transform);
                return;
            }
        }
    }
    
    private (float throttle, float steer) GetInput()
    {
        float throttle = 0f;
        float steer = 0f;
        
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.wKey.isPressed) throttle += 1f;
            if (kb.sKey.isPressed) throttle -= 1f;
            
            if (kb.dKey.isPressed) steer += 1f;
            if (kb.aKey.isPressed) steer -= 1f;
        }
        
        return (throttle, steer);
    }
    
    private void ApplyThrust(BoatController owner, float throttle, float forwardSpeed)
    {
        if (Mathf.Approximately(throttle, 0f)) return;
        
        var rb = owner.Rigidbody;
        var isForward = throttle > 0f;
        var accel = isForward ? owner.forwardAcceleration : owner.reverseAcceleration;
        var maxSpeed = isForward ? owner.maxForwardSpeed : owner.maxReverseSpeed;
        
        // if reaching/near max speed, linearly slow speed down
        var speed01 = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / maxSpeed);
        var throttleScale = 1f - speed01;
        throttleScale = Mathf.Clamp(throttleScale, 0f, 1f);
        
        var force = owner.transform.forward * (throttle * accel * throttleScale);
        rb.AddForce(force, ForceMode.Acceleration);
    }
    
    private void ApplySteering(BoatController owner, float steer, float dt)
    {
        var rb = owner.Rigidbody;
        var desiredYawRateRad = Mathf.Deg2Rad * (steer * owner.maxTurnRate);
        var yawAccelRad = Mathf.Deg2Rad * owner.turnAcceleration;
        
        targetYawRateRad = Mathf.MoveTowards(
            targetYawRateRad,
            desiredYawRateRad,
            yawAccelRad * dt
        );
        
        rb.AddTorque(Vector3.up * targetYawRateRad, ForceMode.Acceleration);
    }
    
    private void ApplyWaterDrag(BoatController owner)
    {
        var rb = owner.Rigidbody;
        
        #region LinearDrag
        
        var v = rb.linearVelocity;
        var speed = v.magnitude;
        
        if (speed > owner.stopThreshold)
        {
            var fLinear = -v * owner.linearDrag;
            
            var quadFactor = speed * owner.quadraticDrag;
            var fQuadratic = -v * quadFactor;
            
            rb.AddForce(fLinear + fQuadratic, ForceMode.Acceleration);
        }
        else
        {
            rb.linearVelocity = Vector3.zero;
        }
        
        #endregion
        
        #region LateralDamping
        
        var right = owner.transform.right;
        var lateralSpeed = Vector3.Dot(v, right);
        
        if (Mathf.Abs(lateralSpeed) > owner.stopThreshold)
        {
            var lateralVel = right * lateralSpeed;
            var fLateral = -lateralVel * owner.lateralExtraDrag;
            rb.AddForce(fLateral, ForceMode.Acceleration);
        }
        
        #endregion
        
        #region AngularDrag
        
        var w = rb.angularVelocity;
        var wMag = w.magnitude;
        
        if (wMag > owner.stopThreshold)
        {
            var tLinear = -w * owner.angularDrag;
            var tQuadratic = Vector3.zero;
            if (owner.angularQuadraticDrag > 0f)
            {
                var angularQuadFactor = wMag * owner.angularQuadraticDrag;
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
