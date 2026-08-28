/// <summary>
/// 풀에서 꺼내거나 되돌릴 때 상태를 초기화해야 하는 오브젝트가 구현한다.
/// 선택 사항이다. PoolManager는 구현되어 있을 때만 호출한다.
/// 정적 게이트처럼 되돌릴 상태가 없으면 구현하지 않아도 된다.
/// </summary>
public interface IPoolable
{
    void OnSpawned();
    void OnDespawned();
}
