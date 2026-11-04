# Prefab Full Sync

[가이드 목록](../README_KR.md) · [日本語](JP.md) / [English](EN.md) / [한국어](KR.md)

개발 대상: **Unity 2022.3.22f1**

개발판 가이드이며 Unity/VRChat SDK 실행 검증은 미실시입니다.

## 기능

Hierarchy 원본의 전체 차이를 백업과 함께 Project 프리팹에 반영합니다.

## 준비

복제 프로젝트에서 먼저 시험하고 대상을 저장한 뒤 프리팹 모드를 닫으세요. 일반 Undo로 복구할 수 없습니다.

## 조작 순서

1. `NekkoYa → Prefab Full Sync`를 엽니다.
2. 일반 씬 원본 루트와 Assets 내부 대상 프리팹 에셋 루트를 지정합니다.
3. 차이 미리보기의 추가/삭제/변경을 읽습니다. 대상에만 있는 요소는 삭제합니다.
4. 중첩/배리언트 상속 해제와 루트 이름/로컬 Transform 교체를 확인합니다.
5. 백업 후 전체 반영을 누르고 확인 창을 승인합니다.
6. 결과/백업을 확인합니다. 복원은 프리팹 모드를 닫고 .prefab과 .prefab.meta를 원래 경로에 함께 복사한 뒤 Refresh합니다.

## 설정

프로젝트 루트 `PrefabFullSyncBackups`에 저장합니다. 최근 백업 폴더 열기와 Restore.txt를 참고하세요.

## 결과 및 복구

대상 프리팹을 덮어씁니다. 머티리얼/메시 자체는 편집하지 않지만 내부 fileID 변경은 외부 참조에 영향을 줄 수 있습니다.

## 문제 해결

읽기 전용, 미저장 변경, 누락 스크립트와 저장 불가 참조를 먼저 해결하세요. 변경 후 갱신된 미리보기를 재확인합니다. 자동 복원 실패 시 저장 전에 백업을 확인하세요.

[상세 사양 및 설치](https://github.com/creator-4oru-nero/NekkoYa-Tools-for-Unity/blob/refactor/unity-v1.0.0/Packages/com.nekkoya.tools/README_KR.md) · [가이드 목록](../README_KR.md)
