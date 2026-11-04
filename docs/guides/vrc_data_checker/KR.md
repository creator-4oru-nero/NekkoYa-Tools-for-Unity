# VRC Data Checker

[가이드 목록](../README_KR.md) · [日本語](JP.md) / [English](EN.md) / [한국어](KR.md)

개발 대상: **Unity 2022.3.22f1**

개발판 가이드이며 Unity/VRChat SDK 실행 검증은 미실시입니다.

## 기능

배포 폴더의 참조, 텍스처, 메시, 머티리얼과 프리팹 문제를 확인합니다.

## 준비

Assets 내부 폴더를 준비하세요. Packages나 외부 폴더는 지원하지 않습니다.

## 조작 순서

1. `NekkoYa → VRC Data Checker`를 엽니다.
2. 폴더를 지정하거나 Project 선택 폴더를 사용합니다.
3. Avatar/World Item 등 용도와 PC/Mobile을 선택합니다.
4. 임포트/컴파일 후 검사를 시작하고 미완료/취소 상태를 확인합니다.
5. 중요도/종류/검색으로 좁혀 원인, 권장 대응과 참조 원본을 읽습니다.
6. Project 표시/프리팹 열기로 확인하고 결과를 복사/Markdown 저장합니다.
7. 설정에서 제외/유지 패턴과 임계값을 조정한 후 재검사합니다.

## 설정

일반 제외라도 의존성/누락 참조는 검사합니다. 필요한 소재는 유지 대상으로 지정하세요. 권장값은 VRChat 절대 제한이 아닙니다.

## 결과 및 복구

검사 에셋을 변경하지 않고 보고서를 표시/내보냅니다.

## 문제 해결

미사용 후보는 삭제 허가가 아니며 동적 로딩은 판별하지 못합니다. MA 상세, Animation/Animator, VRC Parameters/Performance Rank는 미구현입니다. 미완료 원인을 해결하고 재검사하세요.

[상세 사양 및 설치](https://github.com/creator-4oru-nero/NekkoYa-Tools-for-Unity/blob/refactor/unity-v1.0.0/Packages/com.nekkoya.tools/README_KR.md) · [가이드 목록](../README_KR.md)
