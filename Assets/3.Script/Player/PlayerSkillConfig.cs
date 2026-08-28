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

    [Header("Shield - 희망의 보호막 (기획서 10.2, 동작은 4A)")]
    public float shieldCost = 70f;
}
