# Il2CppDumper — Agent Guide

이 파일은 코딩 에이전트(Cursor 등)가 저장소를 빠르게 이해하기 위한 내부 문서다. 사람용 설명은 `README.md`, `README.ko.md`를 본다.

## 프로젝트

Unity il2cpp 바이너리와 `global-metadata.dat`를 파싱해 다음을 생성한다.

- `dump.cs` — 타입/메서드/필드 덤프
- DummyDll — 코드 없는 복원 DLL (Mono.Cecil)
- `il2cpp.h`, `script.json`, `stringliteral.json` — IDA/Ghidra/Binary Ninja 분석용

단일 C# 콘솔 앱이다. 솔루션: `Il2CppDumper.sln`. 타겟: `net6.0`, `net8.0`. 외부 의존성은 `Mono.Cecil` 하나다.

```
Il2CppDumper.exe <executable-file> <global-metadata> <output-directory>
```

Windows에서는 인자 없이 실행하면 파일 선택 대화상자가 열린다.

## 디렉터리

모든 소스의 루트는 `Il2CppDumper/`. 네임스페이스는 전부 `Il2CppDumper`이며 폴더별 하위 네임스페이스를 쓰지 않는다.

| 경로 | 역할 |
| --- | --- |
| `Program.cs` | 엔트리. 매직으로 포맷 판별 → `Init` → `Dump` |
| `Config.cs` / `config.json` | 런타임 옵션. JSON은 출력 디렉터리로 복사됨 |
| `Il2Cpp/` | 메타데이터 + il2cpp 런타임 구조체/파서 |
| `ExecutableFormats/` | ELF/PE/Mach-O/NSO/WASM 로더 |
| `IO/` | `BinaryStream`, LZ4 |
| `Outputs/` | dump.cs, DummyDll, 구조체/스크립트 생성 |
| `Utils/` | 검색, 실행기, DummyAssembly, PE 로더 |
| `Attributes/` | `[Version]`, `[ArrayLength]` — 구조체 필드 게이트 |
| `*.py`, `Il2CppBinaryNinja/` | 디스어셈블러 플러그인/스크립트. 빌드 출력으로 복사 |

## 처리 파이프라인

`Program.Init` → `Program.Dump` 순서로 고정되어 있다.

1. `Metadata`가 `global-metadata.dat`를 읽고 버전을 보정한다. 매직은 `0xFAB11BAF`. 지원 메타데이터 버전은 16–108.
   - v32–34는 v31과 같은 레이아웃
   - v35: `elementTypeIndex`, `Il2CppStringLiteral.length` 제거
   - v38: `Il2CppSectionMetadata` 헤더 + 가변 폭 인덱스(`TypeIndex` 등)
   - v39: `ParamIndex`
   - v104–106: 더 많은 인덱스가 가변 폭
   - 파일 버전 106/107: 바이너리에 `alwaysInitMetadataUsages`가 있으면 내부 버전 106.1, 없으면 106 (Unity는 107을 실제 레이아웃으로 쓰지 않음)
   - v108: method spec / generic method table / RGCTX / invoker 인덱스가 메타데이터로 이동. RGCTX는 5바이트(`byte` + `int32`)
2. 바이너리 선두 매직으로 포맷을 고른다.
   - `0x6D736100` WASM
   - `0x304F534E` NSO
   - `0x905A4D` PE
   - `0x464c457f` ELF / ELF64 (`ei_class == 2`면 64비트)
   - `0xFEEDFACE` / `0xFEEDFACF` Mach-O
   - `0xCAFEBABE` / `0xBEBAFECA` Fat Mach-O
3. `il2Cpp.SetProperties(version, metadataUsagesCount)` 후 덤프 파일 여부를 검사한다. ELF 덤프면 사용자가 입력한 dump address를 `ImageBase`로 쓴다.
4. 등록 테이블 검색 순서는 `PlusSearch` → (Windows PE면 `PELoader`) → `Search` → `SymbolSearch` → 수동 `CodeRegistration`/`MetadataRegistration` 입력.
5. `Il2CppExecutor`가 메타데이터와 바이너리를 연결한다.
6. `Il2CppDecompiler.Decompile`이 `dump.cs`를 쓴다. `GenerateStruct`면 `StructGenerator`, `GenerateDummyDll`이면 `DummyAssemblyExporter`.

포맷 로더를 추가할 때는 `Program.Init`의 switch와 `Il2Cpp` 추상 메서드 구현이 둘 다 필요하다.

## 핵심 타입

- `BinaryStream` — 버전 인지 바이너리 IO. `ReadClass<T>()`가 `[Version(Min, Max)]`를 보고 필드를 건너뛰거나 읽는다. 포인터 크기는 `Is32Bit`.
- `Metadata` : `BinaryStream` — `Il2CppGlobalMetadataHeader`와 정의 배열. 버전 24.x는 헤더 레이아웃으로 24.1/24.2/24.4를 재분류한다.
- `Il2Cpp` : `BinaryStream` (abstract) — `CodeRegistration`/`MetadataRegistration`을 찾아 메서드 포인터, 타입, generic 테이블을 채운다. `MapVATR`/`MapRTVA`로 가상주소↔파일오프셋을 변환한다.
- 포맷 클래스: `Elf`/`Elf64`(`ElfBase`), `PE`, `Macho`/`Macho64`, `NSO`, `WebAssembly`(+`WebAssemblyMemory`).
- 구조체 정의: `Il2Cpp/Il2CppClass.cs`, `Il2Cpp/MetadataClass.cs`, 각 `*Class.cs`. 필드에 `[Version]`을 걸어서 Unity/il2cpp 버전별 레이아웃을 표현한다.

새 il2cpp 버전을 넣을 때는 파서 분기보다 **구조체 필드 `[Version]`부터** 맞추는 것이 일반적이다.

## 설정

`config.json` ↔ `Config`. 기본값은 `Config.cs`와 JSON이 다를 수 있다. 실행 시 JSON이 이긴다.

의미 있는 플래그:

- `DumpMethod` 등 — `dump.cs`에 무엇을 쓸지
- `GenerateDummyDll`, `GenerateStruct` — 산출물
- `ForceIl2CppVersion` + `ForceVersion` — 메타데이터 버전과 다르게 바이너리 파서를 고름
- `ForceDump`, `NoRedirectedPointer` — 메모리 덤프 `libil2cpp.so` 처리

README의 `GenerateScript`는 코드상 `GenerateStruct`다.

## 컨벤션

- .NET 스타일: 4칸 들여쓰기, Allman 브레이스, 기존 파일의 `var`/명시 타입 혼용을 유지한다.
- 새 폴더 네임스페이스를 만들지 않는다.
- 버전 의존 필드는 if-ladder보다 `[Version(Min = …, Max = …)]`를 쓴다.
- 매직 넘버와 에러 문자열(`ERROR: …`)은 기존 문구를 유지한다. 프론트엔드/이슈 검색이 이 문자열에 의존한다.
- 출력 파일 이름(`dump.cs`, `il2cpp.h`, `script.json`, DummyDll)을 바꾸지 않는다.
- Python 스크립트는 C#이 만든 `script.json`/`il2cpp.h`를 소비한다. JSON 스키마를 바꾸면 `ida.py`, `ghidra.py`, `Il2CppBinaryNinja`도 같이 본다.

## 빌드

```
dotnet build Il2CppDumper.sln
dotnet build Il2CppDumper/Il2CppDumper.csproj -f net8.0
```

테스트 프로젝트는 없다. 동작 확인은 실제 il2cpp 바이너리 + `global-metadata.dat`로 한다.

## 에이전트 주의

- 난독화 해제, 보호 우회 익스플로잇, 치트용 패치를 추가하지 않는다. 이 도구는 메타데이터/심볼 복원과 분석 스크립트 생성까지다.
- `ForceVersion` 기본값(`Config.cs`의 24.3 vs `config.json`의 16)처럼 문서와 코드가 어긋날 수 있다. 동작은 실행 시 읽히는 `config.json` 기준이다.
- 큰 리팩터(네임스페이스 분리, 비동기화, 새 DI)는 요청 없이 하지 않는다.
