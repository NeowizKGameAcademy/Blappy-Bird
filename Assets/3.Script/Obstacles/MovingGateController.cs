using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingGateController : MonoBehaviour
{
    [Header("Gates")]
    [SerializeField] private Transform[] gates;

    [Header("Grid Points")]
    [SerializeField] private Transform[] gridPoints;

    [Header("Start Points")]
    [SerializeField] private int[] startPointIndices = { 0, 5, 7 };

    [Header("Movement")]
    [SerializeField] private float moveDuration = 0.6f;
    [SerializeField] private float waitTime = 1f;

    [Header("Option")]
    [SerializeField] private float randomStartDelay = 0.3f;

    private int[] currentPointIndices;

    // 현재 Gate가 차지하고 있는 위치
    private readonly HashSet<int> occupiedPoints = new();

    // 다른 Gate가 이동하려고 예약한 위치
    private readonly HashSet<int> reservedPoints = new();

    private void Start()
    {
        currentPointIndices = new int[gates.Length];

        for (int i = 0; i < gates.Length; i++)
        {
            int startIndex = startPointIndices[i];

            currentPointIndices[i] = startIndex;

            gates[i].position = gridPoints[startIndex].position;

            occupiedPoints.Add(startIndex);

            StartCoroutine(MoveGateRoutine(i));
        }
    }

    private IEnumerator MoveGateRoutine(int gateIndex)
    {
        // 세 Gate가 완전히 동시에 움직이지 않게 약간 차이를 줌
        if (randomStartDelay > 0f)
        {
            yield return new WaitForSeconds(
                Random.Range(0f, randomStartDelay)
            );
        }

        while (true)
        {
            yield return new WaitForSeconds(waitTime);

            int currentIndex = currentPointIndices[gateIndex];

            int nextIndex = GetRandomAvailableNeighbor(currentIndex);

            // 갈 수 있는 곳이 없으면 이번 턴은 대기
            if (nextIndex == -1)
            {
                continue;
            }

            // 다른 Gate가 같은 곳을 선택하지 못하도록 예약
            reservedPoints.Add(nextIndex);

            Vector3 startPosition = gates[gateIndex].position;
            Vector3 targetPosition = gridPoints[nextIndex].position;

            float elapsed = 0f;

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;

                float t = Mathf.Clamp01(elapsed / moveDuration);

                gates[gateIndex].position =
                    Vector3.Lerp(
                        startPosition,
                        targetPosition,
                        t
                    );

                yield return null;
            }

            gates[gateIndex].position = targetPosition;

            // 기존 위치 해제
            occupiedPoints.Remove(currentIndex);

            // 새로운 위치 점유
            occupiedPoints.Add(nextIndex);

            currentPointIndices[gateIndex] = nextIndex;

            // 예약 해제
            reservedPoints.Remove(nextIndex);
        }
    }

    private int GetRandomAvailableNeighbor(int currentIndex)
    {
        List<int> neighbors = GetNeighbors(currentIndex);

        List<int> available = new();

        foreach (int index in neighbors)
        {
            if (occupiedPoints.Contains(index))
                continue;

            if (reservedPoints.Contains(index))
                continue;

            available.Add(index);
        }

        if (available.Count == 0)
            return -1;

        return available[Random.Range(0, available.Count)];
    }

    private List<int> GetNeighbors(int index)
    {
        List<int> neighbors = new();

        int row = index / 3;
        int col = index % 3;

        // 위
        if (row > 0)
            neighbors.Add(index - 3);

        // 아래
        if (row < 2)
            neighbors.Add(index + 3);

        // 왼쪽
        if (col > 0)
            neighbors.Add(index - 1);

        // 오른쪽
        if (col < 2)
            neighbors.Add(index + 1);

        return neighbors;
    }
}