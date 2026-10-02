# StackBox 게임 기획서

> 문서 버전: v0.4 / 2026-10-02  
> 장르: 3D 캐주얼 / 원터치 / 스코어 어택  
> 플랫폼: PC·Mobile / PC 우선 개발·검증 / PC 배포 Steam  
> 엔진: Unity  
> 플레이 방식: Endless  
> 핵심 차별점: 안전한 중앙 배치와 크기를 희생하는 보너스 배치 사이의 선택  
> 기본 경험: 배치 선택 → 크기와 Combo 관리 → Fever 보상 → 새로운 환경 도달  
> 상태: 구현 전 기획. 수치는 플레이 테스트용 초기값이며 재미와 성능은 검증되지 않았다.

원본 v0.1부터 수정본 v0.3까지의 게임 규칙을 바탕으로 정리했다. v0.4에서는 사용자의 결정에 따라 UI의 HM.Codebase 기반 MVP 구조와 UniTask를 사용하는 비동기 흐름을 추가한다. PC와 Mobile의 출시 날짜 및 동시 출시 여부는 별도로 정한다.

## 1. 수정본에서 확정한 사항

| 항목 | v0.4 결정 | 이유 |
|---|---|---|
| 플랫폼 | PC·Mobile 지원, PC·Steam 우선 개발·검증 | PC가 테스트용 플랫폼에 그치지 않도록 범위 수정 |
| 배치 입력 | 좌클릭 / Space / 컨트롤러 하단 버튼 / Mobile 터치 | 서로 다른 장치에서 동일한 행동 수행 |
| 차별점 | 중앙 Perfect 또는 표식 보너스 선택 | 터치·버튼 하나로 크기 보존과 보상을 선택 |
| Fever 진입 | Gauge 100만 사용. Perfect +10, 표식 수집 Normal +25 | 서로 다른 발동 조건 제거, 위험 선택의 보상 연결 |
| Normal의 Gauge 처리 | 기본은 유지. 표식을 수집했을 때만 +25 | 단순 실수와 의도적인 보너스 배치 구분 |
| Fever 지속 | 다음 성공 배치 5회 | 시간 압박보다 정확한 배치에 집중. 기존 8초 타이머 대체 |
| Biome / 난이도 | 성공 배치 수 기준 | 점수 보상이 난이도와 높이 진행을 직접 가속하지 않도록 구분 |
| 회복 대상 | 배치 완료한 최상단 블록의 X/Z 각각 +0.15 | 회복 축과 다음 블록의 크기 기준 확정 |
| 회복 횟수 | 연속 Combo 5, 10, 15…에서 각 1회 | 같은 콤보 구간에서 중복 지급 방지 |
| 판정 크기 | 기본 Threshold 0.05, 이동 축 길이의 10% 이하 | 작은 블록에서 거의 모든 겹침이 Perfect가 되는 현상 제한 |
| 카메라 | 상승 추적 유지, Fever의 FOV 변경 제외 | 배치 기준의 시각적 안정성 우선 |
| 저장 / Retry | MVP에 포함 | 기록 갱신과 반복 플레이 검증 |
| 게임 상태 | READY / PLAYING / PAUSED / GAME_OVER, Fever는 별도 상태 | Fever에서도 동일한 입력과 배치 규칙 사용 |
| UI 구조 | HM.Codebase의 AView / APresenter를 우선 활용하는 MVP | 화면 표시·입력 전달과 게임 규칙의 책임 구분 |
| 비동기 흐름 | UniTask로 순차 연출과 대기를 처리 | 재시작·화면 종료 시 진행 중 작업을 취소할 수 있도록 구성 |

Fever 5회와 회복량 등은 검증용 결정이다. 확정된 규칙과 검증된 밸런스는 구분한다.

## 2. 게임 컨셉과 목표

플레이어는 X축과 Z축을 번갈아 왕복하는 블록을 한 번의 클릭·버튼·터치로 배치한다. 이전 블록과 겹치지 않은 부분은 잘리고, 남은 크기가 다음 블록의 기준이 된다. 전혀 겹치지 않으면 게임이 끝난다.

특정 층에서는 중앙 옆에 보너스 표식이 나타난다. 중앙에 놓으면 크기와 Combo를 보존하고, 표식에 맞추면 일부 크기와 Combo를 희생하는 대신 Fever Gauge를 더 빠르게 채운다. 별도 수집 버튼은 없다.

- 주 목표: 최종 점수로 개인 최고 기록 갱신.
- 단기 목표: Perfect Combo 유지와 줄어든 블록 회복.
- 중기 목표: Gauge를 채워 Fever 진입.
- 장기 목표: 더 많은 층을 쌓고 새로운 Biome 도달.

Perfect Combo, Fever, Biome은 기본 게임의 보상 구조이다. 차별화의 중심은 연출의 개수가 아니라 배치할 때 안전과 보상을 선택하는 행동이다. 다른 게임에 없는 기능이라고 검증한 것은 아니다.

### 2.1 안전과 보상 선택 규칙

| 목표 | 판정과 크기 | Combo | Fever 밖의 Gauge |
|---|---|---|---|
| 이전 블록 중앙 | Perfect, 크기 유지 | +1 | +10 |
| 보너스 표식 | Normal, 튀어나온 부분 절단 | 0으로 초기화 | +25 |
| 중앙·표식을 모두 빗맞췄지만 겹침 있음 | Normal, 절단 | 0으로 초기화 | 유지 |
| 겹침 없음 | Miss, Game Over | 0으로 초기화 | 초기화 |

표식은 장식이 아니라 현재 블록의 중심을 맞출 별도 목표이다. 표식에 맞춘 배치도 Normal이며 절단과 Combo 손실을 생략하지 않는다.

- 생성 기회: 첫 5회 성공 이후 6번째 이동 블록, 이후 11·16·21…번째 이동 블록.
- Fever 중에는 표식을 생성하지 않는다. 건너뛴 기회는 종료 후 이월하지 않는다.
- 이동 축의 이전 블록 크기가 0.4 미만이면 표식을 생성하지 않는다.
- 표식은 이전 블록 중심에서 이동 축 길이의 25% 옆에 표시하며, 실제 생성 기회마다 좌우 방향을 교대한다.
- 수집 허용 오차: `Min(0.04, 이전 이동 축 길이 × 0.05)`.
- 표식의 모든 수집 가능 위치가 Perfect 판정 구간 밖이고, 이전 이동 축 길이의 65% 이상이 겹치는 경우에만 생성한다.
- 생성된 표식은 이동 도중 위치나 보상을 바꾸지 않는다. 해당 블록 배치가 끝나면 사라진다.
- 수집 판정은 Snap이나 절단 전의 현재 블록 중심으로 계산한다. 육안으로 표식이 블록에 가려졌다는 이유만으로 수집하지 않는다.
- Miss와 Perfect는 표식 보상을 받지 않는다. 한 표식은 1회만 수집한다.
- 보너스는 Gauge +25만 지급한다. 추가 점수·별도 재화·새 UI 바는 넣지 않는다.
- 보너스 Normal도 일반 성공 점수 +1이다. Gauge가 100이 되면 다음 블록부터 Fever를 적용한다.

플레이어는 항상 중앙 배치를 선택할 수 있다. 보너스를 무시했다고 별도 불이익을 주지 않는다. 표식은 모양·윤곽과 `BONUS +25` 표시로 중앙 Perfect 목표와 구분하고, 색상만으로 구분하지 않는다.

핵심 검증은 두 선택 모두 쓸 이유가 있는지이다. 보너스가 너무 강해 항상 표식을 노리거나, 너무 약해 항상 무시한다면 회복량·충전량·출현 간격을 조정한다. 수치는 초기 밸런스이며 검증되지 않았다.

## 3. 플레이 루프와 처리 순서

1. 시작 시 고정된 기초 블록 1개와 이동 블록 1개를 생성한다.
2. 조건에 맞으면 고정된 보너스 표식을 표시한다. 이동 블록이 왕복하며 플레이어 입력을 기다린다.
3. 입력을 수락하면 즉시 해당 블록의 추가 입력을 차단한다.
4. 현재 위치로 Miss / Perfect / Normal을 판정하고, Normal이면 절단 전 좌표로 표식 수집 여부를 확정한다.
5. Miss면 낙하 연출과 Game Over를 처리하고 다음 블록을 생성하지 않는다.
6. 성공이면 Snap 또는 절단을 적용한다.
7. 해당 입력 시점에 이미 Fever가 활성화돼 있었는지에 따라 점수를 지급한다.
8. 성공 배치 수와 Combo를 갱신하고, 해당하면 블록을 회복한다.
9. Fever 중이었다면 남은 배치 수를 1 감소시킨다. Fever 밖이었다면 Perfect +10, 표식 수집 Normal +25, 일반 Normal 유지로 Gauge를 갱신한다.
10. Gauge가 100에 도달하면 다음 블록부터 적용되는 Fever를 시작한다.
11. 성공 배치 수에 따라 Biome, 다음 블록의 속도와 색상을 결정한다.
12. 다음 블록을 생성한다.

기초 블록은 점수와 성공 배치 수에 포함하지 않는다. Miss 이후에는 점수·Combo 보상·회복·Gauge 충전을 지급하지 않는다.

## 4. 조작과 입력

### 4.1 배치 입력

| 장치 | 기본 배치 입력 |
|---|---|
| PC 마우스 | 좌클릭 |
| PC 키보드 | Space |
| PC 컨트롤러 | 오른쪽 전면 버튼 중 하단 버튼. Xbox A / PlayStation × |
| Mobile | 화면 터치 시작 |

모든 장치 입력은 하나의 Place 행동으로 연결한다. 장치에 따라 블록 속도, 판정 범위, 표식 위치나 보상을 바꾸지 않는다.

- 누르고 있는 동안 반복 배치하지 않는다.
- 같은 프레임의 클릭·Space·컨트롤러 입력과 멀티 터치 등 중복 입력은 현재 블록에 1회만 적용한다.
- UI 버튼 입력은 게임 배치 입력으로 전달하지 않는다.
- 새 블록 생성에 사용된 이전 입력을 새 블록에 재사용하지 않는다.
- PAUSED와 GAME_OVER에서는 배치 입력을 받지 않는다.

배치 후 다음 블록이 움직이기 전 짧은 준비 구간을 둔다. 초기값은 0.15초이며 모든 성공 판정에 동일하게 적용한다. 이 대기와 다음 블록 생성의 순차 흐름에는 UniTask를 사용한다. 길게 누르는 별도 기능이나 수동 Fever 버튼은 추가하지 않는다.

### 4.2 메뉴와 일시정지

| 행동 | 마우스 / 키보드 | 컨트롤러 |
|---|---|---|
| 메뉴 이동 | 포인터 / 방향키 / Tab | D-pad / 왼쪽 스틱 |
| 선택·확인 | 버튼 좌클릭 / Enter / Space | 하단 버튼 A / × |
| 취소·뒤로 | Esc | 오른쪽 버튼 B / ○ |
| 플레이 중 일시정지 | Esc / Pause UI | Start / Menu / Options |

컨트롤러만으로 시작, 설정 변경, 일시정지, 재개, Retry, Home, 종료까지 조작할 수 있어야 한다. 메뉴 진입 시 선택된 버튼을 명확하게 표시하며, Game Over 기본 선택은 Retry이다.

메뉴 확인과 게임 배치 행동을 상태별로 분리한다. 게임 시작·Retry·재개에 사용한 A·Space·클릭을 새 블록의 배치로 전달하지 않는다. 해당 입력이 해제된 뒤 새 입력을 받는다.

### 4.3 입력 장치 전환과 연결

마우스·키보드·컨트롤러를 동시에 사용할 수 있으며 별도의 입력 모드 선택을 요구하지 않는다. 실제로 사용한 장치에 맞춰 버튼 안내를 바꾸되, 미세한 스틱 드리프트나 단순 커서 이동으로 안내를 계속 바꾸지 않는다.

컨트롤러로 플레이하던 중 해당 컨트롤러 연결이 끊기면 일시정지하고, 재연결 또는 마우스·키보드로 재개할 수 있도록 한다. 사용하지 않던 컨트롤러의 연결 해제만으로 플레이를 멈추지는 않는다. 재개 입력은 배치로 소비하지 않는다.

Steam Input이 장치를 가상 Xbox 컨트롤러로 전달하면 원래 기종을 단순 장치 이름만으로 확정할 수 없을 수 있다. 버튼 안내는 실제 인식 결과에 맞추며, 필요하면 Xbox / PlayStation 표기를 사용자가 선택할 수 있게 한다. Steam Input API 통합 여부는 구현 단계에서 정하고 컨트롤러 지원 자체와 구분한다.

## 5. 블록 크기와 이동

| 항목 | 초기값 |
|---|---:|
| 최대 크기 X / Z | 각각 3.0 |
| 블록 높이 Y | 0.4 |
| 첫 이동 축 | X |
| 이후 이동 축 | X / Z 교대 |
| 왕복 범위 | 이전 블록 중심에서 이동 축 방향으로 ±3.5 |

새 블록은 회복까지 완료된 최상단 블록의 X/Z 크기를 그대로 사용한다. Y는 이전 블록보다 0.4 높은 위치에 놓으며, 이동하지 않는 축의 중심은 이전 블록과 일치시킨다.

왕복 범위는 블록 생성 시 확정하고 이동 도중 바꾸지 않는다. 속도도 한 블록의 왕복 이동 동안 일정하게 유지한다. 프레임마다 별도의 임의 속도 변화나 무작위 방향 전환은 사용하지 않는다.

## 6. 겹침과 판정

이동 축에서 이전 블록과 현재 블록의 구간을 계산한다.

```text
Previous Left  = Previous Center - Previous Size / 2
Previous Right = Previous Center + Previous Size / 2
Current Left   = Current Center - Current Size / 2
Current Right  = Current Center + Current Size / 2

Overlap Left   = Max(Previous Left, Current Left)
Overlap Right  = Min(Previous Right, Current Right)
Overlap Length = Overlap Right - Overlap Left
Offset         = Current Center - Previous Center
```

두 블록의 크기가 같은 기본 규칙에서는 `Overlap Length = Previous Size - Abs(Offset)`과 같다. 구간 계산은 이후 다른 크기의 블록을 검토할 때도 사용할 수 있다.

판정 순서는 반드시 다음과 같다.

| 조건 | 결과 |
|---|---|
| Overlap Length ≤ 0 | Miss |
| 겹침이 있고 Abs(Offset) ≤ Effective Perfect Threshold | Perfect |
| 그 외 | Normal |

최소 크기 도달만으로 별도의 Game Over를 발생시키지 않는다. 아주 작은 겹침과 좌우 경계도 판정 검증 대상이다.

## 7. Normal 처리

- 이동을 멈추고 현재 블록을 겹친 영역만큼 줄인다.
- 이동 축의 중심을 `(Overlap Left + Overlap Right) / 2`로 옮긴다.
- 이동하지 않는 축의 크기와 위치, Y 위치는 유지한다.
- 튀어나온 조각은 별도 오브젝트로 생성해 낙하시킨다.
- Combo는 0으로 초기화한다.
- Fever 밖에서는 표식을 수집한 Normal만 Gauge +25를 지급한다. 그 외 Normal은 Gauge를 유지하고 추가 충전하지 않는다.
- Fever 중에는 정상적인 성공 배치로 취급해 점수 2와 남은 횟수 감소를 적용한다.

현재 규칙에서는 같은 크기의 블록끼리 한 축으로 움직이므로 잘린 조각은 한쪽에 최대 1개 발생한다.

## 8. Miss와 실패 원인

겹침이 없으면 현재 블록 전체를 낙하시키고 즉시 게임 진행을 종료한다. 약 0.5초 뒤 결과 UI를 표시하는 순차 연출에는 UniTask를 사용한다. 다음 블록은 생성하지 않는다.

탑의 기존 블록은 고정한다. 잘린 조각과 실패 블록의 물리가 탑을 밀거나 무너뜨려 추가 실패를 유발하지 않도록 한다. 조각이 플레이 중인 블록을 가리는 현상도 확인한다.

기본 버전의 실패 원인은 블록을 완전히 빗맞춘 배치이다. 무작위 낙사나 예고 없는 장애물은 추가하지 않는다.

## 9. Perfect 판정과 피드백

```text
Base Perfect Threshold = 0.05
Effective Perfect Threshold = Min(0.05, Previous Axis Size × 0.10)
```

Perfect면 현재 블록의 X/Z 중심을 이전 블록에 정렬하되, 현재 층의 Y 위치는 유지한다. 크기는 줄이지 않는다.

- Combo +1.
- Fever 밖에서는 Gauge +10.
- 짧은 Perfect 텍스트, 효과음, 블록 윤곽 강조.
- Combo가 5의 배수에 도달하면 회복 보상.

위 Threshold는 초기값이다. 속도가 초당 2.5라면 폭 0.1의 판정 구간 통과 시간은 약 40ms, 속도 6이라면 약 17ms이다. 블록이 작아 Threshold가 제한되면 더 짧아진다. 입력 지연과 프레임 영향까지 포함해 실제 기기에서 확인한다.

검증 전 자동으로 Threshold를 속도에 비례해 확대하거나 Perfect가 쉽다고 단정하지 않는다. 입력을 처리하는 시점과 판정 위치의 기준을 일관되게 적용한다.

## 10. Combo와 블록 회복

Perfect에서 연속 Combo를 1 늘리고, Normal과 Miss에서 0으로 초기화한다. 한 판의 최대 Combo는 별도로 기록한다.

Combo 5, 10, 15…에 도달한 배치에서 각각 1회 회복한다.

- 대상: Snap까지 완료된 현재 최상단 블록.
- X와 Z: 각각 현재 길이 +0.15, 최대 3.0.
- 중심: 현재 중심 유지.
- Y: 변경 없음.
- 이미 최대 크기인 축에는 추가 효과 없음. 보상을 저장하거나 다른 축으로 이월하지 않음.
- 다음 블록: 회복된 크기를 그대로 상속.

아래층까지 확대하거나 다시 계산하지 않는다. 회복된 상단이 아래층보다 조금 돌출되는 것은 보상 연출로 허용한다. 전체 탑을 물리 시뮬레이션하는 게임으로 취급하지 않는다.

회복은 Fever 중에도 적용한다. Combo 5의 배수라는 이유로 Gauge 추가 보너스를 주지는 않는다.

## 11. Fever 충전과 진입

| 상황 | Gauge |
|---|---|
| Fever 밖에서 Perfect | +10, 최대 100 |
| Fever 밖에서 표식 수집 Normal | +25, 최대 100 |
| Fever 밖에서 일반 Normal | 유지 |
| Fever 중 Perfect / Normal | 충전하지 않음 |
| Miss / Retry / Home | 0으로 초기화 |

Gauge 100만이 발동 조건이다. Combo 10은 별도 발동 조건이 아니다. 표식을 수집하지 않았다면 중간에 Normal이 있어도 Fever 밖에서 Perfect를 총 10번 성공하면 진입할 수 있다. 표식을 수집하면 더 적은 Perfect 횟수로 진입할 수 있다.

Gauge를 완성한 배치는 기존 상태의 점수를 받는다. Fever 보너스는 다음 이동 블록부터 적용한다. Fever 시작 시 Gauge를 소모해 0으로 만들고, Fever Count를 1 늘린다.

## 12. Fever 지속과 종료

Fever는 다음 성공 배치 5회 동안 유지한다.

- Perfect / Normal 모두 남은 횟수를 1 줄인다.
- 마지막 5번째 성공 배치까지 점수 2를 지급한다.
- 5번째 성공 배치에서는 Gauge를 충전하지 않는다.
- 이후 새 블록부터 일반 점수와 Gauge 충전 규칙을 적용한다.
- 기다리는 동안이나 일시정지 중에는 횟수가 줄지 않는다.
- Miss면 남은 횟수와 관계없이 Game Over.
- Retry / Home에서 Fever 상태와 연출을 초기화한다.

연출은 종료 후 0.5~1초에 걸쳐 복귀할 수 있지만, 점수 배율은 배치 규칙에 따라 즉시 복귀한다.

Fever UI는 타이머 대신 남은 배치 수를 표시한다. 예: `FEVER 5 → 4 → 3 → 2 → 1`.

## 13. 점수와 높이 진행

| 항목 | 의미 |
|---|---|
| Score | 일반 성공 +1, Fever 성공 +2 |
| Placed Block Count | 성공 배치마다 +1. 높이·난이도·Biome 기준 |
| Max Combo | 해당 판의 최대 연속 Perfect |
| Fever Count | 해당 판에서 Fever를 시작한 횟수 |
| Highest Biome | 해당 판 또는 누적 기록의 최고 도달 환경 |

Perfect 자체에 별도 점수를 더하지 않는다. 점수 보상은 Fever를 통해 전달한다.

예: 표식을 무시하고 처음 10회 모두 Perfect면 Score 10, 성공 배치 수 10이며 다음 블록부터 Fever가 시작된다. 이어서 5회 성공하면 Score 20, 성공 배치 수 15이고 Fever가 끝난다.

보너스 선택 예: 처음 5회 Perfect면 Gauge 50이다. 6번째에서 표식을 수집하면 Gauge 75, Combo 0, Score 6이며 블록이 줄어든다. 7~9번째를 Perfect로 성공하면 Gauge가 100에 도달하고 다음 10번째 블록부터 Fever가 시작된다. 표식 없는 경우보다 한 층 빨리 진입하지만 크기와 Combo를 희생한다.

## 14. 난이도

다음 블록의 이동 속도는 현재 성공 배치 수로 결정한다.

```text
Move Speed = Min(2.5 + Placed Block Count × 0.015, 6.0)
```

Score, 경과 시간, Fever 여부는 속도 계산에 사용하지 않는다. 배치 후 다음 블록 생성 시에만 속도를 갱신한다.

크기 감소가 이미 난이도를 높이므로, 초기에는 추가 가속 패턴과 Perfect Threshold의 진행도별 축소를 넣지 않는다. Max Speed 6.0은 출시 밸런스가 아니라 테스트 상한이다.

## 15. Biome과 전환

| 성공 배치 수 | Biome | 환경과 색상 |
|---:|---|---|
| 0~49 | SKY | 밝은 하늘, 구름, 청색 |
| 50~99 | SUNSET | 노을, 따뜻한 조명, Orange / Pink |
| 100~149 | NIGHT | 별, 달빛, Blue / Purple |
| 150~199 | SPACE | 우주, 별, Purple / Cyan |
| 200~249 | AURORA | 오로라, Gradient Fog |
| 250 이후 | 위 순서 반복 | 무작위 전환 없음 |

Biome Index는 `Floor(Placed Block Count / 50) % 5`로 결정한다. 최고 도달 Biome 기록은 반복으로 낮은 Biome에 돌아와도 내려가지 않는다.

배경·조명·Fog·색상은 약 3초 동안 보간한다. Particle과 Audio 전환은 PC와 Mobile 각각의 가독성과 성능 비용을 확인한 뒤 추가한다.

Fever는 현재 Biome 위에 적용되는 짧은 연출이다. Fever 종료 시 고정된 이전 환경으로 되돌리지 않고, 현재 전환 중인 Biome의 상태로 복귀한다.

Biome별 게임 판정이나 물리 규칙은 기본 버전에서 바꾸지 않는다. 최초 테스트는 SKY와 SUNSET 2개로 진행한다. 50층 간격의 도달 빈도는 플레이 테스트로 확인한다.

## 16. 카메라와 색상

- 카메라는 탑을 비스듬히 내려다보는 구도로 시작한다.
- 투영 방식과 구도는 기본 배치 테스트에서 결정하고 플레이 도중 유지한다.
- 배치 완료한 최상단 블록을 기준으로 높이와 탑 중심을 부드럽게 추적한다.
- 이동 중인 블록을 따라 좌우로 왕복하지 않는다.
- Fever에서 FOV 변경이나 Camera Shake를 적용하지 않는다.
- 화면비와 Safe Area가 달라도 블록의 이동 범위와 배치 경계가 보이도록 확인한다.
- PC는 가로 화면을 기준으로 UI를 설계하고 창 모드·전체 화면을 지원한다. 창 크기가 바뀌어도 표식과 배치 경계를 읽을 수 있어야 한다.
- Mobile은 화면비와 Safe Area에 맞춰 UI 배치를 조정하되, 월드 기준의 판정과 보상 규칙은 PC와 동일하게 유지한다.

색상 순환은 Score가 아닌 성공 배치 수를 기준으로 한다. GradientLength는 1 이상이며 색상 샘플의 비율은 실수 나눗셈을 사용한다. 예: `(Placed Block Count % GradientLength) / GradientLength`.

색상이나 발광을 위해 Material을 무조건 블록마다 복제하지 않는다. MaterialPropertyBlock 사용 여부는 선택한 렌더 파이프라인과 실제 성능 측정에 따라 결정한다.

## 17. 연출, Audio, 진동

우선 적용할 효과는 Place, Cut, Perfect, Bonus Collect, Fever Start / End, Biome Change, Game Over이다. Bonus Collect는 Perfect와 다른 짧은 시각·Audio 피드백을 준다. 이동 효과음의 반복 재생은 필요성이 확인되면 추가한다.

Fever는 블록 윤곽과 제한된 Emission, UI 강조, 짧은 Particle, Audio Layer로 표현한다. Bloom과 전체 화면 효과는 조각과 배치 경계를 가리지 않는 범위에서 선택한다.

기본 BGM 1개를 먼저 사용한다. Biome별 Pad / Synth / Atmosphere와 Fever Rhythm Layer는 폴리싱 단계에서 추가한다. 레이어는 박자와 재생 위치를 맞춰 전환한다.

Mobile 진동과 PC 컨트롤러 진동은 각각 실제 지원과 세기를 확인한다. Normal 약하게, Perfect 짧고 선명하게, 회복 중간, Fever / Miss 강하게를 초기안으로 두며 타이밍을 방해하지 않는 범위로 조정한다. 사운드·진동은 설정에서 끌 수 있다. 진동을 지원하지 않는 장치에서도 다른 피드백으로 동일한 플레이가 가능해야 한다.

## 18. 조각과 오브젝트 수명

잘린 조각과 Miss 블록은 MeshRenderer, BoxCollider, Rigidbody를 가진다. 약 3초 뒤 정리한다.

- 고정된 탑은 낙하 물리 대상이 아니다.
- 필요 시 충돌 레이어를 구분해 플레이 중인 블록과 조각의 상호작용을 막는다.
- 풀에서 재사용할 때 위치·크기·색상·속도·회전·물리 상태를 초기화한다.
- Retry / Home에서는 이전 판의 조각을 모두 정리한다.
- MVP는 생성과 제거로 검증할 수 있으며, 풀은 PC·Mobile 성능 측정 후 도입한다.
- 오래 플레이할 때 탑과 조각의 누적 오브젝트 수, 프레임, 메모리를 측정한다. 풀을 사용한다는 사실만으로 고정 탑의 무한 누적이 해결되지는 않는다.

## 19. UI와 재시작

인게임 UI는 Score, 짧은 Perfect Combo, Fever Gauge 또는 남은 Fever 배치 수를 표시한다. 화면 중앙의 배치 영역을 확보한다. 보너스 표식이 있는 층에서는 표식 옆에 `BONUS +25`를 표시하고, 배치 후 표식과 안내를 제거한다. 기본 조작 안내는 마지막 사용 장치에 맞춘다. HUD는 HM.Codebase 기반 MVP로 구성하며 점수 계산이나 배치 판정을 View에서 수행하지 않는다.

Game Over UI는 Score, Best Score, Max Combo, Fever Count, 최고 도달 Biome을 표시하고 Retry / Home 버튼을 제공한다. Result 화면도 HM.Codebase 기반 MVP로 구성하며 버튼 입력은 Presenter가 게임 흐름으로 전달한다.

Retry 시 Score, 성공 배치 수, Combo, Gauge, Fever, 이동 축, 탑·조각, 보너스 표식과 생성 방향 순서, 카메라, 환경·Audio 연출을 초기 상태로 되돌린다. 최고 기록과 설정은 유지한다. Home은 READY 화면으로 돌아간다.

## 20. 저장과 일시정지

MVP 저장 항목은 Best Score, Best Combo, Sound Setting이다. 확장 시 Highest Biome, Total Play Count, Total Fever Count, Haptic Setting을 추가한다.

- Total Play Count는 새 판 시작 시 1 증가.
- Total Fever Count는 실제 Fever 시작 시 1 증가.
- 최고 기록은 갱신되는 성공 배치 직후 저장 데이터에 반영.
- 영구 저장은 Game Over와 앱 일시정지 등 정해진 시점에 수행.
- 첫 실행 기본값과 데이터 키 변경을 고려한다.
- 실행 중인 판의 중도 저장과 복원은 초기 범위에 포함하지 않는다.

PC 창이 포커스를 잃거나 Mobile 앱이 백그라운드로 가면 이동, 입력, 생성 대기와 연출 진행을 일시정지한다. 복귀 시 재개 입력을 받고, 그 입력으로 현재 블록을 배치하지 않는다. Steam Overlay 활성화 중에도 배치 입력을 차단하고 진행을 일시정지하며, 감지 방식은 Steam 통합 단계에서 확인한다. Time Scale은 Game Over 연출을 위해 임의로 0으로 바꾸지 않는다.

## 21. 구조와 책임

| 구성 | 역할 |
|---|---|
| StackGameProvider | 전체 흐름, Score, 성공 배치 수, 배치 결과 반영 순서 |
| StackInputController | 장치 입력을 Place / Navigate / Submit / Cancel / Pause로 연결, 중복·UI 입력 차단 |
| StackBlockSpawner / StackBlock | 블록 생성, 크기·좌표·물리 상태 |
| StackBlockMover | 고정된 축·범위·속도로 왕복 |
| StackPlacementRule | 순수 계산으로 겹침, 판정, 남을 영역과 잘릴 영역 반환 |
| StackBlockCutter | 계산 결과를 블록과 낙하 조각에 적용 |
| StackComboController | 연속 Combo와 회복 발생 여부 |
| StackBonusController | 표식 생성 조건·위치, 절단 전 수집 판정, Gauge 보너스 발생 여부 |
| StackFeverController | Gauge, 활성 상태, 남은 배치 수, 배율 |
| StackBiomeController | 성공 배치 수 기준 환경 선택과 전환 |
| StackCameraController / StackColorController | 카메라 추적과 색상 |
| StackEffectController | 시각·Audio 피드백 |
| StackHUDModel_model / StackHUD_view / StackHUDPresenter_presenter | HM.Codebase 기반 HUD용 데이터, 표시, 이벤트 바인딩 |
| StackResultModel_model / StackResult_view / StackResultPresenter_presenter | HM.Codebase 기반 결과 데이터, 표시, Retry / Home 입력 전달 |

GAME_STATE는 READY / PLAYING / PAUSED / GAME_OVER로 구분한다. Fever는 PLAYING과 동시에 유지되는 별도 상태이다.

UI에만 MVP 구조를 적용한다. 모든 클래스를 처음부터 따로 구현할 필요는 없으며, 실제 책임이 생겼을 때 분리한다. HUD와 Result를 우선 분리하고, Title / Pause / Settings 화면은 해당 UI를 구현할 때 같은 방식을 적용한다.

현재 프로젝트에는 Unity Input System 1.19.0이 포함되어 있다. 입력 수집은 이 시스템의 Action을 기준으로 설계하고, 플랫폼별 입력 차이가 배치 계산에 들어가지 않도록 한다. Steam Input 지원을 위해 동일한 물리 입력과 에뮬레이션 입력이 동시에 들어오더라도 중복 배치하지 않게 검증한다. 이 문서는 입력 구현이나 컨트롤러 호환성을 증명하지 않는다.

### 21.1 HM.Codebase 기반 UI MVP

현재 프로젝트의 `Packages/manifest.json`에는 `com.hm.codebase`가 등록되어 있으며, 확인한 패키지 버전은 1.0.0이다. 다음 규칙을 UI 구현 기준으로 사용한다.

- View: `HM.CodeBase.AView`를 상속해 텍스트·Gauge·버튼·표식 안내를 표시한다. `Open`, `Close`, `Clear`로 화면 표시와 초기화를 처리한다. 게임 점수나 절단 여부를 계산하지 않는다.
- Model: Score, Combo, Gauge, Fever 남은 배치 수, 결과 통계처럼 화면에 필요한 확정 데이터를 가진다. UI Model은 게임 규칙의 원본 상태를 대신 소유하지 않는다.
- Presenter: `HM.CodeBase.APresenter`를 상속해 `Open`, `Close`, `Dispose` 수명주기를 구현한다. 게임에서 확정된 데이터를 Model에 반영하고 View를 갱신한다. 버튼 입력은 게임 흐름 담당자에게 전달한다.
- 이벤트: 게임 상태 변경을 여러 화면에 알릴 필요가 있으면 패키지 `EventProvider`의 `Subscribe<T>` / `Publish<T>`를 사용한다. Presenter가 닫히거나 폐기될 때 자신이 등록한 핸들러를 `Unsubscribe<T>`로 해제한다.
- 화면 구성: HUD와 Result의 Model·View·Presenter를 별도로 둔다. 게임 규칙, 입력 판정, 블록 이동, 절단 계산은 UI MVP 바깥에 둔다.

게임 흐름 담당자가 배치 결과와 점수를 확정한 뒤 이벤트를 발행하고, 열린 Presenter가 이를 받아 View를 갱신한다. 이벤트 수신 순서로 점수·Gauge·Combo의 계산 순서를 결정하지 않는다. Retry를 반복해도 Presenter의 구독이 중복되지 않아야 한다.

### 21.2 UniTask 기반 비동기 흐름

현재 프로젝트에는 `Assets/Plugins/UniTask` 2.5.11이 포함되어 있다. 시간 간격이나 여러 단계의 순서가 필요한 흐름에 UniTask를 적극 활용한다.

| 흐름 | UniTask 적용 범위 |
|---|---|
| 성공 배치 | 배치 확정 → 짧은 대기·연출 → 다음 블록 생성 |
| Game Over | Miss 확정 → 낙하 연출 대기 → Result UI 열기 |
| UI | HUD 피드백, Fever 시작·종료, 결과·화면 전환 애니메이션 |
| Biome | 색·조명·오디오 전환의 순차 진행 |
| 씬 전환 | 실제 로딩 과정이 생기면 완료를 기다리는 흐름 |

UniTask가 게임의 배치 판정 순서를 결정하지는 않는다. 입력 수락, 겹침·절단·점수 계산과 매 프레임 블록 왕복 이동은 즉시 또는 정해진 갱신 루프에서 처리한다. Fever 지속 기준도 기존 규칙대로 성공 배치 5회다.

각 비동기 흐름은 현재 판 또는 UI 화면의 수명에 연결해 취소할 수 있게 만든다. Retry·Home·씬 종료 때 이전 판의 다음 블록 생성, UI 표시, Biome 전환이 뒤늦게 실행되지 않아야 한다. UI가 닫힐 때는 해당 View의 연출 작업을 취소한다. 일시정지 중에는 배치 대기와 연출 진행이 멈추고, 재개 입력이 배치로 이어지지 않아야 한다.

풀에서 재사용하는 오브젝트의 연출은 파괴 시 취소만으로 관리하지 않는다. 풀 반환 시 해당 활성화 기간의 작업도 취소한다. 실제로 기다리는 작업이 있는 UniTask 함수는 사용자 컨벤션에 따라 `_async` 접미사를 사용한다.

## 22. 이벤트와 적용 순서

배치 결과에는 판정, 이전 크기, 남은 크기·중심, 잘린 영역, 표식 수집 여부와 Gauge 보너스를 담는다. UI와 연출은 확정된 결과를 받아 표시한다.

예상 알림은 BlockPlaced, BonusCollected, ComboChanged, BlockRestored, FeverStarted, FeverEnded, BiomeChanged, GameOver이다. 바인딩 함수명은 사용자 컨벤션에 따라 OnPerfectActioned 등으로 작성한다.

점수 지급·회복·Fever 시작 순서를 이벤트 구독 순서에 맡기지 않는다. StackGameProvider가 3절 순서대로 결과를 반영하고, 확정된 값으로 알림을 발행한다. Retry 시 구독이 중복되지 않도록 관리한다.

## 23. 초기값 요약

| 항목 | 초기값 |
|---|---:|
| 최대 블록 X / Z | 각각 3.0 |
| 블록 Y | 0.4 |
| Move Range | 이전 중심 기준 ±3.5 |
| Base / Max Speed | 2.5 / 6.0 |
| Speed Increase | 성공 배치당 0.015 |
| Base Perfect Threshold | 0.05 |
| Perfect Threshold 상한 | 이동 축 길이의 10% |
| Restore Combo | 5의 배수 |
| Restore Amount | X/Z 각각 0.15 |
| Gauge Max / Perfect 충전 | 100 / 10 |
| 일반 Normal 충전 / 감소 | 0 / 0 |
| 표식 수집 Normal 충전 | 25 |
| 첫 보너스 대상 블록 | 6번째 이동 블록 |
| 보너스 생성 간격 | 이후 5층 간격 |
| 보너스 최소 이동 축 크기 | 0.4 |
| 보너스 위치 | 이전 중심에서 이동 축 길이의 ±25% |
| 보너스 수집 허용 오차 | Min(0.04, 이동 축 길이 × 5%) |
| 보너스 최소 겹침 비율 | 65% |
| Fever 성공 배치 수 | 5 |
| Fever Score | 성공당 2 |
| Biome Interval | 성공 배치 50회 |
| Biome Transition | 약 3초 |
| Next Block Delay | 약 0.15초 |
| Cut Lifetime | 약 3초 |

## 24. 개발 범위와 검증 단계

| 단계 | 범위 | 다음 단계로 넘어갈 기준 |
|---|---|---|
| PC 기본 루프 | 좌클릭·Space·컨트롤러 배치, 왕복, X/Z 교대, 판정·절단·낙하, Perfect, Combo 회복, Score, Camera, HM.Codebase 기반 Result UI, Retry, 최고 기록 저장. UniTask로 Game Over 연출·다음 블록 대기 | 세 입력 장치로 배치·메뉴·반복 플레이 확인 |
| PC MVP 완성 | Gauge, 5회 Fever, 보너스 표식, HM.Codebase 기반 HUD MVP, 최소 UI·효과음과 UniTask 피드백 | 안전과 보상 선택, 발동·종료·점수 순서와 보상 체감 확인 |
| Steam 검증 | 실제 PC 빌드, Steam Input 설정, Overlay, 창 모드·전체 화면, 패드 단독 조작 | 지원할 컨트롤러의 Steam 환경 동작 확인 |
| 2차 확장 | Biome 2개와 전환 | 가독성과 도달 빈도 확인 후 최대 5개로 확장 |
| 폴리싱 / Mobile 대응 | Audio Layer, Particle, 진동, Pool, Mobile 터치·화면비·성능 개선 | PC와 실제 Mobile 기기에서 입력·표식 가독성·성능 확인 |

PC·Steam을 우선 개발하고 검증한다. Mobile도 지원 대상이므로 공통 게임 규칙과 입력 행동을 유지하며, PC 기본 루프가 마련되면 Android 테스트 빌드로 터치와 화면비를 확인한다. 모바일 배포 스토어와 추가 지원 OS는 별도로 정한다. 7일 일정은 전체 게임 완성 약속으로 사용하지 않는다. 검증 실패 시 해당 단계의 규칙과 수치를 먼저 수정한다.

Steam의 업적·랭킹·Cloud 저장은 현재 핵심 범위에 자동 추가하지 않는다. 컨트롤러 지원은 PC 필수 범위이며, 보너스 표식을 제외한 버전은 PC MVP 완성으로 보지 않는다.

## 25. 필수 검증 항목

- 정확히 일치, 좌우 절단, 접촉만 하는 경계, 완전한 Miss, 매우 작은 겹침.
- 회복 전후 두 블록의 크기·중심과 다음 블록 상속.
- Combo 5 / 10 / 15의 회복이 한 번씩 발생하는지.
- 일반 Normal은 Combo를 끊지만 Gauge는 유지하고, 표식 수집 Normal은 Combo를 끊으면서 Gauge +25만 지급하는지.
- 보너스 표식이 6·11·16…번째 블록에서 조건에 맞게 생성되고, Fever·작은 블록에서 건너뛰는지.
- 좌우·X/Z 표식 수집 경계, 최소 겹침 비율, Perfect 구간과의 분리, Miss에서 보상 미지급.
- 보너스 수집으로 Gauge 100에 도달한 배치는 +1, 다음 블록부터 Fever인지.
- Gauge를 완성한 블록은 +1, 다음 5개는 +2인지.
- Fever 마지막 배치의 Perfect가 Gauge를 동시에 충전하지 않는지.
- 성공 배치 49 / 50, 249 / 250 경계와 Fever·Biome 동시 전환.
- 마우스·Space·컨트롤러 각각의 단독 배치와 동시 입력, 누르고 있기, 메뉴 확인 후 입력 해제.
- 컨트롤러만으로 시작·설정·Pause·재개·Retry·Home·종료, 기본 선택과 포커스 표시.
- 입력 장치 전환, 컨트롤러 연결·해제·재연결, 연결 해제 뒤 키보드·마우스로 재개.
- Steam Input 켜짐·꺼짐, 실제 Xbox·PlayStation 계열 지원 대상, Steam Overlay와 PC 창 포커스 복귀.
- Mobile 연속 터치·버튼 터치·Retry 직후 입력·백그라운드 복귀.
- Retry 시 남은 조각·효과·이벤트 구독·점수 상태가 초기화되는지.
- HM.Codebase Presenter를 열고 닫거나 Retry를 반복해도 HUD·Result 이벤트 구독과 버튼 호출이 중복되지 않는지.
- UniTask 진행 중 Retry·Home·씬 종료·UI Close가 발생하면 이전 판의 블록 생성·결과 UI·Biome 연출이 재개되지 않는지.
- 일시정지 중 비동기 대기·연출이 멈추며, 재개 후 한 번만 이어지는지.
- 풀 반환 후 이전 활성화 기간의 조각·UI 연출이 다시 실행되지 않는지.
- 첫 실행과 재실행 후 저장 데이터 유지.
- PC 창 모드·전체 화면, 서로 다른 화면비와 30 / 60fps 및 PC 고주사율 조건의 입력·판정 감각.
- PC 실제 빌드와 Mobile에서 긴 플레이의 프레임·메모리·표식·발광과 Particle 가독성.

정적 계산 확인, Unity 컴파일, Play Mode 확인, 실제 기기 확인, 출시 빌드 확인은 각각 별도로 기록한다. 현재 문서는 어느 단계의 성공도 증명하지 않는다.

## 26. 확정 범위와 참고 자료

정식 차별점은 **안전하게 쌓을지, 일부를 잘라 보너스를 얻을지 선택한다**이다. 설계도 챌린지와 리듬 쌓기는 이번 개발 범위에 포함하지 않는다. 핵심 규칙은 2.1절에 정의하며, PC·Steam 검증을 먼저 진행하고 Mobile에도 같은 선택 구조를 제공한다.

Ketchapp의 Stack 공식 App Store 설명은 높은 탑과 최고 점수를 목표로 안내하며, 현재 버전 이력에는 Stack City의 건물·도시 건설도 소개한다. 따라서 건물 테마나 도시 수집만으로 차별성이 확보됐다고 주장하지 않는다. 이 자료만으로 경쟁 게임의 모든 상세 규칙이나 선택한 차별점의 독창성을 판단할 수는 없다.

참고 자료:

- [Stack 공식 App Store 및 개발사 업데이트 안내](https://apps.apple.com/us/app/stack/id1080487957): 비교 게임의 공식 설명과 도시 건설 업데이트.
- [Unity Input System 1.19 Gamepad 문서](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.19/manual/Gamepad.html): 위치 기준 버튼과 Xbox A / PlayStation × 대응.
- [Steam Input 개발자 시작 문서](https://partner.steamgames.com/doc/features/steam_controller/getting_started_for_devs): 장치별 버튼 안내, 혼합 입력, Steam Input 통합 방향.
- [Steam Input Gamepad Emulation 권장 사항](https://partner.steamgames.com/doc/features/steam_controller/steam_input_gamepad_emulation_bestpractices): 에뮬레이션 입력과 컨트롤러 경험 검토.

지원할 실제 컨트롤러, Steam Input 동작과 기기별 버튼 표시는 빌드 테스트로 확정한다. 기획의 지원 목표를 검증 완료된 호환성이나 Steam Deck 인증으로 표현하지 않는다.
