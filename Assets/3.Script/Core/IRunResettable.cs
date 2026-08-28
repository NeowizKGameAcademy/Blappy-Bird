/// <summary>
/// Retry 시 한 판의 상태를 초기화해야 하는 시스템이 구현한다.
/// GameSceneController가 씬 안의 구현체를 모두 찾아 호출하므로,
/// 각 브랜치는 이 메서드 하나만 구현하면 Retry에 자동으로 참여한다.
///
/// 구현 예정: PlayerController(1A), SurvivalTimer/HappinessManager(4B),
/// ObstacleSpawner/DifficultyManager(3), GaugeController(4A), FollowCamera(1B)
/// </summary>
public interface IRunResettable
{
    void ResetRun();
}
