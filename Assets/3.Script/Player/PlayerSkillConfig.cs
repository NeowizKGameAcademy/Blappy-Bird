using UnityEngine;

/// <summary>스킬 수치 (기획서 10). 코드에 수치를 박지 않는다.</summary>
[CreateAssetMenu(menuName = "SkyFlap/Player Skill Config", fileName = "PlayerSkillConfig")]
public sealed class PlayerSkillConfig : ScriptableObject
{
    [Header("Gauge")]
    public float maxGauge = 100f;

    [Tooltip("Passzone 레이어 트리거 진입 시 충전량")]
    public float passZoneGain = 20f;

    [Header("Time Slow - 희망의 날갯짓 (기획서 10.1)")]
    public float timeSlowCost = 100f;
    public float timeSlowDuration = 2f;
    [Range(0.05f, 1f)] public float timeSlowScale = 0.6f;

    [Header("Shield - 희망의 보호막 (기획서 10.2)")]
    [Tooltip("E 입력으로 발동. 기획서 원문은 충돌 시 자동이지만 팀 결정으로 능동 발동으로 변경")]
    public float shieldCost = 70f;

    [Tooltip("방어 직후 무적 시간. 동일 장애물과의 연속 충돌 방지 (기획서 10.2)")]
    public float invincibleDuration = 0.5f;
}
