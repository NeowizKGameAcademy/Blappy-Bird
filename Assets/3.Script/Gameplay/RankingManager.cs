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
}

/// <summary>
/// 기록 저장의 최소 구현. GameOver 시 이번 런을 ranking.json에 기록하고
/// BestTime을 노출한다 (기획서 15, 17).
/// 정렬: survivalTime DESC, 동률이면 happiness DESC. Top10 유지.
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

        data.rankings.Add(new RankingEntry
        {
            playerName = "YOU",
            survivalTime = timer.CurrentTime,
            happiness = happiness != null ? happiness.Current : 0,
            date = DateTime.Now.ToString("s")
        });

        data.rankings.Sort((a, b) =>
        {
            int byTime = b.survivalTime.CompareTo(a.survivalTime);
            return byTime != 0 ? byTime : b.happiness.CompareTo(a.happiness);
        });
        if (data.rankings.Count > 10)
            data.rankings.RemoveRange(10, data.rankings.Count - 10);

        Save();

        if (data.rankings.Count > 0 && data.rankings[0].survivalTime > BestTime)
        {
            BestTime = data.rankings[0].survivalTime;
            OnBestTimeChanged?.Invoke(BestTime);
        }
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

        BestTime = data.rankings.Count > 0 ? data.rankings[0].survivalTime : 0f;
    }

    private void Save()
    {
        try { File.WriteAllText(SavePath, JsonUtility.ToJson(data, true)); }
        catch (Exception e) { Debug.LogError("RankingManager: 저장 실패. " + e.Message); }
    }
}
