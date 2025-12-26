using UnityEngine;
using Unity.Cinemachine;

/// <summary>
/// FINAL STABLE VERSION
/// Dynamically adjusts the Cinemachine Camera's offset based on the boat's movement speed.
/// Uses Math.SmoothDamp to filter out physics jitter and locks zoom during turns for maximum stability.
/// </summary>
public class DynamicCameraZoom : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Cinemachine Camera controlling the view")]
    public CinemachineCamera sailingCamera; 
    
    [Tooltip("The Rigidbody of the boat to read velocity from")]
    public Rigidbody shipRigidbody;

    [Header("Zoom Settings")]
    [Tooltip("Target Z-offset when the boat is idle")]
    public float baseDistance = 20f; 
    
    [Tooltip("Target Z-offset when the boat reaches max speed")]
    public float maxDistance = 35f;  
    
    [Tooltip("The speed at which the camera reaches max distance")]
    public float maxSpeed = 15f;     
    
    [Tooltip("Controls how fast the camera visually moves to the new position (Second layer of smoothing)")]
    public float zoomDamping = 2f;     

    [Header("Stabilization")]
    [Tooltip("Time to smooth the raw physics speed. Higher values = slower reaction but smoother camera. Recommended: 0.5 - 1.0")]
    public float speedSmoothTime = 0.5f; 

    [Tooltip("If rotation speed (angular velocity) exceeds this threshold, the script stops updating the target speed to prevent zoom jitter.")]
    public float rotationThreshold = 0.1f;

    private CinemachinePositionComposer composer;
    
    // Stores the smoothed speed value (damped) instead of raw instantaneous speed
    private float _currentSmoothedSpeed; 
    // Helper variable for Mathf.SmoothDamp
    private float _speedVelocity; 

    void Start()
    {
        if (sailingCamera != null)
        {
            // Attempt to get the Position Composer component to control the offset
            composer = sailingCamera.GetComponent<CinemachinePositionComposer>();
            
            if (composer == null)
            {
                Debug.LogWarning("CinemachinePositionComposer not found on the camera!");
            }
        }
    }

    void Update()
    {
        if (composer == null || shipRigidbody == null) return;

        // 1. Get raw instantaneous speed from physics engine
        // Note: Using linearVelocity for Unity 6 compatibility (use .velocity for older Unity versions)
        float rawSpeed = shipRigidbody.linearVelocity.magnitude; 

        // 2. Rotation Check (Stabilization)
        // Check if the boat is turning. We use the absolute value of the Y-axis angular velocity.
        bool isTurning = Mathf.Abs(shipRigidbody.angularVelocity.y) > rotationThreshold;
        
        float targetSpeedToUse = rawSpeed;
        
        if (isTurning)
        {
            // If turning, ignore the raw speed drop (drag) and lock the target to the current smoothed speed.
            // This prevents the camera from zooming in and out rapidly during turns.
            targetSpeedToUse = _currentSmoothedSpeed;
        }

        // 3. Core Stabilization: Speed Smoothing
        // Use SmoothDamp to filter out micro-jitters and physics edge cases.
        // This converts choppy physics data (e.g., 14.9 -> 15.1) into a clean, continuous value.
        _currentSmoothedSpeed = Mathf.SmoothDamp(_currentSmoothedSpeed, targetSpeedToUse, ref _speedVelocity, speedSmoothTime);

        // 4. Calculate target distance
        // Map the smoothed speed to a 0-1 range
        float t = Mathf.InverseLerp(0, maxSpeed, _currentSmoothedSpeed);
        // Calculate the desired Z-offset based on the smoothed speed
        float targetDist = Mathf.Lerp(baseDistance, maxDistance, t);
        
        // 5. Apply to Camera
        Vector3 currentOffset = composer.TargetOffset;
        
        // Apply the new Z-offset (negative value to pull back)
        // Using Lerp here adds a second layer of "visual weight" to the camera movement
        currentOffset.z = Mathf.Lerp(currentOffset.z, -targetDist, Time.deltaTime * zoomDamping);
        
        composer.TargetOffset = currentOffset;
    }
}