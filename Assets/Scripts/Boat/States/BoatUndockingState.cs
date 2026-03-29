using Boat.Feedback;
using UnityEngine;

/// <summary>
/// Undocking state - handles boat behavior when leaving a dock
/// Uses DockingFeedbacks for auto movement animation
/// Triggered by UI event (e.g. button click)
/// Transitions to normal control when undocking is complete
/// </summary>
public class BoatUndockingState : BoatBaseState
{
    private UndockingFeedbacks _undockingFeedback;
    
    /// <summary>
    /// Enter the undocking state
    /// </summary>
    /// <param name="owner">The boat controller</param>
    /// <param name="args">
    /// args[0]: (Required) DockingFeedbacks - Docking feedback to play undocking
    /// </param>
    public override void EnterState(BoatController owner, params object[] args)
    {
        // TODO: we are still use docking feedback to implement undocking,
        //   should create a undocking feedback or rename current docking feedback to automove feedback
        
        // args[0]: (Required) DockingFeedbacks - docking feedback
        if (args.Length > 0 && args[0] is UndockingFeedbacks feedback)
        {
            _undockingFeedback = feedback;
        }
        
        // args[1]: (Required) Transform - dock transform
        if (args.Length > 1 && args[1] is Transform undockTarget && undockTarget != null)
        {
            _undockingFeedback.SetUndockDestination(undockTarget);
        }
        
        // as undocking state can only be transition from docking state, the velocity has already been set to zero
        // // Keep boat stationary during undocking animation
        // var rb = owner.Rigidbody;
        // if (rb != null)
        // {
        //     rb.linearVelocity = Vector3.zero;
        //     rb.angularVelocity = Vector3.zero;
        // }
        
        // Play undocking feedback (reverse of docking)
        _undockingFeedback.PlayFeedbacks();
    }
    
    public override void HandleFixedUpdate(BoatController owner)
    {
        // Keep boat stationary while undocking animation plays
        var rb = owner.Rigidbody;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
    
    public override void HandleUpdate(BoatController owner)
    {
        // Check if undocking feedback has finished
        if (_undockingFeedback != null && !_undockingFeedback.IsPlaying)
        {
            owner.TransitionToNormalControl();
        }
    }
    
    public override void ExitState(BoatController owner)
    {
        _undockingFeedback.SetUndockDestination(null);
        _undockingFeedback = null;
    }
}
