namespace Il2CppDumper
{
    public interface IDumperHost
    {
        void Write(string value);
        void WriteLine(string value = "");
        string ReadLine();
        char ReadKey();
    }
}
