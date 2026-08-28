using UnityEngine;

public class PropellerController : MonoBehaviour
{
    public enum RotationDirection
    {
        Clockwise,
        CounterClockwise
    }

    [SerializeField]
    private RotationDirection direction =
        RotationDirection.Clockwise;

    [SerializeField]
    private float rotationSpeed = 90f;

    [Header("Rotation Pivot")]
    [SerializeField]
    private float pivotYOffset = 3f;

    private Vector3 pivotLocalPosition;

    private void Awake()
    {
        // 회전 중심
        pivotLocalPosition =
            transform.localPosition + Vector3.up * pivotYOffset;
    }

    private void Update()
    {
        float dir =
            direction == RotationDirection.Clockwise
            ? -1f
            : 1f;

        float angle =
            rotationSpeed * dir * Time.deltaTime;

        Vector3 pivotWorldPosition;

        if (transform.parent != null)
        {
            pivotWorldPosition =
                transform.parent.TransformPoint(pivotLocalPosition);
        }
        else
        {
            pivotWorldPosition =
                pivotLocalPosition;
        }

        transform.RotateAround(
            pivotWorldPosition,
            Vector3.forward,
            angle
        );
    }
}