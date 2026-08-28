using UnityEngine;

/// <summary>
/// 행복 수집물 (기획서 15).
/// PlayerCollision이 Trigger 분기에서 Collect를 호출하면
/// HappinessManager.Add(1) 후 풀로 돌아간다.
/// </summary>
public sealed class HappinessCollectible : MonoBehaviour, IPoolable
{
    private bool collected;

    public void Collect()
    {
        if (collected) return;   // 같은 프레임 중복 트리거 방지
        collected = true;

        if (HappinessManager.Instance != null)
            HappinessManager.Instance.Add(1);

        if (PoolManager.Instance != null) PoolManager.Instance.Release(gameObject);
        else gameObject.SetActive(false);
    }

    public void OnSpawned() => collected = false;
    public void OnDespawned() { }
}
