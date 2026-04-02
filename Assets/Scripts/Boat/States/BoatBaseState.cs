using KToolkit;
using UnityEngine;

/// <summary>
/// Base state class for all boat states
/// Provides default empty implementations of state interface methods
/// </summary>
public abstract class BoatBaseState : KIBaseState<BoatController>
{
    /// <summary>
    /// Called when entering this state
    /// </summary>
    /// <param name="owner">The boat controller that owns this state</param>
    /// <param name="args">State-specific arguments (see derived class documentation for expected args)</param>
    public virtual void EnterState(BoatController owner, params object[] args) { }
    
    public virtual void HandleFixedUpdate(BoatController owner) { }
    
    public virtual void HandleUpdate(BoatController owner) { }
    
    public virtual void ExitState(BoatController owner) { }
    
    // 2D collision handlers (not used for boat, but required by interface)
    public virtual void HandleCollide2D(BoatController owner, Collision2D collision) { }
    
    public virtual void HandleTrigger2D(BoatController owner, Collider2D collider) { }
    
    // 3D collision handlers for boat physics
    public virtual void HandleCollisionEnter(BoatController owner, Collision collision) { }
    
    public virtual void HandleTriggerEnter(BoatController owner, Collider other) { }

    public virtual void HandleTriggerExit(BoatController owner, Collider other) { }
}
