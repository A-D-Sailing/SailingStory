using UnityEngine;

public class VisualsYawLock : MonoBehaviour
{
    [Tooltip("拖入父物体")]
    public Transform targetParent;

    // 在 LateUpdate 执行，确保在水面脚本计算完之后再修正
    void LateUpdate()
    {
        if (targetParent == null) return;

        // 1. 获取当前（被水面脚本修改过的）旋转
        Vector3 currentEuler = transform.eulerAngles;

        // 2. 强行把 Y 轴（朝向）改成父物体的 Y 轴
        // 这样：X 和 Z 听水面的 (起伏)，Y 听父物体的 (转向)
        currentEuler.y = targetParent.eulerAngles.y;

        // 3. 应用回去
        transform.eulerAngles = currentEuler;
    }
}