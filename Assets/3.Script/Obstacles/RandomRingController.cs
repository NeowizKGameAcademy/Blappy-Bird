using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class RandomRingController : MonoBehaviour
{
    [Header("Rings")]
    [SerializeField] private Transform ringA;
    [SerializeField] private Transform ringB;

    [Header("Grid Points")]
    [SerializeField] private Transform[] gridPoints;

    private void OnEnable()
    {
        RandomizeRingPositions();
    }

    public void RandomizeRingPositions()
    {
        if (gridPoints == null || gridPoints.Length < 2)
        {
            Debug.LogWarning("GridPoint가 2개 이상 필요합니다.");
            return;
        }

        // 첫 번째 링 위치
        int indexA = Random.Range(0, gridPoints.Length);

        // 두 번째 링 위치
        int indexB;

        do
        {
            indexB = Random.Range(0, gridPoints.Length);
        }
        while (indexB == indexA);

        // 같은 부모 기준이라면 localPosition 사용
        ringA.localPosition = gridPoints[indexA].localPosition;
        ringB.localPosition = gridPoints[indexB].localPosition;
    }
}