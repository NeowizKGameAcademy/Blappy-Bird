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

    [Tooltip("인접 칸이 막혀있을 때 다시 확인하는 시간")]
    [SerializeField] private float blockedRetryDelay = 0.15f;

    private int[] currentPointIndices;

    private readonly HashSet<int> occupiedPoints = new();
    private readonly HashSet<int> reservedPoints = new();

    private void OnEnable()
    {
        InitializeGates();
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        occupiedPoints.Clear();
        reservedPoints.Clear();
    }

    private void InitializeGates()
    {
        // Pool에서 다시 등장했을 수도 있으므로 상태 초기화
        StopAllCoroutines();

        occupiedPoints.Clear();
        reservedPoints.Clear();

        if (gates == null || gridPoints == null)
            return;

        if (gates.Length != startPointIndices.Length)
        {
            Debug.LogError(
                "MovingGateController: Gates 개수와 StartPointIndices 개수가 다릅니다.",
                this
            );

            return;
        }

        currentPointIndices = new int[gates.Length];

        for (int i = 0; i < gates.Length; i++)
        {
            int startIndex = startPointIndices[i];

            if (startIndex < 0 || startIndex >= gridPoints.Length)
                continue;

            currentPointIndices[i] = startIndex;

            // ★ World Position이 아니라 Local Position
            gates[i].localPosition =
                gridPoints[startIndex].localPosition;

            occupiedPoints.Add(startIndex);

            StartCoroutine(MoveGateRoutine(i));
        }
    }

    private IEnumerator MoveGateRoutine(int gateIndex)
    {
        if (randomStartDelay > 0f)
        {
            yield return new WaitForSeconds(
                Random.Range(0f, randomStartDelay)
            );
        }

        while (true)
        {
            // 정상적인 이동 후 대기
            yield return new WaitForSeconds(waitTime);

            int currentIndex =
                currentPointIndices[gateIndex];

            int nextIndex =
                GetRandomAvailableNeighbor(currentIndex);

            // 갈 곳이 없으면 긴 waitTime을 다시 기다리지 않고
            // 잠깐 후 다시 탐색
            while (nextIndex == -1)
            {
                yield return new WaitForSeconds(
                    blockedRetryDelay
                );

                nextIndex =
                    GetRandomAvailableNeighbor(currentIndex);
            }

            // 목표 위치 예약
            reservedPoints.Add(nextIndex);

            Vector3 startPosition =
                gates[gateIndex].localPosition;

            Vector3 targetPosition =
                gridPoints[nextIndex].localPosition;

            float elapsed = 0f;

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;

                float t =
                    Mathf.Clamp01(
                        elapsed / moveDuration
                    );

                // 출발/도착을 조금 부드럽게
                float smoothT =
                    Mathf.SmoothStep(0f, 1f, t);

                gates[gateIndex].localPosition =
                    Vector3.Lerp(
                        startPosition,
                        targetPosition,
                        smoothT
                    );

                yield return null;
            }

            gates[gateIndex].localPosition =
                targetPosition;

            // 기존 위치 해제
            occupiedPoints.Remove(currentIndex);

            // 새로운 위치 점유
            occupiedPoints.Add(nextIndex);

            currentPointIndices[gateIndex] =
                nextIndex;

            // 목표 예약 해제
            reservedPoints.Remove(nextIndex);
        }
    }

    private int GetRandomAvailableNeighbor(
        int currentIndex)
    {
        List<int> neighbors =
            GetNeighbors(currentIndex);

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

        return available[
            Random.Range(0, available.Count)
        ];
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