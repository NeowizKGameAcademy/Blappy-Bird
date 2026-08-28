using UnityEngine;

/// <summary>
/// 월드 스크롤 속도의 단일 소유자.
/// 플레이어는 Z=0에 머물고 월드가 -Z 방향으로 흐른다.
///
/// 이 클래스는 "얼마나 빠른가"와 "지금 흐르는가"만 관리한다.
/// 실제 이동은 각 소비자(WorldChunkManager, 장애물 ScrollingObject)가 수행한다.
/// 소비자가 넷 이상이므로 의존 표면을 최소로 유지한다.
/// </summary>
public sealed class WorldScrollManager : MonoBehaviour, IRunResettable
{
    public static WorldScrollManager Instance { get; private set; }

    [Header("Scroll")]
    [Tooltip("Stage 0 기준 속도. 기획서 13: 0~30초 구간 9 m/s")]
    [SerializeField] private float baseSpeed = 9f;

    /// <summary>현재 스크롤 속도(m/s). DifficultyManager가 Stage에 따라 갱신한다.</summary>
    public float CurrentSpeed { get; private set; }

    /// <summary>스크롤 진행 여부. Playing 상태에서만 true.</summary>
    public bool IsScrolling { get; private set; }

    /// <summary>
    /// FixedUpdate 한 스텝 동안 월드가 -Z로 이동해야 하는 거리.
    /// 정지 중에는 0을 돌려주므로 소비자가 IsScrolling을 따로 검사하지 않아도 된다.
    /// Time Slow는 Time.timeScale로 구현되므로 여기서 별도 배율을 곱하지 않는다.
    /// (곱하면 0.6 x 0.6 = 0.36배로 이중 적용된다)
    /// </summary>
    public float FixedScrollDelta => IsScrolling ? CurrentSpeed * Time.fixedDeltaTime : 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        CurrentSpeed = baseSpeed;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>DifficultyManager가 Stage 변경 시 호출한다.</summary>
    public void SetSpeed(float speed)
    {
        CurrentSpeed = Mathf.Max(0f, speed);
    }

    /// <summary>GameManager가 Playing 진입 시 호출한다.</summary>
    public void StartScroll()
    {
        IsScrolling = true;
    }

    /// <summary>
    /// GameManager가 GameOver 진입 시 호출한다.
    /// CurrentSpeed는 보존하므로 재개 시 속도를 다시 조회할 필요가 없다.
    ///
    /// Paused는 Time.timeScale이 0이 되어 FixedUpdate 자체가 멈추므로
    /// 이 메서드를 호출할 필요가 없다.
    /// </summary>
    public void StopScroll()
    {
        IsScrolling = false;
    }

    /// <summary>Retry 시 속도를 초기 상태로 되돌린다.</summary>
    public void ResetRun()
    {
        IsScrolling = false;
        CurrentSpeed = baseSpeed;
    }
}
