/// <summary>
/// 게임 전체 상태. 기획서 7의 enum에서 Intro를 제외했다.
/// Intro는 6A(intro-shadow)가 후순위 브랜치이므로 상태로 박지 않고,
/// GameSceneController가 Ready 단계에서 IntroDirector를 선택 실행한 뒤 Playing으로 넘긴다.
/// 이렇게 하면 Intro가 나중에 붙어도 상태 머신을 고칠 필요가 없다.
/// </summary>
public enum GameState
{
    Ready,
    Playing,
    Paused,
    GameOver
}
