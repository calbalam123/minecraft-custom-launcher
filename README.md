# CALBALAM Minecraft Custom Launcher

Windows용 Minecraft Java Edition 커스텀 런처입니다.

## 기능
- Minecraft 버전 목록 조회 및 자동 설치
- Vanilla / 설치된 Fabric / Forge / NeoForge / Quilt / OptiFine 프로필 실행
- Microsoft 계정 로그인 및 로그아웃
- 오프라인 테스트 세션
- 플레이어 이름 / RAM / Java 경로 저장
- 서버 목록 추가 / 삭제
- 서버 선택 후 Minecraft 시작과 동시에 직접 접속
- GitHub Releases 기반 업데이트 확인
- GitHub Actions Windows x64 단일 EXE 빌드

## 빌드
GitHub의 Actions → Build Windows Launcher에서 CalbalamLauncher-win-x64 artifact를 받을 수 있습니다.

로컬 빌드:
```powershell
dotnet restore
dotnet publish MinecraftCustomLauncher.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

## 기술
- .NET 8 WinForms
- CmlLib.Core 4.0.6
- CmlLib.Core.Auth.Microsoft 3.2.2

CmlLib.Core는 Vanilla와 여러 커스텀 Minecraft 프로필 실행, Microsoft 인증, 직접 서버 접속 옵션 등을 지원합니다.

## 주의
Microsoft 로그인은 Windows WebView2 Runtime이 필요할 수 있습니다.
자동 업데이트는 GitHub Releases의 최신 태그를 확인한 뒤 최신 버전이 있으면 Releases 페이지를 엽니다. 실제 EXE 교체는 사용자가 다운로드해 실행하는 방식입니다.