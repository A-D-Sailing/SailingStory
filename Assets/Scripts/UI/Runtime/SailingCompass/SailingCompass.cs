using UnityEngine;
using UnityEngine.UIElements;

namespace UIRuntime.SailingCompass
{
    public class SailingCompass : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform sailingBoat;
        
        [Header("Target")]
        public Vector3 targetPosition;
        public bool hasTarget = true;

        // UI Elements
        private VisualElement _compassDial;
        private VisualElement _targetIndicator;
        private Label _headingValue;
        private Label _targetDistance;
        private VisualElement _targetDisplay;

        // Cached values for smooth updates
        private float _currentDialRotation;
        private float _currentTargetRotation;
        
        private const float RotationSmoothSpeed = 10f;

        void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null)
            {
                Debug.LogError("[SailingCompass]: No UIDocument found!");
                return;
            }

            var root = uiDocument.rootVisualElement;
            
            // Query UI elements
            _compassDial = root.Q<VisualElement>("compass-dial");
            _targetIndicator = root.Q<VisualElement>("target-indicator");
            _headingValue = root.Q<Label>("heading-value");
            _targetDistance = root.Q<Label>("target-distance");
            _targetDisplay = root.Q<VisualElement>("target-display");

            if (_compassDial == null)
            {
                Debug.LogError("[SailingCompass]: Could not find compass-dial element!");
            }
        }

        void Update()
        {
            if (!sailingBoat || _compassDial == null) return;

            UpdateCompassDial();
            UpdateTargetIndicator();
            UpdateDisplays();
        }

        /// <summary>
        /// Updates the compass dial rotation based on boat's Y-axis rotation.
        /// The dial rotates opposite to the boat so North always points correctly.
        /// </summary>
        private void UpdateCompassDial()
        {
            // Get boat's Y rotation (heading)
            float boatHeading = sailingBoat.eulerAngles.y;
            
            // Dial rotates opposite to boat heading
            float targetDialRotation = -boatHeading;
            
            // Smooth the rotation
            _currentDialRotation = Mathf.LerpAngle(_currentDialRotation, targetDialRotation, Time.deltaTime * RotationSmoothSpeed);
            
            // Apply rotation to dial
            _compassDial.style.rotate = new Rotate(Angle.Degrees(_currentDialRotation));
        }

        /// <summary>
        /// Updates the target indicator to point towards the target position.
        /// </summary>
        private void UpdateTargetIndicator()
        {
            if (_targetIndicator == null) return;

            if (!hasTarget)
            {
                _targetIndicator.AddToClassList("hidden");
                return;
            }
            
            _targetIndicator.RemoveFromClassList("hidden");

            // Calculate direction to target in world space
            Vector3 toTarget = targetPosition - sailingBoat.position;
            toTarget.y = 0; // Flatten to horizontal plane
            
            if (toTarget.sqrMagnitude < 0.01f)
            {
                // Too close to target, hide indicator
                _targetIndicator.AddToClassList("hidden");
                return;
            }

            // Calculate world angle to target (0 = North/+Z, clockwise positive)
            float worldAngleToTarget = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
            
            // Get boat's heading
            float boatHeading = sailingBoat.eulerAngles.y;
            
            // Calculate relative angle (target direction relative to boat's forward)
            float relativeAngle = worldAngleToTarget - boatHeading;
            
            // Since the dial also rotates, we need the world angle for the indicator
            // The indicator should point in the direction of the target in world space
            // but displayed on the rotating dial, so we use the absolute world angle
            float targetRotation = worldAngleToTarget;
            
            // Smooth the rotation
            _currentTargetRotation = Mathf.LerpAngle(_currentTargetRotation, targetRotation, Time.deltaTime * RotationSmoothSpeed);
            
            // Apply rotation - account for dial rotation
            // Since dial rotates by -boatHeading, indicator needs to compensate
            _targetIndicator.style.rotate = new Rotate(Angle.Degrees(_currentTargetRotation + _currentDialRotation));
        }

        /// <summary>
        /// Updates the heading and distance text displays.
        /// </summary>
        private void UpdateDisplays()
        {
            // Update heading display
            if (_headingValue != null)
            {
                float heading = NormalizeAngle(sailingBoat.eulerAngles.y);
                _headingValue.text = $"{heading:F0}°";
            }

            // Update target distance display
            if (_targetDistance != null && _targetDisplay != null)
            {
                if (hasTarget)
                {
                    _targetDisplay.RemoveFromClassList("hidden");
                    
                    Vector3 toTarget = targetPosition - sailingBoat.position;
                    toTarget.y = 0;
                    float distance = toTarget.magnitude;
                    
                    // Format distance nicely
                    if (distance >= 1000f)
                    {
                        _targetDistance.text = $"{distance / 1000f:F1} km";
                    }
                    else
                    {
                        _targetDistance.text = $"{distance:F0} m";
                    }
                }
                else
                {
                    _targetDisplay.AddToClassList("hidden");
                    _targetDistance.text = "-- m";
                }
            }
        }

        /// <summary>
        /// Normalizes an angle to 0-360 range.
        /// </summary>
        private float NormalizeAngle(float angle)
        {
            angle %= 360f;
            if (angle < 0) angle += 360f;
            return angle;
        }

        /// <summary>
        /// Sets a new target position for the compass to track.
        /// </summary>
        public void SetTarget(Vector3 position)
        {
            targetPosition = position;
            hasTarget = true;
        }

        /// <summary>
        /// Clears the current target.
        /// </summary>
        public void ClearTarget()
        {
            hasTarget = false;
        }

        /// <summary>
        /// Gets the current heading of the boat in degrees (0-360).
        /// </summary>
        public float GetCurrentHeading()
        {
            if (sailingBoat == null) return 0f;
            return NormalizeAngle(sailingBoat.eulerAngles.y);
        }

        /// <summary>
        /// Gets the bearing to the current target in degrees (0-360).
        /// </summary>
        public float GetTargetBearing()
        {
            if (sailingBoat == null || !hasTarget) return 0f;
            
            Vector3 toTarget = targetPosition - sailingBoat.position;
            toTarget.y = 0;
            
            float angle = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
            return NormalizeAngle(angle);
        }

        /// <summary>
        /// Gets the distance to the current target.
        /// </summary>
        public float GetTargetDistance()
        {
            if (sailingBoat == null || !hasTarget) return 0f;
            
            Vector3 toTarget = targetPosition - sailingBoat.position;
            toTarget.y = 0;
            return toTarget.magnitude;
        }
    }
}
