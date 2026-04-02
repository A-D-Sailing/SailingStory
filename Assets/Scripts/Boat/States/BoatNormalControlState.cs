using Boat.Feedback;
using UI.Runtime;
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

    //Ambience Gameobject
    private GameObject _oceanAmbObject;

    // Wind audio event names
    private const string PLAY_WIND_BASE_EVENT = "Play_Wind_Base_2D";
    private const string PLAY_WIND_DIRECTIONAL_EVENT = "Play_Wind_Directional";
    private const string STOP_WIND_BASE_EVENT = "Stop_Wind_Base_2D";
    private const string STOP_WIND_DIRECTIONAL_EVENT = "Stop_Wind_Directional";

    //Ocean RTPC Control
    private const string OCEAN_AMB_RTPC_NAME = "Ocean_Amb_Control";
    private const float RTPC_UPDATE_INTERVAL = 0.1f; 
    private float lastRtpcUpdateTime;
    private float currentSpeed = 0f;

    // Wind system RTPC names
    private const string WIND_BLEND_RTPC = "Wind_Field_Blend";
    private const string WIND_LEFT_DB_RTPC = "Wind_Left_dB";
    private const string WIND_RIGHT_DB_RTPC = "Wind_Right_dB";
    private const string WIND_INTENSITY_RTPC = "Wind_Intensity";
    private const string WIND_PITCH_RTPC = "Wind_Pitch";
    private const string WIND_LPF_RTPC = "Wind_LPF";

    // Wind audio target object
    private GameObject _windAudioObject;
    private bool _windLoopStarted = false;

    private UICargoLoad _cargoLoadUI;

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
        lastRtpcUpdateTime = 0f; 
        currentSpeed = 0f;

        // args[0]: (Required) CameraShakeFeedbacks - camera shake feedback
        if (args.Length > 0 && args[0] is CameraShakeFeedbacks cameraShake)
        {
            _cameraShakePlayer = cameraShake;
        }
        
        // args[1]: (Required) UICargoLoad - cargo load ui
        if (args.Length > 1 && args[1] is UICargoLoad cargoLoadUI && cargoLoadUI != null)
        {
            _cargoLoadUI = cargoLoadUI;
        }

        // Get Boat Audio Component
        _boatMoveSound = owner.GetComponent<AkAmbient>();
        if (_boatMoveSound == null)
        {
            Debug.LogWarning("[BoatSound] AkAmbient component not found on boat!");
        }

        //Play Ambience Gameobject Check
        if (_oceanAmbObject == null)
        {
            _oceanAmbObject = GameObject.Find("Ambience"); 
        }

        // Wind directional layer is attached to the boat/player listening object
        _windAudioObject = owner.gameObject;

        StartWindLoopsIfNeeded();
        ResetWindRTPCs();

    }


    
    public override void HandleUpdate(BoatController owner)
    {
        // Check for nearby docks periodically
        if (Time.time - lastDockCheckTime > DOCK_CHECK_INTERVAL)
        {
            lastDockCheckTime = Time.time;
            CheckForNearbyDock(owner);
        }

        if (Time.time - lastRtpcUpdateTime > RTPC_UPDATE_INTERVAL)
        {
            lastRtpcUpdateTime = Time.time;
            UpdateRTPCValue(owner);
            UpdateWindFieldAudio(owner);
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

        currentSpeed = rb.linearVelocity.magnitude;
    }

    private void UpdateRTPCValue(BoatController owner)
    {
        if (_boatMoveSound == null) return;

        float clampedSpeed = Mathf.Clamp(currentSpeed, 0f, owner.maxForwardSpeed);

        AKRESULT result = AkUnitySoundEngine.SetRTPCValue(OCEAN_AMB_RTPC_NAME, clampedSpeed, _oceanAmbObject);

        if (result != AKRESULT.AK_Success)
        {
            Debug.LogError($"[BoatRTPC] Failed to set RTPC: {result}");
        }
        else
        {
            Debug.Log($"[BoatRTPC] Updated {OCEAN_AMB_RTPC_NAME} to {clampedSpeed:F2} (Speed: {currentSpeed:F2})");
        }
    }

    private void StartWindLoopsIfNeeded()
    {
        if (_windLoopStarted) return;

        if (_windAudioObject != null)
        {
            AkUnitySoundEngine.PostEvent(PLAY_WIND_DIRECTIONAL_EVENT, _windAudioObject);
        }

        _windLoopStarted = true;
    }

    private void UpdateWindFieldAudio(BoatController owner)
    {
        if (_windAudioObject == null) return;

        WindFieldZone zone = owner.CurrentWindZone;

        // Outside wind field: directional layer muted, fallback to normal 2D ambience only
        if (zone == null)
        {
            AkUnitySoundEngine.SetRTPCValue(WIND_BLEND_RTPC, 0f, _windAudioObject);
            AkUnitySoundEngine.SetRTPCValue(WIND_LEFT_DB_RTPC, -96f, _windAudioObject);
            AkUnitySoundEngine.SetRTPCValue(WIND_RIGHT_DB_RTPC, -96f, _windAudioObject);
            AkUnitySoundEngine.SetRTPCValue(WIND_INTENSITY_RTPC, 0f, _windAudioObject);
            AkUnitySoundEngine.SetRTPCValue(WIND_PITCH_RTPC, 0f, _windAudioObject);
            return;
        }

        Vector3 zoneDirection = zone.worldWindDirection.sqrMagnitude > 0.0001f
            ? zone.worldWindDirection.normalized
            : owner.transform.forward;

        Vector3 worldWindVelocity = zoneDirection * zone.windSpeed;

        // Relative wind = air velocity - boat velocity
        Vector3 relativeWind = worldWindVelocity - owner.Rigidbody.linearVelocity;

        // Convert to boat local space
        Vector3 localWind = owner.transform.InverseTransformDirection(relativeWind);

        float relativeSpeed = relativeWind.magnitude;

        // Speed threshold based on design doc:
        // low speed => nearly no perceived wind
        // high speed => strong wind perception
        float intensity01 = Mathf.InverseLerp(4.0f, 12.0f, relativeSpeed);
        intensity01 = Mathf.Clamp01(intensity01);

        // Pan logic:
        // localWind.x > 0 means wind moves toward boat's local right in local space.
        // We negate to make the "virtual wind source" feel like it wraps around the head.
        float pan = 0f;
        if (relativeSpeed > 0.001f)
        {
            pan = -localWind.x / relativeSpeed;
            pan = Mathf.Clamp(pan, -1f, 1f);
        }

        // Equal-power panning -> more stable energy
        float leftLinear = Mathf.Sqrt(0.5f * (1f - pan));
        float rightLinear = Mathf.Sqrt(0.5f * (1f + pan));

        float leftDb = LinearToDb(leftLinear);
        float rightDb = LinearToDb(rightLinear);

        // Blend:
        // 0 = only base 2D ambience
        // 100 = directional wind field layer fully active
        float blend = Mathf.Clamp01(zone.directionalBlend) * 100f;

        // Intensity and pitch are normalized control values for Wwise
        float intensity = intensity01 * 100f;
        float pitch = Mathf.Lerp(-50f, 100f, intensity01);

        AkUnitySoundEngine.SetRTPCValue(WIND_BLEND_RTPC, blend, _windAudioObject);
        AkUnitySoundEngine.SetRTPCValue(WIND_LEFT_DB_RTPC, leftDb, _windAudioObject);
        AkUnitySoundEngine.SetRTPCValue(WIND_RIGHT_DB_RTPC, rightDb, _windAudioObject);
        AkUnitySoundEngine.SetRTPCValue(WIND_INTENSITY_RTPC, intensity, _windAudioObject);
        AkUnitySoundEngine.SetRTPCValue(WIND_PITCH_RTPC, pitch, _windAudioObject);

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

    private void ResetWindRTPCs()
    {
        if (_windAudioObject == null) return;

        AkUnitySoundEngine.SetRTPCValue(WIND_BLEND_RTPC, 0f, _windAudioObject);
        AkUnitySoundEngine.SetRTPCValue(WIND_LEFT_DB_RTPC, -96f, _windAudioObject);
        AkUnitySoundEngine.SetRTPCValue(WIND_RIGHT_DB_RTPC, -96f, _windAudioObject);
        AkUnitySoundEngine.SetRTPCValue(WIND_INTENSITY_RTPC, 0f, _windAudioObject);
        AkUnitySoundEngine.SetRTPCValue(WIND_PITCH_RTPC, 0f, _windAudioObject);
    }

    private float LinearToDb(float linear)
    {
        return 20f * Mathf.Log10(Mathf.Max(linear, 0.0001f));
    }

    public override void ExitState(BoatController owner)
    {
        if (_boatMoveSound != null)
        {
            if (_isMoving)
            {
                AkUnitySoundEngine.PostEvent("Stop_Boat_Sailing_Slow", _boatMoveSound.gameObject);
                _isMoving = false;
            }

        }

        if (_oceanAmbObject != null)
        {
            AkUnitySoundEngine.SetRTPCValue(OCEAN_AMB_RTPC_NAME, 0f, _oceanAmbObject);
            
        }

        if (_windAudioObject != null)
        {
            ResetWindRTPCs();
            AkUnitySoundEngine.PostEvent(STOP_WIND_DIRECTIONAL_EVENT, _windAudioObject);
        }

        _windLoopStarted = false;
        _cameraShakePlayer = null;
        _boatMoveSound = null;
        _oceanAmbObject = null;
        _windAudioObject = null;
    }
    
    public override void HandleCollisionEnter(BoatController owner, Collision collision)
    {

        AkUnitySoundEngine.PostEvent("Play_Boat_Impact", owner.gameObject);

        if (_cameraShakePlayer && collision.gameObject.CompareTag("Obstacle"))
        {
            _cameraShakePlayer.PlayFeedbacks(true);
            _cargoLoadUI?.ResponseToHittingObstacle();
        }
    }

   
    private void CheckForNearbyDock(BoatController owner)
    {
        // Find all objects with Dock tag
        // TODO: move find objects to EnterState
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
