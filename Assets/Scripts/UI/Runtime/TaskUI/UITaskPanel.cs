using UnityEngine;
using UnityEngine.UIElements;
using Task;

namespace UI.Runtime
{
    /// <summary>
    /// Displays current task information in the top-right corner.
    /// Subscribes to TaskManager events to update display.
    /// </summary>
    public class UITaskPanel : MonoBehaviour
    {
        private VisualElement _root;
        private Label _titleLabel;
        private Label _descriptionLabel;

        private void Awake()
        {
            var uiDocument = GetComponent<UIDocument>();
            _root = uiDocument.rootVisualElement.Q<VisualElement>("task-panel-root");
            _titleLabel = _root.Q<Label>("task-title");
            _descriptionLabel = _root.Q<Label>("task-description");
        }

        private void OnEnable()
        {
            // TaskManager might not be ready yet, try to subscribe
            TrySubscribe();
        }

        private void TrySubscribe()
        {
            if (TaskManager.Instance != null)
            {
                Debug.Log("[UITaskPanel] Subscribing to TaskManager events");
                TaskManager.Instance.OnPhaseChanged += OnPhaseChanged;
                TaskManager.Instance.OnQuestCompleted += OnQuestCompleted;
                
                // Initialize with current task
                UpdateDisplay(TaskManager.Instance.CurrentTaskData);
            }
            else
            {
                Debug.LogWarning("[UITaskPanel] TaskManager.Instance is null, will retry in Start");
            }
        }

        private void OnDisable()
        {
            if (TaskManager.Instance != null)
            {
                TaskManager.Instance.OnPhaseChanged -= OnPhaseChanged;
                TaskManager.Instance.OnQuestCompleted -= OnQuestCompleted;
            }
        }

        private void Start()
        {
            // Fallback initialization if TaskManager wasn't ready in OnEnable
            if (TaskManager.Instance != null)
            {
                // Re-subscribe in case OnEnable failed
                TaskManager.Instance.OnPhaseChanged -= OnPhaseChanged;
                TaskManager.Instance.OnQuestCompleted -= OnQuestCompleted;
                TaskManager.Instance.OnPhaseChanged += OnPhaseChanged;
                TaskManager.Instance.OnQuestCompleted += OnQuestCompleted;
                
                Debug.Log("[UITaskPanel] Subscribed in Start");
                UpdateDisplay(TaskManager.Instance.CurrentTaskData);
            }
        }

        private void OnPhaseChanged(TaskPhase oldPhase, TaskPhase newPhase)
        {
            Debug.Log($"[UITaskPanel] OnPhaseChanged: {oldPhase} -> {newPhase}");
            var taskData = TaskManager.Instance.GetTaskData(newPhase);
            PlayFadeTransition(taskData);
        }

        private void OnQuestCompleted()
        {
            PlayFadeTransition(null);
        }

        private void PlayFadeTransition(TaskData newTaskData)
        {
            // Fade out
            _root.AddToClassList("fade-out");
            _root.RemoveFromClassList("fade-in");

            // After fade out, update content and fade in
            _root.schedule.Execute(() =>
            {
                UpdateDisplay(newTaskData);
                _root.RemoveFromClassList("fade-out");
                _root.AddToClassList("fade-in");
            }).ExecuteLater(200);
        }

        private void UpdateDisplay(TaskData taskData)
        {
            if (taskData == null)
            {
                _titleLabel.text = "Quest Complete";
                _descriptionLabel.text = "Thanks for playing!";
                return;
            }

            _titleLabel.text = taskData.taskTitle;
            _descriptionLabel.text = taskData.taskDescription;
        }
    }
}