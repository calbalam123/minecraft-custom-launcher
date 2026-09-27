# CALBALAM Minecraft Custom Launcher

Windows용 Minecraft Java Edition 커스텀 런처입니다.

## 현재 기능
- Minecraft 버전 목록 조회
- 선택한 버전 자동 설치
- Minecraft 실행
- 오프라인 테스트 세션
- 플레이어 이름 저장
- RAM 설정
- 사용자 지정 Java 경로
- 설정 자동 저장
- GitHub Actions를 통한 Windows x64 단일 EXE 빌드

## 빌드
GitHub의 **Actions → Build Windows Launcher**에서 실행 결과의
`CalbalamLauncher-win-x64` artifact를 다운로드할 수 있습니다.

로컬 빌드:
```powershell
dotnet restore
dotnet publish MinecraftCustomLauncher.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish
```

## 기술
- .NET 8 WinForms
- CmlLib.Core 4.0.4

## 다음 확장
- Microsoft 계정 로그인
- Fabric / Forge 설치 UI
- 런처 아이콘 및 스킨
- 서버 목록
