using UnityEngine;

/// <summary>
/// 씬 안의 효과음 재생 창구. 원샷 전용이다.
///
/// 씬마다 하나씩 두고 DontDestroyOnLoad는 하지 않는다.
/// BGM은 각 씬의 별도 AudioSource가 Play On Awake + Loop로 직접 재생하므로
/// 이 클래스는 BGM을 알지 못한다.
///
/// 풀로 돌아가거나 파괴되는 오브젝트(HappinessCollectible 등)가
/// 자기 AudioSource로 재생하면 비활성화되면서 소리가 끊긴다.
/// 그런 소리는 전부 여기로 보낸다.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public sealed class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

    [Header("Clips")]
    [Tooltip("날갯짓. PlayerController가 호출한다.")]
    [SerializeField] private AudioClip flapClip;

    [Tooltip("게임오버 진입 시. GameOverlayController가 호출한다.")]
    [SerializeField] private AudioClip gameOverClip;

    [Tooltip("행복 수집 시. HappinessCollectible이 호출한다.")]
    [SerializeField] private AudioClip happinessClip;

    [Tooltip("희망의 날갯짓 발동 시. PlayerSkillController가 호출한다.")]
    [SerializeField] private AudioClip timeSlowClip;

    [Tooltip("장애물 충돌 시. PlayerCollision이 호출한다.")]
    [SerializeField] private AudioClip bonkClip;

    [Header("Playback")]
    [Tooltip("재생마다 피치를 ±이 값만큼 흔든다. 0이면 끈다. 연타 시 기계적으로 들리는 것을 막는다.")]
    [SerializeField, Range(0f, 0.3f)] private float pitchJitter = 0f;

    private AudioSource source;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // 씬 파일에 컴포넌트를 직접 기술한 경우 RequireComponent가 붙여주지 않는다.
        source = GetComponent<AudioSource>();
        if (source == null)
            source = gameObject.AddComponent<AudioSource>();

        source.playOnAwake = false;
        source.loop = false;

        // 2D. 리스너와의 거리에 따라 볼륨이 흔들리지 않게 한다.
        source.spatialBlend = 0f;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>날갯짓.</summary>
    public void PlayFlap() => Play(flapClip);

    /// <summary>게임오버 진입.</summary>
    public void PlayGameOver() => Play(gameOverClip);

    /// <summary>행복 수집.</summary>
    public void PlayHappiness() => Play(happinessClip);

    /// <summary>희망의 날갯짓 발동.</summary>
    public void PlayTimeSlow() => Play(timeSlowClip);

    /// <summary>장애물 충돌.</summary>
    public void PlayBonk() => Play(bonkClip);

    /// <summary>원샷 재생. clip이 null이면 아무 일도 하지 않는다.</summary>
    public void Play(AudioClip clip, float volumeScale = 1f)
    {
        if (clip == null)
            return;

        // 슬로우에 맞춰 효과음도 함께 늘어지게 한다.
        // Current(세 채널의 최솟값)를 쓰면 히트스톱 0.1과 Pause 0까지 따라가므로
        // TimeSlow 채널만 본다. BgmPlayer와 같은 기준이다.
        TimeScaleManager tsm = TimeScaleManager.Instance;
        float scale = tsm != null ? tsm.Get(TimeScaleChannel.TimeSlow) : 1f;

        // PlayOneShot은 소스의 현재 pitch를 쓴다. 재생 중인 다른 원샷의 피치도 함께
        // 바뀌지만, 짧은 소리에서는 들리지 않는다.
        float jitter = pitchJitter > 0f ? Random.Range(-pitchJitter, pitchJitter) : 0f;
        source.pitch = scale * (1f + jitter);

        source.PlayOneShot(clip, volumeScale);
    }
}
