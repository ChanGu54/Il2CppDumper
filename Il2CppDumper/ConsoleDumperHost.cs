using System;

namespace Il2CppDumper
{
    public class ConsoleDumperHost : IDumperHost
    {
        public void Write(string value)
        {
            Console.Write(value);
        }

        public void WriteLine(string value = "")
        {
            Console.WriteLine(value);
        }

        public string ReadLine()
        {
            return Console.ReadLine();
        }

        public char ReadKey()
        {
            return Console.ReadKey(true).KeyChar;
        }
    }
}
