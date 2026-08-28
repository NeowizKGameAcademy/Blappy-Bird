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

        // 4A: 여기서 skill.TryConsumeShield() 성공 시 무적 0.5초 후 return (기획서 10.2)
        gm.EndGame();
    }

    private void OnTriggerEnter(Collider other)
    {
        var gm = GameManager.Instance;
        if (gm == null || !gm.IsPlaying) return;

        if (other.gameObject.layer == passzoneLayer && skill != null)
            skill.AddPassGauge();
    }
}
