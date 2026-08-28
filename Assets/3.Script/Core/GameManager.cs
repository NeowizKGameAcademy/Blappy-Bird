using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 게임 전체 상태와 씬 전환만 담당한다.
/// 각 시스템의 초기화, 스폰, 타이머, 점수 계산은 여기서 하지 않고
/// GameSceneController와 전용 시스템이 상태 변경 이벤트를 구독해 처리한다.
///
/// Time.timeScale은 직접 건드리지 않고 TimeScaleManager에 위임한다.
/// </summary>
public sealed class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameState CurrentState { get; private set; } = GameState.Ready;
    public bool IsPlaying => CurrentState == GameState.Playing;

    /// <summary>상태가 실제로 바뀐 경우에만 발행된다.</summary>
    public event Action<GameState> OnStateChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void ChangeState(GameState next)
    {
        if (CurrentState == next) return;

        CurrentState = next;

        // Pause만 timeScale에 영향을 준다.
        // GameOver를 0으로 만들면 기획서 18의 충격 연출과 결과 UI가 멈춘다.
        if (TimeScaleManager.Instance != null)
            TimeScaleManager.Instance.SetPaused(next == GameState.Paused);

        OnStateChanged?.Invoke(next);
    }

    public void StartGame()
    {
        if (CurrentState is GameState.Ready or GameState.GameOver)
            ChangeState(GameState.Playing);
    }

    public void PauseGame()
    {
        if (CurrentState == GameState.Playing) ChangeState(GameState.Paused);
    }

    public void ResumeGame()
    {
        if (CurrentState == GameState.Paused) ChangeState(GameState.Playing);
    }

    public void EndGame()
    {
        if (CurrentState is GameState.Playing or GameState.Paused)
            ChangeState(GameState.GameOver);
    }

    /// <summary>Retry 등으로 한 판을 처음 상태로 되돌린다. 실제 초기화는 GameSceneController가 한다.</summary>
    public void SetReady() => ChangeState(GameState.Ready);

    // ---- 씬 전환 ----

    public void LoadMainScene()    => LoadScene("MainScene");
    public void LoadGameScene()    => LoadScene("GameScene");
    public void LoadRankingScene() => LoadScene("RankingScene");

    private void LoadScene(string sceneName)
    {
        if (TimeScaleManager.Instance != null) TimeScaleManager.Instance.ResetRun();
        CurrentState = GameState.Ready;
        SceneManager.LoadScene(sceneName);
    }
}
