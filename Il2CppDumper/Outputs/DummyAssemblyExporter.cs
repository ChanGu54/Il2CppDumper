using System.IO;

namespace Il2CppDumper
{
    public static class DummyAssemblyExporter
    {
        public static void Export(Il2CppExecutor il2CppExecutor, string outputDir, bool addToken)
        {
            var dummyDllDir = Path.Combine(outputDir, "DummyDll");
            if (Directory.Exists(dummyDllDir))
                Directory.Delete(dummyDllDir, true);
            Directory.CreateDirectory(dummyDllDir);
            var dummy = new DummyAssemblyGenerator(il2CppExecutor, addToken);
            foreach (var assembly in dummy.Assemblies)
            {
                using var stream = new MemoryStream();
                assembly.Write(stream);
                File.WriteAllBytes(Path.Combine(dummyDllDir, assembly.MainModule.Name), stream.ToArray());
            }
        }
    }
}
