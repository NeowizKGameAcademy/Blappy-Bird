using UnityEngine;

/// <summary>
/// 생존 시간에 따라 월드 스크롤 속도를 올린다.
///
/// 기획서 13은 구간별 Stage로 정의했지만(0~30초 9 m/s ... 180초+ 13~14 m/s),
/// Stage 테이블 없이 연속 램프로 같은 곡선을 만든다.
/// 나중에 DifficultyManager(3)가 들어오면 이 컴포넌트를 빼고
/// 같은 자리에서 SetSpeed를 호출하면 된다. 계약은 그대로다.
///
/// 경과 시간은 스케일된 시간을 쓴다. Time Slow 중에는 난이도 상승도 함께 느려지고,
/// 기획서 15의 SurvivalTimer(deltaTime 누적)와도 기준이 같아진다.
/// </summary>
[RequireComponent(typeof(WorldScrollManager))]
public sealed class WorldSpeedRamp : MonoBehaviour, IRunResettable
{
    [Header("Speed (기획서 13)")]
    [SerializeField] private float baseSpeed = 9f;
    [SerializeField] private float maxSpeed = 14f;

    [Tooltip("이 시간(초)에 maxSpeed에 도달한다.")]
    [SerializeField] private float rampDuration = 180f;

    [Tooltip("가속 형태. 기본은 선형. 후반을 급하게 하려면 곡선을 조정한다.")]
    [SerializeField] private AnimationCurve rampShape = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    private WorldScrollManager scroll;
    private float elapsed;

    /// <summary>Playing 상태에서 누적된 시간. 표시용.</summary>
    public float Elapsed => elapsed;

    private void Awake()
    {
        scroll = GetComponent<WorldScrollManager>();
        Apply();
    }

    private void FixedUpdate()
    {
        if (scroll == null || !scroll.IsScrolling) return;

        elapsed += Time.fixedDeltaTime;
        Apply();
    }

    public void ResetRun()
    {
        elapsed = 0f;
        Apply();
    }

    private void Apply()
    {
        if (scroll == null) return;

        float t = rampDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / rampDuration);
        float shaped = rampShape != null ? rampShape.Evaluate(t) : t;

        scroll.SetSpeed(Mathf.Lerp(baseSpeed, maxSpeed, shaped));
    }
}
