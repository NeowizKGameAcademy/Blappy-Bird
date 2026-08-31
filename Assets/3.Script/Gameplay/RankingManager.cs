using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>기획서 15의 직렬화 스키마 그대로. 4B가 Top10 UI를 붙일 때 재사용한다.</summary>
[Serializable]
public class RankingEntry
{
    public string playerName;
    public float survivalTime;
    public int happiness;
    public string date;
}

[Serializable]
public class RankingSaveData
{
    public int version = 1;
    public List<RankingEntry> rankings = new List<RankingEntry>();

    /// <summary>
    /// 역대 최고 생존 시간. Top10과 별도로 보관한다.
    ///
    /// 정렬 1순위가 happiness가 되면서, 시간은 좋지만 행복이 적은 판이
    /// Top10 밖으로 밀려날 수 있게 되었다. 목록에서만 최댓값을 구하면
    /// 그 순간 "MY BEST"가 줄어든다. 기록은 줄어들지 않아야 하므로 따로 든다.
    ///
    /// 이 필드가 없던 시절의 ranking.json은 0으로 읽히고, Load에서
    /// 목록 최댓값으로 메워지므로 별도 마이그레이션이 필요 없다.
    /// </summary>
    public float bestSurvivalTime;
}

/// <summary>
/// 기록 저장의 최소 구현. GameOver 시 이번 런을 ranking.json에 기록하고
/// BestTime을 노출한다 (기획서 15, 17).
/// 정렬: happiness DESC, 동률이면 survivalTime DESC. Top10 유지.
/// RankingScene의 목록 UI는 4B 범위 - 이 파일 스키마를 그대로 읽으면 된다.
/// </summary>
public sealed class RankingManager : MonoBehaviour
{
    [SerializeField] private SurvivalTimer timer;
    [SerializeField] private HappinessManager happiness;

    private RankingSaveData data = new RankingSaveData();
    private bool recorded;   // GameOver당 1회만 기록

    public float BestTime { get; private set; }

    /// <summary>HUD가 구독한다 (기획서 17: 게임 시작/기록 갱신 시).</summary>
    public event Action<float> OnBestTimeChanged;

    public static string SavePath => Path.Combine(Application.persistentDataPath, "ranking.json");

    /// <summary>
    /// 순위 비교. 행복이 1순위, 생존 시간이 2순위다.
    ///
    /// happiness는 int라 동률이 흔하게 나오므로 2순위가 실제로 자주 동작한다.
    /// (시간이 1순위였을 때는 float 동률이 사실상 불가능해 2순위가 죽은 코드였다)
    ///
    /// 목록 정렬과 화면 표시가 어긋나지 않도록 RankingScreenController도
    /// 이 순서를 전제로 한다. 기준을 바꿀 때는 양쪽을 함께 본다.
    /// </summary>
    public static int Compare(RankingEntry a, RankingEntry b)
    {
        int byHappiness = b.happiness.CompareTo(a.happiness);
        return byHappiness != 0 ? byHappiness : b.survivalTime.CompareTo(a.survivalTime);
    }

    private void Awake() => Load();

    private void OnEnable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged += HandleStateChanged;
    }

    private void Start()
    {
        // Awake 시점에는 GameManager가 없을 수 있다 (Dev 씬 부트스트랩 순서)
        GameManager.Instance.OnStateChanged -= HandleStateChanged;
        GameManager.Instance.OnStateChanged += HandleStateChanged;
        OnBestTimeChanged?.Invoke(BestTime);
    }

    private void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
    }

    private void HandleStateChanged(GameState state)
    {
        if (state == GameState.Playing) recorded = false;
        if (state != GameState.GameOver || recorded) return;

        recorded = true;
        RecordRun();
    }

    private void RecordRun()
    {
        if (timer == null) return;

        float runTime = timer.CurrentTime;

        data.rankings.Add(new RankingEntry
        {
            playerName = "YOU",
            survivalTime = runTime,
            happiness = happiness != null ? happiness.Current : 0,
            date = DateTime.Now.ToString("s")
        });

        // Top10에서 밀려나더라도 최고 시간은 남겨야 하므로 자르기 전에 반영한다.
        if (runTime > data.bestSurvivalTime) data.bestSurvivalTime = runTime;

        data.rankings.Sort(Compare);
        if (data.rankings.Count > 10)
            data.rankings.RemoveRange(10, data.rankings.Count - 10);

        Save();

        SetBestTime(data.bestSurvivalTime);
    }

    private void SetBestTime(float value)
    {
        if (Mathf.Approximately(value, BestTime)) return;

        BestTime = value;
        OnBestTimeChanged?.Invoke(BestTime);
    }

    private void Load()
    {
        try
        {
            if (File.Exists(SavePath))
                data = JsonUtility.FromJson<RankingSaveData>(File.ReadAllText(SavePath))
                       ?? new RankingSaveData();
        }
        catch (Exception e)
        {
            Debug.LogWarning("RankingManager: 로드 실패, 새로 시작. " + e.Message);
            data = new RankingSaveData();
        }

        // 구버전 파일에는 bestSurvivalTime이 없다. 목록 최댓값으로 메운다.
        foreach (var entry in data.rankings)
            if (entry.survivalTime > data.bestSurvivalTime)
                data.bestSurvivalTime = entry.survivalTime;

        BestTime = data.bestSurvivalTime;
    }

    private void Save()
    {
        try { File.WriteAllText(SavePath, JsonUtility.ToJson(data, true)); }
        catch (Exception e) { Debug.LogError("RankingManager: 저장 실패. " + e.Message); }
    }
}
