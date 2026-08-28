using System;
using UnityEngine;

/// <summary>
/// Happiness 수집 수치 (기획서 15). MVP에서는 보조 기록으로만 쓴다.
/// 수집물(HappinessCollectible)은 아직 없다 - Trigger에서 Add(1)를 호출하면 된다.
/// </summary>
public sealed class HappinessManager : MonoBehaviour, IRunResettable
{
    public int Current { get; private set; }

    /// <summary>HUD가 구독한다 (기획서 17: 수집 이벤트 발생 시 갱신).</summary>
    public event Action<int> OnChanged;

    public void Add(int amount)
    {
        if (amount <= 0) return;
        Current += amount;
        OnChanged?.Invoke(Current);
    }

    public void ResetRun()
    {
        Current = 0;
        OnChanged?.Invoke(Current);
    }
}
