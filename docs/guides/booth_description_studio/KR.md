# BOOTH Description Studio

[가이드 목록](../README_KR.md) · [日本語](JP.md) / [English](EN.md) / [한국어](KR.md)

개발 대상: **Unity 2022.3.22f1**

개발판 가이드이며 Unity/VRChat SDK 실행 검증은 미실시입니다.

## 기능

BOOTH 상품 설명을 작성하고 상품/프리셋/등록 목록을 관리합니다.

## 준비

기존 JSON을 백업하세요. UI 언어를 바꿔도 상품 본문은 자동 번역하지 않습니다.

## 조작 순서

1. `NekkoYa → BOOTH Description Studio`를 엽니다.
2. 프리셋과 새로 만들기를 선택하거나 상품 이름/카테고리/번호를 입력합니다.
3. 아바타, 사전 다운로드, 내용물, 프리팹 요약과 이력을 편집합니다. 항목을 켜고 화살표로 순서를 바꿉니다.
4. 완성 미리보기와 경고를 확인합니다. 경고를 해결하고 전체 복사하여 BOOTH에 붙입니다.
5. 상품 등록으로 저장하고 열기로 기존 상품에 돌아갑니다.
6. 백업 내보내기로 등록 정보/상품/프리셋을 JSON 저장합니다. 가져오기는 현재 데이터를 교체하므로 확인하세요.

## 설정

등록 목록에서 이름/URL을 관리하며 현재 상품에 명시적으로 반영합니다. 이름 있는 프리셋을 저장해 재사용하세요.

## 결과 및 복구

OS LocalApplicationData의 `BoothDescriptionStudio/library.json`에 자동 저장합니다. 지금 저장으로 상태를 확인합니다.

## 문제 해결

중복 ID, 날짜 형식과 붙여넣은 숫자 정보를 확인하세요. 저장 실패 시 폴더 권한을 확인하고 초안을 백업 내보내세요. 저장 데이터를 못 읽으면 원본 보호를 위해 자동 저장을 중단합니다.

[상세 사양 및 설치](https://github.com/creator-4oru-nero/NekkoYa-Tools-for-Unity/blob/refactor/unity-v1.0.0/Packages/com.nekkoya.tools/README_KR.md) · [가이드 목록](../README_KR.md)
