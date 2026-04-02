using UnityEngine;

public class WindFieldZone : MonoBehaviour
{
    [Tooltip("Wind Direction")]
    public Vector3 worldWindDirection = Vector3.forward;

    [Tooltip("Base Wind Speed")]
    public float windSpeed = 8f;

    [Tooltip("Blend Intensity")] //进入风场后的混合强度
    [Range(0f, 1f)]
    public float directionalBlend = 1f;

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 dir = worldWindDirection.normalized;
        Gizmos.DrawRay(transform.position, dir * 15f);
    }
}
