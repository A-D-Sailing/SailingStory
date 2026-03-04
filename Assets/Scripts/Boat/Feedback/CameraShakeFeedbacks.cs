using MoreMountains.Feedbacks;
using UnityEngine;

namespace Boat.Feedback
{
    public class CameraShakeFeedbacks : MonoBehaviour
    {
        private MMF_Player _feedbackPlayer;

        private void Awake()
        {
            _feedbackPlayer = GetComponent<MMF_Player>();
        }

        public void PlayFeedbacks(bool onlyWhenNotPlaying = false)
        {
            if (onlyWhenNotPlaying)
            {
                if (!_feedbackPlayer.IsPlaying)
                {
                    _feedbackPlayer.PlayFeedbacks();
                }
            }
            else
            {
                _feedbackPlayer.PlayFeedbacks();
            }
        }
    
    }
}
