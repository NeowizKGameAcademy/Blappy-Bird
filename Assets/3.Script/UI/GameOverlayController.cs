using TMPro;
using UnityEngine;

/// <summary>
/// Pause 팝업과 GameOver 오버레이 (기획서 16).
/// GameManager.OnStateChanged를 구독해 상태에 맞는 창만 보여준다.
///
/// 버튼:
///  - Resume: Paused -> Playing
///  - Retry:  GameSceneController.RetryRun() - 모든 IRunResettable 초기화 후 재시작
///  - Main:   인트로 씬으로 (기획서 8: "Ranking JSON 갱신 후 Retry 또는 Main")
/// </summary>
public sealed class GameOverlayController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameSceneController sceneController;
    [SerializeField] private SurvivalTimer timer;
    [SerializeField] private HappinessManager happiness;

    [Header("UI")]
    [SerializeField] private GameObject pausePopup;
    [SerializeField] private GameObject gameOverOverlay;
    [SerializeField] private TMP_Text resultTimeText;
    [SerializeField] private TMP_Text resultHappyText;

    [Header("Scenes")]
    [Tooltip("Main 버튼이 돌아갈 씬. 샌드박스에서는 IntroSandbox, 통합 시 MainScene")]
    [SerializeField] private string mainSceneName = "IntroSandbox";

    private void Start()
    {
        if (sceneController == null) sceneController = FindFirstObjectByType<GameSceneController>();
        if (timer == null) timer = FindFirstObjectByType<SurvivalTimer>();
        if (happiness == null) happiness = FindFirstObjectByType<HappinessManager>();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += HandleStateChanged;
            Apply(GameManager.Instance.CurrentState);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameState state)
    {
        Apply(state);
        if (state == GameState.GameOver) FillResult();
    }

    private void Apply(GameState state)
    {
        if (pausePopup != null) pausePopup.SetActive(state == GameState.Paused);
        if (gameOverOverlay != null) gameOverOverlay.SetActive(state == GameState.GameOver);
    }

    /// <summary>기획서 16: Game Over 화면은 Time / Happiness를 표시한다.</summary>
    private void FillResult()
    {
        if (resultTimeText != null && timer != null)
            resultTimeText.text = RankingScreenController.FormatTime(timer.CurrentTime);
        if (resultHappyText != null && happiness != null)
            resultHappyText.text = happiness.Current.ToString();
    }

    public void OnResumeClicked()
    {
        if (GameManager.Instance != null) GameManager.Instance.ResumeGame();
    }

    public void OnRetryClicked()
    {
        if (sceneController != null) sceneController.RetryRun();
    }

    public void OnMainClicked()
    {
        if (GameManager.Instance != null) GameManager.Instance.LoadScene(mainSceneName);
    }
}
