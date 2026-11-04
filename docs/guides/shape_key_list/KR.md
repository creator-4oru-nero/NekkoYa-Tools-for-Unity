# Shape Key List

[가이드 목록](../README_KR.md) · [日本語](JP.md) / [English](EN.md) / [한국어](KR.md)

개발 대상: **Unity 2022.3.22f1**

개발판 가이드이며 Unity/VRChat SDK 실행 검증은 미실시입니다.

## 기능

프리팹/씬 계층의 메시와 블렌드 셰이프 이름을 표시하고 복사합니다.

## 준비

프리팹 또는 씬 루트를 준비하세요. 비활성 자식도 포함합니다.

## 조작 순서

1. `NekkoYa → Shape Key List`를 엽니다.
2. Project/Hierarchy 대상을 지정란에 드래그합니다.
3. 메시/키 개수와 계층 경로를 확인합니다.
4. 이름 복사, 복사 또는 전체 복사를 사용합니다.
5. 변경 후 새로고침하여 목록을 다시 만듭니다.

## 설정

텍스트를 직접 선택해 복사할 수도 있습니다. 대상이 비어 있으면 목록을 표시하지 않습니다.

## 결과 및 복구

클립보드로 텍스트만 내보내며 에셋/키/웨이트는 편집하지 않습니다.

## 문제 해결

MeshFilter/SkinnedMeshRenderer에 메시가 있는지 확인하세요. 키가 없는 메시는 없음으로 표시합니다.

[상세 사양 및 설치](https://github.com/creator-4oru-nero/NekkoYa-Tools-for-Unity/blob/refactor/unity-v1.0.0/Packages/com.nekkoya.tools/README_KR.md) · [가이드 목록](../README_KR.md)
