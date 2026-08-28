using UnityEngine;

/// <summary>
/// 월드 스크롤에 실려 -Z로 흐르고, 후방 기준선을 넘으면 스스로 풀에 돌아간다.
///
/// 프레임을 뒤집으면서 빠르게 움직이는 쪽이 플레이어에서 이 오브젝트로 넘어왔다.
/// Kinematic Rigidbody + ContinuousSpeculative + MovePosition이 아니면
/// 14 m/s에서 플레이어를 그냥 통과한다.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public sealed class ScrollingObject : MonoBehaviour
{
    [Tooltip("이 Z보다 뒤로 가면 회수한다. 플레이어는 Z=0에 있다.")]
    [SerializeField] private float despawnZ = -20f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void FixedUpdate()
    {
        var scroll = WorldScrollManager.Instance;
        if (scroll == null) return;

        float delta = scroll.FixedScrollDelta;
        if (delta <= 0f) return;

        rb.MovePosition(rb.position + Vector3.back * delta);

        if (rb.position.z <= despawnZ && PoolManager.Instance != null)
            PoolManager.Instance.Release(gameObject);
    }
}
