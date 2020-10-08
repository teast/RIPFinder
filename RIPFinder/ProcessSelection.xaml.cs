using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace RIPFinder
{
    /// <summary>
    /// ProcessSelection.xaml の相互作用ロジック
    /// </summary>
    public partial class ProcessSelection : Window
    {
        public ListBox ListBox_Processes { get; }
        List<ProcessModel> ProcessList { get; set; }
        public Process SelectedProcess { get; set; }
        public ProcessSelection()
        {
            InitializeComponent();
            ListBox_Processes = this.FindControl<ListBox>("ListBox_Processes");

            var pp = Process.GetProcesses();
            var ppp = pp.Select(p => p.MainWindowTitle).ToList();
            var pp2 = pp.Where(p => p.ProcessName == "code").ToList();

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // TODO: .net core do not support fetching window names for given PID
                ProcessList = Process.GetProcesses()
                                .Select(p => new ProcessModel
                                {
                                    Name = p.ProcessName,
                                    Process = p
                                })
                                .OrderBy(p => p.Name)
                                .ToList();
            }
            else
            {
                ProcessList = Process.GetProcesses()
                                .Where(p => p.MainWindowTitle.Length != 0)
                                .Select(p => new ProcessModel
                                {
                                    Name = p.ProcessName,
                                    Process = p
                                })
                                .OrderBy(p => p.Name)
                                .ToList();
            }
            
            ListBox_Processes.DataContext = ProcessList;
        }

        private void Button_OpenThisProcess_Click(object sender, RoutedEventArgs e)
        {
            if (ListBox_Processes.SelectedItems.Count == 1)
            {
                var p = ListBox_Processes.SelectedItems[0] as ProcessModel;
                this.SelectedProcess = p.Process;
                Close(true);
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }

    class ProcessModel
    {
        public string Name { get; set; }
        public Process Process { get; set; }
    }
}
