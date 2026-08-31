using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 희망의 날갯짓(Time Slow) 발동 중 화면 그레이딩.
/// 쿨톤 틴트 + 채도 감소 + 비네트가 담긴 Volume의 weight만 움직이는 표시 계층이다.
/// 스킬 로직에는 관여하지 않고 TimeScaleManager의 TimeSlow 채널 상태를 그대로 따라간다 -
/// 발동 주체가 무엇이든(스킬, 추후 다른 연출) 채널이 느려지면 화면도 물든다.
///
/// 페이드는 unscaledDeltaTime 기준이다. 발동 순간 timeScale이 0.6으로 떨어지는데,
/// 스케일된 시간으로 페이드하면 진입 연출까지 함께 느려져 굼떠 보인다.
/// Pause 중에도 weight는 움직이지만 화면이 멈춰 있어 보이지 않는다.
/// </summary>
[RequireComponent(typeof(Volume))]
public sealed class TimeSlowScreenEffect : MonoBehaviour, IRunResettable
{
    [Tooltip("발동 시 페이드 인 속도. 클수록 빨리 물든다")]
    [SerializeField] private float fadeInResponse = 18f;

    [Tooltip("해제 시 페이드 아웃 속도. 진입보다 느리게 두면 여운이 남는다")]
    [SerializeField] private float fadeOutResponse = 8f;

    private Volume volume;

    private void Awake()
    {
        volume = GetComponent<Volume>();
        volume.weight = 0f;
    }

    private void Update()
    {
        var tsm = TimeScaleManager.Instance;
        bool active = tsm != null && tsm.IsTimeSlowActive;

        float target = active ? 1f : 0f;
        float response = active ? fadeInResponse : fadeOutResponse;

        // 프레임레이트 무관 지수 감쇠 (PlayerAnimationController.StepRoll과 같은 패턴)
        float next = Mathf.Lerp(volume.weight, target,
            1f - Mathf.Exp(-response * Time.unscaledDeltaTime));

        if (Mathf.Abs(next - target) < 0.001f) next = target;
        volume.weight = next;
    }

    public void ResetRun()
    {
        volume.weight = 0f;
    }
}
