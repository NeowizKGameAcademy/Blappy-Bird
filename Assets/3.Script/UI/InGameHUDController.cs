using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 인게임 HUD (기획서 17).
/// 계산하지 않고 각 시스템의 이벤트를 받아 표시만 갱신한다 (기획서 16 원칙).
/// TIME만 예외적으로 매 프레임 갱신한다 (기획서 17 허용).
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
    [SerializeField] private Image gaugeFrame;
    [SerializeField] private Image gaugeFill;
    [SerializeField] private Image slowIcon;
    [SerializeField] private Image shieldIcon;
    [SerializeField] private Sprite frameNormal;
    [SerializeField] private Sprite frameReady;

    private static readonly Color Dim = new Color(0.38f, 0.4f, 0.5f, 0.85f);

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
            OnGaugeChanged(skill.Gauge);
        }
    }

    private void OnDestroy()
    {
        if (ranking != null) ranking.OnBestTimeChanged -= SetBest;
        if (happiness != null) happiness.OnChanged -= SetHappiness;
        if (skill != null) skill.OnGaugeChanged -= OnGaugeChanged;
    }

    private void Update()
    {
        if (timer != null && timeText != null)
            timeText.text = RankingScreenController.FormatTime(timer.CurrentTime);
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

        if (gaugeFill != null) gaugeFill.fillAmount = skill.GaugeNormalized;

        // 기획서 17: READY 상태는 게이지 임계값 변경 시 갱신 - 글로우 프레임으로 교체
        bool slowReady = skill.CanUseTimeSlow;
        bool shieldReady = skill.CanUseShield;

        if (gaugeFrame != null && frameNormal != null && frameReady != null)
            gaugeFrame.sprite = slowReady ? frameReady : frameNormal;
        if (slowIcon != null) slowIcon.color = slowReady ? Color.white : Dim;
        if (shieldIcon != null) shieldIcon.color = shieldReady ? Color.white : Dim;
    }
}
