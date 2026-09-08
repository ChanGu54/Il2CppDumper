# Il2CppDumper

Maintained by [ChanGu54](https://github.com/ChanGu54)

[简体中文](README.zh-CN.md) | [한국어](README.ko.md)

Unity il2cpp reverse engineering

## Features

* Restores DLLs without code, so you can extract `MonoBehaviour` and `MonoScript`
* Supports ELF, ELF64, Mach-O (including Fat Mach-O), PE, NSO, and WASM
* Supports Unity 5.3 through Unity 6, metadata versions 16–108 (layout variants such as 24.x, 27.x, 29.x, and 106.1 are detected automatically)
* Generates scripts for IDA, Ghidra, Binary Ninja, and Hopper to make il2cpp analysis easier
* Generates a header file with structure definitions
* Supports `libil2cpp.so` dumped from Android memory, which can help bypass some protections
* Can bypass simple PE protections
* Includes a Windows and macOS GUI, added and maintained by ChanGu54

## Usage

Requires the [.NET 8.0 runtime](https://dotnet.microsoft.com/download).

Run `Il2CppDumper` with no arguments to open the GUI on Windows and macOS. Choose the il2cpp executable, `global-metadata.dat`, and an output folder, then click Dump.

You can also drag and drop files onto the window.

When the dump finishes, the log shows the output path and the output folder opens automatically. Files are written to the folder you selected (the application directory by default).

### Command-line

```
Il2CppDumper.exe <executable-file> <global-metadata> <output-directory>
```

### Outputs

#### dump.cs

A text dump of all types, methods, fields, and properties, including offsets and tokens.

What is written is controlled by the `Dump*` options in `config.json`.

#### DummyDll

A folder of restored DLL files.

Open them with [dnSpy](https://github.com/0xd4d/dnSpy), [ILSpy](https://github.com/icsharpcode/ILSpy), or another .NET decompiler.

You can use them to extract Unity `MonoBehaviour` and `MonoScript` for tools such as [UtinyRipper](https://github.com/mafaca/UtinyRipper) and [UABE](https://7daystodie.com/forums/showthread.php?22675-Unity-Assets-Bundle-Extractor).

#### il2cpp.h

A header file with structure definitions.

#### script.json

Input for `ida.py`, `ghidra.py`, `hopper-py3.py`, and Il2CppBinaryNinja.

#### stringliteral.json

Contains all stringLiteral data.

`il2cpp.h`, `script.json`, and `stringliteral.json` are generated only when `GenerateStruct` is `true`.

### Disassembler scripts

These files are not produced by the dump. They ship next to `Il2CppDumper.exe` and read `script.json` / `il2cpp.h`.

#### ida.py / ida_py3.py

For IDA. `ida_py3.py` is the Python 3 version.

#### ida_with_struct.py / ida_with_struct_py3.py

For IDA. Reads `il2cpp.h` and applies structure information. `ida_with_struct_py3.py` is the Python 3 version.

#### ghidra.py

For Ghidra.

#### ghidra_with_struct.py

For Ghidra. Applies structure information. Import `il2cpp.h` first (see `il2cpp_header_to_ghidra.py`).

#### ghidra_wasm.py

For Ghidra. Use with [ghidra-wasm-plugin](https://github.com/nneonneo/ghidra-wasm-plugin).

#### il2cpp_header_to_ghidra.py

Rewrites `il2cpp.h` into a form Ghidra's C parser accepts.

#### hopper-py3.py

For Hopper (Python 3).

#### Il2CppBinaryNinja

For Binary Ninja.

#### il2cpp_header_to_binja.py

Rewrites `il2cpp.h` into a form Binary Ninja's type parser accepts.

### Configuration

All options are in `config.json`.

Available options:

* `DumpMethod`, `DumpField`, `DumpProperty`, `DumpAttribute`, `DumpFieldOffset`, `DumpMethodOffset`, `DumpTypeDefIndex`
  * Whether to include this information in `dump.cs`

* `GenerateDummyDll`, `GenerateStruct`
  * Whether to generate these outputs. `GenerateStruct` controls `il2cpp.h`, `script.json`, and `stringliteral.json`

* `DummyDllAddToken`
  * Whether to add tokens in DummyDll

* `RequireAnyKey`
  * Whether to wait for a key press before exiting

* `ForceIl2CppVersion`, `ForceVersion`
  * If `ForceIl2CppVersion` is `true`, the program uses the version in `ForceVersion` to choose the il2cpp binary parser. This does not affect the metadata parser. It can help with some older il2cpp builds. For example, Android il2cpp v20 binaries may need the v16 parser.

* `ForceDump`
  * Treat files as memory dumps

* `NoRedirectedPointer`
  * Treat pointers in dumped files as unredirected. Set this to `true` for dumps from some devices.

* `DisablePlusSearch`
  * Skip `PlusSearch` and look up CodeRegistration/MetadataRegistration without the version auto-adjust that can crash on some dumped `GameAssembly.dll` files (mostly Unity 2021.3). Falls back to manual input if the addresses are not found.

## Build

```
dotnet build Il2CppDumper.sln
```

Targets `net8.0`. The only dependency is `Mono.Cecil`.

## Common errors

#### `ERROR: Metadata file supplied is not valid metadata file.`

Make sure you chose the correct file. Some games obfuscate this file for content protection. Deobfuscating such files is outside the scope of this program, so please **DO NOT** file an issue about deobfuscation.

If your file is `libil2cpp.so` and you have a rooted Android phone, you can try [Zygisk-Il2CppDumper](https://github.com/Perfare/Zygisk-Il2CppDumper). It can bypass this protection.

#### `ERROR: Metadata file supplied is not a supported version[x].`

The metadata version is outside the supported range (16–108). The file is either from a newer Unity release than this build supports, or it is obfuscated.

#### `ERROR: Can't use auto mode to process file, try manual mode.`

On PC, the executable is `GameAssembly.dll` or `*Assembly.dll`.

You can open a new issue and upload the file.

#### `ERROR: This file may be protected.`

Il2CppDumper detected that the executable is protected. Dump `libil2cpp.so` from the game's memory with `GameGuardian`, then load it in Il2CppDumper and follow the prompts. This can bypass most protections.

## Credits

- Perfare - [Il2CppDumper](https://github.com/Perfare/Il2CppDumper)
- Jumboperson - [Il2CppDumper](https://github.com/Jumboperson/Il2CppDumper)
