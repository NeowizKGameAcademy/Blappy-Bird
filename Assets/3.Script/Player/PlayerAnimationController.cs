using UnityEngine;

/// <summary>
/// 플레이어 표현 계층. 기획서 5의 책임 분리:
/// PlayerController는 위치/속도만, 외형 Roll과 애니메이션은 여기서.
///
/// 기획서 6: "PlayerVisual 자식만 이동 방향으로 ±8° 수준 Roll.
/// 실제 Rigidbody 회전은 최소화."
/// Rigidbody는 FreezeRotation이므로 물리에는 아무 영향이 없다.
/// </summary>
public sealed class PlayerAnimationController : MonoBehaviour, IRunResettable
{
    private static readonly int IsFalling = Animator.StringToHash("isFalling");

    [Header("References")]
    [SerializeField] private PlayerController controller;
    [SerializeField] private Transform visual;
    [SerializeField] private Animator animator;

    [Header("Roll")]
    [Tooltip("최대 기울기(도). 기획서 6: ±8° 수준에서 시작")]
    [SerializeField] private float maxRollDegrees = 8f;

    [Tooltip("기울기 반응 속도. 클수록 빨리 따라간다")]
    [SerializeField] private float rollResponse = 10f;

    private float roll;

    private void LateUpdate()
    {
        if (controller == null || visual == null) return;

        // 오른쪽 이동(+X)일 때 오른쪽으로 기울인다(-Z 회전)
        float target = -(controller.HorizontalVelocity / controller.MaxHorizontalSpeed) * maxRollDegrees;

        roll = StepRoll(roll, target, rollResponse, Time.deltaTime);
        visual.localRotation = Quaternion.Euler(0f, 0f, roll);

        if (animator != null) animator.SetBool(IsFalling, controller.VerticalVelocity < 0f);
    }

    /// <summary>
    /// 프레임레이트와 무관한 지수 감쇠. Pause(timeScale 0)에서는 dt=0이라 멈춘다.
    /// 순수 함수라 배치모드 검증이 dt를 직접 넣어 확인할 수 있다.
    /// (internal은 Editor 어셈블리에서 보이지 않으므로 public)
    /// </summary>
    public static float StepRoll(float current, float target, float response, float dt)
    {
        return Mathf.LerpAngle(current, target, 1f - Mathf.Exp(-response * dt));
    }

    public void ResetRun()
    {
        roll = 0f;
        if (visual != null) visual.localRotation = Quaternion.identity;
    }
}
