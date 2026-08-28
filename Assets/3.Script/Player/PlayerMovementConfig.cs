using UnityEngine;

/// <summary>
/// 플레이어 이동 성능과 플레이 영역. 코드에 수치를 박지 않기 위한 데이터 (기획서 13 원칙).
///
/// 두 곳에서 참조한다.
///  - PlayerController(1A): 이동 계산과 경계 Clamp
///  - ObstacleSpawner(3):   도달 가능 검사 (기획서 13.1-5)
///
/// 같은 에셋을 공유해야 "생성된 패턴이 현재 이동 성능으로 통과 가능"(기획서 21 Fairness)이
/// 보장된다. Spawner가 플레이어 인스턴스를 참조할 필요는 없다.
///
/// 런타임에 변하는 값(현재 속도 등)은 여기 두지 않는다.
/// ScriptableObject는 에디터 플레이 중 변경이 영속되므로 튜닝값 전용이다.
/// </summary>
[CreateAssetMenu(menuName = "SkyFlap/Player Movement Config", fileName = "PlayerMovementConfig")]
public sealed class PlayerMovementConfig : ScriptableObject
{
    [Header("수직 (기획서 5)")]
    [Tooltip("Space 입력 시 이 값으로 재설정한다. 가산이 아니다.")]
    public float flapVelocity = 6.2f;
    public float customGravity = -18f;
    public float maxFallSpeed = -11f;

    [Header("수평 (기획서 4)")]
    public float horizontalAccel = 28f;
    public float maxHorizontalSpeed = 7f;

    [Header("플레이 영역")]
    [Tooltip("영역 중심. 플레이어는 Z=0 평면에서만 움직인다.")]
    public Vector2 boundsCenter = Vector2.zero;

    [Tooltip("가로 x 세로 크기.")]
    public Vector2 boundsSize = new Vector2(30f, 30f);

    public float MinX => boundsCenter.x - boundsSize.x * 0.5f;
    public float MaxX => boundsCenter.x + boundsSize.x * 0.5f;
    public float MinY => boundsCenter.y - boundsSize.y * 0.5f;
    public float MaxY => boundsCenter.y + boundsSize.y * 0.5f;
}
