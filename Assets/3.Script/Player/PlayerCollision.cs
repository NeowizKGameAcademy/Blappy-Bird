using UnityEngine;

/// <summary>
/// 플레이어 충돌/트리거 판정.
/// - Obstacle: 실드 검사 후 GameOver
/// - Passzone: 스킬 게이지 충전
/// - HappinessCollectible: 행복 수치 획득
/// </summary>
[RequireComponent(typeof(PlayerController))]
public sealed class PlayerCollision : MonoBehaviour
{
    [SerializeField] private PlayerSkillController skill;

    private int obstacleLayer = -1;
    private int passzoneLayer = -1;

    private void Awake()
    {
        obstacleLayer = LayerMask.NameToLayer("Deadzone");
        passzoneLayer = LayerMask.NameToLayer("Passzone");

        if (obstacleLayer < 0)
            Debug.LogError(
                "PlayerCollision: Deadzone 레이어가 없습니다.",
                this
            );

        if (passzoneLayer < 0)
            Debug.LogError(
                "PlayerCollision: Passzone 레이어가 없습니다.",
                this
            );

        if (skill == null)
            TryGetComponent(out skill);
    }

    /// <summary>
    /// 아직 일반 Collider를 사용하는 장애물이 있다면 유지.
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer != obstacleLayer)
            return;

        HandleObstacleHit();
    }

    private void OnTriggerEnter(Collider other)
    {
        var gm = GameManager.Instance;

        if (gm == null || !gm.IsPlaying)
            return;

        // 1. Happiness 수집물
        HappinessCollectible collectible =
            other.GetComponentInParent<HappinessCollectible>();

        if (collectible != null)
        {
            collectible.Collect();
            return;
        }

        // 2. 장애물 Trigger
        if (other.gameObject.layer == obstacleLayer)
        {
            HandleObstacleHit();
            return;
        }

        // 3. PassZone
        if (other.gameObject.layer == passzoneLayer)
        {
            if (skill != null)
                skill.AddPassGauge();

            return;
        }
    }

    private void HandleObstacleHit()
    {
        var gm = GameManager.Instance;

        if (gm == null || !gm.IsPlaying)
            return;

        // 실드 또는 방어 직후 무적 상태라면 충돌 무효
        if (skill != null && skill.TryConsumeShield())
            return;

        gm.EndGame();
    }
}