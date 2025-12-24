using UnityEngine;
using Unity.Cinemachine; 

public class DynamicCameraZoom : MonoBehaviour
{
    [Header("References")]
  
    public CinemachineCamera sailingCamera; 
    public Rigidbody shipRigidbody;

    [Header("Zoom Settings")]
    public float baseDistance = 20f; 
    public float maxDistance = 35f;  
    public float maxSpeed = 15f;     
    public float zoomSpeed = 2f;     

  
    private CinemachinePositionComposer composer;

    void Start()
    {
        if (sailingCamera != null)
        {
            // 尝试获取位置控制组件
            composer = sailingCamera.GetComponent<CinemachinePositionComposer>();
            
           
            if (composer == null)
            {
                Debug.LogWarning("未找到 Position Composer，请检查相机是否添加了该组件用于控制 Offset");
            }
        }
    }

    void Update()
    {
        if (composer == null || shipRigidbody == null) return;

        // 1. 获取速度
        float currentSpeed = shipRigidbody.linearVelocity.magnitude; // Unity 6 建议用 linearVelocity，旧版用 velocity

        // 2. 计算目标插值 t
        float t = Mathf.InverseLerp(0, maxSpeed, currentSpeed);
        float targetDist = Mathf.Lerp(baseDistance, maxDistance, t);
        
        // 3. 修改 Offset (新版属性叫 TargetOffset)
        Vector3 currentOffset = composer.TargetOffset;
        
        // 假设我们只拉远 Z 轴 (负数)
        // Lerp 插值让变化更平滑
        currentOffset.z = Mathf.Lerp(currentOffset.z, -targetDist, Time.deltaTime * zoomSpeed);
        
   
        composer.TargetOffset = currentOffset;
    }
}