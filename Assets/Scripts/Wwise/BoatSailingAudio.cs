using UnityEngine;

public class BoatSailingAudio : MonoBehaviour
{
    [Header("Wwise Events")]
    [Tooltip("Wwise event triggered when W key is pressed")]
    public AK.Wwise.Event playSailingSlow;
    [Tooltip("Wwise event triggered when W key is released")]
    public AK.Wwise.Event stopSailingSlow;

    private bool _isPlaying = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            OnSailingStart();
        }
        else if (Input.GetKeyUp(KeyCode.W))
        {
            OnSailingStop();
        }
    }

    private void OnSailingStart()
    {
        if (_isPlaying) return;
        _isPlaying = true;
        playSailingSlow.Post(gameObject);
    }

    private void OnSailingStop()
    {
        if (!_isPlaying) return;
        _isPlaying = false;
        stopSailingSlow.Post(gameObject);
    }

    private void OnDisable()
    {
        
        if (_isPlaying)
        {
            OnSailingStop();
        }
    }
}