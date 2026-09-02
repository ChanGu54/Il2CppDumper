# Il2CppDumper

维护者：[ChanGu54](https://github.com/ChanGu54)

English: [README.md](README.md)

한국어 설명은 [여기](README.ko.md)

Unity il2cpp逆向工程

## 功能

* 还原DLL文件（不包含代码），可用于提取`MonoBehaviour`和`MonoScript`
* 支持ELF, ELF64, Mach-O（含Fat Mach-O）, PE, NSO和WASM格式
* 支持Unity 5.3 - Unity 6，metadata版本 16 - 108（24.x / 27.x / 29.x / 106.1 等细分布局会自动识别）
* 生成IDA, Ghidra, Binary Ninja和Hopper的脚本，帮助它们更好的分析il2cpp文件
* 生成结构体头文件
* 支持从内存dump的`libil2cpp.so`文件以绕过保护
* 支持绕过简单的PE保护

## 使用说明

需要[.NET 8.0运行时](https://dotnet.microsoft.com/download)。

直接运行Il2CppDumper.exe并依次选择il2cpp的可执行文件和global-metadata.dat文件，然后根据提示输入相应信息。

程序运行完成后将在当前运行目录下生成输出文件

### 命令行

```
Il2CppDumper.exe <executable-file> <global-metadata> <output-directory>
```

### 输出文件

#### dump.cs

包含所有类型、方法、字段和属性的文本dump，附带偏移和token

具体输出的内容由`config.json`中的`Dump*`选项控制

#### DummyDll

文件夹，包含所有还原的DLL文件

使用[dnSpy](https://github.com/0xd4d/dnSpy)，[ILSpy](https://github.com/icsharpcode/ILSpy)或者其他.Net反编译工具即可查看具体信息

可用于提取Unity的`MonoBehaviour`和`MonoScript`，适用于[UtinyRipper](https://github.com/mafaca/UtinyRipper)或者[UABE](https://7daystodie.com/forums/showthread.php?22675-Unity-Assets-Bundle-Extractor)等

#### il2cpp.h

包含结构体的头文件

#### script.json

用于ida.py, ghidra.py, hopper-py3.py和Il2CppBinaryNinja

#### stringliteral.json

包含所有stringLiteral信息

`il2cpp.h`, `script.json`和`stringliteral.json`仅在`GenerateStruct`为`true`时生成

### 反汇编器脚本

以下文件不是dump生成的，它们随`Il2CppDumper.exe`一起发布，输入是`script.json`和`il2cpp.h`

#### ida.py / ida_py3.py

用于IDA，`ida_py3.py`是Python 3版本

#### ida_with_struct.py / ida_with_struct_py3.py

用于IDA，读取il2cpp.h文件并在IDA中应用结构信息，`ida_with_struct_py3.py`是Python 3版本

#### ghidra.py

用于Ghidra

#### ghidra_with_struct.py

用于Ghidra，应用结构信息，需要先导入il2cpp.h（参见`il2cpp_header_to_ghidra.py`）

#### ghidra_wasm.py

用于Ghidra, 和[ghidra-wasm-plugin](https://github.com/nneonneo/ghidra-wasm-plugin)一起工作

#### il2cpp_header_to_ghidra.py

把`il2cpp.h`转换成Ghidra的C解析器能接受的形式

#### hopper-py3.py

用于Hopper，Python 3

#### Il2CppBinaryNinja

用于BinaryNinja

#### il2cpp_header_to_binja.py

把`il2cpp.h`转换成Binary Ninja的类型解析器能接受的形式

### 关于config.json

* `DumpMethod`，`DumpField`，`DumpProperty`，`DumpAttribute`，`DumpFieldOffset`, `DumpMethodOffset`, `DumpTypeDefIndex`
  * 是否在dump.cs输出相应的内容

* `GenerateDummyDll`，`GenerateStruct`
  * 是否生成这些内容，`GenerateStruct`控制`il2cpp.h`, `script.json`和`stringliteral.json`

* `DummyDllAddToken`
  * 是否在DummyDll中添加token

* `RequireAnyKey`
  * 在程序结束时是否需要按键退出

* `ForceIl2CppVersion`，`ForceVersion`  
  * 当ForceIl2CppVersion为`true`时，程序将根据ForceVersion指定的版本读取il2cpp的可执行文件（Metadata仍然使用header里的版本），在部分低版本的il2cpp中可能会用到（比如安卓20版本下，你可能需要设置ForceVersion为16程序才能正常工作）

* `ForceDump`
  * 强制将文件视为dump文件

* `NoRedirectedPointer`
  * 将dump文件中的指针视为未重定向的, 从某些设备dump出的文件需要设置该项为`true`

* `DisablePlusSearch`
  * 跳过`PlusSearch`，不进行可能在部分dump的GameAssembly.dll（多为Unity 2021.3）上崩溃的版本自动校正。找不到地址时回退到手动输入

## 编译

```
dotnet build Il2CppDumper.sln
```

目标框架为`net8.0`，唯一的依赖是`Mono.Cecil`

## 常见问题

#### `ERROR: Metadata file supplied is not valid metadata file.`

global-metadata.dat已被加密。关于解密的问题请去相关破解论坛寻求帮助，请不要在issues提问！

如果你的文件是`libil2cpp.so`并且你拥有一台已root的安卓手机，你可以尝试[Zygisk-Il2CppDumper](https://github.com/Perfare/Zygisk-Il2CppDumper)，它能够无视global-metadata.dat加密

#### `ERROR: Metadata file supplied is not a supported version[x].`

metadata版本超出支持范围（16 - 108），该文件来自比当前版本支持的更新的Unity，或者已被加密

#### `ERROR: Can't use auto mode to process file, try manual mode.`

请注意PC平台的可执行文件是`GameAssembly.dll`或者`*Assembly.dll`

你可以打开一个新的issue，并上传文件

#### `ERROR: This file may be protected.`

Il2CppDumper检测到可执行文件已被保护，使用`GameGuardian`从游戏内存中dump `libil2cpp.so`，然后使用Il2CppDumper载入按提示操作，可绕过大部分保护

## 感谢

- Perfare - [Il2CppDumper](https://github.com/Perfare/Il2CppDumper)
- Jumboperson - [Il2CppDumper](https://github.com/Jumboperson/Il2CppDumper)
