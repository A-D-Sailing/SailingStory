using KToolkit;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BoatController : MonoBehaviour
{
    [Header("Speed")] 
    [Tooltip("Maximum speed that a boat can achieve in velocity, with direction of local front")]
    public float maxForwardSpeed;
    [Tooltip("Maximum speed that a boat can achieve in velocity, with direction of local back")]
    public float maxReverseSpeed;
    [Tooltip("Acceleration adding to forward velocity")]
    public float forwardAcceleration;
    [Tooltip("Acceleration adding to backward velocity")]
    public float reverseAcceleration;

    [Header("Turning")] 
    [Tooltip("Maximum turning speed of the boat")]
    public float maxTurnRate;
    [Tooltip("The acceleration adding to turning speed")]
    public float turnAcceleration;

    [Header("Water Drag")]
    [Tooltip("The initial drag adding to all direction to the boat to simulate water friction")]
    public float linearDrag;
    [Tooltip("Second level dragging force adding, only affect when boat in high speed movement")]
    public float quadraticDrag;
    [Tooltip("The dragging force adding to the boat's sideway movement")]
    public float lateralExtraDrag;
    [Tooltip("The dragging force adding to the boat's turning force")]
    public float angularDrag;
    [Tooltip("Second level dragging force adding, only affect when boat in high speed rotation")]
    public float angularQuadraticDrag;
    [Tooltip("The threshold value to apply dragging, to prevent visual vibration when boat is in still")]
    public float stopThreshold;
    
    [Header("Docking")]
    
    [Tooltip("The distance limit when the boat should start auto docking"), Range(0f, 200f)]
    public float autoDockDistance = 50;

    [Header("Audio")]
    public float soundSpeedThreshold = 3f;
    private BoatRoot boatRoot;
    private bool isSoundPlaying = false;

    // Components
    private Rigidbody rb;
    
    // Feel Feedbacks
    private CameraShakeFeedbacks _cameraShakePlayer;
    private DockingFeedbacks _dockingPlayer;
    
    // State Machine
    private KStateMachine<BoatController> _stateMachine;
    
    // Public accessors for states
    public Rigidbody Rigidbody => rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        boatRoot = GetComponent<BoatRoot>();

        // Cancel out the preset damping
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        
        _cameraShakePlayer = transform.GetComponentInChildren<CameraShakeFeedbacks>();
        _dockingPlayer = transform.GetComponentInChildren<DockingFeedbacks>();
        
        if (_dockingPlayer == null)
        {
            Debug.LogError("[BoatController] DockingFeedbacks component not found in children!");
        }
        
        // Initialize state machine with Normal Control as the initial state
        _stateMachine = new KStateMachine<BoatController>(this, new BoatNormalControlState(), _cameraShakePlayer);
    }


    private void FixedUpdate()
    {
        _stateMachine.currentState?.HandleFixedUpdate(this);
    }
    
    private void Update()
    {
        _stateMachine.currentState?.HandleUpdate(this);
    }

    private void OnCollisionEnter(Collision other)
    {
        // HandleCollisionEnter is not defined in the KBaseState interface
        if (_stateMachine.currentState is BoatBaseState baseState)
        {
            baseState.HandleCollisionEnter(this, other);
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Forward trigger to current state
        if (_stateMachine.currentState is BoatBaseState baseState)
        {
            baseState.HandleTriggerEnter(this, other);
        }
    }
    
    #region State Transitions
    
    /// <summary>
    /// Transition to normal sailing control state
    /// </summary>
    public void TransitionToNormalControl()
    {
        _stateMachine.TransitState<BoatNormalControlState>(_cameraShakePlayer);
    }
    
    /// <summary>
    /// Transition to docking state
    /// </summary>
    public void TransitionToDocking(Transform dockTransform)
    {
        _stateMachine.TransitState<BoatDockingState>(_dockingPlayer, dockTransform);
    }
    
    #endregion

    private void OnDestroy()
    {
        _stateMachine.DestroySelf();
    }
}    
