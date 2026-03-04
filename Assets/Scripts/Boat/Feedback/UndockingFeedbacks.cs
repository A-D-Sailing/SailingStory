using MoreMountains.Feedbacks;
using UnityEngine;

namespace Boat.Feedback
{
    public class UndockingFeedbacks : MonoBehaviour
    {
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private MMF_Player _feedbackPlayer;
    
        private MMF_DestinationTransform _destinationFeedback;
    
        /// <summary>
        /// Returns true if the feedback is currently playing
        /// </summary>
        public bool IsPlaying => _feedbackPlayer != null && _feedbackPlayer.IsPlaying;

        private void Awake()
        {
            _feedbackPlayer = GetComponent<MMF_Player>();
            _destinationFeedback = _feedbackPlayer.GetFeedbackOfType<MMF_DestinationTransform>("UndockingMovement");
        }
    
        public void SetUndockDestination(Transform undockDestination)
        {
            _destinationFeedback.Destination = undockDestination;
        }
    
        public void PlayFeedbacks()
        {
            _feedbackPlayer.PlayFeedbacks();
        }
    }
}
