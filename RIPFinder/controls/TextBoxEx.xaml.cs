using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace RIPFinder.Controls
{
    public class TextBoxEx : TextBox, IStyleable
    {
        public TextBoxEx()
        {
            InitializeComponent();
        }

        public void AppendText(string text)
        {
            //this.Text += ((this.Text?.Length > 0) ? Environment.NewLine : string.Empty) + text;
            this.Text += text;
        }

        Type IStyleable.StyleKey => typeof(TextBox);

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}