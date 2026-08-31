using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 랭킹 화면 (기획서 15, 16).
/// ranking.json을 읽어 Top10 목록을 만든다. 정렬은 저장 시점에 이미
/// happiness DESC / survivalTime DESC로 되어 있으므로 그대로 표시한다.
/// 기준은 RankingManager.Compare 한 곳에서만 정한다.
///
/// 행 3상태: 홀수 행 밝게, 짝수 행 파랗게 교대하고
/// 가장 최근 플레이 기록은 네온 행으로 강조한다.
/// </summary>
public sealed class RankingScreenController : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string backSceneName = "IntroSandbox";

    [Header("References")]
    [SerializeField] private GameObject rowTemplate;
    [SerializeField] private Transform rowContainer;
    [SerializeField] private TMP_Text myBestText;
    [SerializeField] private TMP_Text myHappinessText;
    [SerializeField] private TMP_Text emptyText;

    [Header("Sprites")]
    [SerializeField] private Sprite[] medals;      // 1~3위
    [SerializeField] private Sprite rowLight;
    [SerializeField] private Sprite rowBlue;
    [SerializeField] private Sprite rowRecent;

    private void Start()
    {
        var data = Load();
        Populate(data);
    }

    private RankingSaveData Load()
    {
        try
        {
            if (File.Exists(RankingManager.SavePath))
                return JsonUtility.FromJson<RankingSaveData>(File.ReadAllText(RankingManager.SavePath))
                       ?? new RankingSaveData();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("RankingScreen: 로드 실패. " + e.Message);
        }
        return new RankingSaveData();
    }

    private void Populate(RankingSaveData data)
    {
        bool empty = data.rankings.Count == 0;
        if (emptyText != null) emptyText.gameObject.SetActive(empty);

        if (empty)
        {
            if (myBestText != null) myBestText.text = "-";
            if (myHappinessText != null) myHappinessText.text = "-";
            return;
        }

        // 가장 최근 플레이 기록 (date는 sortable "s" 포맷이라 문자열 비교로 충분)
        int recent = 0;
        for (int i = 1; i < data.rankings.Count; i++)
            if (string.CompareOrdinal(data.rankings[i].date, data.rankings[recent].date) > 0)
                recent = i;

        int count = Mathf.Min(10, data.rankings.Count);
        for (int i = 0; i < count; i++)
        {
            var entry = data.rankings[i];
            var row = Instantiate(rowTemplate, rowContainer);
            row.SetActive(true);

            var bg = row.GetComponent<Image>();
            if (bg != null)
                bg.sprite = i == recent ? rowRecent : (i % 2 == 0 ? rowLight : rowBlue);

            var rankIcon = row.transform.Find("RankIcon")?.GetComponent<Image>();
            var rankText = row.transform.Find("RankText")?.GetComponent<TMP_Text>();
            if (i < 3 && medals != null && i < medals.Length)
            {
                if (rankIcon != null) { rankIcon.enabled = true; rankIcon.sprite = medals[i]; }
                if (rankText != null) rankText.text = "";
            }
            else
            {
                if (rankIcon != null) rankIcon.enabled = false;
                if (rankText != null) rankText.text = (i + 1).ToString();
            }

            SetText(row, "NameText", entry.playerName);
            SetText(row, "TimeText", FormatTime(entry.survivalTime));
            SetText(row, "HappyText", entry.happiness.ToString());
        }

        // MY BEST는 항목별 최댓값이다.
        // 정렬 1순위가 happiness이므로 rankings[0]은 최고 행복이지 최고 시간이 아니다.
        // 시간은 Top10에서 밀려난 판까지 포함하는 bestSurvivalTime을 쓴다.
        if (myHappinessText != null)
            myHappinessText.text = data.rankings[0].happiness.ToString();

        if (myBestText != null)
        {
            float bestTime = data.bestSurvivalTime;
            foreach (var entry in data.rankings)          // 구버전 파일 대비
                if (entry.survivalTime > bestTime) bestTime = entry.survivalTime;

            myBestText.text = FormatTime(bestTime);
        }
    }

    private static void SetText(GameObject row, string child, string value)
    {
        var t = row.transform.Find(child)?.GetComponent<TMP_Text>();
        if (t != null) t.text = value;
    }

    /// <summary>87.34초 -> "1:27.34"</summary>
    public static string FormatTime(float seconds)
    {
        int m = Mathf.FloorToInt(seconds / 60f);
        float s = seconds - m * 60f;
        return m + ":" + s.ToString("00.00");
    }

    public void OnBackClicked()
    {
        if (GameManager.Instance != null) GameManager.Instance.LoadScene(backSceneName);
        else UnityEngine.SceneManagement.SceneManager.LoadScene(backSceneName);
    }
}
