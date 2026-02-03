using UnityEngine;

public class BoatParticleController : MonoBehaviour
{
    [Header("核心设置")]
    public Rigidbody boatRB;
    public float moveThreshold = 1.0f; // 速度超过多少算“动”

    [Header("特效引用")]
    // 注意：这里变成了数组 []，意味着你可以拖进去任意多个静态涟漪
    public ParticleSystem[] stationaryRipples; 
    
    // 拖尾通常还是一个
    public ParticleSystem moveTrail;        

    void Update()
    {
        // 1. 计算水平速度
        float speed = new Vector3(boatRB.linearVelocity.x, 0, boatRB.linearVelocity.z).magnitude;
        bool isMoving = speed > moveThreshold;

        // 2. 控制所有静态涟漪 (遍历数组)
        // 没动的时候(isMoving=false) -> 开启(!isMoving=true)
        foreach (var ripple in stationaryRipples)
        {
            if (ripple != null)
            {
                var emission = ripple.emission;
                emission.enabled = !isMoving;
            }
        }

        // 3. 控制动态拖尾
        // 动的时候(isMoving=true) -> 开启
        if (moveTrail != null)
        {
            var trailEmission = moveTrail.emission;
            trailEmission.enabled = isMoving;
        }
    }
}