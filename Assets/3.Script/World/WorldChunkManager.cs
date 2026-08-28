using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 배경 World Chunk를 -Z 방향으로 흘려보내고,
/// 후방 기준선을 넘은 Chunk를 맨 앞으로 재배치해 무한 스크롤을 만든다.
///
/// 장애물은 여기서 다루지 않는다.
/// 기획서 14: 장애물은 Chunk에 고정하지 않고 별도 Spawner/Pool이 관리한다.
/// 배경 Chunk는 3~6개를 고정 재사용하므로 별도 Pool이 필요 없다.
/// </summary>
public sealed class WorldChunkManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldScrollManager scroll;

    [Tooltip("순환에 사용할 Chunk Prefab. 개수가 activeChunkCount보다 적으면 반복 사용한다.")]
    [SerializeField] private GameObject[] chunkPrefabs;

    [Header("Layout")]
    [Tooltip("Chunk 하나의 Z 길이. 모든 Chunk가 동일한 길이라고 가정한다.")]
    [SerializeField] private float chunkLength = 40f;

    [Tooltip("동시에 유지할 Chunk 개수. 기획서 14: 3~6개")]
    [SerializeField] private int activeChunkCount = 4;

    [Tooltip("가장 뒤쪽 Chunk의 시작 Z. 플레이어는 Z=0에 머문다.")]
    [SerializeField] private float startZ = -40f;

    [Tooltip("Chunk 원점이 이 Z보다 뒤로 가면 맨 앞으로 재배치한다.")]
    [SerializeField] private float recycleZ = -80f;

    /// <summary>Z 오름차순으로 정렬된 활성 Chunk. Peek()이 가장 뒤쪽 Chunk다.</summary>
    private readonly Queue<Transform> chunks = new Queue<Transform>();

    /// <summary>현재 가장 앞쪽 Chunk. 재배치 기준점으로 사용한다.</summary>
    private Transform frontChunk;

    private void Start()
    {
        if (!Validate())
        {
            enabled = false;
            return;
        }

        BuildChunks();
    }

    private void FixedUpdate()
    {
        float delta = scroll.FixedScrollDelta;
        if (delta <= 0f)
            return;

        foreach (Transform chunk in chunks)
            chunk.position += Vector3.back * delta;

        // 한 스텝에 두 개가 넘어갈 일은 없지만 저속 프레임 대비로 while을 쓴다.
        while (chunks.Count > 0 && chunks.Peek().position.z <= recycleZ)
            RecycleRearmost();
    }

    /// <summary>Retry 시 모든 Chunk를 초기 배치로 되돌린다.</summary>
    public void ResetRun()
    {
        int index = 0;

        foreach (Transform chunk in chunks)
        {
            chunk.position = new Vector3(0f, 0f, startZ + chunkLength * index);
            frontChunk = chunk;
            index++;
        }
    }

    private bool Validate()
    {
        if (scroll == null)
        {
            Debug.LogError($"{nameof(WorldChunkManager)}: scroll 참조가 비어 있습니다.", this);
            return false;
        }

        if (chunkPrefabs == null || chunkPrefabs.Length == 0)
        {
            Debug.LogError($"{nameof(WorldChunkManager)}: chunkPrefabs가 비어 있습니다.", this);
            return false;
        }

        if (chunkLength <= 0f)
        {
            Debug.LogError($"{nameof(WorldChunkManager)}: chunkLength는 0보다 커야 합니다.", this);
            return false;
        }

        // 재배치 기준선이 시작 위치보다 앞이면 생성 즉시 재배치가 반복된다.
        if (recycleZ >= startZ)
        {
            Debug.LogError($"{nameof(WorldChunkManager)}: recycleZ는 startZ보다 뒤에 있어야 합니다.", this);
            return false;
        }

        return true;
    }

    private void BuildChunks()
    {
        for (int i = 0; i < activeChunkCount; i++)
        {
            GameObject prefab = chunkPrefabs[i % chunkPrefabs.Length];
            Transform instance = Instantiate(prefab, transform).transform;

            instance.position = new Vector3(0f, 0f, startZ + chunkLength * i);
            chunks.Enqueue(instance);
            frontChunk = instance;
        }
    }

    private void RecycleRearmost()
    {
        Transform rearmost = chunks.Dequeue();

        // 누적 오차를 피하려고 항상 현재 맨 앞 Chunk의 실제 위치를 기준으로 재계산한다.
        // rearmost.z += totalLength 방식은 오차가 쌓여 Chunk 사이에 틈이 생긴다.
        Vector3 front = frontChunk.position;
        rearmost.position = new Vector3(front.x, front.y, front.z + chunkLength);

        chunks.Enqueue(rearmost);
        frontChunk = rearmost;
    }
}
