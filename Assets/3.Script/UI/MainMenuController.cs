using UnityEngine;

/// <summary>
/// 인트로(타이틀) 화면 (기획서 16 Main).
/// Start -> GameScene, Ranking -> RankingScene, HowToPlay 팝업, Exit.
/// 씬 전환은 GameManager가 담당하고, 여기는 버튼을 연결만 한다.
/// </summary>
public sealed class MainMenuController : MonoBehaviour
{
    [SerializeField] private GameObject howToPlayPopup;

    private void Awake()
    {
        // 최초 실행 씬이므로 전역 매니저를 여기서 부트스트랩한다 (기획서 GameManager 문서 방침)
        if (TimeScaleManager.Instance == null)
            new GameObject("TimeScaleManager").AddComponent<TimeScaleManager>();
        if (GameManager.Instance == null)
            new GameObject("GameManager").AddComponent<GameManager>();

        if (howToPlayPopup != null) howToPlayPopup.SetActive(false);
    }

    public void OnStartClicked() => GameManager.Instance.LoadGameScene();
    public void OnRankingClicked() => GameManager.Instance.LoadRankingScene();
    public void OnHowToPlayClicked() { if (howToPlayPopup != null) howToPlayPopup.SetActive(true); }
    public void OnCloseHowToPlay() { if (howToPlayPopup != null) howToPlayPopup.SetActive(false); }

    public void OnExitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
