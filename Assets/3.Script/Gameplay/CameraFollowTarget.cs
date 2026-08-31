using UnityEngine;

public sealed class CameraFollowTarget : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Play Area")]
    [SerializeField] private PlayerMovementConfig movementConfig;

    [Header("Camera Follow Margin")]
    [SerializeField] private float horizontalMargin = 3f;
    [SerializeField] private float verticalMargin = 4f;

    private void LateUpdate()
    {
        if (player == null || movementConfig == null)
            return;

        Vector2 center = movementConfig.boundsCenter;
        Vector2 size = movementConfig.boundsSize;

        float halfWidth = size.x * 0.5f;
        float halfHeight = size.y * 0.5f;

        float minX =
            center.x - halfWidth + horizontalMargin;

        float maxX =
            center.x + halfWidth - horizontalMargin;

        float minY =
            center.y - halfHeight + verticalMargin;

        float maxY =
            center.y + halfHeight - verticalMargin;

        Vector3 targetPosition = player.position;

        targetPosition.x =
            Mathf.Clamp(targetPosition.x, minX, maxX);

        targetPosition.y =
            Mathf.Clamp(targetPosition.y, minY, maxY);

        // Z는 플레이어 기준 유지
        transform.position = targetPosition;
    }
}