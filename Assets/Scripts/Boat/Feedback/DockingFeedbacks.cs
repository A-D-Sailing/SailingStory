using MoreMountains.Feedbacks;
using UnityEngine;

public class DockingFeedbacks : MonoBehaviour
{
    private MMF_Player _feedbackPlayer;
    
    private MMF_DestinationTransform _destinationFeedback;

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
}