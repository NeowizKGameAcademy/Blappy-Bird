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

    private AudioSource source;

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

    private void Update()
    {
        if (!followTimeSlow)
            return;

        // AudioSource는 Time.timeScale의 영향을 받지 않는다.
        // 스크롤과 정반대로, 여기서는 직접 걸어주지 않으면 아무 일도 일어나지 않는다.
        //
        // Current(세 채널의 최솟값)를 쓰면 히트스톱 0.1과 Pause 0까지 따라가서
        // BGM이 씹히거나 얼어붙는다. TimeSlow 채널만 본다.
        TimeScaleManager tsm = TimeScaleManager.Instance;
        float target = tsm != null ? tsm.Get(TimeScaleChannel.TimeSlow) : 1f;

        if (Mathf.Approximately(source.pitch, target))
            return;

        // 슬로우가 걸린 상태에서 보간까지 느려지지 않도록 unscaled를 쓴다.
        float maxDelta = pitchChangeSpeed > 0f
            ? pitchChangeSpeed * Time.unscaledDeltaTime
            : Mathf.Abs(target - source.pitch);

        source.pitch = Mathf.MoveTowards(source.pitch, target, maxDelta);
    }
}
