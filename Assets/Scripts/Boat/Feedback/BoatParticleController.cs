using UnityEngine;

/// <summary>
/// The switches of static ripples and dynamic trailing ripples are controlled by calculating the speed threshold of the ship's movement.
/// When the ship doesn't move, activate the static ripple effect. When the speed of the ship exceeds a certain threshold, the dynamic trailing ripples effect is activated.
/// The particle system has a built-in "fade out" effect. Therefore, when you set emission.enabled = false to disable it, the particles that have already been generated will not disappear instantly but will naturally complete their lifecycle.
/// </summary>

public class BoatParticleController : MonoBehaviour
{
    [Header("Core Settings")]
    public Rigidbody boatRB;
    public float moveThreshold = 1.0f; // how many speed threshold is considered "moving"?

    [Header("VFX References")]
    public ParticleSystem[] stationaryRipples; 
    
    // RippleTrail
    public ParticleSystem moveTrail;        

    void Update()
    {
        // 1. Calculate the horizontal velocity
        float speed = new Vector3(boatRB.linearVelocity.x, 0, boatRB.linearVelocity.z).magnitude;
        bool isMoving = speed > moveThreshold;

        // 2. Control all static ripples (iterate through the array)
        // When not moving (isMoving = false) -> Enable (isMoving = true)
        foreach (var ripple in stationaryRipples)
        {
            if (ripple != null)
            {
                var emission = ripple.emission;
                emission.enabled = !isMoving;
            }
        }

        // 3. Control dynamic trailing effect
        // When moving (isMoving = true) -> Enable
        if (moveTrail != null)
        {
            var trailEmission = moveTrail.emission;
            trailEmission.enabled = isMoving;
        }
    }
}