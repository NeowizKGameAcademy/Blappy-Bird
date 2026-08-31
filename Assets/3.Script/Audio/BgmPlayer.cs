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

    private void Awake()
    {
        // 씬 파일에 컴포넌트를 직접 기술한 경우 RequireComponent가 붙여주지 않는다.
        AudioSource source = GetComponent<AudioSource>();
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
}
