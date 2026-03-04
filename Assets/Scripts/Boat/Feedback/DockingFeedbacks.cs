using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.Events;

namespace Boat.Feedback
{
    public class DockingFeedbacks : MonoBehaviour
    {
        private MMF_Player _feedbackPlayer;
    
        private MMF_DestinationTransform _destinationFeedback;
    
        /// <summary>
        /// Returns true if the feedback is currently playing
        /// </summary>
        public bool IsPlaying => _feedbackPlayer != null && _feedbackPlayer.IsPlaying;

        private void Awake()
        {
            _feedbackPlayer = GetComponent<MMF_Player>();
            _destinationFeedback = _feedbackPlayer.GetFeedbackOfType<MMF_DestinationTransform>("DockingMovement");
        }

        public void SetDockTransform(Transform dockTransform)
        {
            _destinationFeedback.Destination = dockTransform;
        }

        public void PlayFeedbacks()
        {
            _feedbackPlayer.PlayFeedbacks();
        }

        public void RegisterOnFeedbackPlayComplete(UnityAction call)
        {
            _feedbackPlayer.Events.OnComplete.AddListener(call);
        }
    
        public void UnregisterOnFeedbackPlayComplete(UnityAction call)
        {
            _feedbackPlayer.Events.OnComplete.RemoveListener(call);
        }
    }
}