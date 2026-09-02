# Il2CppDumper

[![Build status](https://ci.appveyor.com/api/projects/status/anhqw33vcpmp8ofa?svg=true)](https://ci.appveyor.com/project/Perfare/il2cppdumper/branch/master/artifacts)

English: [README.md](README.md)

中文说明请戳[这里](README.zh-CN.md)

Unity il2cpp 리버스 엔지니어링

## 기능

* DLL을 완전히 복원합니다(코드 제외). `MonoBehaviour`와 `MonoScript` 추출에 사용할 수 있습니다
* ELF, ELF64, Mach-O, PE, NSO, WASM 형식을 지원합니다
* Unity 5.3 - 2022.2를 지원합니다. 메타데이터 버전은 16 - 108까지입니다 (Unity 6 / 6000.x, 106.1 / 107 포함)
* IDA, Ghidra, Binary Ninja에서 il2cpp 파일을 더 쉽게 분석할 수 있도록 스크립트 생성을 지원합니다
* 구조체 헤더 파일 생성을 지원합니다
* 보호를 우회하기 위해 Android 메모리에서 덤프한 `libil2cpp.so` 파일을 지원합니다
* 간단한 PE 보호 우회를 지원합니다

## 사용법

`Il2CppDumper.exe`를 실행한 뒤 il2cpp 실행 파일과 `global-metadata.dat` 파일을 선택하고, 안내에 따라 정보를 입력하세요.

이후 프로그램은 현재 작업 디렉터리에 모든 출력 파일을 생성합니다.

### 명령줄

```
Il2CppDumper.exe <executable-file> <global-metadata> <output-directory>
```

### 출력

#### DummyDll

폴더입니다. 복원된 모든 dll 파일이 들어 있습니다.

[dnSpy](https://github.com/0xd4d/dnSpy), [ILSpy](https://github.com/icsharpcode/ILSpy) 또는 다른 .NET 디컴파일러로 확인할 수 있습니다.

Unity `MonoBehaviour`와 `MonoScript` 추출에 사용할 수 있습니다. [UtinyRipper](https://github.com/mafaca/UtinyRipper), [UABE](https://7daystodie.com/forums/showthread.php?22675-Unity-Assets-Bundle-Extractor)와 함께 사용할 수 있습니다.

#### ida.py

IDA용입니다.

#### ida_with_struct.py

IDA용입니다. il2cpp.h 파일을 읽고 IDA에 구조체 정보를 적용합니다.

#### il2cpp.h

구조체 정보 헤더 파일입니다.

#### ghidra.py

Ghidra용입니다.

#### Il2CppBinaryNinja

BinaryNinja용입니다.

#### ghidra_wasm.py

Ghidra용입니다. [ghidra-wasm-plugin](https://github.com/nneonneo/ghidra-wasm-plugin)과 함께 사용합니다.

#### script.json

ida.py, ghidra.py, Il2CppBinaryNinja용입니다.

#### stringliteral.json

모든 stringLiteral 정보를 포함합니다.

### 설정

모든 설정 옵션은 `config.json`에 있습니다.

사용 가능한 옵션:

* `DumpMethod`, `DumpField`, `DumpProperty`, `DumpAttribute`, `DumpFieldOffset`, `DumpMethodOffset`, `DumpTypeDefIndex`
  * 해당 정보를 dump.cs에 출력할지 여부

* `GenerateDummyDll`, `GenerateScript`
  * 해당 항목을 생성할지 여부

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

## 자주 발생하는 오류

#### `ERROR: Metadata file supplied is not valid metadata file.`

올바른 파일을 선택했는지 확인하세요. 어떤 게임은 콘텐츠 보호 등을 위해 이 파일을 난독화하기도 합니다. 이러한 파일의 난독화 해제는 이 프로그램의 범위를 벗어나므로, 난독화 해제와 관련한 이슈는 **제출하지 마세요**.

파일이 `libil2cpp.so`이고 루팅된 Android 폰이 있다면, 다른 프로젝트인 [Zygisk-Il2CppDumper](https://github.com/Perfare/Zygisk-Il2CppDumper)를 사용해 볼 수 있습니다. 이 보호를 우회할 수 있습니다.

#### `ERROR: Can't use auto mode to process file, try manual mode.`

PC 플랫폼의 실행 파일은 `GameAssembly.dll` 또는 `*Assembly.dll`입니다.

새 이슈를 열고 파일을 업로드해 주시면 해결을 시도하겠습니다.

#### `ERROR: This file may be protected.`

Il2CppDumper가 실행 파일이 보호되어 있음을 감지했습니다. `GameGuardian`으로 게임 메모리에서 `libil2cpp.so`를 덤프한 뒤 Il2CppDumper로 로드하고 안내에 따르면, 대부분의 보호를 우회할 수 있습니다.

루팅된 Android 폰이 있다면, 다른 프로젝트인 [Zygisk-Il2CppDumper](https://github.com/Perfare/Zygisk-Il2CppDumper)를 사용해 볼 수 있습니다. 거의 모든 보호를 우회할 수 있습니다.

## 크레딧

- Jumboperson - [Il2CppDumper](https://github.com/Jumboperson/Il2CppDumper)
