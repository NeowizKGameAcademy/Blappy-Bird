using UnityEngine;

/// <summary>
/// 플레이 영역 계산. MonoBehaviour가 아니므로 단위 테스트로 검증할 수 있다.
/// PlayerController(1A)와 ObstacleSpawner(3)가 같은 함수를 쓴다.
/// </summary>
public static class PlayArea
{
    /// <summary>영역 밖 좌표를 경계로 끌어온다. Z는 건드리지 않는다.</summary>
    public static Vector3 Clamp(Vector3 position, PlayerMovementConfig cfg)
    {
        return new Vector3(
            Mathf.Clamp(position.x, cfg.MinX, cfg.MaxX),
            Mathf.Clamp(position.y, cfg.MinY, cfg.MaxY),
            position.z);
    }

    public static bool Contains(Vector3 position, PlayerMovementConfig cfg)
    {
        return position.x >= cfg.MinX && position.x <= cfg.MaxX
            && position.y >= cfg.MinY && position.y <= cfg.MaxY;
    }

    /// <summary>
    /// 경계에 닿았는지. 축별로 반환하므로 해당 축 속도만 0으로 만들 수 있다.
    /// 벽에 붙었을 때 다른 축 이동까지 죽는 것을 막는다.
    /// </summary>
    public static void GetEdgeContact(Vector3 position, PlayerMovementConfig cfg,
                                      out bool atX, out bool atY)
    {
        atX = position.x <= cfg.MinX || position.x >= cfg.MaxX;
        atY = position.y <= cfg.MinY || position.y >= cfg.MaxY;
    }

    /// <summary>
    /// 장애물 안전 지점 후보. margin만큼 안쪽으로 좁혀서 뽑는다 (기획서 13.1-4).
    /// </summary>
    public static Vector2 RandomPoint(PlayerMovementConfig cfg, float margin, System.Random rng)
    {
        float minX = cfg.MinX + margin, maxX = cfg.MaxX - margin;
        float minY = cfg.MinY + margin, maxY = cfg.MaxY - margin;

        if (minX > maxX) minX = maxX = cfg.boundsCenter.x;
        if (minY > maxY) minY = maxY = cfg.boundsCenter.y;

        return new Vector2(
            minX + (float)rng.NextDouble() * (maxX - minX),
            minY + (float)rng.NextDouble() * (maxY - minY));
    }
}
