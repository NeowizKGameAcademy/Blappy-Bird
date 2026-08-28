using UnityEngine;

/// <summary>Time.timeScale을 낮추는 독립적인 요청 출처.</summary>
public enum TimeScaleChannel
{
    /// <summary>Pause. GameManager 전용.</summary>
    Pause = 0,
    /// <summary>피격 순간 짧은 슬로모션 (기획서 6, 0.1~0.15초).</summary>
    HitStop = 1,
    /// <summary>희망의 날갯짓 (기획서 10, 약 2초).</summary>
    TimeSlow = 2
}

/// <summary>
/// Time.timeScale의 단일 소유자. 다른 어떤 클래스도 직접 쓰지 않는다.
///
/// 채널마다 독립적으로 배율을 요청하고, 실제 적용은 활성 채널의 최솟값이다.
/// 최솟값 규칙 하나로 우선순위가 자연히 해결되므로 별도 우선순위 표가 필요 없다.
///
///   평상시        1 / 1 / 1     -> 1
///   Time Slow     1 / 1 / 0.6   -> 0.6
///   Slow 중 피격   1 / 0.1 / 0.6 -> 0.1
///   피격 중 Pause  0 / 0.1 / 0.6 -> 0
///   Pause 해제     1 / 0.1 / 0.6 -> 0.1   (히트스톱과 Slow 모두 살아남음)
///
/// 채널이 서로를 덮어쓰지 않으므로, 히트스톱이 끝나도 진행 중이던
/// Time Slow의 남은 시간이 유지된다.
///
/// GameOver는 채널에 없다. timeScale을 0으로 만들면 기획서 18의
/// "0.1~0.15초 충격 연출"과 결과 Overlay 애니메이션이 함께 멈춘다.
/// </summary>
public sealed class TimeScaleManager : MonoBehaviour
{
    public static TimeScaleManager Instance { get; private set; }

    private const int ChannelCount = 3;
    private readonly float[] channels = { 1f, 1f, 1f };

    public float Current { get; private set; } = 1f;
    public bool IsTimeSlowActive => channels[(int)TimeScaleChannel.TimeSlow] < 1f;
    public bool IsPaused => channels[(int)TimeScaleChannel.Pause] <= 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        Apply();
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        Instance = null;
        Time.timeScale = 1f;
    }

    /// <summary>해당 채널의 배율을 설정한다. 1f가 해제다.</summary>
    public void Set(TimeScaleChannel channel, float scale)
    {
        channels[(int)channel] = Mathf.Clamp01(scale);
        Apply();
    }

    public float Get(TimeScaleChannel channel) => channels[(int)channel];

    /// <summary>GameManager 전용 편의 메서드.</summary>
    public void SetPaused(bool value) => Set(TimeScaleChannel.Pause, value ? 0f : 1f);

    /// <summary>PlayerSkillController(4A) 전용 편의 메서드. 해제는 1f.</summary>
    public void SetTimeSlow(float scale) => Set(TimeScaleChannel.TimeSlow, scale);

    public void ResetRun()
    {
        for (int i = 0; i < ChannelCount; i++) channels[i] = 1f;
        Apply();
    }

    private void Apply()
    {
        float min = 1f;
        for (int i = 0; i < ChannelCount; i++)
            if (channels[i] < min) min = channels[i];

        Current = min;
        Time.timeScale = min;
    }
}
