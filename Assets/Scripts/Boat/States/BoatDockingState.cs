using Boat.Feedback;
using UI.Runtime;
using UnityEngine;
using Task;

/// <summary>
/// Docking state - handles boat behavior when docking/docked
/// Uses Feel package feedbacks for docking animation
/// Player can press SPACE to undock and return to normal control
/// </summary>
public class BoatDockingState : BoatBaseState
{
    private DockingFeedbacks _dockingFeedback;
    
    private UICargoLoad _cargoLoadUI;
    
    // Reference to current activated dock for Task
    private Transform _currentDock;
    
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
        // args[0]: (Required) DockingFeedbacks - docking feedback
        if (args.Length > 0 && args[0] is DockingFeedbacks feedback)
        {
            _dockingFeedback = feedback;
        }
        
        // args[2]: (Required) UICargoLoad - cargo load ui
        if (args.Length > 2 && args[2] is UICargoLoad cargoLoadUI && cargoLoadUI != null)
        {
            _cargoLoadUI = cargoLoadUI;
        }

        // args[1]: (Required) Transform - dock transform
        // Also finds the "Undock" child to set on the cargo UI for the depart action
        if (args.Length > 1 && args[1] is Transform dock && dock != null)
        {
            _currentDock = dock;
            _dockingFeedback.SetDockTransform(dock);

            var undockChild = dock.Find("Undock");
            if (undockChild != null)
            {
                _cargoLoadUI?.SetUndockTarget(undockChild);
            }
            else
            {
                Debug.LogWarning($"[BoatDockingState] No 'Undock' child found under dock '{dock.name}'.");
            }
        }
        
        _dockingFeedback.RegisterOnFeedbackPlayComplete(OnDockingFeedbackPlayComplete);
        
        // Play docking feedback
        _dockingFeedback.PlayFeedbacks();
    }

    private void OnDockingFeedbackPlayComplete()
    {
        _cargoLoadUI.SetCurrentDock(_currentDock);
        _cargoLoadUI.Show();
        
        // Trigger The TaskManager to the next Task Phase
        TaskManager.Instance?.TryAdvanceAtDock(_currentDock);
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
        _dockingFeedback.UnregisterOnFeedbackPlayComplete(OnDockingFeedbackPlayComplete);
        _dockingFeedback = null;
        _dockingFeedback = null;
    }
}
