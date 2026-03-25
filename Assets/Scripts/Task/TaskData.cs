using UnityEngine;

namespace Task
{
    /// <summary>
    /// ScriptableObject containing configuration for a single task phase.
    /// Create via Assets > Create > Task > Task Phase Data
    /// </summary>
    [CreateAssetMenu(fileName = "NewTaskData", menuName = "Task/Task Phase Data")]
    public class TaskData : ScriptableObject
    {
        [Header("Display")]
        [Tooltip("Title shown in task panel")]
        public string taskTitle;

        [Tooltip("Brief description of the objective")]
        [TextArea(2, 4)]
        public string taskDescription;

        [Header("Compass Settings")]
        [Tooltip("Whether compass should highlight target during this phase")]
        public bool showCompassMarker;

        [Tooltip("Tag to find target object in scene (e.g., 'WarehousePort', 'Shipyard')")]
        public string targetTag;

        [Header("Trigger Settings")]
        [Tooltip("What triggers transition to the next phase")]
        public TaskTriggerType triggerType;

        [Tooltip("For Distance trigger: how close player must be")]
        public float triggerDistance = 15f;
    }

    /// <summary>
    /// Defines how a phase transition is triggered.
    /// </summary>
    public enum TaskTriggerType
    {
        /// <summary>Player enters dock range and presses E</summary>
        DockInteraction,

        /// <summary>Player completes cargo UI action (load/unload)</summary>
        CargoUIComplete,

        /// <summary>Player completes repair/upgrade UI</summary>
        UpgradeComplete,

        /// <summary>Automatic transition (e.g., after cutscene)</summary>
        Automatic
    }
}