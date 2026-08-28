using UnityEngine;

/// <summary>
/// 생존 시간의 단일 소유자 (기획서 15, 17).
/// Playing 상태에서만 누적하고 GameOver가 되면 자동으로 고정된다.
/// WorldSpeedRamp(난이도)와 RankingManager(기록)가 모두 이 값을 읽는다.
/// </summary>
public sealed class SurvivalTimer : MonoBehaviour, IRunResettable
{
    public float CurrentTime { get; private set; }

    private void Update()
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.IsPlaying)
            CurrentTime += Time.deltaTime;
    }

    public void ResetRun() => CurrentTime = 0f;
}
