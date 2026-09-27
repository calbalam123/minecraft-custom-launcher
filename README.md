# CALBALAM Minecraft Custom Launcher

Windows용 Minecraft Java Edition 커스텀 런처입니다.

## 주요 기능

- Microsoft 계정 로그인 / 로그아웃
- 오프라인 테스트 세션
- Minecraft 버전 목록 조회 및 설치
- Vanilla 프로필 실행
- 설치되어 있는 Fabric / Forge / NeoForge / Quilt / OptiFine 프로필 실행
- 서버 목록 추가 / 삭제
- 서버 이름 / 주소 / 포트 저장
- 서버 선택 후 Minecraft 실행과 동시에 직접 접속
- 플레이어 이름 저장
- RAM 직접 설정
- RAM 자동 권장값 설정
- Java 실행 파일 직접 지정
- Minecraft 설치 / 다운로드 진행률 표시
- 게임 설치 및 실행 취소
- 마지막으로 선택한 버전 / 프로필 기억
- GitHub Releases 기반 업데이트 확인
- Windows x64 단일 EXE 빌드
- 설정 파일 자동 저장 및 안전한 교체 저장

## 사용 방법

1. GitHub Actions에서 `Build Windows Launcher`를 실행하거나 `main`에 커밋합니다.
2. 성공한 Actions 실행의 `CalbalamLauncher-win-x64` artifact를 받습니다.
3. `CalbalamLauncher.exe`를 실행합니다.
4. Microsoft 계정으로 로그인하거나 플레이어 이름을 입력해 오프라인 테스트를 사용할 수 있습니다.
5. Minecraft 버전을 선택합니다.
6. 이미 설치된 커스텀 프로필이 있다면 `프로필 / 로더`에서 선택합니다.
7. 서버를 등록한 경우 서버를 선택하고 `게임 실행`을 누르면 서버 주소와 포트가 실행 옵션으로 전달됩니다.

## 서버 설정

서버 주소와 포트는 다음 형식으로 저장됩니다.

- 주소: `play.example.com`
- 포트: `25565`

저장된 서버를 선택하면 다음 실행부터 같은 서버로 바로 접속할 수 있습니다.

## RAM / Java

RAM은 MB 단위로 직접 지정할 수 있으며 `자동` 버튼을 사용하면 시스템 메모리를 기준으로 권장값을 계산합니다.

Java 경로를 비워 두면 런처 / CmlLib의 기본 Java 탐색을 사용합니다. 직접 지정하려면 `java.exe` 파일을 선택할 수 있습니다.

## 설정 파일

런처 설정은 Windows 사용자 AppData 아래에 저장됩니다.

`%AppData%\CalbalamLauncher\settings.json`

저장 항목:

- 플레이어 이름
- RAM
- Java 경로
- 마지막 Minecraft 버전
- 마지막 프로필
- 서버 목록

## 빌드

GitHub Actions:

`Actions → Build Windows Launcher → CalbalamLauncher-win-x64`

로컬 빌드:

```powershell
dotnet restore
dotnet publish MinecraftCustomLauncher.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=None -p:DebugSymbols=false -o publish
```

릴리스 빌드는 Windows x64 self-contained 단일 EXE를 생성하며 Actions 단계에서 publish 폴더에 EXE 하나만 생성되는지도 검사합니다.

## 기술

- .NET 8 WinForms
- CmlLib.Core 4.0.6
- CmlLib.Core.Auth.Microsoft 3.2.2
- GitHub Actions
- Windows x64 self-contained single-file publish

CmlLib.Core는 Vanilla 및 여러 커스텀 Minecraft 버전 실행, Microsoft 인증, 설치, Java 관련 기능, 직접 서버 접속 옵션 등을 제공합니다.

## Microsoft 로그인

Microsoft 인증은 `CmlLib.Core.Auth.Microsoft`를 사용합니다. Windows 환경에서는 Microsoft OAuth/WebView2 기반 인증이 사용될 수 있습니다.

## 업데이트

런처는 GitHub Releases의 최신 `tag_name`을 확인하고 현재 런처 버전보다 새로운 버전이 있으면 Releases 페이지를 엽니다.

현재 자동 업데이트는 실행 중인 EXE를 강제로 교체하지 않습니다. 새 EXE를 다운로드하여 실행하는 안전한 방식입니다.

## 고급 관리

`고급 관리`에서 다음 기능을 사용할 수 있습니다.

- Minecraft 게임 디렉터리 변경 및 폴더 열기
- Fabric / Forge / NeoForge / Quilt 자동 설치
- 설치 후 필요한 Minecraft 파일 자동 보완
- `mods` 폴더의 `.jar` 모드 추가 / 삭제 / 새로고침
- Microsoft 계정의 Minecraft 프로필 조회
- URL 또는 PNG 파일을 이용한 스킨 적용
- 스킨 초기화
- 런처 및 Minecraft stdout/stderr 로그 확인

Forge/NeoForge 설치기는 CmlLib의 전용 installer 패키지를 사용합니다. Fabric/Quilt는 각 로더의 공식 메타데이터에서 프로필을 받아 설치합니다. CmlLib 문서에서도 Forge/Fabric/Quilt 자동 설치 흐름을 별도로 안내합니다.

## 주의

- 모드 `.jar`는 사용자가 신뢰하는 출처에서 받아야 합니다. Fabric 문서도 모드 출처와 Minecraft/로더 버전 호환성을 확인하도록 안내합니다.
- OptiFine은 CmlLib 공식 코어에 직접 포함된 자동 설치기가 아니라 별도 커뮤니티 installer가 안내되어 있으므로, 이 런처에서는 기존 OptiFine 프로필 실행을 유지합니다.
- Forge installer는 공식 다운로드 흐름과 광고 페이지 정책이 있어 자동 설치 후 관련 페이지가 열릴 수 있습니다.

## 현재 버전

**1.1.0**

## 라이선스

MIT
