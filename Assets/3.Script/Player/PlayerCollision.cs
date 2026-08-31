using UnityEngine;

/// <summary>
/// 장애물/트리거 분기 (기획서 5의 책임 분리).
/// - 일반 콜라이더 충돌: GameOver. Shield(4A)가 붙으면 TryConsumeShield를 먼저 검사한다
/// - Passzone 레이어 트리거: 게이지 충전
/// </summary>
[RequireComponent(typeof(PlayerController))]
public sealed class PlayerCollision : MonoBehaviour
{
    [SerializeField] private PlayerSkillController skill;

    private int passzoneLayer = -1;

    private void Awake()
    {
        passzoneLayer = LayerMask.NameToLayer("Passzone");
        if (passzoneLayer < 0)
            Debug.LogError("PlayerCollision: Passzone 레이어가 없습니다.", this);
        if (skill == null) TryGetComponent(out skill);
    }

    private void OnCollisionEnter(Collision collision)
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.IsPlaying) return;

        // 타격음. 보호막으로 막아내든 죽든 부딪힌 것은 사실이므로 둘 다 울린다.
        //
        // 무적(피격 후 0.5초) 중에는 내지 않는다. 그 동안에는 장애물을 통과하는
        // 취급이고, 콜라이더에 스치면 OnCollisionEnter가 연달아 들어와
        // 같은 소리가 연타로 겹쳐 들린다.
        if ((skill == null || !skill.IsInvincible) && SoundManager.Instance != null)
            SoundManager.Instance.PlayBonk();

        // 보호막(E로 발동)이 켜져 있으면 충돌 1회 무효 + 무적 0.5초 (기획서 10.2)
        if (skill != null && skill.TryConsumeShield()) return;

        gm.EndGame();
    }

    private void OnTriggerEnter(Collider other)
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.IsPlaying) return;

        // 수집물 (기획서 5: 장애물/수집물 Trigger 분기)
        var collectible = other.GetComponentInParent<HappinessCollectible>();
        if (collectible != null)
        {
            collectible.Collect();
            return;
        }

        if (other.gameObject.layer == passzoneLayer && skill != null)
            skill.AddPassGauge();
    }
}
