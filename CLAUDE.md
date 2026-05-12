# Unity 수박 게임 — CLAUDE.md

## 프로젝트 개요
Unity 6000.3.15f1로 제작 중인 2D 수박 게임 (떨어뜨리고 합치는 퍼즐 게임).
초기 개발 단계: 아직 커스텀 C# 스크립트 없음. 게임 로직은 비주얼 스크립팅으로 구현될 수 있음.

## Unity 환경
- Unity 버전: 6000.3.15f1
- 렌더 파이프라인: URP 17.3.0 (2D용 Renderer2D)
- 메인 씬: `Assets/Scenes/SampleScene.unity`

## 주요 패키지
- `com.unity.visualscripting` 1.9.11 — 비주얼 스크립팅 (게임 로직이 여기 있을 수 있음)
- `com.unity.inputsystem` 1.19.0 — 새 입력 시스템 (레거시 아님)
- `com.unity.2d.animation` 13.0.4 — 2D 스켈레탈 애니메이션
- `com.unity.ai.assistant` 2.7.0-pre.3 — Unity AI 어시스턴트 (프리릴리즈)

## Unity MCP
Claude Code 세션에서 `unity-mcp`를 통해 에디터 명령 실행, 씬 캡처,
콘솔 로그 읽기, 에셋 생성 등이 가능함.

## 주의사항
- 입력 처리는 **새 Input System** 사용 — `Input.GetKey()` 등 레거시 API 사용 금지
- C# 스크립트가 아직 없음 — 스크립트 위치 가정 전에 `Assets/` 먼저 확인
- URP 2D는 `Global Light 2D` 사용 — 일반 Directional Light는 씬에서 동작하지 않음
