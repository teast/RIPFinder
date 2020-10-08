using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace RIPFinder.Dialogs
{
    public class MessageBox : Window
    {
        public MessageBox()
        {
        }

        public MessageBox(string message, string title = "", MessageBoxButton buttons = MessageBoxButton.OK)
        {
            this.Title = title;

            InitializeComponent();

            this.FindControl<TextBlock>("TextBlock_Message").Text = message;
            var ok = this.FindControl<Button>("Button_OK");
            var cancel = this.FindControl<Button>("Button_Cancel");

            cancel.IsVisible = buttons == MessageBoxButton.OKCancel;
            cancel.Click += delegate
            {
                Close(MessageBoxResult.Cancel);
            };

            ok.Click += delegate
            {
                Close(MessageBoxResult.OK);
            };
        }

        public static void Show(string message)
        {
            // TODO
            //Console.WriteLine(message);
            var mainWindow = (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            var msg = new MessageBox(message);
            msg.ShowDialog(mainWindow);
        }

        public static async Task<MessageBoxResult> Show(string message, string title, MessageBoxButton buttons)
        {
            // TODO
            //Console.WriteLine(message);
            var mainWindow = (App.Current.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
            var msg = new MessageBox(message, title, buttons);
            return await msg.ShowDialog<MessageBoxResult>(mainWindow);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }

    public enum MessageBoxButton
    {
        OK,
        OKCancel
    }

    public enum MessageBoxResult
    {
        OK,
        Cancel
    }
}