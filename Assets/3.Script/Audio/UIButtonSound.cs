using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI 버튼 하나의 hover / press 사운드. 버튼마다 붙인다.
///
/// Button의 OnClick 배선은 건드리지 않는다. 기능 호출과 소리가 별개 경로라
/// 사운드를 넣다가 버튼 기능이 깨질 일이 없다.
///
/// press를 Down 시점으로 잡은 이유:
/// Button.onClick은 뗄 때(up) 발동하므로 거기에 걸면 눌린 표시보다 소리가 늦고,
/// 씬을 전환하는 버튼(Start · Ranking)에서는 씬이 파괴되며 소리가 잘린다.
/// </summary>
public sealed class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler
{
    [Header("Clips")]
    [Tooltip("마우스가 올라올 때. 비워두면 재생하지 않는다.")]
    [SerializeField] private AudioClip hoverClip;

    [Tooltip("누르는 순간. 비워두면 재생하지 않는다.")]
    [SerializeField] private AudioClip pressClip;

    /// <summary>같은 오브젝트의 Button. 비활성 버튼에서 소리가 나지 않게 검사한다.</summary>
    private Selectable selectable;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
    }

    public void OnPointerEnter(PointerEventData eventData) => Play(hoverClip);

    public void OnPointerDown(PointerEventData eventData) => Play(pressClip);

    private void Play(AudioClip clip)
    {
        // interactable이 false여도 포인터 이벤트 자체는 들어온다.
        // 눌리지 않는 버튼에서 소리만 나는 것을 막는다.
        if (selectable != null && !selectable.interactable)
            return;

        if (SoundManager.Instance != null)
            SoundManager.Instance.Play(clip);
    }
}
