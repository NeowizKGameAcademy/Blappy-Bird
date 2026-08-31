using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 한 판(run)의 생성, 연결, 정리를 담당한다. GameScene에만 존재한다.
///
/// GameManager는 상태만 관리하고, 그 상태에 맞춰 실제 시스템을 켜고 끄는 것은 여기다.
/// 아직 없는 시스템(Player 1A, Spawner 3, Timer 4B, UI 5)은 구현되는 대로
/// IRunResettable을 붙이면 Retry에 자동으로 참여한다.
/// </summary>
public sealed class GameSceneController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("비워두면 Awake가 끝난 뒤 WorldScrollManager.Instance로 자동 연결한다.")]
    [SerializeField] private WorldScrollManager scroll;

    [Header("Dev")]
    [Tooltip("UI(5)가 붙기 전까지 Play 즉시 시작. UI 연결 후 꺼야 한다.")]
    [SerializeField] private bool autoStartForDev = true;

    private readonly List<IRunResettable> resettables = new List<IRunResettable>();

    /// <summary>
    /// 부트스트랩은 Awake에서 한다. Start에서 하면 다른 컴포넌트의 Start와
    /// 실행 순서가 보장되지 않아, 먼저 실행된 쪽(예: RankingManager.Start)이
    /// 아직 null인 GameManager.Instance를 참조해 NRE가 났다.
    /// AddComponent는 대상의 Awake를 즉시 실행하므로, 여기서 만들면
    /// 씬의 어떤 Start보다도 먼저 Instance가 준비된다.
    /// </summary>
    private void Awake()
    {
        EnsureBootstrap();
    }

    private void Start()
    {
        ResolveReferences();
        CollectResettables();

        GameManager.Instance.OnStateChanged += HandleStateChanged;
        GameManager.Instance.SetReady();

        if (autoStartForDev) GameManager.Instance.StartGame();
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
    }

    private void Update()
    {
        // Pause 토글 (기획서 16의 Pause Popup 진입점. 4 입력 표에는 빠져 있던 키)
        var kb = UnityEngine.InputSystem.Keyboard.current;
        if (kb == null || !kb.escapeKey.wasPressedThisFrame) return;

        var gm = GameManager.Instance;
        if (gm == null) return;

        if (gm.CurrentState == GameState.Playing) gm.PauseGame();
        else if (gm.CurrentState == GameState.Paused) gm.ResumeGame();
    }

    /// <summary>
    /// Dev 씬처럼 GameManager가 없는 씬에서도 단독 실행할 수 있게 한다.
    /// 정식 흐름에서는 MainScene이 이미 만들어 DontDestroyOnLoad로 넘겨준다.
    /// </summary>
    private void EnsureBootstrap()
    {
        if (TimeScaleManager.Instance == null)
            new GameObject("TimeScaleManager").AddComponent<TimeScaleManager>();

        if (GameManager.Instance == null)
            new GameObject("GameManager").AddComponent<GameManager>();
    }

    /// <summary>
    /// 인스펙터에서 연결하지 않았다면 싱글턴으로 대체한다.
    /// Start는 모든 Awake 이후에 실행되므로 이 시점에 Instance는 이미 설정돼 있다.
    /// </summary>
    private void ResolveReferences()
    {
        if (scroll == null) scroll = WorldScrollManager.Instance;
        if (scroll == null) scroll = FindFirstObjectByType<WorldScrollManager>();

        if (scroll == null)
            Debug.LogError("GameSceneController: WorldScrollManager를 찾을 수 없습니다.", this);
    }

    private void CollectResettables()
    {
        resettables.Clear();
        resettables.AddRange(
            FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .OfType<IRunResettable>());
    }

    private void HandleStateChanged(GameState state)
    {
        if (scroll == null) return;

        switch (state)
        {
            case GameState.Playing:
                scroll.StartScroll();
                break;
            case GameState.GameOver:
                scroll.StopScroll();
                break;
        }
        // Paused는 Time.timeScale이 0이라 FixedUpdate가 멈춘다. 별도 처리 불필요.
    }

    /// <summary>Retry. 모든 IRunResettable을 초기화한 뒤 다시 시작한다.</summary>
    public void RetryRun()
    {
        GameManager.Instance.SetReady();

        foreach (var r in resettables) r.ResetRun();
        if (TimeScaleManager.Instance != null) TimeScaleManager.Instance.ResetRun();

        GameManager.Instance.StartGame();
    }
}
