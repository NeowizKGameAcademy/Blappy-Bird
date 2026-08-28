using System;
using UnityEngine;

/// <summary>
/// Perfect Gauge 보관/소비 (기획서 20의 별도 클래스).
/// MonoBehaviour가 아니라 PlayerSkillController가 인라인으로 소유한다 -
/// Unity 콜백이 없고, 컴포넌트로 두면 인스펙터에서 실수로 떼어낼 수 있다.
/// </summary>
[Serializable]
public sealed class GaugeController
{
    [SerializeField] private float current;

    public float Current => current;

    /// <summary>HUD가 구독한다 (기획서 17: 이벤트 기반 갱신).</summary>
    public event Action<float> OnChanged;

    public void Add(float amount, float max)
    {
        if (amount <= 0f) return;
        float next = Mathf.Min(current + amount, max);
        if (Mathf.Approximately(next, current)) return;
        current = next;
        OnChanged?.Invoke(current);
    }

    public bool TrySpend(float cost)
    {
        if (current < cost) return false;
        current -= cost;
        OnChanged?.Invoke(current);
        return true;
    }

    public void ResetRun()
    {
        current = 0f;
        OnChanged?.Invoke(current);
    }
}
