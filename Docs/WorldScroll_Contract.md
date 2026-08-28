# World Scroll · Game Flow 계약

`feature/world-chunk` 산출. 2A / 1B / 3 / 4A / 4B / 5 담당자가 이 API에 맞춰 병렬 작업할 수 있습니다.

이 브랜치에는 원래 2B(월드 스크롤)만 들어갈 예정이었으나,
스크롤을 기동할 주체가 없으면 아무것도 검증할 수 없어 2C(게임 흐름)까지 함께 구현했습니다.

## 좌표계 전제

```text
플레이어는 Z = 0에 고정, 월드가 -Z 방향으로 흐른다.
```

기획서 v1.4와 다릅니다. §1 "실제 +Z 전진", §5 "매 FixedUpdate마다 +Z 방향 속도 유지",
§14 "플레이어는 실제 월드 좌표에서 +Z 방향으로 이동한다" → 모두 트레드밀 방식으로 변경되었습니다.

기획서의 `forwardSpeed` / `targetForwardSpeed`는 전부 `scrollSpeed`로 읽으십시오.

---

# 1. 월드 스크롤 (2B)

```csharp
public sealed class WorldScrollManager : MonoBehaviour, IRunResettable
{
    public static WorldScrollManager Instance { get; }

    public float CurrentSpeed { get; }       // m/s
    public bool  IsScrolling  { get; }
    public float FixedScrollDelta { get; }   // 정지 중에는 0

    public void SetSpeed(float speed);       // 3  DifficultyManager
    public void StartScroll();               // 2C GameSceneController
    public void StopScroll();                // 2C GameSceneController
    public void ResetRun();                  // Retry
}
```

`FixedScrollDelta`는 `IsScrolling` 검사를 이미 포함합니다.
소비자는 이 값만 쓰면 되고 정지 상태를 따로 검사할 필요가 없습니다.

`WorldChunkManager`는 배경 Chunk 순환만 담당하며 외부에서 참조할 일이 없습니다.

---

# 2. 게임 흐름 (2C)

```csharp
public enum GameState { Ready, Playing, Paused, GameOver }

public sealed class GameManager : MonoBehaviour   // DontDestroyOnLoad
{
    public static GameManager Instance { get; }

    public GameState CurrentState { get; }
    public bool      IsPlaying    { get; }
    public event Action<GameState> OnStateChanged;

    public void StartGame();       // Ready 또는 GameOver -> Playing
    public void PauseGame();       // Playing -> Paused
    public void ResumeGame();      // Paused  -> Playing
    public void EndGame();         // Playing/Paused -> GameOver
    public void SetReady();

    public void LoadMainScene();
    public void LoadGameScene();
    public void LoadRankingScene();
}
```

상태를 직접 폴링하지 말고 `OnStateChanged`를 구독하십시오.
같은 상태로의 재전환은 이벤트를 발행하지 않습니다.

## Intro를 상태에 넣지 않은 이유

기획서 §7 enum에는 `Intro`가 있지만 6A(intro-shadow)는 후순위 브랜치입니다.
상태로 박으면 6A가 끝나기 전까지 상태 머신에 빈 칸이 생깁니다.

`Ready` 단계에서 `GameSceneController`가 `IntroDirector`를 선택 실행한 뒤
`Playing`으로 넘기는 구조이므로, Intro가 나중에 붙어도 상태 머신은 그대로입니다.

```text
현재: GameScene 로드 -> Ready -> Playing
추후: GameScene 로드 -> Ready -> IntroDirector -> Playing
```

---

# 3. TimeScale (2C)

```csharp
public enum TimeScaleChannel { Pause, HitStop, TimeSlow }

public sealed class TimeScaleManager : MonoBehaviour
{
    public static TimeScaleManager Instance { get; }

    public float Current          { get; }   // 활성 채널의 최솟값
    public bool  IsTimeSlowActive { get; }
    public bool  IsPaused         { get; }

    public void  Set(TimeScaleChannel channel, float scale);   // 1f가 해제
    public float Get(TimeScaleChannel channel);

    public void SetPaused(bool value);      // GameManager 전용
    public void SetTimeSlow(float scale);   // 4A. 해제는 1f
    public void ResetRun();
}
```

**어떤 클래스도 `Time.timeScale`에 직접 쓰지 않습니다.** 이 클래스가 유일한 소유자입니다.

## 채널이 셋입니다

느리게 만드는 소스가 둘이므로 슬롯 하나로는 부족합니다.

- **희망의 날갯짓** (§10) — `Q` 입력, 게이지 100, 약 2초, 0.5~0.65
- **피격 히트스톱** (§6) — 0.1~0.15초

채널마다 독립적으로 배율을 요청하고, **실제 적용은 활성 채널의 최솟값**입니다.
최솟값 규칙 하나로 우선순위가 해결되므로 별도 우선순위 표가 필요 없습니다.

| 상황 | Pause | HitStop | TimeSlow | 적용 |
|---|---|---|---|---|
| 평상시 | 1 | 1 | 1 | **1** |
| Time Slow | 1 | 1 | 0.6 | **0.6** |
| Slow 중 피격 | 1 | 0.1 | 0.6 | **0.1** |
| 피격 중 Pause | 0 | 0.1 | 0.6 | **0** |
| Pause 해제 | 1 | 0.1 | 0.6 | **0.1** |

채널이 서로를 덮어쓰지 않으므로 **히트스톱이 끝나도 진행 중이던 Time Slow의 남은 시간이 유지됩니다.**
단일 슬롯 구조였다면 2초짜리 Time Slow가 0.15초 만에 풀렸습니다.

`SetPaused`와 `SetTimeSlow`는 `Set(channel, scale)`의 편의 메서드입니다.
히트스톱은 `Set(TimeScaleChannel.HitStop, 0.1f)`로 요청하고 `1f`로 해제합니다.

## GameOver를 우선순위에서 뺐습니다

`timeScale`을 0으로 만들면 기획서 §18의 "0.1~0.15초 충격 연출"과
결과 Overlay 애니메이션이 함께 멈춥니다.

GameOver의 정지는 `WorldScrollManager.StopScroll()`과
각 시스템의 상태 게이팅(`GameManager.IsPlaying`)이 담당합니다.

---

# 4. IRunResettable — 모든 브랜치가 구현합니다

```csharp
public interface IRunResettable
{
    void ResetRun();
}
```

`GameSceneController`가 씬 안의 구현체를 전부 찾아 호출합니다.
**메서드 하나만 구현하면 Retry에 자동으로 참여합니다.** 등록 절차는 없습니다.

| 브랜치 | ResetRun에서 되돌릴 것 | 상태 |
|---|---|---|
| 2B | 스크롤 속도, Chunk 시작 배치 | 구현됨 |
| 1A | Player X/Y 위치와 속도, 수직 속도 | 필요 |
| 1B | 카메라 위치 | 필요 |
| 2A · 1C | 활성 장애물 · 수집물 Pool 반환 | 필요 |
| 3 | Spawner 최근 패턴 기록, 난이도 Stage | 필요 |
| 4A | 게이지, Shield 준비 상태, 무적 타이머 | 필요 |
| 4B | SurvivalTimer, Happiness | 필요 |

---

# 5. 브랜치별 사용법

## 2A obstacle-core

공통 이동은 `ScrollingObject`가 담당하고, 회전·개폐 같은 고유 동작은 별도로 처리합니다.

```csharp
void FixedUpdate()
{
    float delta = WorldScrollManager.Instance.FixedScrollDelta;
    if (delta <= 0f) return;
    rb.MovePosition(rb.position + Vector3.back * delta);
}
```

Rigidbody 설정이 필수입니다. 아래 "주의" 1번을 반드시 확인하십시오.

## 1B camera

- 플레이어가 Z 이동을 하지 않으므로 카메라도 Z 고정. X/Y만 부드럽게 추적합니다.
- 기획서 §6의 "전진 속도에 따라 FOV 소폭 증가"는 `CurrentSpeed`를 참조합니다.

## 3 obstacle-spawner

```csharp
WorldScrollManager.Instance.SetSpeed(stage.scrollSpeed);

// 도달 가능 검사 (기획서 13.1-5)
float arrivalTime = spawnDistance / WorldScrollManager.Instance.CurrentSpeed;
```

스폰 위치는 플레이어 상대 좌표가 아니라 **월드 상수**입니다. `z = +45~55` 고정.
Despawn도 후방 고정 평면(`z < -15` 등)으로 처리하면 됩니다.

## 4A perfect-skill

```csharp
TimeScaleManager.Instance.SetTimeSlow(0.6f);   // 발동
TimeScaleManager.Instance.SetTimeSlow(1f);     // 해제
```

`Time.timeScale`을 직접 만지지 마십시오. Pause와 충돌합니다.

## 5 ui

`GameManager.OnStateChanged`를 구독해 화면을 전환합니다.
`GameSceneController.autoStartForDev`를 **끄고**, Start 버튼이 `StartGame()`을 호출하게 바꾸십시오.

---

# 6. 주의

## 1. 장애물에 Kinematic Rigidbody + ContinuousSpeculative

프레임을 뒤집으면서 **빠르게 움직이는 물체가 플레이어에서 장애물로 넘어갔습니다.**

| 설정 | 값 |
|---|---|
| Rigidbody | Kinematic |
| collisionDetectionMode | **ContinuousSpeculative** |
| 이동 | `rb.MovePosition()` — `transform.position` 대입 금지 |

기본값(Discrete)으로 두면 14 m/s에서 장애물이 플레이어를 그냥 통과합니다.
Rigidbody 없이 콜라이더만 옮기면 PhysX가 매 프레임 static broadphase를 다시 굽습니다.

배경 Chunk는 콜라이더가 최소이므로 transform 이동으로 충분합니다 (2B가 처리).

## 2. Time Slow를 스크롤 속도에 이중 적용하지 말 것

Time Slow는 `Time.timeScale`로 구현됩니다. 스크롤은 `Time.fixedDeltaTime`을 쓰므로
**자동으로 감속됩니다.** 여기에 `SetSpeed(speed * 0.6f)`를 또 걸면 0.36배가 됩니다.

스크롤 쪽에서는 아무것도 하지 마십시오.

## 3. 판정 트리거 두께는 0.3m 이상

14 m/s ÷ fixedDeltaTime 0.02s = 스텝당 **0.28m** 이동.
Perfect · NearMiss · Pass 트리거가 이보다 얇으면 통과가 누락됩니다.

## 4. 장애물을 Chunk에 넣지 말 것

기획서 §14 원칙 그대로입니다. 청크에 고정하면 난이도 조절(§13),
도달 가능 검사(§13.1-5), 반복 패턴 제한(§13.1-2)이 모두 불가능해집니다.

배경 요소(구름 · 부유섬 · 탑 · 새장 잔해)만 Chunk에 넣고, 장애물은 Spawner + Pool로 관리합니다.

---

# 7. 현재 상태

## 구현 완료

- `WorldScrollManager`, `WorldChunkManager` — 청크 순환, 누적 오차 없음
- `GameManager`, `GameState`, `TimeScaleManager`, `GameSceneController`, `IRunResettable`
- `WorldSystems.prefab` — 매니저 묶음. GameScene 배치는 통합 담당자 몫
- `Chunk_Placeholder.prefab` + URP 머티리얼 2종 — 6B에서 교체될 임시 아트

배치모드 검증 31항목 통과: 상태 전환, Pause/TimeSlow 우선순위,
GameOver 시 timeScale 유지, Retry 초기화, 30000스텝 후 청크 간격 유지.

## 씬 수정 규칙

GameScene은 통합 담당자만 수정합니다 (기획서 §24).
각 브랜치는 Prefab을 완성해서 올리고, 로컬 검증은 `Assets/1.Scene/Dev/`에서 하십시오.
해당 폴더는 `.gitignore`에 등록되어 있습니다.
