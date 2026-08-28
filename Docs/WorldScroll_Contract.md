# World Scroll 계약

`feature/world-chunk` (2B) 산출. 2A / 1B / 2C / 3 담당자가 이 API에 맞춰 병렬 작업할 수 있습니다.
2B의 청크 구현을 기다릴 필요가 없습니다.

## 좌표계 전제

```text
플레이어는 Z = 0에 고정, 월드가 -Z 방향으로 흐른다.
```

기획서 v1.4와 다릅니다. §1 "실제 +Z 전진", §5 "매 FixedUpdate마다 +Z 방향 속도 유지",
§14 "플레이어는 실제 월드 좌표에서 +Z 방향으로 이동한다" → 모두 트레드밀 방식으로 변경되었습니다.

기획서의 `forwardSpeed` / `targetForwardSpeed`는 전부 `scrollSpeed`로 읽으십시오.

## API

```csharp
public sealed class WorldScrollManager : MonoBehaviour
{
    public static WorldScrollManager Instance { get; }

    public float CurrentSpeed { get; }       // m/s
    public bool  IsScrolling  { get; }
    public float FixedScrollDelta { get; }   // 정지 중에는 0을 반환

    public void SetSpeed(float speed);       // 3  DifficultyManager
    public void StartScroll();               // 2C GameManager (Playing 진입)
    public void StopScroll();                // 2C GameManager (GameOver 진입)
    public void ResetRun();                  // 2C GameManager (Retry)
}
```

`FixedScrollDelta`는 `IsScrolling` 검사를 이미 포함합니다. 소비자는 이 값만 쓰면 되고,
정지 상태를 따로 검사할 필요가 없습니다.

## 브랜치별 사용법

### 2A obstacle-core

장애물 공통 이동은 `ScrollingObject`가 담당하고, 회전/개폐 같은 고유 동작은 별도로 처리합니다.

```csharp
void FixedUpdate()
{
    float delta = WorldScrollManager.Instance.FixedScrollDelta;
    if (delta <= 0f) return;
    rb.MovePosition(rb.position + Vector3.back * delta);
}
```

Rigidbody 설정이 필수입니다. 아래 "주의" 1번을 반드시 확인하십시오.

### 1B camera

- 플레이어가 Z 이동을 하지 않으므로 카메라도 Z 고정. X/Y만 부드럽게 추적합니다.
- 기획서 §6의 "전진 속도에 따라 FOV 소폭 증가"는 `CurrentSpeed`를 참조합니다.

### 3 obstacle-spawner

```csharp
WorldScrollManager.Instance.SetSpeed(stage.scrollSpeed);

// 도달 가능 검사 (기획서 13.1-5)
float arrivalTime = spawnDistance / WorldScrollManager.Instance.CurrentSpeed;
// arrivalTime 안에 플레이어가 현재 X/Y 이동 성능으로 안전 구역까지 갈 수 있는가?
```

스폰 위치는 이제 플레이어 상대 좌표가 아니라 **월드 상수**입니다. `z = +45~55` 고정.
Despawn도 후방 고정 평면(`z < -15` 등)으로 처리하면 됩니다.

### 2C game-flow

```text
Playing  진입 → StartScroll()
GameOver 진입 → StopScroll()
Retry         → ResetRun()
Paused        → 호출 불필요
```

Pause는 `Time.timeScale = 0`이라 FixedUpdate 자체가 멈춥니다.
`StopScroll()`은 `CurrentSpeed`를 보존하므로 재개 시 속도를 다시 조회할 필요가 없습니다.

## 주의 4가지

### 1. 장애물에 Kinematic Rigidbody + ContinuousSpeculative

프레임을 뒤집으면서 **빠르게 움직이는 물체가 플레이어에서 장애물로 넘어갔습니다.**

| 설정 | 값 |
|---|---|
| Rigidbody | Kinematic |
| collisionDetectionMode | **ContinuousSpeculative** |
| 이동 | `rb.MovePosition()` — `transform.position` 대입 금지 |

기본값(Discrete)으로 두면 14 m/s에서 장애물이 플레이어를 그냥 통과합니다.
Rigidbody 없이 콜라이더만 옮기면 PhysX가 매 프레임 static broadphase를 다시 굽습니다.

배경 Chunk는 콜라이더가 최소이므로 transform 이동으로 충분합니다 (2B가 처리).

### 2. Time Slow를 스크롤 속도에 이중 적용하지 말 것

Time Slow는 `Time.timeScale`로 구현됩니다. 스크롤은 `Time.fixedDeltaTime`을 쓰므로
**자동으로 감속됩니다.** 여기에 `SetSpeed(speed * 0.6f)`를 또 걸면 0.36배가 됩니다.

스크롤 쪽에서는 아무것도 하지 마십시오.

### 3. 판정 트리거 두께는 0.3m 이상

14 m/s / fixedDeltaTime 0.02s = 스텝당 0.28m 이동.
Perfect / NearMiss / Pass 트리거가 이보다 얇으면 통과가 누락됩니다.

### 4. 장애물을 Chunk에 넣지 말 것

기획서 §14 원칙 그대로입니다. 청크에 고정하면 난이도 조절(§13),
도달 가능 검사(§13.1-5), 반복 패턴 제한(§13.1-2)이 모두 불가능해집니다.

배경 요소(구름·부유섬·탑·새장 잔해)만 Chunk에 넣고, 장애물은 Spawner + Pool로 관리합니다.

## 2B에 남은 작업

계약과 무관하게 진행되므로 다른 브랜치를 막지 않습니다.

- Chunk Prefab 2~3종
- `WorldSystems.prefab` (매니저 2개 묶음)
- Dev 씬 동작 검증

## 씬 수정 규칙

GameScene은 통합 담당자만 수정합니다 (기획서 §24).
각 브랜치는 Prefab을 완성해서 올리고, 로컬 검증은 `Assets/1.Scene/Dev/`에서 하십시오.
해당 폴더는 `.gitignore`에 등록되어 있습니다.
