using UnityEngine;

public class CameraTargetFollower : MonoBehaviour
{
    public Transform targetBoat; // 把船拖进来

    void LateUpdate()
    {
        if (targetBoat == null) return;

        // 只复制位置
        transform.position = targetBoat.position;
        
        // 强制锁定旋转为 0 (世界坐标系正方向)
        transform.rotation = Quaternion.identity; 
    }
}