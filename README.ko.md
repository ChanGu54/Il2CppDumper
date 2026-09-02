# Il2CppDumper

관리자: [ChanGu54](https://github.com/ChanGu54)

English: [README.md](README.md)

中文说明请戳[这里](README.zh-CN.md)

Unity il2cpp 리버스 엔지니어링

## 기능

* DLL을 완전히 복원합니다(코드 제외). `MonoBehaviour`와 `MonoScript` 추출에 사용할 수 있습니다
* ELF, ELF64, Mach-O(Fat Mach-O 포함), PE, NSO, WASM 형식을 지원합니다
* Unity 5.3 - Unity 6을 지원합니다. 메타데이터 버전은 16 - 108까지이며, 24.x / 27.x / 29.x / 106.1 같은 세부 레이아웃은 자동으로 판별합니다
* IDA, Ghidra, Binary Ninja, Hopper에서 il2cpp 파일을 더 쉽게 분석할 수 있도록 스크립트 생성을 지원합니다
* 구조체 헤더 파일 생성을 지원합니다
* 보호를 우회하기 위해 Android 메모리에서 덤프한 `libil2cpp.so` 파일을 지원합니다
* 간단한 PE 보호 우회를 지원합니다

## 사용법

[.NET 6.0 또는 .NET 8.0 런타임](https://dotnet.microsoft.com/download)이 필요합니다.

`Il2CppDumper.exe`를 실행한 뒤 il2cpp 실행 파일과 `global-metadata.dat` 파일을 선택하고, 안내에 따라 정보를 입력하세요.

이후 프로그램은 현재 작업 디렉터리에 모든 출력 파일을 생성합니다.

### 명령줄

```
Il2CppDumper.exe <executable-file> <global-metadata> <output-directory>
```

### 출력

#### dump.cs

모든 타입, 메서드, 필드, 프로퍼티를 오프셋과 토큰과 함께 기록한 텍스트 덤프입니다.

무엇을 기록할지는 `config.json`의 `Dump*` 옵션으로 결정됩니다.

#### DummyDll

폴더입니다. 복원된 모든 dll 파일이 들어 있습니다.

[dnSpy](https://github.com/0xd4d/dnSpy), [ILSpy](https://github.com/icsharpcode/ILSpy) 또는 다른 .NET 디컴파일러로 확인할 수 있습니다.

Unity `MonoBehaviour`와 `MonoScript` 추출에 사용할 수 있습니다. [UtinyRipper](https://github.com/mafaca/UtinyRipper), [UABE](https://7daystodie.com/forums/showthread.php?22675-Unity-Assets-Bundle-Extractor)와 함께 사용할 수 있습니다.

#### il2cpp.h

구조체 정보 헤더 파일입니다.

#### script.json

ida.py, ghidra.py, hopper-py3.py, Il2CppBinaryNinja용입니다.

#### stringliteral.json

모든 stringLiteral 정보를 포함합니다.

`il2cpp.h`, `script.json`, `stringliteral.json`은 `GenerateStruct`가 `true`일 때만 생성됩니다.

### 디스어셈블러 스크립트

아래 파일들은 덤프 결과물이 아니라 `Il2CppDumper.exe`와 함께 배포되는 스크립트이며, `script.json`과 `il2cpp.h`를 입력으로 사용합니다.

#### ida.py / ida_py3.py

IDA용입니다. `ida_py3.py`가 Python 3 버전입니다.

#### ida_with_struct.py / ida_with_struct_py3.py

IDA용입니다. il2cpp.h 파일을 읽고 IDA에 구조체 정보를 적용합니다. `ida_with_struct_py3.py`가 Python 3 버전입니다.

#### ghidra.py

Ghidra용입니다.

#### ghidra_with_struct.py

Ghidra용입니다. 구조체 정보를 적용하며, 먼저 il2cpp.h를 가져와야 합니다(`il2cpp_header_to_ghidra.py` 참고).

#### ghidra_wasm.py

Ghidra용입니다. [ghidra-wasm-plugin](https://github.com/nneonneo/ghidra-wasm-plugin)과 함께 사용합니다.

#### il2cpp_header_to_ghidra.py

`il2cpp.h`를 Ghidra의 C 파서가 받아들이는 형태로 변환합니다.

#### hopper-py3.py

Hopper용입니다. Python 3입니다.

#### Il2CppBinaryNinja

BinaryNinja용입니다.

#### il2cpp_header_to_binja.py

`il2cpp.h`를 Binary Ninja의 타입 파서가 받아들이는 형태로 변환합니다.

### 설정

모든 설정 옵션은 `config.json`에 있습니다.

사용 가능한 옵션:

* `DumpMethod`, `DumpField`, `DumpProperty`, `DumpAttribute`, `DumpFieldOffset`, `DumpMethodOffset`, `DumpTypeDefIndex`
  * 해당 정보를 dump.cs에 출력할지 여부

* `GenerateDummyDll`, `GenerateStruct`
  * 해당 항목을 생성할지 여부. `GenerateStruct`는 `il2cpp.h`, `script.json`, `stringliteral.json`을 제어합니다

* `DummyDllAddToken`
  * DummyDll에 토큰을 추가할지 여부

* `RequireAnyKey`
  * 종료 시 아무 키나 눌러야 할지 여부

* `ForceIl2CppVersion`, `ForceVersion`
  * `ForceIl2CppVersion`이 `true`이면, 프로그램은 `ForceVersion`에 지정된 버전 번호로 il2cpp 바이너리 파서를 선택합니다(메타데이터 파서 선택에는 영향 없음). 일부 오래된 il2cpp 버전에서 유용할 수 있습니다(예: il2cpp v20(Android) 바이너리에서 정상 동작을 위해 v16 파서가 필요할 수 있음)

* `ForceDump`
  * 파일을 덤프된 것으로 강제로 처리합니다

* `NoRedirectedPointer`
  * 덤프 파일의 포인터를 리다이렉트되지 않은 것으로 처리합니다. 일부 기기에서 덤프한 파일은 이 옵션을 `true`로 설정해야 합니다

* `DisablePlusSearch`
  * `PlusSearch`를 건너뛰고 버전 자동 보정을 하지 않은 채 CodeRegistration/MetadataRegistration을 찾습니다. 일부 덤프된 GameAssembly.dll(주로 Unity 2021.3)에서 발생하는 크래시를 피할 수 있습니다. 주소를 못 찾으면 수동 입력으로 넘어갑니다

## 빌드

```
dotnet build Il2CppDumper.sln
```

`net6.0`과 `net8.0`을 타겟으로 하며, 외부 의존성은 `Mono.Cecil` 하나입니다.

## 자주 발생하는 오류

#### `ERROR: Metadata file supplied is not valid metadata file.`

올바른 파일을 선택했는지 확인하세요. 어떤 게임은 콘텐츠 보호 등을 위해 이 파일을 난독화하기도 합니다. 이러한 파일의 난독화 해제는 이 프로그램의 범위를 벗어나므로, 난독화 해제와 관련한 이슈는 **제출하지 마세요**.

파일이 `libil2cpp.so`이고 루팅된 Android 폰이 있다면, [Zygisk-Il2CppDumper](https://github.com/Perfare/Zygisk-Il2CppDumper)를 사용해 볼 수 있습니다. 이 보호를 우회할 수 있습니다.

#### `ERROR: Metadata file supplied is not a supported version[x].`

메타데이터 버전이 지원 범위(16 - 108)를 벗어났습니다. 지원 범위보다 새로운 Unity 릴리스에서 나온 파일이거나, 난독화된 파일입니다.

#### `ERROR: Can't use auto mode to process file, try manual mode.`

PC 플랫폼의 실행 파일은 `GameAssembly.dll` 또는 `*Assembly.dll`입니다.

새 이슈를 열고 파일을 업로드해 주세요.

#### `ERROR: This file may be protected.`

Il2CppDumper가 실행 파일이 보호되어 있음을 감지했습니다. `GameGuardian`으로 게임 메모리에서 `libil2cpp.so`를 덤프한 뒤 Il2CppDumper로 로드하고 안내에 따르면, 대부분의 보호를 우회할 수 있습니다.

## 크레딧

- Perfare - [Il2CppDumper](https://github.com/Perfare/Il2CppDumper)
- Jumboperson - [Il2CppDumper](https://github.com/Jumboperson/Il2CppDumper)
