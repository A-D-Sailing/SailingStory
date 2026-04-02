using Boat.Feedback;
using KToolkit;
using UI.Runtime;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class BoatController : MonoBehaviour
{

    #region PROPERTIES
    
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

    [Tooltip("The script controller of cargo load/unload UI to present when finish docking")]
    public UICargoLoad cargoLoadUI;

    // Components
    private Rigidbody _rb;
    
    // Feel Feedbacks
    private CameraShakeFeedbacks _cameraShakePlayer; // the script component
    private DockingFeedbacks _dockingPlayer; // the script component
    private UndockingFeedbacks _undockingFeedbacks; // the script component
    
    // State Machine
    private KStateMachine<BoatController> _stateMachine;
    
    // Public accessors for states
    public Rigidbody Rigidbody => _rb;

    #endregion

    public WindFieldZone CurrentWindZone { get; private set; }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        // Cancel out the preset damping
        _rb.linearDamping = 0f;
        _rb.angularDamping = 0f;
        
        _cameraShakePlayer = transform.GetComponentInChildren<CameraShakeFeedbacks>();
        _dockingPlayer = transform.GetComponentInChildren<DockingFeedbacks>();
        _undockingFeedbacks = transform.GetComponentInChildren<UndockingFeedbacks>();
        
        if (_dockingPlayer == null)
        {
            Debug.LogError("[BoatController] DockingFeedbacks component not found in children!");
        }
        
        // Initialize state machine with Normal Control as the initial state
        _stateMachine = new KStateMachine<BoatController>(this, new BoatNormalControlState(), _cameraShakePlayer, cargoLoadUI);
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
        if (other.TryGetComponent(out WindFieldZone zone))
        {
            CurrentWindZone = zone;
        }

        // Forward trigger to current state
        if (_stateMachine.currentState is BoatBaseState baseState)
        {
            baseState.HandleTriggerEnter(this, other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out WindFieldZone zone) && CurrentWindZone == zone)
        {
            CurrentWindZone = null;
        }

        if (_stateMachine.currentState is BoatBaseState baseState)
        {
            baseState.HandleTriggerExit(this, other);
        }
    }

    #region State Transitions

    /// <summary>
    /// Transition to normal sailing control state
    /// </summary>
    public void TransitionToNormalControl()
    {
        _stateMachine.TransitState<BoatNormalControlState>(_cameraShakePlayer, cargoLoadUI);
    }
    
    /// <summary>
    /// Transition to docking state
    /// </summary>
    public void TransitionToDocking(Transform dock)
    {
        _stateMachine.TransitState<BoatDockingState>(_dockingPlayer, dock, cargoLoadUI);
    }
    
    /// <summary>
    /// Transition to undocking state (triggered by UI event)
    /// </summary>
    public void TransitionToUndocking(Transform undockTarget)
    {
        _stateMachine.TransitState<BoatUndockingState>(_undockingFeedbacks, undockTarget);
    }
    
    #endregion

    private void OnDestroy()
    {
        _stateMachine.DestroySelf();
    }
}    
