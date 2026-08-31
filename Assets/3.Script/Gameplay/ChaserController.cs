using UnityEngine;

/// <summary>
/// 플레이어를 일정 거리 뒤에서 따라다니는 추격자.
///
/// 좌표계 전제 (Docs/WorldScroll_Contract.md):
/// 플레이어는 Z=0에 고정되고 월드가 -Z로 흐른다. 따라서 "플레이어 뒤"는 -Z 쪽이다.
///
/// Chaser는 월드가 아니라 플레이어와 같은 고정 프레임에 산다.
/// ScrollingObject를 붙이면 안 된다 — 월드에 실려 뒤로 흘러가 버린다.
/// 여기서 Z는 추적 대상이 아니라 항상 offset.z로 유지되는 상수다.
///
/// 추적은 X/Y만 지수 감쇠로 지연시킨다. 즉시 따라붙으면 추격 긴장감이 없고,
/// 지연을 주면 플레이어가 급기동할 때 벌어졌다가 다시 붙는 움직임이 나온다.
/// 대신 지연이 무한정 벌어지지 않도록 maxLag로 잘라 "일정 거리"를 보장한다.
///
/// 위치만 담당한다. 표현(회전/애니메이션)은 필요해지면 별도 컴포넌트로 분리한다.
/// PlayerController와 같은 책임 분리 원칙이다.
/// </summary>
public sealed class ChaserController : MonoBehaviour, IRunResettable
{
    [Header("References")]
    [Tooltip("비워두면 씬에서 PlayerController를 자동으로 찾는다.")]
    [SerializeField] private Transform target;

    [Header("Offset")]
    [Tooltip("플레이어 기준 상대 위치. z가 음수여야 뒤에서 쫓아온다.")]
    [SerializeField] private Vector3 offset = new Vector3(0f, 0f, -6f);

    [Header("Follow")]
    [Tooltip("좌우 추적 반응 속도. 클수록 빨리 따라붙는다.")]
    [SerializeField] private float horizontalResponse = 4f;

    [Tooltip("상하 추적 반응 속도. 좌우보다 느리게 두면 무겁게 처진다.")]
    [SerializeField] private float verticalResponse = 2.5f;

    [Tooltip("목표 지점에서 벌어질 수 있는 최대 거리(m). 0이면 제한 없음.")]
    [SerializeField] private float maxLag = 4f;

    private Vector3 startPosition;
    private bool warnedNoTarget;

    private void Awake()
    {
        startPosition = transform.position;

        if (target == null)
        {
            var player = FindFirstObjectByType<PlayerController>();
            if (player != null) target = player.transform;
        }
    }

    private void Start()
    {
        SnapToTarget();
    }

    /// <summary>
    /// 플레이어는 Rigidbody Interpolate로 움직이므로 LateUpdate에서 읽어야
    /// 보간이 끝난 위치를 따라간다. FixedUpdate에서 읽으면 한 스텝 떨림이 남는다.
    /// </summary>
    private void LateUpdate()
    {
        if (target == null)
        {
            if (!warnedNoTarget)
            {
                Debug.LogWarning("ChaserController: 추적 대상이 없습니다.", this);
                warnedNoTarget = true;
            }
            return;
        }

        // Playing이 아니면 그 자리에 멈춘다. Pause에서는 deltaTime이 0이라 자동으로 멈추지만,
        // Ready/GameOver는 timeScale이 1이므로 명시적으로 막아야 한다.
        var gm = GameManager.Instance;
        if (gm == null || !gm.IsPlaying) return;

        transform.position = Step(transform.position, GetDesiredPosition(), Time.deltaTime);
    }

    /// <summary>플레이어 X/Y에 오프셋을 더한 이상적인 위치. Z는 항상 고정이다.</summary>
    private Vector3 GetDesiredPosition()
    {
        Vector3 t = target.position;
        return new Vector3(t.x + offset.x, t.y + offset.y, offset.z);
    }

    /// <summary>
    /// 한 프레임 추적. 프레임레이트와 무관한 지수 감쇠를 축별로 적용한 뒤
    /// 목표에서 벌어진 거리를 maxLag로 자른다.
    ///
    /// 순수 함수라 배치모드 검증이 dt를 직접 넣어 확인할 수 있다.
    /// </summary>
    public Vector3 Step(Vector3 current, Vector3 desired, float dt)
    {
        float x = Mathf.Lerp(current.x, desired.x, 1f - Mathf.Exp(-horizontalResponse * dt));
        float y = Mathf.Lerp(current.y, desired.y, 1f - Mathf.Exp(-verticalResponse * dt));

        var next = new Vector3(x, y, desired.z);

        // 지수 감쇠는 목표가 계속 도망가면 영원히 따라잡지 못한다.
        // 여기서 잘라야 "일정 거리"가 실제로 보장된다.
        if (maxLag > 0f)
            next = desired + Vector3.ClampMagnitude(next - desired, maxLag);

        return next;
    }

    /// <summary>지연 없이 제 위치로 붙인다. 시작과 Retry에서만 쓴다.</summary>
    private void SnapToTarget()
    {
        transform.position = target != null ? GetDesiredPosition() : startPosition;
    }

    public void ResetRun()
    {
        SnapToTarget();
    }
}
