using UnityEngine;

/// <summary>
/// A "Ghost Target" script that follows the boat's position but ignores its rotation.
/// This allows the camera to follow the boat without spinning around when the boat turns.
/// </summary>

public class CameraTargetFollower : MonoBehaviour
{
    public Transform targetBoat; 

    void LateUpdate()
    {
        if (targetBoat == null) return;

        // Sync position with the boat
        transform.position = targetBoat.position;
        
        // Force rotation to identity (World Zero) to prevent the camera from rotating with the boat.
        // This ensures a fixed viewing angle regardless of the boat's heading.
        transform.rotation = Quaternion.identity; 
    }
}