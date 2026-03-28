using System;
using System.Collections.Generic;
using UnityEngine;
using UI.Runtime;

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

        [Tooltip("Reference to cargo UI for damage check")]
        [SerializeField] private UICargoLoad cargoLoadUI;

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

        /// <summary>
        /// Fired when the entire quest chain is completed.
        /// Game manager subscribes to show "Thanks for playing" screen.
        /// </summary>
        public event Action OnQuestCompleted;

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
            
            // Check if we've completed the quest (looping back to start)
            if (nextPhase == TaskPhase.Departure && _currentPhase == TaskPhase.Upgrade)
            {
                CompleteQuest();
                return;
            }
            
            TransitionToPhase(nextPhase);
        }

        /// <summary>
        /// Completes the entire quest chain.
        /// Call this when player finishes the Upgrade phase.
        /// </summary>
        public void CompleteQuest()
        {
            Log("Quest completed!");
            OnQuestCompleted?.Invoke();
        }

        /// <summary>
        /// Skips the current phase and advances to the next.
        /// Use when a phase is not applicable (e.g., no damage to repair).
        /// </summary>
        public void SkipCurrentPhase()
        {
            Log($"Skipping phase: {_currentPhase}");
            AdvanceToNextPhase();
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

        /// <summary>
        /// Checks if the given dock transform is the current task target.
        /// Use this to validate before advancing phases.
        /// </summary>
        public bool IsCurrentTarget(Transform dock)
        {
            if (dock == null)
                return false;

            var taskData = CurrentTaskData;
            if (taskData == null)
                return false;

            // For Departure/Loading phase: must be a Dock WITHOUT Shipyard child (warehouse port)
            if (_currentPhase == TaskPhase.Departure || _currentPhase == TaskPhase.Loading)
            {
                bool hasShipyard = HasChildWithTag(dock, "Shipyard");
                
                if (hasShipyard)
                {
                    Log("This dock has Shipyard child, not valid for Departure/Loading phase");
                    return false;
                }
                return dock.CompareTag("Dock");
            }

            // For Transport/Unloading phase: must be a Dock WITH Shipyard child
            if (_currentPhase == TaskPhase.Transport || _currentPhase == TaskPhase.Unloading)
            {
                bool hasShipyard = HasChildWithTag(dock, "Shipyard");
                
                if (!hasShipyard)
                {
                    Log("This dock does not have Shipyard child, not valid for Transport/Unloading phase");
                    return false;
                }
                return dock.CompareTag("Dock");
            }

            // For other phases: check against current target
            if (_currentTarget == null)
                return false;

            return dock == _currentTarget 
                || dock.IsChildOf(_currentTarget) 
                || _currentTarget.IsChildOf(dock);
        }

        /// <summary>
        /// Checks if a transform has any child with the specified tag.
        /// </summary>
        private bool HasChildWithTag(Transform parent, string tag)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>())
            {
                if (child != parent && child.CompareTag(tag))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Attempts to advance phase only if at the correct target.
        /// Returns true if phase was advanced.
        /// </summary>
        public bool TryAdvanceAtDock(Transform dock)
        {
            var taskData = CurrentTaskData;
            
            // If current phase doesn't need a specific target, always advance
            if (taskData == null || !taskData.showCompassMarker)
            {
                // Special case: Upgrade phase advances on depart, not on dock
                if (_currentPhase == TaskPhase.Unloading)
                {
                    // After unloading, check if we need repair
                    if (cargoLoadUI != null && cargoLoadUI.HasDamage())
                    {
                        // Go to Upgrade phase, but don't complete yet
                        AdvanceToNextPhase();
                        return true;
                    }
                    else
                    {
                        // No damage, complete quest
                        CompleteQuest();
                        return true;
                    }
                }
                
                // For Upgrade phase: don't advance on dock, wait for depart
                if (_currentPhase == TaskPhase.Upgrade)
                {
                    Log("Upgrade phase: waiting for repair and depart");
                    return false;
                }
                
                AdvanceToNextPhase();
                return true;
            }

            // Check if we're at the correct dock
            if (IsCurrentTarget(dock))
            {
                AdvanceToNextPhase();
                return true;
            }

            Log($"Docked at wrong location. Expected target: {_currentTarget?.name ?? "none"}");
            return false;
        }

        /// <summary>
        /// Called when player departs during Upgrade phase.
        /// Completes quest if repairs were made.
        /// </summary>
        public bool TryCompleteUpgrade()
        {
            if (_currentPhase != TaskPhase.Upgrade)
            {
                Log("TryCompleteUpgrade called but not in Upgrade phase");
                return false;
            }

            if (cargoLoadUI == null)
            {
                LogWarning("CargoLoadUI reference is null");
                return false;
            }

            // Check if player has repaired at least once
            if (!cargoLoadUI.HasRepairedThisSession())
            {
                Log("Cannot complete: player has not repaired anything yet");
                return false;
            }

            // repairs done, complete quest
            CompleteQuest();
            return true;
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
            // For Departure phase: find Dock WITHOUT Shipyard child
            if (_currentPhase == TaskPhase.Departure)
            {
                var allDocks = GameObject.FindGameObjectsWithTag("Dock");
                foreach (var dock in allDocks)
                {
                    if (!HasChildWithTag(dock.transform, "Shipyard"))
                    {
                        SetTarget(dock.transform);
                        Log($"Found warehouse dock (no Shipyard): {dock.name}");
                        return;
                    }
                }
                LogWarning("No warehouse dock found (Dock without Shipyard child)");
                SetTarget(null);
                return;
            }

            // For Transport phase: find Dock WITH Shipyard child
            if (_currentPhase == TaskPhase.Transport)
            {
                var allDocks = GameObject.FindGameObjectsWithTag("Dock");
                foreach (var dock in allDocks)
                {
                    if (HasChildWithTag(dock.transform, "Shipyard"))
                    {
                        SetTarget(dock.transform);
                        Log($"Found shipyard dock (has Shipyard): {dock.name}");
                        return;
                    }
                }
                LogWarning("No shipyard dock found (Dock with Shipyard child)");
                SetTarget(null);
                return;
            }

            // For other phases: use standard tag lookup
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