# Asset Organizer

[가이드 목록](../README_KR.md) · [日本語](JP.md) / [English](EN.md) / [한국어](KR.md)

개발 대상: **Unity 2022.3.22f1**

개발판 가이드이며 Unity/VRChat SDK 실행 검증은 미실시입니다.

## 기능

Project 에셋 이름을 일괄 변경하고 프리셋으로 폴더 트리를 생성합니다.

## 준비

프로젝트를 백업하세요. Assets 내부 메인 에셋만 이름 변경하며 폴더/패키지 에셋은 제외합니다.

## 조작 순서

1. `NekkoYa → Asset Organizer`의 Rename 탭을 엽니다.
2. Project 에셋을 드래그하거나 선택 에셋을 추가하고 행을 드래그해 정렬합니다.
3. 템플릿의 `{00}`은 연속 번호, `{Name}`은 행별 이름입니다. `{00}_{Name}`, 시작 7, Body는 `07_Body`입니다.
4. Name 목록을 입력/편집하고 미리보기와 충돌을 확인합니다.
5. 이름 변경을 실행합니다. 직전 이름 변경 되돌리기를 사용할 수 있으며 확장자는 유지합니다.
6. 폴더는 Folder Generator에서 Assets 내부 대상을 지정합니다.
7. New로 프리셋을 만들거나 기존 프리셋을 Save As하여 Assets에 복사합니다.
8. 루트/자식과 이름/계층을 편집하고 Save합니다.
9. 누락/기존 폴더를 확인하고 Create Folders를 실행합니다. 기존 폴더는 재사용합니다.

## 설정

시작 번호, 템플릿, Name 목록과 폴더 프리셋을 설정합니다. 패키지/읽기 전용 프리셋은 직접 저장/이름 변경/삭제할 수 없습니다.

## 결과 및 복구

Project 경로와 폴더를 갱신합니다. 트리 작업본은 Save 전까지 프리셋에 반영하지 않습니다.

## 문제 해결

일반 Undo로 파일 작업을 복구할 수 없습니다. 이름 변경은 전용 되돌리기, 폴더 실패는 새 빈 폴더만 복구합니다. 이후 이름이 바뀐 에셋은 복구를 거부하므로 경로와 백업을 확인하세요.

[상세 사양 및 설치](https://github.com/creator-4oru-nero/NekkoYa-Tools-for-Unity/blob/refactor/unity-v1.0.0/Packages/com.nekkoya.tools/README_KR.md) · [가이드 목록](../README_KR.md)
