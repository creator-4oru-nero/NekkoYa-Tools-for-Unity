# NekkoYa Tools for Unity

Unity 2022.3.22f1용 Editor 도구 6개입니다. 1.0.0 개수판은 **Unreleased / 1.0.0-pre.1** 상태입니다. Unity 컴파일, 실제 동작 확인 및 정식 공개 승인은 아직 진행하지 않았습니다.

## 도구와 사용법

Unity 최상위 `NekkoYa` 메뉴에서 엽니다. 도구 이름과 창 제목은 영어로 고정됩니다.

| 도구 | 사용 방법 |
| --- | --- |
| BOOTH Description Studio | 상품, 지원 아바타, URL, 내용물, 업데이트 이력을 입력하고 미리보기 후 전체 복사합니다. 상품/프리셋 등록과 JSON 백업 내보내기/가져오기를 지원합니다. |
| Asset Organizer | Rename에서 Project 에셋을 추가하고 정렬하여 `{00}` 연속 번호와 `{Name}`을 미리 본 뒤 이름을 변경합니다. 전용 버튼으로 직전 작업을 되돌립니다. Folder Generator에서는 트리를 편집하고 Assets 내부의 누락 폴더만 생성합니다. |
| Blend Shape Name Presets | FBX 업데이트 전에 씬 또는 프리팹 모드의 루트를 선택하여 값을 JSON으로 저장합니다. 업데이트 후 JSON을 열고 추가/삭제/변경을 확인한 뒤 이름으로 복원합니다. |
| Shape Key List | 프리팹 또는 씬 오브젝트의 메시와 블렌드 셰이프를 비활성 자식까지 표시하고 복사합니다. 에셋은 편집하지 않습니다. |
| VRC Data Checker | Assets 내부 배포 폴더, 용도, PC/Mobile을 선택해 검사합니다. 종류/중요도/검색으로 결과와 권장 조치를 확인하고 복사/Markdown 출력합니다. |
| Prefab Full Sync | Hierarchy 원본과 Assets 내부 대상 프리팹을 지정합니다. 차이를 확인하고 승인한 뒤 백업하여 전체 반영합니다. |

## 설치 및 이전

1. Unity 2022.3.22f1 검증용 프로젝트를 백업합니다.
2. 기존 BOOTH JSON, 블렌드 셰이프 프리셋, 폴더 프리셋 등 사용자 데이터를 백업합니다. 기존 도구 스크립트를 제거하고 설치하세요. 두 버전을 함께 넣지 마세요.
3. Package Manager → `+` → `Add package from git URL...`에 입력합니다.

```text
https://github.com/creator-4oru-nero/NekkoYa-Tools-for-Unity.git?path=/Packages/com.nekkoya.tools#refactor/unity-v1.0.0
```

아직 미공개 개발 브랜치입니다. 비공개 저장소이므로 GitHub 접근 권한과 로컬 Git 인증이 필요합니다. 정식 승인 후 확인된 커밋 또는 태그로 고정하세요. Unity 표준 API를 사용하며 외부 라이브러리를 새로 추가하지 않았습니다. VRChat SDK, Modular Avatar, lilToon은 필수 의존성이 아닙니다. 기존 선택적 연동을 유지하며 특정 버전의 실제 호환성은 미검증입니다.

기존 스크립트 `.meta`, 프리셋 GUID, BOOTH 저장 위치와 JSON 형식, 블렌드 셰이프 JSON format/version을 유지합니다. 폴더 프리셋에는 이전 Editor 어셈블리에서의 이전 정보를 추가했습니다. 실제 Unity 타입 이전은 확인이 필요합니다. BOOTH 데이터는 계속 OS LocalApplicationData의 `BoothDescriptionStudio/library.json`을 사용합니다.

## 설정

- `NekkoYa > Language > JP / EN / KR`: 모든 도구가 공유하며 기본값은 JP입니다. EditorPrefs에 저장하여 재시작 후 유지하고 현재 언어에만 체크합니다.
- 언어 변경 시 열린 창을 다시 그립니다. 이미 생성된 검사 결과, 복원 미리보기와 알림은 이전 언어를 유지할 수 있으므로 재검사/미리보기 갱신/새로고침하세요.
- 상품 본문, 약관, 이름, 사용자 입력 및 분석 정규식은 UI 언어 변경으로 수정하지 않습니다. 기존 일본어 상품 템플릿은 일본어로 유지합니다.
- Asset Organizer: 시작 번호, 이름 템플릿, 대상 폴더와 프리셋을 설정합니다. 패키지 또는 읽기 전용 프리셋은 `Save As`로 Assets 내부에 복사한 뒤 편집하세요.
- Blend Shape Name Presets: 저장된 렌더러에 새로 추가된 키만 0으로 만들지 선택할 수 있습니다.
- VRC Data Checker: 제외/유지 패턴, 권장 임계값, Android 권장 형식, 외부 의존성 패턴 및 제외 프로퍼티를 설정합니다. 기존 `ProjectSettings/NekkoyaVRCDataChecker.asset`을 사용합니다.

## 데이터 보호 및 주의 사항

- 블렌드 셰이프 복원과 폴더 프리셋 편집은 Undo를 지원합니다. 에셋 이름 변경은 전용 직전 작업 되돌리기를 사용합니다. 폴더 생성은 일반 Undo를 지원하지 않습니다. 실패 시 이번에 생성한 빈 폴더만 되돌리고 기존 폴더와 추가된 내용은 삭제하지 않습니다.
- Prefab Full Sync는 대상에만 있는 요소를 삭제하고 중첩 프리팹 및 배리언트 상속을 해제합니다. 일반 Undo로 복구할 수 없습니다. 확인 창을 읽고 백업으로 복원하세요.
- 백업은 프로젝트 루트의 `PrefabFullSyncBackups`에 저장합니다. 프리팹 모드를 닫고 백업의 `.prefab`과 `.prefab.meta`를 원래 위치에 함께 복사한 뒤 Unity를 Refresh하세요. 백업의 `Restore.txt`도 확인하세요.
- VRC Data Checker는 검사 에셋을 수정하지 않습니다. 미사용 후보는 삭제 허가가 아니며 동적 로딩은 판별하지 못합니다. MA 상세, Animation/Animator, VRC Parameters/Performance Rank 검사는 미구현입니다. 권장값은 VRChat 절대 제한이 아닙니다.
- 정상 조작에서는 불필요한 로그를 대량 출력하지 않으며 오류 로그에 `[NekkoYa][ToolName]`을 붙입니다. OS/Unity 예외는 해당 환경의 언어로 표시될 수 있습니다.

## 검증

저장소 `scripts/verify_repository.py`로 원본 일치, GUID, 어셈블리 참조, 번역 키와 서식 인수를 검사합니다. C# 구문 분석은 Unity 컴파일 및 동작 확인과 다릅니다.

검증 프로젝트의 Unity Test Runner EditMode에서 기존 VRC 검사 테스트와 `ToolRegressionTests`를 실행하세요. 패키지 테스트를 검색하려면 프로젝트 `Packages/manifest.json`의 `testables`에 `com.nekkoya.tools`를 추가합니다. 미확인 항목은 저장소 `VALIDATION.md`를 참고하세요.
