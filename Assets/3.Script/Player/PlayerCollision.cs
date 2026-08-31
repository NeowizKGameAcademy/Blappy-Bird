using UnityEngine;

/// <summary>
/// 플레이어 충돌/트리거 판정.
///
/// Obstacle
/// - 실드가 있으면 충돌 1회 방어
/// - 없으면 GameOver
///
/// Passzone
/// - Skill Gauge 충전
///
/// HappinessCollectible
/// - 행복 수치 획득
/// </summary>
[RequireComponent(typeof(PlayerController))]
public sealed class PlayerCollision : MonoBehaviour
{
    [SerializeField]
    private PlayerSkillController skill;

    private int obstacleLayer = -1;
    private int passzoneLayer = -1;

    private void Awake()
    {
        obstacleLayer =
            LayerMask.NameToLayer("Deadzone");

        passzoneLayer =
            LayerMask.NameToLayer("Passzone");

        if (obstacleLayer < 0)
        {
            Debug.LogError(
                "PlayerCollision: Deadzone 레이어가 없습니다.",
                this
            );
        }

        if (passzoneLayer < 0)
        {
            Debug.LogError(
                "PlayerCollision: Passzone 레이어가 없습니다.",
                this
            );
        }

        if (skill == null)
        {
            TryGetComponent(out skill);
        }
    }

    /// <summary>
    /// 일반 Collider를 사용하는 장애물 대응.
    /// 가능하면 장애물은 Trigger 방식으로 통일하는 것을 권장.
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
        var collectible =
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
            {
                skill.AddPassGauge();
            }

            return;
        }
    }

    private void HandleObstacleHit()
    {
        var gm = GameManager.Instance;

        if (gm == null || !gm.IsPlaying)
            return;

        // 이미 피격 후 무적 상태라면 아무것도 하지 않음
        if (skill != null && skill.IsInvincible)
            return;

        // 실제 타격이 발생했으므로 Bonk
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayBonk();
        }

        // Shield가 켜져 있으면 이번 충돌 방어
        if (skill != null &&
            skill.TryConsumeShield())
        {
            return;
        }

        // Shield가 없으면 GameOver
        gm.EndGame();
    }
}