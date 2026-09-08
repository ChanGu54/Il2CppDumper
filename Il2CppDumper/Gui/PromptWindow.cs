using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Il2CppDumper
{
    public class PromptWindow : Window
    {
        private readonly TextBox inputBox;

        public PromptWindow(string message, bool singleKey)
        {
            Title = "Il2CppDumper";
            Width = 440;
            SizeToContent = SizeToContent.Height;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            CanResize = false;

            inputBox = new TextBox
            {
                Watermark = singleKey ? "1" : "0"
            };
            if (singleKey)
            {
                inputBox.MaxLength = 1;
            }

            var ok = new Button
            {
                Content = "OK",
                MinWidth = 88,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                IsDefault = true
            };
            ok.Click += (_, _) => CloseWith(inputBox.Text);

            var cancel = new Button
            {
                Content = "Cancel",
                MinWidth = 88,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                IsCancel = true
            };
            cancel.Click += (_, _) => CloseWith(null);

            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(16),
                Spacing = 12,
                Children =
                {
                    new TextBlock
                    {
                        Text = string.IsNullOrWhiteSpace(message) ? "Input:" : message,
                        TextWrapping = TextWrapping.Wrap
                    },
                    inputBox,
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Right,
                        Spacing = 8,
                        Children = { cancel, ok }
                    }
                }
            };

            Opened += (_, _) => inputBox.Focus();
        }

        public Task<string> ShowPrompt(Window owner)
        {
            return ShowDialog<string>(owner);
        }

        private void CloseWith(string value)
        {
            Close(value);
        }
    }
}
