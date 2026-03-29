using UnityEngine;
using StylizedWater3.Demo; // Reference to the stylized water plugin namespace

[RequireComponent(typeof(Collider))]
public class WeatherTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [Tooltip("The name of the target weather preset (corresponds to the Name in the Lighting object, e.g., 'rain' or 'Sunset').")]
    public string targetPresetName;

    [Tooltip("Transition duration in seconds. Keep at -1 to use the default transition time set in DemoLightingController.")]
    public float customTransitionDuration = -1f;

    private void OnTriggerEnter(Collider other)
    {
        // Assuming your boat's Tag is "Player". Change this if your boat uses a different tag.
        if (other.CompareTag("Boat"))
        {
            if (DemoLightingController.Instance != null)
            {
                Debug.Log($"[Weather] Triggered weather transition to: {targetPresetName}");
                DemoLightingController.Instance.TransitionToPresetByName(targetPresetName, customTransitionDuration);
            }
            else
            {
                Debug.LogError("DemoLightingController not found in the scene!");
            }
        }
    }
}