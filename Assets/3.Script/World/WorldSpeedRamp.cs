using UnityEngine;

/// <summary>
/// 생존 시간에 따라 월드 스크롤 속도를 올린다.
///
/// 기획서 13은 구간별 Stage로 정의했지만(0~30초 9 m/s ... 180초+ 13~14 m/s),
/// Stage 테이블 없이 연속 램프로 같은 곡선을 만든다.
///
/// 시간의 소유자는 SurvivalTimer 하나다. 난이도가 곧 생존 시간이므로
/// 여기서 시간을 따로 세지 않고 타이머를 읽는다. HUD의 TIME과
/// 기록 저장(RankingManager)도 같은 타이머를 쓴다.
/// </summary>
[RequireComponent(typeof(WorldScrollManager))]
public sealed class WorldSpeedRamp : MonoBehaviour, IRunResettable
{
    [Header("Speed (기획서 13)")]
    [SerializeField] private float baseSpeed = 9f;
    [SerializeField] private float maxSpeed = 14f;

    [Tooltip("이 시간(초)에 maxSpeed에 도달한다.")]
    [SerializeField] private float rampDuration = 180f;

    [Tooltip("가속 형태. 기획서 13 구간에 맞춘 곡선을 인스펙터에서 조정한다.")]
    [SerializeField] private AnimationCurve rampShape = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Tooltip("비워두면 같은 오브젝트에서 찾는다.")]
    [SerializeField] private SurvivalTimer timer;

    private WorldScrollManager scroll;

    private void Awake()
    {
        scroll = GetComponent<WorldScrollManager>();
        if (timer == null) TryGetComponent(out timer);
        Apply(0f);
    }

    private void FixedUpdate()
    {
        if (scroll == null || timer == null || !scroll.IsScrolling) return;
        Apply(timer.CurrentTime);
    }

    public void ResetRun() => Apply(0f);

    private void Apply(float time)
    {
        if (scroll == null) return;

        float t = rampDuration <= 0f ? 1f : Mathf.Clamp01(time / rampDuration);
        float shaped = rampShape != null ? rampShape.Evaluate(t) : t;

        scroll.SetSpeed(Mathf.Lerp(baseSpeed, maxSpeed, shaped));
    }
}
