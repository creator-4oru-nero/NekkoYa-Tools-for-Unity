> 이 도구는 AI(Codex)를 활용하여 개발합니다. 결과를 확인하고 사용자 책임하에 이용해 주세요.

# Unity 사용 가이드

[日本語](README_JP.md) / [English](README_EN.md) / [한국어](README_KR.md) · [Repository README](../../README.md)

개발 대상: **Unity 2022.3.22f1**

| 도구 | 기능 |
| --- | --- |
| [BOOTH Description Studio](booth_description_studio/KR.md) | BOOTH 상품 설명을 작성하고 상품/프리셋/등록 목록을 관리합니다. |
| [Asset Organizer](asset_organizer/KR.md) | Project 에셋 이름을 일괄 변경하고 프리셋으로 폴더 트리를 생성합니다. |
| [Blend Shape Name Presets](blend_shape_name_presets/KR.md) | FBX 업데이트 전 블렌드 셰이프 값을 JSON 저장하고 이후 이름으로 복원합니다. |
| [Shape Key List](shape_key_list/KR.md) | 프리팹/씬 계층의 메시와 블렌드 셰이프 이름을 표시하고 복사합니다. |
| [VRC Data Checker](vrc_data_checker/KR.md) | 배포 폴더의 참조, 텍스처, 메시, 머티리얼과 프리팹 문제를 확인합니다. |
| [Prefab Full Sync](prefab_full_sync/KR.md) | Hierarchy 원본의 전체 차이를 백업과 함께 Project 프리팹에 반영합니다. |
| [PhysBone Organizer](physbone_organizer/KR.md) | 본에 붙은 PhysBone을 아바타 바로 아래 PB 컨테이너로 정리합니다. |

## 설치

최상위 NekkoYa 메뉴가 있는 개수판 패키지의 가이드입니다. 현재 7개 도구 개발판은 `refactor/unity-v1.0.0` 브랜치에 있으며 main의 원본 스크립트 구성과 다릅니다.

1. Unity 2022.3.22f1 프로젝트를 백업하고 기존 스크립트와 중복 설치하지 마세요.
2. Package Manager → `+` → `Add package from git URL...`에 입력합니다.

```text
https://github.com/creator-4oru-nero/NekkoYa-Tools-for-Unity.git?path=/Packages/com.nekkoya.tools#refactor/unity-v1.0.0
```

3. 비공개 저장소 Git 인증을 준비하고 컴파일 후 NekkoYa를 엽니다.
4. `NekkoYa → Language → JP / EN / KR`에서 공통 언어를 선택합니다.

1.0.0-pre.1 개발 후보이며 Unity 실행 검증은 미실시입니다. PhysBone Organizer 실행에만 VRChat SDK Avatars / VRCPhysBone이 필요합니다. 구체적인 SDK 호환 버전은 미확정입니다.

## 원본 버전

원본 0.1.0은 `Tools → NekkoYa` 아래 일본어 메뉴입니다. 항목과 보호 동작이 다르므로 [원본 소스](../../Original/v0.1.0)와 설명서를 참고하세요. 기존판과 개수판을 동시에 설치하지 마세요.
