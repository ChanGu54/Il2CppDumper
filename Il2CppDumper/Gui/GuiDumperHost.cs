using System;
using System.Text;
using Avalonia.Threading;

namespace Il2CppDumper
{
    public class GuiDumperHost : IDumperHost
    {
        private readonly MainWindow window;
        private readonly StringBuilder pendingPrompt = new StringBuilder();

        public GuiDumperHost(MainWindow window)
        {
            this.window = window;
        }

        public void Write(string value)
        {
            pendingPrompt.Append(value);
            window.AppendLog(value, newLine: false);
        }

        public void WriteLine(string value = "")
        {
            pendingPrompt.AppendLine(value);
            window.AppendLog(value, newLine: true);
        }

        public string ReadLine()
        {
            return Prompt(singleKey: false);
        }

        public char ReadKey()
        {
            var text = Prompt(singleKey: true);
            if (string.IsNullOrEmpty(text))
            {
                throw new OperationCanceledException();
            }
            return text[0];
        }

        private string Prompt(bool singleKey)
        {
            var message = pendingPrompt.ToString().Trim();
            pendingPrompt.Clear();
            var result = Dispatcher.UIThread.InvokeAsync(() => window.PromptAsync(message, singleKey)).GetAwaiter().GetResult();
            if (result == null)
            {
                throw new OperationCanceledException();
            }
            return result;
        }
    }
}
