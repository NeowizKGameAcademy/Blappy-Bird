using UnityEngine;

/// <summary>
/// 씬 BGM. 씬마다 하나씩 두고 DontDestroyOnLoad는 하지 않는다.
///
/// 씬이 바뀌면 곡도 바뀌므로(IntroBGM / PlaySceneBGM) 이어서 재생할 이유가 없다.
/// 씬 전환 시 재생 중이던 소리가 잘리는 것은 감수한다.
///
/// AudioSource 설정을 인스펙터에 맡기지 않고 코드에서 잡는다.
/// Loop나 Spatial Blend를 빠뜨리면 증상이 미묘해서 찾기 어렵다.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public sealed class BgmPlayer : MonoBehaviour
{
    [Header("Clip")]
    [Tooltip("이 씬에서 반복 재생할 곡. 비워두면 아무것도 재생하지 않는다.")]
    [SerializeField] private AudioClip clip;

    [SerializeField, Range(0f, 1f)] private float volume = 0.5f;

    [Header("Time Slow")]
    [Tooltip("희망의 날갯짓 중 pitch를 함께 내린다. 스킬이 없는 인트로·랭킹 씬은 꺼둔다.")]
    [SerializeField] private bool followTimeSlow;

    [Tooltip("초당 pitch 변화량. 4면 1.0 -> 0.6 전환에 0.1초 걸린다. 0이면 즉시 바뀌어 글리치로 들린다.")]
    [SerializeField] private float pitchChangeSpeed = 4f;

    [Header("Game Over")]
    [Tooltip("게임오버 중 볼륨 배율. 1이면 줄이지 않는다.")]
    [SerializeField, Range(0f, 1f)] private float gameOverVolumeScale = 0.3f;

    [Tooltip("초당 볼륨 변화량. 1이면 0.5 -> 0.15 전환에 0.35초 걸린다.")]
    [SerializeField] private float volumeChangeSpeed = 1f;

    private AudioSource source;

    /// <summary>게임오버 중인지. 볼륨 목표값을 정하는 데만 쓴다.</summary>
    private bool ducked;

    private void Awake()
    {
        // 씬 파일에 컴포넌트를 직접 기술한 경우 RequireComponent가 붙여주지 않는다.
        source = GetComponent<AudioSource>();
        if (source == null)
            source = gameObject.AddComponent<AudioSource>();

        source.clip = clip;
        source.volume = volume;
        source.loop = true;
        source.playOnAwake = false;

        // 2D. 리스너와의 거리에 따라 볼륨이 흔들리지 않게 한다.
        source.spatialBlend = 0f;

        if (clip != null)
            source.Play();
    }

    private void Start()
    {
        // GameManager는 부트스트랩 순서상 Awake 시점에 없을 수 있어 Start에서 붙는다.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnStateChanged += HandleStateChanged;
            HandleStateChanged(GameManager.Instance.CurrentState);
        }
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnStateChanged -= HandleStateChanged;
    }

    /// <summary>Retry로 Ready에 돌아가면 자동으로 원래 볼륨으로 복귀한다.</summary>
    private void HandleStateChanged(GameState state)
    {
        ducked = state == GameState.GameOver;
    }

    private void Update()
    {
        // GameOver는 timeScale을 1로 두므로(연출을 멈추지 않기 위해) 여기서도 unscaled를 쓸 이유는
        // 슬로우 쪽과 같다. 보간이 슬로우에 끌려가지 않게 한다.
        float dt = Time.unscaledDeltaTime;

        float targetVolume = volume * (ducked ? gameOverVolumeScale : 1f);
        source.volume = volumeChangeSpeed > 0f
            ? Mathf.MoveTowards(source.volume, targetVolume, volumeChangeSpeed * dt)
            : targetVolume;

        if (!followTimeSlow)
            return;

        // AudioSource는 Time.timeScale의 영향을 받지 않는다.
        // 스크롤과 정반대로, 여기서는 직접 걸어주지 않으면 아무 일도 일어나지 않는다.
        //
        // Current(세 채널의 최솟값)를 쓰면 히트스톱 0.1과 Pause 0까지 따라가서
        // BGM이 씹히거나 얼어붙는다. TimeSlow 채널만 본다.
        TimeScaleManager tsm = TimeScaleManager.Instance;
        float targetPitch = tsm != null ? tsm.Get(TimeScaleChannel.TimeSlow) : 1f;

        if (Mathf.Approximately(source.pitch, targetPitch))
            return;

        float maxDelta = pitchChangeSpeed > 0f
            ? pitchChangeSpeed * dt
            : Mathf.Abs(targetPitch - source.pitch);

        source.pitch = Mathf.MoveTowards(source.pitch, targetPitch, maxDelta);
    }
}
