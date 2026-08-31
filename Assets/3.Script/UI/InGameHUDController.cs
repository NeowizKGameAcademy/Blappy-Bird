using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인게임 HUD (기획서 17).
/// 계산하지 않고 각 시스템의 이벤트를 받아 표시만 갱신한다 (기획서 16 원칙).
/// 매 프레임 갱신은 TIME(기획서 17 허용)과 게이지 바 보간뿐이다 -
/// 게이지의 목표값은 이벤트로만 받고, Update는 그 목표로 따라가기만 한다.
///
/// 배치 원칙: 중앙 시야를 비우고 TIME·HAPPINESS는 상단,
/// Gauge와 스킬 준비 상태는 하단 가장자리 (기획서 17).
/// </summary>
public sealed class InGameHUDController : MonoBehaviour
{
    [Header("Systems")]
    [SerializeField] private SurvivalTimer timer;
    [SerializeField] private RankingManager ranking;
    [SerializeField] private HappinessManager happiness;
    [SerializeField] private PlayerSkillController skill;

    [Header("Top")]
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text bestText;
    [SerializeField] private TMP_Text happyText;

    [Header("Bottom - Gauge & Skills")]
    [SerializeField] private Image gaugeFill;
    [SerializeField] private Image slowIcon;
    [SerializeField] private Image shieldIcon;

    [Tooltip("게이지 바 보간 속도. 클수록 빨리 따라간다. 0이면 보간 없이 즉시 반영")]
    [SerializeField] private float fillResponse = 10f;

    private static readonly Color Dim = new Color(0.38f, 0.4f, 0.5f, 0.85f);

    // 실제 충전량(0~1). 바는 Update에서 이 값을 향해 보간된다
    private float fillTarget;

    private void Start()
    {
        // 씬 배선이 빠졌을 때의 폴백
        if (timer == null) timer = FindFirstObjectByType<SurvivalTimer>();
        if (ranking == null) ranking = FindFirstObjectByType<RankingManager>();
        if (happiness == null) happiness = FindFirstObjectByType<HappinessManager>();
        if (skill == null) skill = FindFirstObjectByType<PlayerSkillController>();

        if (ranking != null)
        {
            ranking.OnBestTimeChanged += SetBest;
            SetBest(ranking.BestTime);
        }
        if (happiness != null)
        {
            happiness.OnChanged += SetHappiness;
            SetHappiness(happiness.Current);
        }
        if (skill != null)
        {
            skill.OnGaugeChanged += OnGaugeChanged;
            skill.OnShieldChanged += RefreshSkillIcons;
            OnGaugeChanged(skill.Gauge);

            // 씬 로드 직후에는 애니메이션 없이 현재 값에서 시작한다
            if (gaugeFill != null) gaugeFill.fillAmount = fillTarget;
        }
    }

    private void OnDestroy()
    {
        if (ranking != null) ranking.OnBestTimeChanged -= SetBest;
        if (happiness != null) happiness.OnChanged -= SetHappiness;
        if (skill != null)
        {
            skill.OnGaugeChanged -= OnGaugeChanged;
            skill.OnShieldChanged -= RefreshSkillIcons;
        }
    }

    private void Update()
    {
        if (timer != null && timeText != null)
            timeText.text = RankingScreenController.FormatTime(timer.CurrentTime);

        StepGaugeFill();
    }

    /// <summary>
    /// 게이지 바를 목표값으로 부드럽게 보간한다.
    /// PlayerAnimationController.StepRoll과 같은 프레임레이트 무관 지수 감쇠.
    /// 스케일된 deltaTime을 쓰므로 Time Slow에서는 함께 느려지고 Pause에서는 멈춘다.
    /// </summary>
    private void StepGaugeFill()
    {
        if (gaugeFill == null) return;

        float current = gaugeFill.fillAmount;
        if (current == fillTarget) return;   // 목표 도달 후에는 캔버스를 건드리지 않는다

        float next = fillResponse > 0f
            ? Mathf.Lerp(current, fillTarget, 1f - Mathf.Exp(-fillResponse * Time.deltaTime))
            : fillTarget;

        // 지수 감쇠는 목표에 무한히 접근만 하므로 충분히 가까우면 스냅한다
        if (Mathf.Abs(next - fillTarget) < 0.001f) next = fillTarget;

        gaugeFill.fillAmount = next;
    }

    private void SetBest(float best)
    {
        if (bestText != null)
            bestText.text = "BEST " + RankingScreenController.FormatTime(best);
    }

    private void SetHappiness(int value)
    {
        if (happyText != null) happyText.text = value.ToString();
    }

    private void OnGaugeChanged(float _)
    {
        if (skill == null) return;

        // 바를 직접 쓰지 않고 목표만 갱신한다. 표시는 Update가 따라간다.
        // 아이콘의 READY 판정은 실제 값 기준이므로 보간과 무관하게 즉시 바뀐다.
        fillTarget = skill.GaugeNormalized;
        RefreshSkillIcons();
    }

    private static readonly Color ShieldOn = new Color(0.45f, 0.95f, 1f);

    private void RefreshSkillIcons()
    {
        if (skill == null) return;

        // 기획서 17의 READY 표시. 원안은 글로우 프레임(gauge_ready) 교체였지만
        // 밸런스 조정으로 발동 임계값이 만충보다 낮아지면서 팀 결정으로 제거했다 -
        // 부분 충전 상태에서 프레임이 바뀌면 게이지가 다른 것으로 보인다.
        // READY 신호는 하단 스킬 아이콘 밝기가 담당한다.
        bool slowReady = skill.CanUseTimeSlow;
        if (slowIcon != null) slowIcon.color = slowReady ? Color.white : Dim;

        // 실드: 켜짐(청록 강조) / 발동 가능(흰) / 게이지 부족(어둡게)
        if (shieldIcon != null)
            shieldIcon.color = skill.IsShieldActive ? ShieldOn
                             : skill.CanUseShield ? Color.white : Dim;
    }
}
