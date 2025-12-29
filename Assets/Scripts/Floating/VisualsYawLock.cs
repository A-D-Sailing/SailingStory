using UnityEngine;

/// <summary>
/// Fixes the boat visual rotation issue caused by the Water System.
/// 
/// The "Align Transform To Water" script overrides the entire rotation of the object to match the wave normal.
/// This script runs after the water script (in LateUpdate) to overwrite the Y-axis rotation (Yaw)
/// with the actual physics rotation from the parent Rigidbody, ensuring the boat faces the correct direction.
/// </summary>


public class VisualsYawLock : MonoBehaviour
{
    [Tooltip("Reference to the parent Boat object (the one with the Rigidbody and Controller).")]
    public Transform targetParent;

    
    void LateUpdate()
    {
        if (targetParent == null) return;

        // 1. Get the current rotation (already modified by the Water System)
        Vector3 currentEuler = transform.eulerAngles;

        // 2. Override the Y-axis (Yaw) to match the physics parent
        // 这样：X 和 Z 听水面的 (起伏)，Y 听父物体的 (转向)
        currentEuler.y = targetParent.eulerAngles.y;

        // 3. Apply the corrected rotation
        transform.eulerAngles = currentEuler;
    }
}