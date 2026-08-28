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

    [Header("Happiness (기획서 15)")]
    [Tooltip("게이트와 게이트 사이에 배치할 수집물 수. 구간을 (n+1)등분한 지점마다 하나")]
    [SerializeField] private GameObject happinessPrefab;
    [SerializeField] private int happinessPerGap = 3;

    [Tooltip("XY 평면 3x3 영역의 한 칸 크기. 30x30 플레이 영역 기준 10")]
    [SerializeField] private float zoneSize = 10f;

    private float timer;

    // 게이트 스폰 시 예약된 수집물 딜레이 (초)
    private readonly List<float> pendingHappiness = new List<float>();

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

        float dt = Time.fixedDeltaTime;

        // 예약된 수집물: 게이트 사이 구간의 등분 지점에서 하나씩
        for (int i = pendingHappiness.Count - 1; i >= 0; i--)
        {
            pendingHappiness[i] -= dt;
            if (pendingHappiness[i] > 0f) continue;
            pendingHappiness.RemoveAt(i);
            SpawnHappiness();
        }

        timer += dt;
        if (timer < spawnInterval) return;

        timer -= spawnInterval;
        Spawn();
    }

    private void Spawn()
    {
        var prefab = gatePrefabs[Random.Range(0, gatePrefabs.Count)];
        if (prefab == null || PoolManager.Instance == null) return;

        PoolManager.Instance.Get(prefab, new Vector3(0f, 0f, spawnZ));

        // 이번 게이트와 다음 게이트 사이 구간을 (n+1)등분해 수집물을 예약한다.
        // 시간 기준 등분이라 속도가 변해도 공간상 근사 균등이 유지된다.
        if (happinessPrefab != null)
            for (int i = 1; i <= happinessPerGap; i++)
                pendingHappiness.Add(spawnInterval * i / (happinessPerGap + 1));
    }

    /// <summary>XY 평면을 3x3 아홉 영역으로 나눠 랜덤한 영역 중심에 배치한다.</summary>
    private void SpawnHappiness()
    {
        if (happinessPrefab == null || PoolManager.Instance == null) return;

        int zone = Random.Range(0, 9);
        float x = (zone % 3 - 1) * zoneSize;    // -10, 0, +10
        float y = (zone / 3 - 1) * zoneSize;

        PoolManager.Instance.Get(happinessPrefab, new Vector3(x, y, spawnZ));
    }

    public void ResetRun()
    {
        timer = 0f;
        pendingHappiness.Clear();
        if (PoolManager.Instance != null) PoolManager.Instance.ReleaseAll();
    }
}
