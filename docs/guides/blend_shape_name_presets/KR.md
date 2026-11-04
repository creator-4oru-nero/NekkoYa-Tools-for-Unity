# Blend Shape Name Presets

[가이드 목록](../README_KR.md) · [日本語](JP.md) / [English](EN.md) / [한국어](KR.md)

개발 대상: **Unity 2022.3.22f1**

개발판 가이드이며 Unity/VRChat SDK 실행 검증은 미실시입니다.

## 기능

FBX 업데이트 전 블렌드 셰이프 값을 JSON 저장하고 이후 이름으로 복원합니다.

## 준비

씬 루트 또는 프리팹 모드 오브젝트를 준비하고 FBX 업데이트 전에 저장하세요. Project 프리팹 에셋 자체와 다릅니다.

## 조작 순서

1. `NekkoYa → Blend Shape Name Presets`에서 루트를 지정합니다.
2. 현재 값을 JSON으로 저장합니다. 비활성 자식과 0 값도 포함합니다.
3. FBX 업데이트 후 같은 루트와 저장한 JSON을 엽니다.
4. 일치/추가/삭제/변경을 확인하고 계층 오류를 해결합니다.
5. 새 키를 0으로 할지 선택합니다. 저장된 렌더러의 새 키만 해당하며 OFF는 현재 값을 유지합니다.
6. 이름으로 복원하고 결과를 확인하여 씬/프리팹을 저장합니다.

## 설정

이름과 렌더러 계층으로 대응합니다. 삭제된 키는 건너뛰고 저장되지 않은 렌더러는 변경하지 않습니다.

## 결과 및 복구

대응하는 블렌드 셰이프 값을 복원하며 Undo를 지원합니다. JSON을 FBX에 기록하지는 않습니다.

## 문제 해결

같은 이름 형제/키 또는 렌더러 불일치로 모호하면 중지합니다. 계층과 JSON 대상을 확인하고 오류를 먼저 해결하세요.

[상세 사양 및 설치](https://github.com/creator-4oru-nero/NekkoYa-Tools-for-Unity/blob/refactor/unity-v1.0.0/Packages/com.nekkoya.tools/README_KR.md) · [가이드 목록](../README_KR.md)
