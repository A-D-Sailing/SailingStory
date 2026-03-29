using UnityEngine;
using UnityEngine.UI;
using Task;

namespace UI.Runtime
{
    /// <summary>
    /// Manages the compass UI including background and pointer.
    /// Shows/hides based on current task target.
    /// Attach this to a parent GameObject containing the compass background and pointer images.
    /// </summary>
    public class UICompass : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The compass background image")]
        [SerializeField] private Image compassBackground;

        [Tooltip("The compass pointer image (should point up by default)")]
        [SerializeField] private Image compassPointer;

        [Header("Settings")]
        [Tooltip("How fast the pointer rotates to target (degrees per second). Set to 0 for instant.")]
        [SerializeField] private float rotationSpeed = 180f;

        private RectTransform _pointerTransform;
        private float _targetAngle = 0f;
        private bool _isVisible = false;

        private void Awake()
        {
            if (compassPointer != null)
            {
                _pointerTransform = compassPointer.GetComponent<RectTransform>();
            }
        }

        private void OnEnable()
        {
            if (TaskManager.Instance != null)
            {
                TaskManager.Instance.OnTargetChanged += OnTargetChanged;
                TaskManager.Instance.OnQuestCompleted += OnQuestCompleted;

                // Initialize visibility based on current target
                UpdateVisibility(TaskManager.Instance.CurrentTarget);
            }
        }

        private void OnDisable()
        {
            if (TaskManager.Instance != null)
            {
                TaskManager.Instance.OnTargetChanged -= OnTargetChanged;
                TaskManager.Instance.OnQuestCompleted -= OnQuestCompleted;
            }
        }

        private void Start()
        {
            // Fallback if TaskManager wasn't ready in OnEnable
            if (TaskManager.Instance != null)
            {
                TaskManager.Instance.OnTargetChanged -= OnTargetChanged;
                TaskManager.Instance.OnQuestCompleted -= OnQuestCompleted;
                TaskManager.Instance.OnTargetChanged += OnTargetChanged;
                TaskManager.Instance.OnQuestCompleted += OnQuestCompleted;

                UpdateVisibility(TaskManager.Instance.CurrentTarget);
            }
        }

        private void Update()
        {
            if (!_isVisible) return;
            if (TaskManager.Instance == null || TaskManager.Instance.CurrentTarget == null) return;

            UpdateTargetAngle();
            RotatePointer();
        }

        private void OnTargetChanged(Transform newTarget)
        {
            UpdateVisibility(newTarget);
        }

        private void OnQuestCompleted()
        {
            SetCompassVisible(false);
        }

        private void UpdateVisibility(Transform target)
        {
            SetCompassVisible(target != null);
        }

        private void SetCompassVisible(bool visible)
        {
            _isVisible = visible;

            if (compassBackground != null)
            {
                compassBackground.gameObject.SetActive(visible);
            }

            if (compassPointer != null)
            {
                compassPointer.gameObject.SetActive(visible);
            }
        }

        private void UpdateTargetAngle()
        {
            var direction = TaskManager.Instance.GetDirectionToTarget();

            if (direction == Vector3.zero)
                return;

            // Calculate angle: Atan2(x, z) gives angle from +Z axis (north)
            // Negative because UI rotation is clockwise
            _targetAngle = -Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        }

        private void RotatePointer()
        {
            if (_pointerTransform == null) return;

            if (rotationSpeed <= 0)
            {
                // Instant rotation
                _pointerTransform.localRotation = Quaternion.Euler(0, 0, _targetAngle);
            }
            else
            {
                // Smooth rotation
                float currentAngle = _pointerTransform.localEulerAngles.z;
                float newAngle = Mathf.MoveTowardsAngle(currentAngle, _targetAngle, rotationSpeed * Time.deltaTime);
                _pointerTransform.localRotation = Quaternion.Euler(0, 0, newAngle);
            }
        }
    }
}