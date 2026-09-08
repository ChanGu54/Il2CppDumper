using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace Il2CppDumper
{
    public static class DumperEngine
    {
        public static bool Init(string il2cppPath, string metadataPath, Config config, IDumperHost host, out Metadata metadata, out Il2Cpp il2Cpp)
        {
            host.WriteLine("Initializing metadata...");
            var metadataBytes = File.ReadAllBytes(metadataPath);
            metadata = new Metadata(new MemoryStream(metadataBytes));
            host.WriteLine($"Metadata Version: {metadata.Version}");

            host.WriteLine("Initializing il2cpp file...");
            var il2cppBytes = File.ReadAllBytes(il2cppPath);
            var il2cppMagic = BitConverter.ToUInt32(il2cppBytes, 0);
            var il2CppMemory = new MemoryStream(il2cppBytes);
            switch (il2cppMagic)
            {
                default:
                    throw new NotSupportedException("ERROR: il2cpp file not supported.");
                case 0x6D736100:
                    var web = new WebAssembly(il2CppMemory);
                    il2Cpp = web.CreateMemory();
                    break;
                case 0x304F534E:
                    var nso = new NSO(il2CppMemory);
                    il2Cpp = nso.UnCompress();
                    break;
                case 0x905A4D: //PE
                    il2Cpp = new PE(il2CppMemory);
                    break;
                case 0x464c457f: //ELF
                    if (il2cppBytes[4] == 2) //ELF64
                    {
                        il2Cpp = new Elf64(il2CppMemory);
                    }
                    else
                    {
                        il2Cpp = new Elf(il2CppMemory);
                    }
                    break;
                case 0xCAFEBABE: //FAT Mach-O
                case 0xBEBAFECA:
                    var machofat = new MachoFat(new MemoryStream(il2cppBytes));
                    host.Write("Select Platform: ");
                    for (var i = 0; i < machofat.fats.Length; i++)
                    {
                        var fat = machofat.fats[i];
                        host.Write(fat.magic == 0xFEEDFACF ? $"{i + 1}.64bit " : $"{i + 1}.32bit ");
                    }
                    host.WriteLine();
                    var key = host.ReadKey();
                    var index = int.Parse(key.ToString()) - 1;
                    var magic = machofat.fats[index % 2].magic;
                    il2cppBytes = machofat.GetMacho(index % 2);
                    il2CppMemory = new MemoryStream(il2cppBytes);
                    if (magic == 0xFEEDFACF)
                        goto case 0xFEEDFACF;
                    else
                        goto case 0xFEEDFACE;
                case 0xFEEDFACF: // 64bit Mach-O
                    il2Cpp = new Macho64(il2CppMemory);
                    break;
                case 0xFEEDFACE: // 32bit Mach-O
                    il2Cpp = new Macho(il2CppMemory);
                    break;
            }
            var version = config.ForceIl2CppVersion ? config.ForceVersion : metadata.Version;
            il2Cpp.SetProperties(version, metadata.metadataUsagesCount, metadata);
            host.WriteLine($"Il2Cpp Version: {il2Cpp.Version}");
            if (config.ForceDump || il2Cpp.CheckDump())
            {
                if (il2Cpp is ElfBase elf)
                {
                    host.WriteLine("Detected this may be a dump file.");
                    host.WriteLine("Input il2cpp dump address or input 0 to force continue:");
                    var DumpAddr = Convert.ToUInt64(host.ReadLine(), 16);
                    if (DumpAddr != 0)
                    {
                        il2Cpp.ImageBase = DumpAddr;
                        il2Cpp.IsDumped = true;
                        if (!config.NoRedirectedPointer)
                        {
                            elf.Reload();
                        }
                    }
                }
                else
                {
                    il2Cpp.IsDumped = true;
                }
            }

            host.WriteLine("Searching...");
            try
            {
                if (config.DisablePlusSearch)
                {
                    host.WriteLine("PlusSearch is disabled...");
                    var sectionHelper = il2Cpp.GetSectionHelper(metadata.methodDefs.Count(), metadata.typeDefs.Length, metadata.imageDefs.Length);
                    var codeRegistration = sectionHelper.FindCodeRegistration();
                    var metadataRegistration = sectionHelper.FindMetadataRegistration();
                    if (codeRegistration != 0 && metadataRegistration != 0)
                    {
                        host.WriteLine($"Code Registration:{codeRegistration:X}\nMetadata Registration:{metadataRegistration:X}");
                        il2Cpp.Init(codeRegistration, metadataRegistration);
                    }
                    else
                    {
                        host.WriteLine("ERROR: Can't use auto mode to process file, try manual mode.");
                        host.Write("Input CodeRegistration: ");
                        codeRegistration = Convert.ToUInt64(host.ReadLine(), 16);
                        host.Write("Input MetadataRegistration: ");
                        metadataRegistration = Convert.ToUInt64(host.ReadLine(), 16);
                        il2Cpp.Init(codeRegistration, metadataRegistration);
                    }
                }
                else
                {
                    var flag = il2Cpp.PlusSearch(metadata.methodDefs.Count(x => x.methodIndex >= 0), metadata.typeDefs.Length, metadata.imageDefs.Length);
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        if (!flag && il2Cpp is PE)
                        {
                            host.WriteLine("Use custom PE loader");
                            il2Cpp = PELoader.Load(il2cppPath);
                            il2Cpp.SetProperties(version, metadata.metadataUsagesCount, metadata);
                            flag = il2Cpp.PlusSearch(metadata.methodDefs.Count(x => x.methodIndex >= 0), metadata.typeDefs.Length, metadata.imageDefs.Length);
                        }
                    }
                    if (!flag)
                    {
                        flag = il2Cpp.Search();
                    }
                    if (!flag)
                    {
                        flag = il2Cpp.SymbolSearch();
                    }
                    if (!flag)
                    {
                        host.WriteLine("ERROR: Can't use auto mode to process file, try manual mode.");
                        host.Write("Input CodeRegistration: ");
                        var codeRegistration = Convert.ToUInt64(host.ReadLine(), 16);
                        host.Write("Input MetadataRegistration: ");
                        var metadataRegistration = Convert.ToUInt64(host.ReadLine(), 16);
                        il2Cpp.Init(codeRegistration, metadataRegistration);
                    }
                }
                if (il2Cpp.Version >= 27 && il2Cpp.IsDumped)
                {
                    var typeDef = metadata.typeDefs[0];
                    var il2CppType = il2Cpp.types[typeDef.byvalTypeIndex];
                    metadata.ImageBase = il2CppType.data.typeHandle - metadata.GetTypeDefinitionsOffset();
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                host.WriteLine(e.ToString());
                host.WriteLine("ERROR: An error occurred while processing.");
                return false;
            }
            return true;
        }

        public static void Dump(Metadata metadata, Il2Cpp il2Cpp, string outputDir, Config config, IDumperHost host)
        {
            host.WriteLine("Dumping...");
            var executor = new Il2CppExecutor(metadata, il2Cpp);
            var decompiler = new Il2CppDecompiler(executor);
            decompiler.Decompile(config, outputDir);
            host.WriteLine("Done!");
            if (config.GenerateStruct)
            {
                host.WriteLine("Generate struct...");
                try
                {
                    var scriptGenerator = new StructGenerator(executor);
                    scriptGenerator.WriteScript(outputDir);
                    host.WriteLine("Done!");
                }
                catch (Exception e)
                {
                    host.WriteLine(e.ToString());
                    host.WriteLine("ERROR: Some errors in generating struct");
                }
            }
            if (config.GenerateDummyDll)
            {
                host.WriteLine("Generate dummy dll...");
                try
                {
                    DummyAssemblyExporter.Export(executor, outputDir, config.DummyDllAddToken);
                    host.WriteLine("Done!");
                }
                catch (Exception e)
                {
                    host.WriteLine(e.ToString());
                    host.WriteLine("ERROR: Some errors in generating dummy dll");
                }
            }
        }
    }
}
