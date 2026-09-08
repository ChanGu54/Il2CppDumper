# Il2CppDumper

유지보수: [ChanGu54](https://github.com/ChanGu54)

[English](README.md) | [简体中文](README.zh-CN.md)

Unity il2cpp 리버스 엔지니어링

## 기능

* 코드 없이 DLL을 복원합니다. `MonoBehaviour`와 `MonoScript` 추출에 사용할 수 있습니다.
* ELF, ELF64, Mach-O(Fat Mach-O 포함), PE, NSO, WASM 형식을 지원합니다.
* Unity 5.3부터 Unity 6까지 지원합니다. 메타데이터 버전은 16~108이며, 24.x / 27.x / 29.x / 106.1 같은 세부 레이아웃은 자동으로 판별합니다.
* IDA, Ghidra, Binary Ninja, Hopper에서 il2cpp 파일을 분석할 수 있도록 스크립트를 생성합니다.
* 구조체 헤더 파일을 생성합니다.
* Android 메모리에서 덤프한 `libil2cpp.so`를 지원합니다. 일부 보호를 우회하는 데 사용할 수 있습니다.
* 간단한 PE 보호를 우회할 수 있습니다.
* Windows와 macOS용 GUI를 제공합니다.

## 사용법

[.NET 8.0 런타임](https://dotnet.microsoft.com/download)이 필요합니다.

### GUI

인자 없이 `Il2CppDumper`를 실행하면 Windows와 macOS에서 GUI가 열립니다. il2cpp 실행 파일, `global-metadata.dat`, 출력 폴더를 선택한 다음 Dump를 누르세요.

파일을 창으로 끌어다 놓을 수도 있습니다.

덤프가 끝나면 로그에 출력 경로가 표시되고, 출력 폴더가 자동으로 열립니다. 파일은 선택한 폴더에 저장되며, 기본값은 프로그램이 있는 디렉터리입니다.

### 명령줄

il2cpp 실행 파일, `global-metadata.dat`, 출력 디렉터리를 인자로 넘깁니다.

```
Il2CppDumper.exe <executable-file> <global-metadata> <output-directory>
```

출력 디렉터리를 생략하면 프로그램이 있는 디렉터리에 저장됩니다.

### 출력

#### dump.cs

모든 타입, 메서드, 필드, 프로퍼티를 오프셋과 토큰과 함께 기록한 텍스트 덤프입니다.

무엇을 기록할지는 `config.json`의 `Dump*` 옵션으로 결정됩니다.

#### DummyDll

복원된 DLL이 들어 있는 폴더입니다.

[dnSpy](https://github.com/0xd4d/dnSpy), [ILSpy](https://github.com/icsharpcode/ILSpy) 같은 .NET 디컴파일러로 내용을 확인할 수 있습니다.

Unity `MonoBehaviour`와 `MonoScript`를 추출할 때 사용할 수 있으며, [UtinyRipper](https://github.com/mafaca/UtinyRipper), [UABE](https://7daystodie.com/forums/showthread.php?22675-Unity-Assets-Bundle-Extractor)와 함께 쓸 수 있습니다.

#### il2cpp.h

구조체 정의가 담긴 헤더 파일입니다.

#### script.json

`ida.py`, `ghidra.py`, `hopper-py3.py`, Il2CppBinaryNinja의 입력입니다.

#### stringliteral.json

모든 stringLiteral 정보를 담습니다.

`il2cpp.h`, `script.json`, `stringliteral.json`은 `GenerateStruct`가 `true`일 때만 생성됩니다.

### 디스어셈블러 스크립트

아래 파일은 덤프 결과물이 아닙니다. `Il2CppDumper.exe`와 함께 배포되며, `script.json`과 `il2cpp.h`를 읽습니다.

#### ida.py / ida_py3.py

IDA용입니다. `ida_py3.py`가 Python 3 버전입니다.

#### ida_with_struct.py / ida_with_struct_py3.py

IDA용입니다. `il2cpp.h`를 읽고 구조체 정보를 적용합니다. `ida_with_struct_py3.py`가 Python 3 버전입니다.

#### ghidra.py

Ghidra용입니다.

#### ghidra_with_struct.py

Ghidra용입니다. 구조체 정보를 적용하려면 먼저 `il2cpp.h`를 가져와야 합니다. `il2cpp_header_to_ghidra.py`를 참고하세요.

#### ghidra_wasm.py

Ghidra용입니다. [ghidra-wasm-plugin](https://github.com/nneonneo/ghidra-wasm-plugin)과 함께 사용합니다.

#### il2cpp_header_to_ghidra.py

`il2cpp.h`를 Ghidra의 C 파서가 받아들일 수 있는 형태로 바꿉니다.

#### hopper-py3.py

Hopper용입니다. Python 3입니다.

#### Il2CppBinaryNinja

Binary Ninja용입니다.

#### il2cpp_header_to_binja.py

`il2cpp.h`를 Binary Ninja의 타입 파서가 받아들일 수 있는 형태로 바꿉니다.

### 설정

모든 옵션은 `config.json`에 있습니다.

사용 가능한 옵션:

* `DumpMethod`, `DumpField`, `DumpProperty`, `DumpAttribute`, `DumpFieldOffset`, `DumpMethodOffset`, `DumpTypeDefIndex`
  * 해당 정보를 `dump.cs`에 넣을지 여부

* `GenerateDummyDll`, `GenerateStruct`
  * 해당 산출물을 생성할지 여부. `GenerateStruct`는 `il2cpp.h`, `script.json`, `stringliteral.json`을 제어합니다.

* `DummyDllAddToken`
  * DummyDll에 토큰을 넣을지 여부

* `RequireAnyKey`
  * 종료 전에 키 입력을 기다릴지 여부

* `ForceIl2CppVersion`, `ForceVersion`
  * `ForceIl2CppVersion`이 `true`이면 `ForceVersion`에 지정한 버전으로 il2cpp 바이너리 파서를 고릅니다. 메타데이터 파서 선택에는 영향을 주지 않습니다. 일부 오래된 il2cpp에서 필요할 수 있습니다. 예를 들어 Android의 il2cpp v20 바이너리는 v16 파서를 써야 정상 동작하는 경우가 있습니다.

* `ForceDump`
  * 파일을 메모리 덤프로 취급합니다.

* `NoRedirectedPointer`
  * 덤프 파일의 포인터를 리다이렉트되지 않은 것으로 취급합니다. 일부 기기에서 덤프한 파일은 이 옵션을 `true`로 두어야 합니다.

* `DisablePlusSearch`
  * `PlusSearch`를 건너뛰고, 버전 자동 보정 없이 CodeRegistration/MetadataRegistration을 찾습니다. 덤프된 일부 `GameAssembly.dll`(주로 Unity 2021.3)에서 크래시가 나는 것을 피할 수 있습니다. 주소를 찾지 못하면 수동 입력으로 넘어갑니다.

## 빌드

```
dotnet build Il2CppDumper.sln
```

대상 프레임워크는 `net8.0`이며, 외부 의존성은 `Mono.Cecil` 하나입니다.

## 자주 발생하는 오류

#### `ERROR: Metadata file supplied is not valid metadata file.`

올바른 파일을 선택했는지 확인하세요. 어떤 게임은 콘텐츠 보호 등을 위해 이 파일을 난독화합니다. 이런 파일의 난독화 해제는 이 프로그램의 범위가 아니므로, 난독화 해제와 관련한 이슈는 **제출하지 마세요**.

파일이 `libil2cpp.so`이고 루팅된 Android 기기가 있다면 [Zygisk-Il2CppDumper](https://github.com/Perfare/Zygisk-Il2CppDumper)를 시도해 볼 수 있습니다. 이 보호를 우회할 수 있습니다.

#### `ERROR: Metadata file supplied is not a supported version[x].`

메타데이터 버전이 지원 범위(16~108)를 벗어났습니다. 이 빌드가 지원하는 것보다 새로운 Unity에서 나온 파일이거나, 난독화된 파일입니다.

#### `ERROR: Can't use auto mode to process file, try manual mode.`

PC 플랫폼의 실행 파일은 `GameAssembly.dll` 또는 `*Assembly.dll`입니다.

새 이슈를 열고 파일을 업로드해 주세요.

#### `ERROR: This file may be protected.`

Il2CppDumper가 실행 파일이 보호되어 있음을 감지했습니다. `GameGuardian`으로 게임 메모리에서 `libil2cpp.so`를 덤프한 뒤 Il2CppDumper로 불러와 안내에 따르면, 대부분의 보호를 우회할 수 있습니다.

## 기여자

- Perfare - [Il2CppDumper](https://github.com/Perfare/Il2CppDumper)
- Jumboperson - [Il2CppDumper](https://github.com/Jumboperson/Il2CppDumper)
