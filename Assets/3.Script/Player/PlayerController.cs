using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어 이동. 트레드밀 구조이므로 X/Y만 움직이고 Z는 0에 고정된다.
///
/// 기획서 4·5 준수 사항:
///  - 입력은 Update에서 수집하고 FixedUpdate에서 적용한다
///  - Space는 홀드가 아니라 눌린 순간만 인정한다
///  - Flap은 verticalVelocity를 더하는 것이 아니라 flapVelocity로 재설정한다
///  - Rigidbody.useGravity=false. customGravity를 누적하고 maxFallSpeed에서 Clamp
///
/// 수치는 전부 PlayerMovementConfig(SO)에 있다. ObstacleSpawner의 도달 가능
/// 수치도 같은 에셋 기준이므로 여기서 임의의 상수를 더하지 않는다.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public sealed class PlayerController : MonoBehaviour, IRunResettable
{
    [Header("References")]
    [SerializeField] private PlayerMovementConfig config;

    private Rigidbody rb;
    private Vector3 startPosition;

    private float verticalVelocity;
    private float horizontalVelocity;

    /// <summary>표현 계층(PlayerAnimationController)이 읽는다. 물리에는 영향 없음.</summary>
    public float VerticalVelocity => verticalVelocity;
    public float HorizontalVelocity => horizontalVelocity;
    public float MaxHorizontalSpeed => config != null ? config.maxHorizontalSpeed : 1f;

    // Update에서 채우고 FixedUpdate에서 소비한다
    private bool flapQueued;
    private float horizontalInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
        startPosition = transform.position;
    }

    private void Update()
    {
        var kb = Keyboard.current;
        if (kb != null)
        {
            // wasPressedThisFrame: 눌린 순간만. FixedUpdate가 소비할 때까지 큐에 유지한다
            if (kb.spaceKey.wasPressedThisFrame) flapQueued = true;

            horizontalInput = 0f;
            if (kb.leftArrowKey.isPressed || kb.aKey.isPressed) horizontalInput -= 1f;
            if (kb.rightArrowKey.isPressed || kb.dKey.isPressed) horizontalInput += 1f;
        }
    }

    private void FixedUpdate()
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.IsPlaying)
        {
            flapQueued = false;          // Intro/Paused/GameOver에서 큐를 버린다 (기획서 5 상태 차단)
            rb.linearVelocity = Vector3.zero;
            return;
        }

        float dt = Time.fixedDeltaTime;

        // 수직: Flap은 재설정, 아니면 중력 누적 후 Clamp
        if (flapQueued)
        {
            verticalVelocity = config.flapVelocity;
            flapQueued = false;
        }
        else
        {
            verticalVelocity = Mathf.Max(
                verticalVelocity + config.customGravity * dt,
                config.maxFallSpeed);
        }

        // 수평: 가속/감속 보간
        horizontalVelocity = Mathf.MoveTowards(
            horizontalVelocity,
            horizontalInput * config.maxHorizontalSpeed,
            config.horizontalAccel * dt);

        // 경계: 이번 스텝의 예측 위치가 영역을 벗어나면 딱 경계까지만 가는 속도로 줄인다.
        // 사후 되돌리기 방식은 경계를 1스텝 침범할 뿐 아니라, 바닥에 붙은 상태에서
        // Flap으로 설정한 상승 속도까지 지워버리는 버그가 있었다.
        Vector3 pos = rb.position;
        Vector3 next = pos + new Vector3(horizontalVelocity, verticalVelocity, 0f) * dt;

        // 바닥은 차단이 아니라 사망이다. 좌우와 천장만 막는다.
        if (next.y <= config.MinY)
        {
            gm.EndGame();
            rb.linearVelocity = Vector3.zero;
            return;
        }

        Vector3 clamped = PlayArea.Clamp(next, config);
        if (!Mathf.Approximately(clamped.x, next.x)) horizontalVelocity = (clamped.x - pos.x) / dt;
        if (!Mathf.Approximately(clamped.y, next.y)) verticalVelocity = (clamped.y - pos.y) / dt;

        rb.linearVelocity = new Vector3(horizontalVelocity, verticalVelocity, 0f);
    }

    /// <summary>
    /// 게이트와 충돌하면 GameOver. Shield(4A)가 붙으면 여기서
    /// TryConsumeShield를 먼저 호출하는 분기가 들어간다.
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.IsPlaying) gm.EndGame();
    }

    public void ResetRun()
    {
        transform.position = startPosition;
        rb.position = startPosition;
        verticalVelocity = 0f;
        horizontalVelocity = 0f;
        flapQueued = false;
        horizontalInput = 0f;
        rb.linearVelocity = Vector3.zero;
    }
}
