using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 게이지 소비와 스킬 발동 (기획서 10).
/// Time.timeScale은 직접 만지지 않고 TimeScaleManager의 TimeSlow 채널만 쓴다.
/// Shield는 준비 상태(CanUseShield)만 노출한다 - 소비 분기는 4A가 PlayerCollision에 붙인다.
/// </summary>
public sealed class PlayerSkillController : MonoBehaviour, IRunResettable
{
    [SerializeField] private PlayerSkillConfig config;
    [SerializeField] private GaugeController gauge = new GaugeController();

    [Header("Shield Visual")]
    [SerializeField] private GameObject shieldVisual;

    private Coroutine timeSlowRoutine;
    private float invincibleUntil;

    public float Gauge => gauge.Current;
    public float GaugeNormalized => config != null ? gauge.Current / config.maxGauge : 0f;
    public bool IsTimeSlowActive => timeSlowRoutine != null;
    public bool CanUseTimeSlow => config != null && gauge.Current >= config.timeSlowCost && !IsTimeSlowActive;
    public bool CanUseShield => config != null && gauge.Current >= config.shieldCost && !IsShieldActive;

    /// <summary>E로 켜둔 보호막이 살아 있는가. 다음 충돌 1회를 무효화한다.</summary>
    public bool IsShieldActive { get; private set; }

    /// <summary>방어 직후 무적 (기획서 10.2: 동일 장애물 연속 충돌 방지).</summary>
    public bool IsInvincible => Time.time < invincibleUntil;

    /// <summary>HUD 바인딩용 (기획서 17). 게이지가 변하면 READY 상태도 다시 계산하면 된다.</summary>
    public event System.Action<float> OnGaugeChanged;

    /// <summary>보호막 켜짐/소비 시 발화. HUD의 실드 아이콘 상태 전환용.</summary>
    public event System.Action OnShieldChanged;

    private void OnEnable() => gauge.OnChanged += RaiseGaugeChanged;
    private void OnDisable() => gauge.OnChanged -= RaiseGaugeChanged;
    private void RaiseGaugeChanged(float v) => OnGaugeChanged?.Invoke(v);
    private void Awake()
    {
        if (shieldVisual != null)
            shieldVisual.SetActive(false);
    }
    private void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.qKey.wasPressedThisFrame) TryActivateTimeSlow();
        if (kb.eKey.wasPressedThisFrame) TryActivateShield();
    }

    /// <summary>Passzone 통과 충전. PlayerCollision이 호출한다.</summary>
    public void AddPassGauge()
    {
        if (config == null) return;
        gauge.Add(config.passZoneGain, config.maxGauge);
    }

    /// <summary>Perfect/NearMiss 판정(4A)이 호출할 범용 충전.</summary>
    public void AddGauge(float amount)
    {
        if (config == null) return;
        gauge.Add(amount, config.maxGauge);
    }

    public bool TryActivateTimeSlow()
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.IsPlaying) return false;          // 기획서 10.1: Playing 검사
        if (!CanUseTimeSlow) return false;
        if (!gauge.TrySpend(config.timeSlowCost)) return false;

        // 코루틴이 SetTimeSlow를 동기로 걸어버리므로 발동음은 그 앞에서 재생해야
        // 정상 pitch로 나온다. 게이지 부족으로 실패하면 여기까지 오지 않는다.
        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayTimeSlow();

        timeSlowRoutine = StartCoroutine(TimeSlowRoutine());
        return true;
    }

    private IEnumerator TimeSlowRoutine()
    {
        TimeScaleManager.Instance.SetTimeSlow(config.timeSlowScale);
        // scaled 시간 대기: Pause(timeScale 0) 중에는 스킬 시간도 함께 멈춘다
        yield return new WaitForSeconds(config.timeSlowDuration * config.timeSlowScale);
        EndTimeSlow();
    }

    private void EndTimeSlow()
    {
        if (timeSlowRoutine != null) StopCoroutine(timeSlowRoutine);
        timeSlowRoutine = null;
        if (TimeScaleManager.Instance != null)
            TimeScaleManager.Instance.SetTimeSlow(1f);
    }

    /// <summary>E 입력. 게이지 70을 소모해 보호막을 켜둔다.</summary>
    public bool TryActivateShield()
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.IsPlaying) return false;
        if (!CanUseShield) return false;
        if (!gauge.TrySpend(config.shieldCost)) return false;

        IsShieldActive = true;
        if (shieldVisual != null)
        {
            shieldVisual.SetActive(true);
        }
        OnShieldChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// PlayerCollision이 장애물 충돌 직전에 호출한다 (기획서 10.2).
    /// true면 이번 충돌을 무효화한다. 무적 중 연속 충돌도 여기서 흡수한다.
    /// </summary>
    public bool TryConsumeShield()
    {
        if (IsInvincible) return true;
        if (!IsShieldActive) return false;

        IsShieldActive = false;
        if (shieldVisual != null)
        {
            shieldVisual.SetActive(false);
        }
        invincibleUntil = Time.time + (config != null ? config.invincibleDuration : 0.5f);
        OnShieldChanged?.Invoke();
        return true;
    }

    public void ResetRun()
    {
        EndTimeSlow();
        IsShieldActive = false;
        invincibleUntil = 0f;
        if (shieldVisual != null)
        {
            shieldVisual.SetActive(false);
        }
        gauge.ResetRun();
        OnShieldChanged?.Invoke();
    }
}
