# PhysBone Organizer

[가이드 목록](../README_KR.md) · [日本語](JP.md) / [English](EN.md) / [한국어](KR.md)

개발 대상: **Unity 2022.3.22f1**

개발판 가이드이며 Unity/VRChat SDK 실행 검증은 미실시입니다.

## 기능

본에 붙은 PhysBone을 아바타 바로 아래 PB 컨테이너로 정리합니다.

## 준비

VRChat SDK Avatars의 VRCPhysBone이 필요합니다. 일반 씬을 사용하고 참조가 있는 다른 열린 씬도 백업하세요.

## 조작 순서

1. `NekkoYa → PhysBone Organizer`를 엽니다.
2. 아바타 루트를 지정하고 Armature/アーマチュア 후보를 선택하거나 수동 지정합니다.
3. 대상 확인으로 컴포넌트와 갱신할 씬 참조 수를 확인합니다.
4. 본 ON/OFF 제어와 AnimationClip 경로는 자동 이전하지 않음을 확인합니다.
5. PhysBone을 PB로 정리하고 수와 범위를 읽어 승인합니다.
6. PB 아래 설정/참조/외형을 확인하고 필요하면 Undo합니다. 문제없으면 관련 씬을 수동 저장합니다.

## 설정

기존 PB를 재사용하지 않고 PB (1) 등을 생성합니다. 비어 있는 Root Transform은 원래 본, 기존 지정과 현재 활성 상태는 유지합니다.

## 결과 및 복구

PhysBone 컴포넌트만 옮기고 본 계층을 유지합니다. 다른 열린 일반 씬의 컴포넌트 참조도 갱신합니다.

## 문제 해결

SDK 없음, 프리팹/플레이 모드, 편집 불가 참조에서는 실행할 수 없습니다. 에셋/닫힌 씬 참조, 애니메이션과 이후 활성 제어는 별도 확인하세요. 대상이 바뀌면 재확인합니다.

[상세 사양 및 설치](https://github.com/creator-4oru-nero/NekkoYa-Tools-for-Unity/blob/refactor/unity-v1.0.0/Packages/com.nekkoya.tools/README_KR.md) · [가이드 목록](../README_KR.md)
