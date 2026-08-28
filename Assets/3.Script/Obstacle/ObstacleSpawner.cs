using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 일정 주기로 게이트 프리팹 하나를 무작위로 골라 전방 중앙에 배치한다.
///
/// 게이트가 어떻게 생겼는지, 구멍이 어디 뚫려 있는지는 알지 않는다.
/// 통과 가능성은 프리팹 저작 단계에서 보장한다.
/// </summary>
public sealed class ObstacleSpawner : MonoBehaviour, IRunResettable
{
    [Header("Gates")]
    [Tooltip("스폰 후보. 무작위로 하나씩 고른다.")]
    [SerializeField] private List<GameObject> gatePrefabs = new();

    [Header("Spawn")]
    [Tooltip("전방 배치 거리. 플레이어는 Z=0에 있다.")]
    [SerializeField] private float spawnZ = 100f;

    [Tooltip("스폰 간격(초).")]
    [SerializeField] private float spawnInterval = 3.5f;

    [Tooltip("프리팹당 미리 만들어둘 개수. 첫 스폰의 프레임 스파이크를 막는다.")]
    [SerializeField] private int prewarmPerPrefab = 4;

    private float timer;

    private void Start()
    {
        if (gatePrefabs.Count == 0)
        {
            Debug.LogError("ObstacleSpawner: gatePrefabs가 비어 있습니다.", this);
            enabled = false;
            return;
        }

        if (PoolManager.Instance == null) return;

        foreach (var prefab in gatePrefabs)
            if (prefab != null) PoolManager.Instance.Prewarm(prefab, prewarmPerPrefab);
    }

    private void FixedUpdate()
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.IsPlaying) return;

        timer += Time.fixedDeltaTime;
        if (timer < spawnInterval) return;

        timer -= spawnInterval;
        Spawn();
    }

    private void Spawn()
    {
        var prefab = gatePrefabs[Random.Range(0, gatePrefabs.Count)];
        if (prefab == null || PoolManager.Instance == null) return;

        PoolManager.Instance.Get(prefab, new Vector3(0f, 0f, spawnZ));
    }

    public void ResetRun()
    {
        timer = 0f;
        if (PoolManager.Instance != null) PoolManager.Instance.ReleaseAll();
    }
}
