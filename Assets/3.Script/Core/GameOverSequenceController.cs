using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class GameOverSequenceController : MonoBehaviour
{
    [Header("Camera Shake")]
    [SerializeField]
    private CinemachineImpulseSource impulseSource;

    [Header("Shadow")]
    [SerializeField]
    private GameObject shadowObject;

    [Header("UI")]
    [SerializeField]
    private GameObject gameOverUI;

    [Header("Timing")]
    [SerializeField]
    private float shadowDelay = 0.25f;

    [SerializeField]
    private float uiDelay = 0.4f;

    private Coroutine sequenceRoutine;

    public void PlayGameOverSequence()
    {
        if (sequenceRoutine != null)
            return;

        sequenceRoutine =
            StartCoroutine(GameOverSequence());
    }

    private IEnumerator GameOverSequence()
    {
        // 1. 충돌 직후 Camera Shake
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse();
        }

        // 2. 흑막 등장까지 대기
        yield return new WaitForSecondsRealtime(shadowDelay);

        // 3. 흑막 등장
        if (shadowObject != null)
        {
            shadowObject.SetActive(true);
        }

        // 4. Game Over UI 등장까지 대기
        yield return new WaitForSecondsRealtime(uiDelay);

        // 5. Game Over UI 표시
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
        }

        sequenceRoutine = null;
    }

    public void ResetSequence()
    {
        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }

        if (shadowObject != null)
            shadowObject.SetActive(false);

        if (gameOverUI != null)
            gameOverUI.SetActive(false);
    }
}