# Blappy-Bird 프로젝트 안내서

처음 이 프로젝트를 여는 사람을 위한 문서입니다.
"이게 무슨 게임이고, 코드가 어디에 어떻게 나뉘어 있는지"를 순서대로 설명합니다.

- 개발 환경: **Unity 6000.2.8f1**, URP(Universal Render Pipeline), New Input System
- 코드 규모: C# 스크립트 40개, 약 3,000줄
- 시스템 간 상세 규격은 별도 문서 [WorldScroll_Contract.md](WorldScroll_Contract.md)에 있습니다.
  (이 문서는 "전체 그림", 그쪽은 "정확한 약속")

---

## 1. 어떤 게임인가

파란 새를 조종해서 앞에서 날아오는 장애물을 피하며 **최대한 오래 버티는** 3D 플래피버드입니다.
뒤에서는 추격자(Chaser)가 계속 따라오고, 시간이 지날수록 세상이 점점 빨라집니다.
날아다니면서 행복(Happiness)을 모으면 기록이 올라갑니다.

### 조작

| 키 | 하는 일 |
|---|---|
| `Space` | 날갯짓 (점프) |
| 방향키 좌우 또는 `A` `D` | 좌우 이동 |
| `Q` | **희망의 날갯짓** — 세상이 잠깐 느려짐 (게이지 50 소모, 2초간 0.6배속) |
| `E` | **희망의 보호막** — 장애물에 한 번 부딪혀도 버팀 (게이지 100 소모) |
| `ESC` | 일시정지 / 재개 |

게이지는 장애물의 통과 구역(Passzone)을 지나갈 때마다 20씩 차고, 최대 200까지 모입니다.

---

## 2. 가장 먼저 알아야 할 것 — "새는 제자리에 있다"

이 프로젝트를 이해하는 열쇠입니다. 보기에는 새가 앞으로 날아가는 것 같지만, 실제로는 **런닝머신(트레드밀)** 구조입니다.

```
      [ 새 ]           <- 항상 Z = 0 에 고정. 위아래·좌우로만 움직인다
        |
   ===============
   장애물 · 배경이 -Z 방향으로 흘러온다   <<<<<
```

- 새는 **Z축으로 전혀 움직이지 않습니다.** 위아래(Y), 좌우(X)로만 움직입니다.
- 대신 장애물과 배경이 새 쪽으로 흘러옵니다.
- 그래서 코드에 "속도"라고 나오면 대부분 새의 속도가 아니라 **세상이 흘러오는 속도**(`scrollSpeed`)입니다.
- **"플레이어 뒤"는 -Z 쪽**입니다. 추격자가 z = -6 쯤에 있는 이유입니다.

> 기획서 원문에는 "새가 +Z로 전진한다"고 되어 있지만, 실제 구현은 위와 같이 바뀌었습니다.
> 기획서의 `forwardSpeed`는 전부 `scrollSpeed`로 읽으세요.

여기서 나오는 예외가 하나 있습니다. **추격자(Chaser)는 세상에 실려 흘러가면 안 됩니다.**
새와 같은 "고정된 세계"에 살아야 하므로, 다른 장애물과 달리 `ScrollingObject`를 붙이지 않습니다.

---

## 3. 한 판이 흘러가는 순서

게임 전체는 상태 4개짜리 아주 단순한 기계입니다.

```
  Ready  --StartGame()-->  Playing  --EndGame()-->  GameOver
                             ^  |                       |
                 ResumeGame()|  |PauseGame()            | Retry
                             |  v                       |
                           Paused                       +--> 다시 Playing
```

- 상태를 관리하는 건 `GameManager` **하나뿐**입니다.
- 다른 스크립트는 상태를 매 프레임 확인하지 말고, `GameManager.OnStateChanged` 이벤트를 **구독**해서 알림을 받습니다.
- 실제로 각 시스템을 켜고 끄는 건 `GameSceneController`입니다.
  (GameManager = "지금 무슨 상태냐", GameSceneController = "그 상태에서 뭘 할 거냐")

### 다시하기(Retry)가 동작하는 방식

다시하기를 누르면 `GameSceneController`가 씬 안에서 **`IRunResettable`을 구현한 스크립트를 전부 찾아** `ResetRun()`을 호출합니다.

즉 새 시스템을 만들 때 **등록 절차 같은 건 없습니다.** 인터페이스 하나만 구현하면 자동으로 초기화 대상이 됩니다.

```csharp
public sealed class MySystem : MonoBehaviour, IRunResettable
{
    public void ResetRun() { /* 한 판 상태를 처음으로 되돌린다 */ }
}
```

---

## 4. 폴더 지도

숫자 접두사는 Unity Project 창에서 보기 좋게 정렬하려고 붙인 것이고, 숫자에 특별한 의미는 없습니다.

| 폴더 | 들어있는 것 |
|---|---|
| `Assets/1.Scene` | 씬. `GameScene`(본편), `Chaser`, `Dev/`(개인 테스트용 샌드박스 3개) |
| `Assets/2.Model` | 3D 모델(.obj)과 **프리팹** — Player, Chaser, Sheld, SkyDome, WorldSystems, 게이트 3종, 장애물 3종 |
| `Assets/3.Script` | **모든 C# 코드** (아래 5장에서 상세히) |
| `Assets/4.Sprite` | 캐릭터 파츠 모델 + UI 이미지 (`Title`, `InGameUI`, `RankingUI`, `HowToPlayUI`) |
| `Assets/5.Animation` | 애니메이터 3종(Bird / Chaser / Title)과 클립(Jump, Fall, Chaser Idle, Title Move) |
| `Assets/6.Materials` | 머티리얼 (청크 바닥, 실드 등) |
| `Assets/8.Audio` | BGM 2곡 + 효과음 12개 |
| `Assets/9.Font` | Pretendard-Bold SDF (TextMeshPro용) |
| `Assets/10.Input` | Input System 액션 에셋 |
| `Assets/11.Data` | **수치 설정 파일** — `PlayerMovementConfig`, `PlayerSkillConfig` |
| `Assets/Settings` | URP 렌더링 설정, `TimeSlowVolumeProfile`(느려질 때 화면 효과) |
| `Assets/SimpleSky` | 외부에서 받은 하늘/구름 에셋 |
| `Docs` | 이 문서와 시스템 계약서 |

### 레이어

태그는 쓰지 않고 **레이어로 판정**합니다.

| 레이어 | 의미 |
|---|---|
| `Player` (6) | 플레이어 |
| `Deadzone` (7) | 닿으면 죽는 곳 (장애물 본체) |
| `Passzone` (8) | 통과하면 게이지가 차는 곳 (구멍 안쪽) |

---

## 5. 코드는 어떻게 나뉘어 있나

`Assets/3.Script/` 아래 폴더 8개입니다. **"하나의 일은 하나의 스크립트만 책임진다"**가 이 프로젝트의 일관된 원칙입니다.

### Core — 게임 전체의 흐름

| 파일 | 쉽게 말하면 |
|---|---|
| `GameState.cs` | 상태 이름 목록 (Ready / Playing / Paused / GameOver) |
| `GameManager.cs` | 지금 어떤 상태인지 관리하고 씬을 바꾼다. 게임 내내 살아있음 |
| `GameSceneController.cs` | 한 판을 시작·정지·초기화한다. ESC 일시정지도 여기 |
| `TimeScaleManager.cs` | 게임 속도(느리게/멈춤)를 **혼자서만** 조절한다 (아래 설명) |
| `IRunResettable.cs` | "다시하기 때 초기화 필요함" 표시용 인터페이스 |
| `GameOverSequenceController.cs` | 죽었을 때 연출 (카메라 흔들림, 그림자, UI 타이밍) |

**`TimeScaleManager`가 왜 따로 있나?**
게임을 느리게 만드는 이유가 여러 개(스킬 사용 / 피격 순간 / 일시정지)라서 서로 덮어쓰면 버그가 납니다.
그래서 채널 3개(`Pause`, `HitStop`, `TimeSlow`)가 각자 원하는 배속을 말하고, **그중 제일 느린 값**이 적용됩니다.
덕분에 히트스톱이 끝나도 진행 중이던 슬로우가 풀리지 않습니다.

> 주의: **어떤 코드도 `Time.timeScale`을 직접 건드리면 안 됩니다.** 반드시 이 매니저를 통해서.

### World — 세상이 흘러가는 부분

| 파일 | 쉽게 말하면 |
|---|---|
| `WorldScrollManager.cs` | "지금 세상이 얼마나 빠른가"를 아는 유일한 곳 |
| `WorldSpeedRamp.cs` | 오래 버틸수록 속도를 올린다 (9 -> 14 m/s, 180초에 걸쳐) |
| `WorldChunkManager.cs` | 배경 조각을 뒤로 흘려보내고, 지나간 건 다시 앞에 갖다 놓는다 (무한 배경) |

난이도 단계표 같은 건 없습니다. **난이도 = 살아남은 시간**, 그게 전부입니다.

### Player — 새

| 파일 | 쉽게 말하면 |
|---|---|
| `PlayerController.cs` | 실제 움직임. Space와 좌우 입력을 받아 위치를 바꾼다 |
| `PlayerCollision.cs` | 뭔가에 닿았을 때 판단 (죽음 / 게이지 충전 / 행복 획득) |
| `PlayerSkillController.cs` | Q·E 스킬과 게이지 |
| `GaugeController.cs` | 게이지 숫자만 담아두는 작은 클래스 |
| `PlayerAnimationController.cs` | 보이는 부분만 담당 — 이동 방향으로 몸을 8도쯤 기울인다 |
| `TimeSlowScreenEffect.cs` | 느려질 때 화면이 푸르게 물드는 효과 |
| `PlayArea.cs` | 새가 나갈 수 없는 영역(30 x 30) 계산 |
| `PlayerMovementConfig.cs` / `PlayerSkillConfig.cs` | **숫자를 코드에 박지 않기 위한 설정 파일 정의** |

`Config` 두 개가 중요한 이유: 점프 힘·중력 같은 값을 코드가 아니라 `Assets/11.Data`의 에셋 파일에 둡니다.
그래서 **밸런스를 고치려면 인스펙터에서 숫자만 바꾸면 되고, 코드는 건드릴 필요가 없습니다.**

### Obstacle / Obstacles — 장애물

이름이 비슷한 폴더가 두 개라 헷갈리기 쉽습니다. 기준은 이렇습니다.

- **`Obstacle/`(단수) = 공통 인프라.** 모든 장애물이 똑같이 하는 일.
  - `ObstacleSpawner.cs` — 3.5초마다 프리팹 하나를 골라 전방 100m에 놓는다
  - `ScrollingObject.cs` — 뒤로 흘러가다가 화면 밖으로 나가면 스스로 사라진다
- **`Obstacles/`(복수) = 장애물별 고유 동작.**
  - `MovingGateController.cs` — 게이트가 격자 위를 옮겨 다님
  - `PropellerController.cs` — 프로펠러 회전
  - `RandomRingController.cs` — 링 두 개를 무작위 위치에 배치

스포너는 **장애물이 어떻게 생겼는지 전혀 모릅니다.** 구멍 위치가 통과 가능한지는 프리팹을 만들 때 보장합니다.

### Gameplay — 점수·기록·카메라

| 파일 | 쉽게 말하면 |
|---|---|
| `SurvivalTimer.cs` | 생존 시간. 이 프로젝트에서 시간을 세는 곳은 여기 하나뿐 |
| `HappinessManager.cs` / `HappinessCollectible.cs` | 행복 수치와 수집 아이템 |
| `RankingManager.cs` | 기록을 `ranking.json`에 저장/정렬 (행복 많은 순 -> 오래 버틴 순) |
| `ChaserController.cs` | 뒤에서 따라오는 추격자 |
| `CameraFollowTarget.cs` | 카메라가 새를 부드럽게 따라감 (Z는 고정) |

### Pool — 재사용

`PoolManager.cs`, `IPoolable.cs`.
장애물을 계속 만들고 없애면 10분쯤 뒤에 프레임이 떨어집니다.
그래서 **다 쓴 오브젝트를 지우지 않고 창고에 넣어뒀다가 다시 꺼내 씁니다.**

### UI

`MainMenuController`(타이틀), `InGameHUDController`(시간·행복·게이지 표시),
`GameOverlayController`(일시정지/게임오버 창), `RankingScreenController`(랭킹 Top10).

HUD는 **아무것도 계산하지 않습니다.** 각 시스템이 보내주는 이벤트를 받아 화면에 쓰기만 합니다.

### Audio

`SoundManager`(효과음 창구), `BgmPlayer`(씬별 배경음), `UIButtonSound`(버튼 소리).

효과음을 각 오브젝트가 직접 내지 않고 `SoundManager`로 몰아준 이유:
행복 아이템처럼 **먹는 순간 사라지는 오브젝트가 자기 소리를 내면 소리도 같이 끊기기** 때문입니다.

---

## 6. 처음 코드를 읽는다면 이 순서로

1. `Docs/WorldScroll_Contract.md` 1~2장 — 좌표계와 상태 흐름
2. `Core/GameManager.cs` -> `Core/GameSceneController.cs` — 게임이 시작되는 지점
3. `World/WorldScrollManager.cs` — 세상이 움직이는 원리
4. `Player/PlayerController.cs` — 조작이 위치로 변환되는 과정
5. `Obstacle/ObstacleSpawner.cs` + `Obstacle/ScrollingObject.cs` — 장애물의 일생
6. 나머지는 필요할 때

---

## 7. 자주 하는 실수 (꼭 읽어주세요)

1. **장애물 Rigidbody 설정을 빠뜨리면 안 됩니다.**
   Kinematic + `ContinuousSpeculative` + `MovePosition` 조합이어야 합니다.
   기본값(Discrete)이면 속도가 14 m/s일 때 **장애물이 플레이어를 그냥 통과해 버립니다.**

2. **느려질 때 스크롤 속도를 또 줄이지 마세요.**
   `Time.timeScale`이 이미 느려지므로 여기에 `speed * 0.6f`를 또 걸면 0.36배가 됩니다.

3. **판정용 트리거는 두께 0.3m 이상으로.**
   한 물리 스텝에 0.28m를 이동하기 때문에, 더 얇으면 그냥 지나쳐 버립니다.

4. **장애물을 배경 청크 안에 넣지 마세요.**
   배경은 청크, 장애물은 스포너 + 풀. 섞으면 난이도 조절이 불가능해집니다.

5. **`GameScene`은 통합 담당자만 수정합니다.**
   각자 검증은 `Assets/1.Scene/Dev/` 샌드박스에서 하세요 (이 폴더는 gitignore 대상입니다).

---

## 8. 숫자 모음 (튜닝할 때 보는 표)

전부 인스펙터에서 바꿀 수 있고 코드 수정이 필요 없습니다.

### 이동 — `Assets/11.Data/Player/PlayerMovementConfig.asset`

| 항목 | 값 |
|---|---|
| 날갯짓 속도 `flapVelocity` | 10 |
| 중력 `customGravity` | -18 |
| 최대 낙하 속도 `maxFallSpeed` | -11 |
| 좌우 가속 `horizontalAccel` | 28 |
| 최대 좌우 속도 `maxHorizontalSpeed` | 7 |
| 플레이 영역 `boundsSize` | 30 x 30 |

### 스킬 — `Assets/11.Data/Player/PlayerSkillConfig.asset`

| 항목 | 값 |
|---|---|
| 최대 게이지 | 200 |
| Passzone 통과 충전량 | 20 |
| Time Slow 비용 / 지속 / 배속 | 50 / 2초 / 0.6배 |
| Shield 비용 | 100 |
| 무적 시간 | 0.5초 |

### 스크롤 속도 — `WorldSpeedRamp` (인스펙터)

| 경과 시간 | 속도 |
|---|---|
| 0초 | 9.0 m/s |
| 30초 | 10.25 |
| 60초 | 11.0 |
| 120초 | 12.5 |
| 180초 이후 | 14.0 |

### 스폰 — `ObstacleSpawner` (인스펙터)

| 항목 | 값 |
|---|---|
| 생성 위치 `spawnZ` | 전방 100 |
| 생성 간격 | 3.5초 |
| 미리 만들어둘 개수 | 프리팹당 4 |

---

## 9. 아직 정리되지 않은 것들

새로 합류한 사람이 헷갈릴 만한 지점을 적어둡니다. 버그라기보다 **정리가 안 된 상태**입니다.

- **빌드 세팅이 개발용 상태입니다.** 존재하지 않는 `MainScene.unity`가 목록에 남아 있고(비활성),
  활성화된 씬은 `Dev/` 샌드박스 3개뿐입니다. 본편인 `GameScene`은 빌드 목록에 없습니다.
- **씬 이름 기준이 통일되지 않았습니다.** `MainMenuController`는 `GameScene` / `RankingScene`을,
  `RankingScreenController`는 `IntroSandbox`를 기본값으로 갖고 있습니다.
- **`Obstacle/`과 `Obstacles/`** 폴더 이름이 한 글자 차이라 헷갈립니다 (구분 기준은 5장 참고).
- **`Happiness 1.prefab`** 이 `Happiness.prefab`과 중복으로 보입니다.
