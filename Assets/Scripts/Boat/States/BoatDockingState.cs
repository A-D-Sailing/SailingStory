using MoreMountains.Feedbacks;
using UnityEngine;

/// <summary>
/// Docking state - handles boat behavior when docking/docked
/// Uses Feel package feedbacks for docking animation
/// Player can press SPACE to undock and return to normal control
/// </summary>
public class BoatDockingState : BoatBaseState
{
    private DockingFeedbacks _dockingFeedback;
    
    /// <summary>
    /// Enter the docking state
    /// </summary>
    /// <param name="owner">The boat controller</param>
    /// <param name="args">
    /// args[0]: (Required) DockingFeedbacks - Docking feedback to play
    /// args[1]: (Required) Transform - Docking transform
    /// </param>
    public override void EnterState(BoatController owner, params object[] args)
    {
        Debug.Log("[BoatState] Entered Docking State");
        
        // args[0]: (Required) DockingFeedbacks - docking feedback
        if (args.Length > 0 && args[0] is DockingFeedbacks feedback)
        {
            _dockingFeedback = feedback;
        }

        // args[1]: (Required) Transform - dock transform
        if (args.Length > 1 && args[1] is Transform dock && dock != null)
        {
            _dockingFeedback.SetDockTransform(dock);
        }

        // Stop boat physics
        var rb = owner.Rigidbody;
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        
        // Play docking feedback
        _dockingFeedback.PlayFeedbacks();
        Debug.Log("[BoatState] Playing docking feedback");
    }
    
    public override void HandleFixedUpdate(BoatController owner)
    {
        // Keep boat stationary while docked (Feel feedback handles the animation)
        var rb = owner.Rigidbody;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }
    
    public override void ExitState(BoatController owner)
    {
        Debug.Log("[BoatState] Exited Docking State");
        _dockingFeedback = null;
    }
}
