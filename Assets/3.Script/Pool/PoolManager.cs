using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

/// <summary>
/// 프리팹별 오브젝트 풀. 기획서 14가 지정한 UnityEngine.Pool.ObjectPool을 사용한다.
/// 기획서 21의 "10분 이상 반복 Instantiate/Destroy 최소화"를 위한 것이다.
/// </summary>
public sealed class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    [SerializeField] private int defaultCapacity = 8;
    [SerializeField] private int maxSize = 64;

    private readonly Dictionary<GameObject, ObjectPool<GameObject>> pools = new();

    /// <summary>인스턴스가 어느 프리팹에서 나왔는지. Release 때 풀을 찾는 데 쓴다.</summary>
    private readonly Dictionary<GameObject, GameObject> originOf = new();

    private readonly List<GameObject> active = new();

    public int ActiveCount => active.Count;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public GameObject Get(GameObject prefab, Vector3 position)
    {
        var pool = GetOrCreate(prefab);
        var instance = pool.Get();

        instance.transform.SetPositionAndRotation(position, prefab.transform.rotation);
        originOf[instance] = prefab;
        active.Add(instance);

        if (instance.TryGetComponent<IPoolable>(out var poolable)) poolable.OnSpawned();
        return instance;
    }

    public void Release(GameObject instance)
    {
        if (instance == null) return;
        if (!originOf.TryGetValue(instance, out var prefab)) { Destroy(instance); return; }

        if (instance.TryGetComponent<IPoolable>(out var poolable)) poolable.OnDespawned();

        active.Remove(instance);
        originOf.Remove(instance);
        pools[prefab].Release(instance);
    }

    /// <summary>Retry 시 활성 오브젝트를 전부 회수한다.</summary>
    public void ReleaseAll()
    {
        for (int i = active.Count - 1; i >= 0; i--) Release(active[i]);
    }

    /// <summary>첫 스폰의 프레임 스파이크를 피하려면 미리 채워둔다.</summary>
    public void Prewarm(GameObject prefab, int count)
    {
        var pool = GetOrCreate(prefab);
        var temp = new List<GameObject>(count);

        for (int i = 0; i < count; i++) temp.Add(pool.Get());
        foreach (var go in temp) pool.Release(go);
    }

    private ObjectPool<GameObject> GetOrCreate(GameObject prefab)
    {
        if (pools.TryGetValue(prefab, out var existing)) return existing;

        var pool = new ObjectPool<GameObject>(
            createFunc:      () => Instantiate(prefab, transform),
            actionOnGet:     go => go.SetActive(true),
            actionOnRelease: go => go.SetActive(false),
            actionOnDestroy: go => { if (go != null) Destroy(go); },
            collectionCheck: true,
            defaultCapacity: defaultCapacity,
            maxSize:         maxSize);

        pools[prefab] = pool;
        return pool;
    }
}
