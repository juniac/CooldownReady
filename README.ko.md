# CooldownReady

[English](README.md) | [한국어](README.ko.md)

## 소개

CooldownReady는 키보드 특정 키를 누르면 설정 시간에 소리 알림 및 카운트다운을 표시합니다.
화면을 계속 확인하지 않아도 반복 동작의 쿨다운을 소리로 확인할 수 있습니다.

### 주요 기능

- 키 입력 시 카운트다운
- 시간 설정
- 타이머 종료 전 알림 시점 설정
- 여러 개의 알림음
- 남은 시간 및 진행 상태 표시
- 창 항상 위 고정

### 요구사항

- Windows 10 1809(빌드 17763) 이상

### 사용 방법

1. CooldownReady를 실행합니다.
2. `모니터링 키` 입력 칸을 선택합니다.
3. 모니터링할 키를 누릅니다.
4. `쿨다운 시간`을 설정합니다.
5. `알림 시간`을 설정합니다.
   - 예: 쿨다운 `30`초, 알림 시간 `5`초입니다.
   - 남은 시간이 `5`초가 되면 알림음이 재생됩니다.
6. `알림 소리 선택`에서 사용할 소리를 고릅니다.
7. `설정 저장`을 누릅니다.
8. `모니터링 시작`을 누릅니다.
9. 설정한 키를 눌러 카운트다운을 시작합니다.

같은 키를 다시 누르면 카운트다운이 처음부터 다시 시작됩니다. `중지`를 누르면 모니터링과 타이머가 정지됩니다.

### 설정 저장

설정은 Windows 앱 로컬 설정에 `CooldownReadySettings` 이름으로 저장됩니다.

저장 항목:

- 모니터링 키
- 쿨다운 시간
- 알림 시간
- 선택한 알림음
- 항상 위 설정

오류 로그는 다음 경로에 기록됩니다.

```text
%LOCALAPPDATA%\CooldownReady\error.log
```

### 라이선스

이 프로젝트는 [MIT 라이선스](LICENSE)를 사용합니다.

## 2. 개발 방법

### 기술 스택

- .NET 8
- WinUI 3
- Windows App SDK
- MSIX 패키지 지원
- Unpackaged 데스크톱 실행

### 소스에서 실행

Visual Studio:

1. `CooldownReady.slnx`를 엽니다.
2. 실행 프로필을 선택합니다.
   - `CooldownReady (Unpackaged)`: 일반 데스크톱 앱으로 실행합니다.
   - `CooldownReady (Package)`: MSIX 패키지 모드로 실행합니다.
3. `F5`로 실행합니다.

.NET CLI:

```powershell
dotnet restore .\CooldownReady.slnx
dotnet run --project .\CooldownReady.csproj -c Debug -p:Platform=x64
```

### 빌드

```powershell
dotnet build .\CooldownReady.slnx -c Debug -p:Platform=x64
dotnet build .\CooldownReady.slnx -c Release -p:Platform=x64
dotnet publish .\CooldownReady.csproj -c Release -p:Platform=x64 -r win-x64
```

### 프로젝트 구조

```text
CooldownReady.csproj        프로젝트 설정
CooldownReady.slnx          솔루션 파일
App.xaml                    앱 리소스
App.xaml.cs                 앱 시작 및 전역 예외 처리
MainWindow.xaml             메인 화면 UI
MainWindow.xaml.cs          타이머, 설정, 사운드, 창 제어 로직
GlobalKeyboardHook.cs       전역 키보드 훅
Assets\                     아이콘, 이미지, 사운드
Package.appxmanifest        MSIX 패키지 매니페스트
```

### 참고

- `WindowsPackageType`은 `None`으로 설정되어 있어 Unpackaged 실행을 지원합니다.
- 사운드 파일은 패키징 실행과 직접 실행에서 모두 `Assets` 경로에서 로드됩니다.
