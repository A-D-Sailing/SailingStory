using System;
using System.Collections.Generic;
using UnityEngine;

namespace Task
{
    /// <summary>
    /// Central manager for the task/mission system.
    /// Handles phase transitions and notifies UI components via events.
    /// </summary>
    public class TaskManager : MonoBehaviour
    {
        public static TaskManager Instance { get; private set; }

        [Header("Task Configuration")]
        [Tooltip("Task data for each phase, indexed by TaskPhase enum order")]
        [SerializeField] private List<TaskData> taskDataList = new();

        [Header("Scene References")]
        [Tooltip("Reference to player's boat for position calculations")]
        [SerializeField] private Transform playerBoat;

        [Header("Debug")]
        [SerializeField] private bool debugMode = true;

        private TaskPhase _currentPhase = TaskPhase.Departure;
        private Transform _currentTarget;
        private Dictionary<TaskPhase, TaskData> _taskDataMap;

        /// <summary>
        /// Current active task phase.
        /// </summary>
        public TaskPhase CurrentPhase => _currentPhase;

        /// <summary>
        /// Task data for the current phase.
        /// </summary>
        public TaskData CurrentTaskData => GetTaskData(_currentPhase);

        /// <summary>
        /// Current target transform (for compass direction calculation).
        /// Null if current phase has no target.
        /// </summary>
        public Transform CurrentTarget => _currentTarget;

        /// <summary>
        /// Player boat transform for distance/direction calculations.
        /// </summary>
        public Transform PlayerBoat => playerBoat;

        #region Events

        /// <summary>
        /// Fired when task phase changes. Passes (oldPhase, newPhase).
        /// UI components subscribe to update their display.
        /// </summary>
        public event Action<TaskPhase, TaskPhase> OnPhaseChanged;

        /// <summary>
        /// Fired when target transform updates (e.g., new destination found).
        /// Compass subscribes to update direction indicator.
        /// </summary>
        public event Action<Transform> OnTargetChanged;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            BuildTaskDataMap();
        }

        private void Start()
        {
            // Initialize first phase
            EnterPhase(_currentPhase);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Advances to the next phase in sequence.
        /// Called by triggers (dock interaction, cargo UI, etc.)
        /// </summary>
        public void AdvanceToNextPhase()
        {
            var nextPhase = GetNextPhase(_currentPhase);
            TransitionToPhase(nextPhase);
        }

        /// <summary>
        /// Transitions directly to a specific phase.
        /// Use for special cases like game load or debug.
        /// </summary>
        public void TransitionToPhase(TaskPhase newPhase)
        {
            if (newPhase == _currentPhase)
            {
                Log($"Already in phase {newPhase}, ignoring transition");
                return;
            }

            var oldPhase = _currentPhase;
            ExitPhase(oldPhase);

            _currentPhase = newPhase;
            EnterPhase(newPhase);

            Log($"Phase transition: {oldPhase} -> {newPhase}");
            OnPhaseChanged?.Invoke(oldPhase, newPhase);
        }

        /// <summary>
        /// Gets task data for a specific phase.
        /// </summary>
        public TaskData GetTaskData(TaskPhase phase)
        {
            return _taskDataMap.TryGetValue(phase, out var data) ? data : null;
        }

        /// <summary>
        /// Calculates direction from player boat to current target.
        /// Returns Vector3.zero if no target or no boat reference.
        /// </summary>
        public Vector3 GetDirectionToTarget()
        {
            if (playerBoat == null || _currentTarget == null)
                return Vector3.zero;

            var direction = _currentTarget.position - playerBoat.position;
            direction.y = 0; // Flatten to horizontal plane
            return direction.normalized;
        }

        /// <summary>
        /// Calculates distance from player boat to current target.
        /// Returns -1 if no target or no boat reference.
        /// </summary>
        public float GetDistanceToTarget()
        {
            if (playerBoat == null || _currentTarget == null)
                return -1f;

            var offset = _currentTarget.position - playerBoat.position;
            offset.y = 0;
            return offset.magnitude;
        }

        #endregion

        #region Phase Lifecycle

        private void EnterPhase(TaskPhase phase)
        {
            var taskData = GetTaskData(phase);
            if (taskData == null)
            {
                LogWarning($"No TaskData configured for phase {phase}");
                return;
            }

            Log($"Entering phase: {phase} - {taskData.taskTitle}");

            // Find target if this phase has compass marker
            if (taskData.showCompassMarker && !string.IsNullOrEmpty(taskData.targetTag))
            {
                FindAndSetTarget(taskData.targetTag);
            }
            else
            {
                SetTarget(null);
            }
        }

        private void ExitPhase(TaskPhase phase)
        {
            Log($"Exiting phase: {phase}");
        }

        #endregion

        #region Internal Helpers

        private void BuildTaskDataMap()
        {
            _taskDataMap = new Dictionary<TaskPhase, TaskData>();

            var phases = (TaskPhase[])Enum.GetValues(typeof(TaskPhase));

            for (int i = 0; i < phases.Length && i < taskDataList.Count; i++)
            {
                if (taskDataList[i] != null)
                {
                    _taskDataMap[phases[i]] = taskDataList[i];
                }
            }

            Log($"Built task data map with {_taskDataMap.Count} entries");
        }

        private TaskPhase GetNextPhase(TaskPhase current)
        {
            var phases = (TaskPhase[])Enum.GetValues(typeof(TaskPhase));
            int currentIndex = Array.IndexOf(phases, current);
            int nextIndex = (currentIndex + 1) % phases.Length;
            return phases[nextIndex];
        }

        private void FindAndSetTarget(string targetTag)
        {
            var targetObj = GameObject.FindGameObjectWithTag(targetTag);

            if (targetObj != null)
            {
                SetTarget(targetObj.transform);
                Log($"Found target with tag '{targetTag}': {targetObj.name}");
            }
            else
            {
                LogWarning($"No GameObject found with tag '{targetTag}'");
                SetTarget(null);
            }
        }

        private void SetTarget(Transform target)
        {
            if (_currentTarget != target)
            {
                _currentTarget = target;
                OnTargetChanged?.Invoke(target);
            }
        }

        private void Log(string message)
        {
            if (debugMode)
                Debug.Log($"[TaskManager] {message}");
        }

        private void LogWarning(string message)
        {
            Debug.LogWarning($"[TaskManager] {message}");
        }

        #endregion
    }
}