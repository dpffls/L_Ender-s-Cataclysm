# GyuminServer Forge 1.20.1 모드팩 자동 설치기

친구가 exe 하나만 실행하면 Forge 47.4.26 설치 + 모드 6개 다운로드가 끝나는 프로그램입니다.

설치되는 모드:
- Just Enough Items (JEI)
- Open Parties and Claims
- Xaero's Minimap / Xaero's World Map
- Lionfish API
- L_Ender's Cataclysm

## 구조
- `installer.py` : 설치 프로그램 본체 (파이썬)
- `manifest.json` : Forge 버전 + 모드 목록 (여기만 고치면 모드를 바꿀 수 있어요)
- `.github/workflows/build.yml` : GitHub가 exe를 자동으로 만들어주는 설정

## 1. 저장소에 파일 올리기
저장소: https://github.com/dpffls/L_Ender-s-Cataclysm (공개)

1. 예전 C# 파일(`Program.cs`, `mods.json`, `ModAutoInstaller.csproj`)은 지워도 돼요.
2. `installer.py`, `manifest.json`을 올려요. (이름에 `(1)`이 붙으면 안 돼요)
3. `build.yml`은 폴더 안에 있어야 합니다.
   - 저장소에서 **Add file → Create new file**
   - 이름 칸에 `.github/workflows/build.yml` 을 입력 (슬래시를 치면 폴더가 자동으로 만들어져요)
   - 내용을 붙여넣고 **Commit changes**

## 2. 릴리즈에 올려둘 파일 (태그 `mod`)
jar 6개 + `forge-installer.jar` (이미 올려둔 상태 그대로 사용)

## 3. exe 만들기 (자동)
`installer.py`가 올라가면 GitHub가 자동으로 exe를 만들어요.
1. 저장소의 **Actions** 탭 → 맨 위 "Build Installer EXE" 클릭
2. 초록 체크가 뜨면 아래 **Artifacts** 에서 `GyuminModpackInstaller` 다운로드
3. 압축을 풀면 `GyuminModpackInstaller.exe` 가 나와요. 이걸 친구에게 보내면 됩니다.

## 4. 친구가 할 일
exe 더블클릭. Windows 경고가 뜨면 "추가 정보 → 실행".
(Java가 없어도 알아서 준비해줍니다.)

## 나중에 모드를 바꾸고 싶을 때
`manifest.json`만 고쳐서 올리면 돼요. exe를 다시 만들 필요 없어요.
