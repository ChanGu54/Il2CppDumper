# Il2CppDumper

维护者：[ChanGu54](https://github.com/ChanGu54)

[English](README.md) | [한국어](README.ko.md)

Unity il2cpp 逆向工程

## 功能

* 还原 DLL（不含代码），可用于提取 `MonoBehaviour` 和 `MonoScript`
* 支持 ELF、ELF64、Mach-O（含 Fat Mach-O）、PE、NSO 和 WASM
* 支持 Unity 5.3 至 Unity 6，metadata 版本 16–108（24.x、27.x、29.x、106.1 等细分布局会自动识别）
* 生成 IDA、Ghidra、Binary Ninja 和 Hopper 脚本，便于分析 il2cpp 文件
* 生成结构体头文件
* 支持从 Android 内存 dump 的 `libil2cpp.so`，可用于绕过部分保护
* 支持绕过简单的 PE 保护
* 提供 Windows 和 macOS GUI，由 ChanGu54 添加并维护

## 使用说明

需要 [.NET 8.0 运行时](https://dotnet.microsoft.com/download)。

不带参数运行 `Il2CppDumper` 会打开 GUI（Windows 和 macOS）。选择 il2cpp 可执行文件、`global-metadata.dat` 和输出目录，然后点击 Dump。

也可以把文件拖放到窗口中。

dump 完成后，日志会显示输出路径，并自动打开输出目录。输出文件会写入所选目录（默认为程序所在目录）。

### 命令行

```
Il2CppDumper.exe <executable-file> <global-metadata> <output-directory>
```

### 输出文件

#### dump.cs

包含所有类型、方法、字段和属性的文本 dump，并附带偏移和 token。

具体输出内容由 `config.json` 中的 `Dump*` 选项控制。

#### DummyDll

包含所有还原 DLL 的文件夹。

可用 [dnSpy](https://github.com/0xd4d/dnSpy)、[ILSpy](https://github.com/icsharpcode/ILSpy) 或其他 .NET 反编译工具查看。

可用于提取 Unity 的 `MonoBehaviour` 和 `MonoScript`，适用于 [UtinyRipper](https://github.com/mafaca/UtinyRipper)、[UABE](https://7daystodie.com/forums/showthread.php?22675-Unity-Assets-Bundle-Extractor) 等工具。

#### il2cpp.h

包含结构体定义的头文件。

#### script.json

供 `ida.py`、`ghidra.py`、`hopper-py3.py` 和 Il2CppBinaryNinja 使用。

#### stringliteral.json

包含所有 stringLiteral 信息。

`il2cpp.h`、`script.json` 和 `stringliteral.json` 仅在 `GenerateStruct` 为 `true` 时生成。

### 反汇编器脚本

以下文件不是 dump 生成的。它们随 `Il2CppDumper.exe` 一起发布，读取 `script.json` 和 `il2cpp.h`。

#### ida.py / ida_py3.py

用于 IDA。`ida_py3.py` 是 Python 3 版本。

#### ida_with_struct.py / ida_with_struct_py3.py

用于 IDA。读取 `il2cpp.h` 并应用结构体信息。`ida_with_struct_py3.py` 是 Python 3 版本。

#### ghidra.py

用于 Ghidra。

#### ghidra_with_struct.py

用于 Ghidra。应用结构体信息，需要先导入 `il2cpp.h`（参见 `il2cpp_header_to_ghidra.py`）。

#### ghidra_wasm.py

用于 Ghidra，配合 [ghidra-wasm-plugin](https://github.com/nneonneo/ghidra-wasm-plugin) 使用。

#### il2cpp_header_to_ghidra.py

将 `il2cpp.h` 转换成 Ghidra 的 C 解析器能接受的形式。

#### hopper-py3.py

用于 Hopper（Python 3）。

#### Il2CppBinaryNinja

用于 Binary Ninja。

#### il2cpp_header_to_binja.py

将 `il2cpp.h` 转换成 Binary Ninja 的类型解析器能接受的形式。

### 配置

所有选项都在 `config.json` 中。

可用选项：

* `DumpMethod`、`DumpField`、`DumpProperty`、`DumpAttribute`、`DumpFieldOffset`、`DumpMethodOffset`、`DumpTypeDefIndex`
  * 是否在 `dump.cs` 中输出相应内容

* `GenerateDummyDll`、`GenerateStruct`
  * 是否生成这些输出。`GenerateStruct` 控制 `il2cpp.h`、`script.json` 和 `stringliteral.json`

* `DummyDllAddToken`
  * 是否在 DummyDll 中添加 token

* `RequireAnyKey`
  * 结束时是否等待按键退出

* `ForceIl2CppVersion`、`ForceVersion`
  * 当 `ForceIl2CppVersion` 为 `true` 时，程序会按 `ForceVersion` 指定的版本选择 il2cpp 二进制解析器，不影响 metadata 解析器的选择。在部分较旧的 il2cpp 上可能会用到。例如 Android 上的 il2cpp v20 二进制可能需要使用 v16 解析器才能正常工作。

* `ForceDump`
  * 强制将文件视为内存 dump

* `NoRedirectedPointer`
  * 将 dump 文件中的指针视为未重定向。从某些设备 dump 出的文件需要将该项设为 `true`

* `DisablePlusSearch`
  * 跳过 `PlusSearch`，不进行可能在部分 dump 的 `GameAssembly.dll`（多为 Unity 2021.3）上崩溃的版本自动校正。找不到地址时回退到手动输入。

## 编译

```
dotnet build Il2CppDumper.sln
```

目标框架为 `net8.0`，唯一依赖是 `Mono.Cecil`。

## 常见问题

#### `ERROR: Metadata file supplied is not valid metadata file.`

请确认选择了正确的文件。有些游戏会出于内容保护等目的对该文件进行混淆。此类文件的去混淆不在本程序范围内，请**不要**就此提交 issue。

如果你的文件是 `libil2cpp.so`，并且拥有已 root 的 Android 设备，可以尝试 [Zygisk-Il2CppDumper](https://github.com/Perfare/Zygisk-Il2CppDumper)，它可能绕过该保护。

#### `ERROR: Metadata file supplied is not a supported version[x].`

metadata 版本超出支持范围（16–108）。该文件来自比当前版本所支持的更新的 Unity，或已被混淆。

#### `ERROR: Can't use auto mode to process file, try manual mode.`

请注意，PC 平台的可执行文件是 `GameAssembly.dll` 或 `*Assembly.dll`。

你可以打开一个新的 issue，并上传文件。

#### `ERROR: This file may be protected.`

Il2CppDumper 检测到可执行文件已被保护。使用 `GameGuardian` 从游戏内存中 dump `libil2cpp.so`，再用 Il2CppDumper 载入并按提示操作，可绕过大部分保护。

## 感谢

- Perfare - [Il2CppDumper](https://github.com/Perfare/Il2CppDumper)
- Jumboperson - [Il2CppDumper](https://github.com/Jumboperson/Il2CppDumper)
