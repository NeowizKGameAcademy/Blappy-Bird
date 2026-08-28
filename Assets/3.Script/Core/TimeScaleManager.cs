using UnityEngine;

/// <summary>
/// Time.timeScale의 단일 소유자.
/// 기획서 10.1이 요구하는 "Pause와 Time Slow 우선순위 관리"를 담당한다.
///
/// 다른 어떤 클래스도 Time.timeScale에 직접 쓰지 않는다.
/// PlayerSkillController(4A)는 SetTimeSlow만 호출한다.
///
/// 우선순위: Paused(0) > TimeSlow(0.5~0.65) > Normal(1)
///
/// GameOver는 여기에 포함하지 않는다. timeScale을 0으로 만들면
/// 기획서 18의 "0.1~0.15초 충격 연출"과 결과 UI 애니메이션이 멈춘다.
/// GameOver의 정지는 WorldScrollManager.StopScroll()과 각 시스템의 상태 게이팅이 담당한다.
/// </summary>
public sealed class TimeScaleManager : MonoBehaviour
{
    public static TimeScaleManager Instance { get; private set; }

    private bool paused;
    private float slowScale = 1f;

    public float Current => paused ? 0f : slowScale;
    public bool IsTimeSlowActive => slowScale < 1f;

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

    /// <summary>GameManager가 Paused 전환 시 호출한다.</summary>
    public void SetPaused(bool value)
    {
        paused = value;
        Apply();
    }

    /// <summary>PlayerSkillController(4A)가 Time Slow 발동 시 호출한다. 해제는 1f.</summary>
    public void SetTimeSlow(float scale)
    {
        slowScale = Mathf.Clamp(scale, 0.05f, 1f);
        Apply();
    }

    public void ResetRun()
    {
        paused = false;
        slowScale = 1f;
        Apply();
    }

    private void Apply() => Time.timeScale = Current;
}
