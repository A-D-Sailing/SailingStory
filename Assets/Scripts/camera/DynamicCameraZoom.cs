using UnityEngine;
using Unity.Cinemachine; 

/// <summary>
/// Dynamically adjusts the Cinemachine Camera's offset based on the boat's movement speed.
/// Increases distance when sailing fast (for better visibility) and decreases distance when slow (for better control).
/// </summary>

public class DynamicCameraZoom : MonoBehaviour
{
    [Header("References")]
  
    public CinemachineCamera sailingCamera; 
    public Rigidbody shipRigidbody;

    [Header("Zoom Settings")]
    public float baseDistance = 20f; 
    public float maxDistance = 35f;  
    public float maxSpeed = 15f;     
    public float zoomSpeed = 2f;     

    // Reference to the component that controls the camera's offset
    private CinemachinePositionComposer composer;

    void Start()
    {
        if (sailingCamera != null)
        {
            // Attempt to get the Position Composer component
            composer = sailingCamera.GetComponent<CinemachinePositionComposer>();
            
           
            if (composer == null)
            {
                Debug.LogWarning("CinemachinePositionComposer not found! Make sure the camera has this component to control the offset.");
            }
        }
    }

    void Update()
    {
        if (composer == null || shipRigidbody == null) return;

        // 1. Get current speed magnitude
        // Using linearVelocity for Unity 6 compatibility
        float currentSpeed = shipRigidbody.linearVelocity.magnitude; 

        // 2. Calculate interpolation factor t based on speed (0 to 1)
        float t = Mathf.InverseLerp(0, maxSpeed, currentSpeed);
        float targetDist = Mathf.Lerp(baseDistance, maxDistance, t);
        
        // 3. Calculate target distance
        Vector3 currentOffset = composer.TargetOffset;
        
        // We modify the Z axis (negative value) to pull the camera back
        currentOffset.z = Mathf.Lerp(currentOffset.z, -targetDist, Time.deltaTime * zoomSpeed);
        
        // Apply changes to the camera
        composer.TargetOffset = currentOffset;
    }
}