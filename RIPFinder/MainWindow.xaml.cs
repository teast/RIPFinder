using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using RIPFinder.Controls;
using RIPFinder.Dialogs;
using RIPFinder.TextCopy;

namespace RIPFinder
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        Process TargetProcess { get; set; }
        IntPtr TargetProcessHandle { get; set; }
        string BinFileName { get; set; }
        public TextBoxEx TextBox_ProcessName { get; }
        public DataGrid DataGrid_RIP { get; }
        public TextBoxEx TextBox_Log { get; }
        public TextBoxEx TextBox_Stack { get; }
        public TextBoxEx TextBox_FilterString { get; }
        public RadioButton RadioButton_Group1_Process { get; }
        public RadioButton RadioButton_Group1_File { get; }
        public TextBoxEx TextBox_BinFileName { get; }
        public Button Button_SelectBinFile { get; }
        public CheckBox CheckBox_AllModules { get; }
        public CheckBox CheckBox_OnlyLEA { get; }
        public TextBoxEx TextBox_LogScan { get; }
        public TextBoxEx TextBox_Signature { get; }
        public TextBoxEx TextBox_Offset1 { get; }
        public Button Button_SelectProcess { get; }
        public Dispatcher Dispatcher { get; }
        public Button Button_SaveDump { get; }
        public Button Button_StartScan { get; }
        public Button Button_ExportResults { get; }
        public Button Button_SignatureScan { get; }
        public TabControl TabControl_Signatures { get; }
        public ComboBox ComboBox_SpecificModule { get; }
        public ComboBox ComboBox_ScanSpecificModule { get; }

        List<RIPEntry> entries = new List<RIPEntry>();
        const int MaxEntries = 1 * 1000 * 1000;

        public MainWindow()
        {
            InitializeComponent();

            Dispatcher = new Dispatcher();
            TextBox_ProcessName = this.FindControl<TextBoxEx>("TextBox_ProcessName");
            DataGrid_RIP = this.FindControl<DataGrid>("DataGrid_RIP");
            TextBox_Log = this.FindControl<TextBoxEx>("TextBox_Log");
            TextBox_Stack = this.FindControl<TextBoxEx>("TextBox_Stack");
            TextBox_FilterString = this.FindControl<TextBoxEx>("TextBox_FilterString");
            RadioButton_Group1_Process = this.FindControl<RadioButton>("RadioButton_Group1_Process");
            RadioButton_Group1_File = this.FindControl<RadioButton>("RadioButton_Group1_File");
            TextBox_BinFileName = this.FindControl<TextBoxEx>("TextBox_BinFileName");
            Button_SelectBinFile = this.FindControl<Button>("Button_SelectBinFile");
            CheckBox_AllModules = this.FindControl<CheckBox>("CheckBox_AllModules");
            CheckBox_OnlyLEA = this.FindControl<CheckBox>("CheckBox_OnlyLEA");
            TextBox_LogScan = this.FindControl<TextBoxEx>("TextBox_LogScan");
            TextBox_Signature = this.FindControl<TextBoxEx>("TextBox_Signature");
            TextBox_Offset1 = this.FindControl<TextBoxEx>("TextBox_Offset1");
            Button_SelectProcess = this.FindControl<Button>("Button_SelectProcess");
            Button_SaveDump = this.FindControl<Button>("Button_SaveDump");
            Button_StartScan = this.FindControl<Button>("Button_StartScan");
            Button_ExportResults = this.FindControl<Button>("Button_ExportResults");
            Button_SignatureScan = this.FindControl<Button>("Button_SignatureScan");
            TabControl_Signatures = this.FindControl<TabControl>("TabControl_Signatures");
            ComboBox_SpecificModule = this.FindControl<ComboBox>("ComboBox_SpecificModule");
            ComboBox_ScanSpecificModule = this.FindControl<ComboBox>("ComboBox_ScanSpecificModule");

            Button_SelectProcess.Click += Button_SelectProcess_Click;
            Button_SaveDump.Click += Button_SaveDump_Click;
            Button_ExportResults.Click += Button_ExportResults_Click;
            Button_StartScan.Click += Button_StartScan_Click;
            Button_SignatureScan.Click += Button_SignatureScan_Click;
            Button_SelectBinFile.Click += Button_SelectBinFile_Click;

            TextBox_ProcessName.PropertyChanged += (_, e) =>
            {
                if (e.Property.Name == nameof(TextBox.Text))
                    TextBox_ProcessName_TextChanged();
            };
            TextBox_BinFileName.PropertyChanged += (_, e) =>
            {
                if (e.Property.Name == nameof(TextBox.Text))
                    TextBox_BinFileName_TextChanged();
            };
        }

        private async void Button_SelectProcess_Click(object sender, RoutedEventArgs e)
        {
            var window = (Window)this.VisualRoot;
            ProcessSelection processSelection = new ProcessSelection();
            var dialogResult = await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync<bool>(() => processSelection.ShowDialog<bool>(window));
            if (dialogResult != true)
                return;
            if (processSelection.SelectedProcess != null)
            {
                TargetProcess = processSelection.SelectedProcess;
                TargetProcessHandle = Helper.OpenProcess(TargetProcess.Id);
                TextBox_ProcessName.Text = processSelection.SelectedProcess.ProcessName;
                DataGrid_RIP.DataContext = null;
                TextBox_Log.Clear();

                TextBox_Stack.Clear();
                List<ProcessModule> modules = new List<ProcessModule>();
                var mainModule = TargetProcess.MainModule;
                foreach (ProcessModule m in TargetProcess.Modules)
                {
                    modules.Add(m);
                }

                modules.Sort((a, b) =>
                {
                    if (a.ModuleName == mainModule.ModuleName) return -1;
                    if (b.ModuleName == mainModule.ModuleName) return 1;
                    return (int)(a.BaseAddress.ToInt64() - b.BaseAddress.ToInt64());
                });

                ComboBox_SpecificModule.Items = modules;
                ComboBox_ScanSpecificModule.Items = modules;
                foreach (ProcessModule m in modules)
                {
                    int moduleMemorySize = m.ModuleMemorySize;
                    IntPtr startAddres = m.BaseAddress;
                    IntPtr endAddres = new IntPtr(m.BaseAddress.ToInt64() + moduleMemorySize);

                    TextBox_Stack.AppendText($"Module Name: {m.ModuleName}" + Environment.NewLine);
                    TextBox_Stack.AppendText($"File Name: {m.FileName}" + Environment.NewLine);
                    TextBox_Stack.AppendText($"Module Size: {moduleMemorySize.ToString("#,0")} Byte" + Environment.NewLine);
                    TextBox_Stack.AppendText($"Start Address: {startAddres.ToInt64().ToString("X2")} ({startAddres.ToInt64()})" + Environment.NewLine);
                    TextBox_Stack.AppendText($"End Address  : {endAddres.ToInt64().ToString("X2")} ({endAddres.ToInt64()})" + Environment.NewLine);
                    TextBox_Stack.AppendText($"---------------------------------------------------------------" + Environment.NewLine);
                }

            }
        }


        private async void Button_StartScan_Click(object sender, RoutedEventArgs e)
        {
            DataGrid_RIP.DataContext = null;
            entries = new List<RIPEntry>();
            TextBox_Log.Clear();
            GC.Collect();

            if (string.IsNullOrWhiteSpace(TextBox_FilterString.Text))
            {
                var result = await MessageBox.Show("Requires huge memories to run without filter.\n If results are more than 1M, snip them.\n Procced?", "Caution", MessageBoxButton.OKCancel);
                if (result != MessageBoxResult.OK)
                {
                    return;
                }
            }


            if (this.RadioButton_Group1_Process.IsChecked == true)
            {
                if (TargetProcess == null) { return; }

                //List<RIPEntry> entries = new List<RIPEntry>();
                Stopwatch stopwatch = new Stopwatch();
                SetUIEnabled(false);

                List<string> filters = ParseFilter();
                foreach (var f in filters)
                {
                    TextBox_Log.AppendText($"filter: {f}" + Environment.NewLine);
                }

                bool searchFromAllModules = CheckBox_AllModules.IsChecked ?? false;
                bool onlyLEA = CheckBox_OnlyLEA.IsChecked ?? false;

                List<ProcessModule> ProcessModules = new List<ProcessModule>();

                if (searchFromAllModules)
                {
                    foreach (ProcessModule m in TargetProcess.Modules)
                    {
                        ProcessModules.Add(m);
                    }

                }
                else
                {
                    if (ComboBox_SpecificModule.SelectedItem != null)
                        ProcessModules.Add((ProcessModule)ComboBox_SpecificModule.SelectedItem);
                    else
                        ProcessModules.Add(TargetProcess.MainModule);
                }


                var task = Task.Run(() =>
                {
                    foreach (var m in ProcessModules)
                    {
                        int moduleMemorySize = m.ModuleMemorySize;
                        IntPtr startAddres = m.BaseAddress;
                        IntPtr endAddres = new IntPtr(m.BaseAddress.ToInt64() + moduleMemorySize);

                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            TextBox_Log.AppendText($"----------------------------------------------------" + Environment.NewLine);

                            TextBox_Log.AppendText($"Module Name: {m.ModuleName}" + Environment.NewLine);
                            TextBox_Log.AppendText($"File Name: {m.FileName}" + Environment.NewLine);
                            TextBox_Log.AppendText($"Module Size: {moduleMemorySize.ToString("#,0")} Byte" + Environment.NewLine);
                            TextBox_Log.AppendText($"Start Address: {startAddres.ToInt64().ToString("X2")} ({startAddres.ToInt64()})" + Environment.NewLine);
                            TextBox_Log.AppendText($"End Address  : {endAddres.ToInt64().ToString("X2")} ({endAddres.ToInt64()})" + Environment.NewLine);
                            TextBox_Log.AppendText($"Scan started. Please wait..." + "  ");

                        }));

                        stopwatch.Start();

                        IntPtr currentAddress = startAddres;
                        int bufferSize = 1 * 1024 * 1024;
                        byte[] buffer = new byte[bufferSize];

                        while (currentAddress.ToInt64() < endAddres.ToInt64())
                        {
                            // size
                            IntPtr nSize = new IntPtr(bufferSize);

                            // if remaining memory size is less than splitSize, change nSize to remaining size
                            if (IntPtr.Add(currentAddress, bufferSize).ToInt64() > endAddres.ToInt64())
                            {
                                nSize = (IntPtr)(endAddres.ToInt64() - currentAddress.ToInt64());
                            }

                            IntPtr numberOfBytesRead = IntPtr.Zero;
                            if (Helper.ReadProcessMemory(TargetProcessHandle, currentAddress, buffer, nSize, ref numberOfBytesRead))
                            {

                                for (int i = 0; i < numberOfBytesRead.ToInt64() - 4; i++)
                                {
                                    var entry = new RIPEntry();
                                    entry.Address = new IntPtr(currentAddress.ToInt64() + i);
                                    entry.AddressValueInt64 = BitConverter.ToInt32(buffer, i);
                                    entry.TargetAddress = new IntPtr(entry.Address.ToInt64() + entry.AddressValueInt64 + 4);


                                    if (entry.TargetAddress.ToInt64() < startAddres.ToInt64() || entry.TargetAddress.ToInt64() > endAddres.ToInt64())
                                    {
                                        continue;
                                    }

                                    var offsetString1 = (entry.Address.ToInt64() - startAddres.ToInt64()).ToString("X");
                                    if (offsetString1.Length % 2 == 1) offsetString1 = "0" + offsetString1;
                                    entry.AddressRelativeString = '"' + m.ModuleName + '"' + "+" + offsetString1;

                                    var offsetString2 = (entry.TargetAddress.ToInt64() - startAddres.ToInt64()).ToString("X");
                                    if (offsetString2.Length % 2 == 1) offsetString2 = "0" + offsetString2;
                                    entry.TargetAddressRelativeString = '"' + m.ModuleName + '"' + "+" + offsetString2;

                                    if (filters.Any() &&
                                    !filters.Any(x => x == entry.TargetAddressString) &&
                                    !filters.Any(x => x == entry.TargetAddressRelativeString))
                                    {
                                        continue;
                                    }

                                    // Signature
                                    int bufferSize2 = 64;
                                    byte[] buffer2 = new byte[bufferSize2];
                                    IntPtr nSize2 = new IntPtr(bufferSize2);
                                    IntPtr numberOfBytesRead2 = IntPtr.Zero;
                                    if (Helper.ReadProcessMemory(TargetProcessHandle, new IntPtr(entry.Address.ToInt64() - bufferSize2), buffer2, nSize2, ref numberOfBytesRead2))
                                    {
                                        if (numberOfBytesRead2.ToInt64() == bufferSize2)
                                        {
                                            if (onlyLEA && (buffer2.Length < 2 || buffer2[buffer2.Length - 2] != 0x8D))
                                            {
                                                continue;
                                            }
                                            entry.Signature = BitConverter.ToString(buffer2).Replace("-", "");
                                        }
                                    }

                                    if (entries.Count < MaxEntries)
                                    {
                                        entries.Add(entry);
                                    }
                                }
                            }
                            if ((currentAddress.ToInt64() + numberOfBytesRead.ToInt64()) == endAddres.ToInt64())
                            {
                                currentAddress = new IntPtr(currentAddress.ToInt64() + numberOfBytesRead.ToInt64());
                            }
                            else
                            {
                                currentAddress = new IntPtr(currentAddress.ToInt64() + numberOfBytesRead.ToInt64() - 4);
                            }
                        }

                        stopwatch.Stop();

                        this.Dispatcher.Invoke((Action)(() =>
                        {
                            TextBox_Log.AppendText($"Complete." + Environment.NewLine);
                            TextBox_Log.AppendText($"Result Count: {entries.Count.ToString("#,0")}" + Environment.NewLine);
                            TextBox_Log.AppendText($"Scan Time: {stopwatch.ElapsedMilliseconds}ms" + Environment.NewLine);
                        }));
                    }

                });

                await task;
                DataGrid_RIP.Items = entries;
                SetUIEnabled(true);
            }
            else if (this.RadioButton_Group1_File.IsChecked == true)
            {
                if (string.IsNullOrWhiteSpace(BinFileName)) { return; }
                Stopwatch stopwatch = new Stopwatch();

                SetUIEnabled(false);

                List<string> filters = ParseFilter();
                foreach (var f in filters)
                {
                    TextBox_Log.AppendText($"filter: {f}" + Environment.NewLine);
                }


                var task = Task.Run(() =>
                {
                    System.IO.FileStream fs = new System.IO.FileStream(BinFileName, System.IO.FileMode.Open, System.IO.FileAccess.Read);
                    System.IO.FileStream fs2 = new System.IO.FileStream(BinFileName, System.IO.FileMode.Open, System.IO.FileAccess.Read);

                    long startPosition = 0;
                    long endPosition = fs.Length;
                    long currentPosition = 0;

                    int bufferSize = 8 * 1024 * 1024;
                    byte[] buffer = new byte[bufferSize];

                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        TextBox_Log.AppendText($"----------------------------------------------------" + Environment.NewLine);
                        TextBox_Log.AppendText($"File Name: {BinFileName}" + Environment.NewLine);
                        TextBox_Log.AppendText($"Module Size: {endPosition.ToString("#,0")} Byte" + Environment.NewLine);
                        TextBox_Log.AppendText($"Scan started. Please wait..." + "  ");
                    }));

                    stopwatch.Start();

                    while (currentPosition < endPosition)
                    {
                        int readSize = fs.Read(buffer, 0, bufferSize);

                        for (int i = 0; i < readSize - 4; i++)
                        {
                            var entry = new RIPEntry();
                            entry.Address = new IntPtr(currentPosition + i);
                            entry.AddressValueInt64 = BitConverter.ToInt32(buffer, i);
                            entry.TargetAddress = new IntPtr(entry.Address.ToInt64() + entry.AddressValueInt64 + 4);

                            if (entry.TargetAddress.ToInt64() < startPosition || entry.TargetAddress.ToInt64() > endPosition)
                            {
                                continue;
                            }

                            var offsetString1 = (entry.Address.ToInt64() - startPosition).ToString("X");
                            if (offsetString1.Length % 2 == 1) offsetString1 = "0" + offsetString1;
                            entry.AddressRelativeString = '"' + System.IO.Path.GetFileName(BinFileName) + '"' + "+" + offsetString1;

                            var offsetString2 = (entry.TargetAddress.ToInt64() - startPosition).ToString("X");
                            if (offsetString2.Length % 2 == 1) offsetString2 = "0" + offsetString2;
                            entry.TargetAddressRelativeString = '"' + System.IO.Path.GetFileName(BinFileName) + '"' + "+" + offsetString2;

                            if (filters.Any() &&
                            !filters.Any(x => x == entry.TargetAddressString) &&
                            !filters.Any(x => x == entry.TargetAddressRelativeString))
                            {
                                continue;
                            }

                            // Signature

                            int bufferSize2 = 64;
                            byte[] buffer2 = new byte[bufferSize2];
                            int offset2 = entry.Address.ToInt32() - bufferSize2;
                            if (offset2 >= 0 && offset2 + bufferSize2 <= endPosition)
                            {
                                fs2.Seek(offset2, System.IO.SeekOrigin.Begin);
                                var readBytes = fs2.Read(buffer2, 0, bufferSize2);
                                if (readBytes == bufferSize2)
                                {
                                    entry.Signature = BitConverter.ToString(buffer2).Replace("-", "");
                                }

                            }

                            if (entries.Count < MaxEntries)
                            {
                                entries.Add(entry);
                            }
                        }

                        if (readSize < bufferSize)
                        {
                            currentPosition += readSize;
                        }
                        else
                        {
                            currentPosition += (readSize - 4);
                            fs.Seek(-4, System.IO.SeekOrigin.Current);
                        }

                    }

                    fs.Close();
                    fs2.Close();

                    stopwatch.Stop();

                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        TextBox_Log.AppendText($"Complete." + Environment.NewLine);
                        TextBox_Log.AppendText($"Result Count: {entries.Count.ToString("#,0")}" + Environment.NewLine);
                        TextBox_Log.AppendText($"Scan Time: {stopwatch.ElapsedMilliseconds}ms" + Environment.NewLine);
                    }));


                });

                await task;
                DataGrid_RIP.DataContext = entries;

                SetUIEnabled(true);

            }

            GC.Collect();

        }

        private List<string> ParseFilter()
        {
            List<string> filters = new List<string>();
            if (!string.IsNullOrWhiteSpace(TextBox_FilterString.Text))
            {
                var filterStrings = TextBox_FilterString.Text.Replace(Environment.NewLine, ",").Replace("\n", ",").Replace("\r", ",").Split(',');
                foreach (var filterString in filterStrings)
                {
                    var str = filterString.Trim();
                    filters.Add(str);

                    if (Int64.TryParse(str, out Int64 out1))
                    {
                        filters.Add(out1.ToString("X"));
                    }

                    if (Int64.TryParse(str, NumberStyles.HexNumber, new CultureInfo("en-US"), out Int64 out2))
                    {
                        filters.Add(out2.ToString("X"));
                        filters.Add(out2.ToString("X").PadLeft(16, '0'));
                    }
                }
            }

            filters = filters.Distinct().ToList();
            return filters;
        }


        private void DataGrid_RIP_CopyBaseRelativeAddressString(object sender, RoutedEventArgs e)
        {
            RIPEntry entry = this.DataGrid_RIP.SelectedItem as RIPEntry;
            if (entry != null)
            {
                try
                {
                    Clipboard.SetDataObject(entry.AddressRelativeString);
                }
                catch
                { }
            }
        }
        private void DataGrid_RIP_CopyBaseAddressString(object sender, RoutedEventArgs e)
        {
            RIPEntry entry = this.DataGrid_RIP.SelectedItem as RIPEntry;
            if (entry != null)
            {
                try
                {
                    Clipboard.SetDataObject(entry.AddressString);
                }
                catch
                { }
            }
        }
        private void DataGrid_RIP_CopyTargetRelativeAddressString(object sender, RoutedEventArgs e)
        {
            RIPEntry entry = this.DataGrid_RIP.SelectedItem as RIPEntry;
            if (entry != null)
            {
                try
                {
                    Clipboard.SetDataObject(entry.TargetAddressRelativeString);
                }
                catch
                { }
            }
        }
        private void DataGrid_RIP_CopyTargetAddressString(object sender, RoutedEventArgs e)
        {
            RIPEntry entry = this.DataGrid_RIP.SelectedItem as RIPEntry;
            if (entry != null)
            {
                try
                {
                    Clipboard.SetDataObject(entry.TargetAddressString);
                }
                catch
                { }
            }
        }
        private void DataGrid_RIP_CopySignature(object sender, RoutedEventArgs e)
        {
            RIPEntry entry = this.DataGrid_RIP.SelectedItem as RIPEntry;
            if (entry != null)
            {
                try
                {
                    Clipboard.SetDataObject(entry.Signature);
                }
                catch
                { }
            }
        }


        private async void Button_SelectBinFile_Click(object sender, RoutedEventArgs e)
        {
            var window = (Window)this.VisualRoot;
            var dialog = new OpenFileDialog
            {
                AllowMultiple = false,
                Filters = new List<FileDialogFilter>{
                    new FileDialogFilter { Name = "binary file (*.bin)", Extensions = new List<string> { "bin" }},
                    new FileDialogFilter { Name = "all files (*.*)", Extensions = new List<string> { "*" }},
                },
                Title = "Select Bin File"
            };

            var files = await dialog.ShowAsync(window);
            if (files != null && files.Any())
            {
                this.BinFileName = files.First();
                this.TextBox_BinFileName.Text = System.IO.Path.GetFileName(BinFileName);
            }
        }


        private async void Button_SaveDump_Click(object sender, RoutedEventArgs e)
        {
            if (TargetProcess == null) { return; }

            var window = (Window)this.VisualRoot;
            var dialog = new SaveFileDialog
            {
                Filters = new List<FileDialogFilter>{
                    new FileDialogFilter { Name = "binary file (*.bin)", Extensions = new List<string> { "bin" }},
                    new FileDialogFilter { Name = "all files (*.*)", Extensions = new List<string> { "*" }},
                }
            };

            var file = await dialog.ShowAsync(window);

            if (string.IsNullOrEmpty(file))
                return;
            SetUIEnabled(false);

            var task = Task.Run(() =>
            {

                var binFile = file;
                System.IO.FileStream binFs = new System.IO.FileStream(binFile, System.IO.FileMode.Create, System.IO.FileAccess.Write);
                System.IO.StreamWriter txt128Sr = new System.IO.StreamWriter(binFile + ".128.txt", false, System.Text.Encoding.ASCII);

                int moduleMemorySize = TargetProcess.MainModule.ModuleMemorySize;
                IntPtr startAddres = TargetProcess.MainModule.BaseAddress;
                IntPtr endAddres = new IntPtr(TargetProcess.MainModule.BaseAddress.ToInt64() + moduleMemorySize);

                IntPtr currentAddress = startAddres;
                int bufferSize = 1 * 1024 * 1024;
                byte[] buffer = new byte[bufferSize];


                while (currentAddress.ToInt64() < endAddres.ToInt64())
                {
                    // size
                    IntPtr nSize = new IntPtr(bufferSize);

                    // if remaining memory size is less than splitSize, change nSize to remaining size
                    if (IntPtr.Add(currentAddress, bufferSize).ToInt64() > endAddres.ToInt64())
                    {
                        nSize = (IntPtr)(endAddres.ToInt64() - currentAddress.ToInt64());
                    }

                    IntPtr numberOfBytesRead = IntPtr.Zero;
                    if (Helper.ReadProcessMemory(TargetProcessHandle, currentAddress, buffer, nSize, ref numberOfBytesRead))
                    {
                        binFs.Write(buffer, 0, numberOfBytesRead.ToInt32());

                        byte[] b = new byte[numberOfBytesRead.ToInt32()];
                        Array.Copy(buffer, 0, b, 0, numberOfBytesRead.ToInt32());
                        var t = System.Text.RegularExpressions.Regex.Replace(BitConverter.ToString(b).Replace("-", ""), @"(?<=\G.{128})(?!$)", Environment.NewLine);
                        txt128Sr.Write(t);


                    }

                    currentAddress = new IntPtr(currentAddress.ToInt64() + numberOfBytesRead.ToInt64());
                }
                binFs.Close();
                txt128Sr.Close();
            });

            await task;
            MessageBox.Show("Complete.");

            SetUIEnabled(true);
        }

        private async void Button_ExportResults_Click(object sender, RoutedEventArgs e)
        {
            if (this.DataGrid_RIP == null) { return; }
            if (this.DataGrid_RIP.Items == null) { return; }

            var window = (Window)this.VisualRoot;
            var dialog = new SaveFileDialog
            {
                Filters = new List<FileDialogFilter>{
                    new FileDialogFilter { Name = "binary file (*.bin)", Extensions = new List<string> { "bin" }},
                    new FileDialogFilter { Name = "all files (*.*)", Extensions = new List<string> { "*" }},
                }
            };

            var file = await dialog.ShowAsync(window);

            if (string.IsNullOrEmpty(file))
                return;

            SetUIEnabled(false);

            var task = Task.Run(() =>
            {
                System.IO.StreamWriter sr = new System.IO.StreamWriter(file, false, System.Text.Encoding.ASCII);

                foreach (var item in this.DataGrid_RIP.Items)
                {
                    RIPEntry entry = item as RIPEntry;
                    string csv = "";
                    csv += String.Format("{0, -30}", entry.AddressRelativeString) + " ";
                    csv += String.Format("{0, -16}", entry.AddressString) + " ";
                    csv += String.Format("{0, -30}", entry.TargetAddressRelativeString) + " ";
                    csv += String.Format("{0, -16}", entry.TargetAddressString) + " ";
                    csv += String.Format("{0, -64}", entry.Signature) + Environment.NewLine;
                    sr.Write(csv);
                }

                sr.Close();
            });

            await task;
            MessageBox.Show("Complete.");
            SetUIEnabled(true);
        }

        private async void Button_SignatureScan_Click(object sender, RoutedEventArgs e)
        {
            if (this.RadioButton_Group1_Process.IsChecked == true)
            {
                if (TargetProcess == null) return;
                if (TargetProcess.HasExited) return;
                if (string.IsNullOrWhiteSpace(TextBox_Signature.Text)) return;

                SetUIEnabled(false);
                TextBox_LogScan.Clear();

                if (int.TryParse(TextBox_Offset1.Text, out int offset1) == false)
                {
                    offset1 = 0;
                }

                var module = TargetProcess.MainModule;

                if (ComboBox_ScanSpecificModule.SelectedItem != null)
                    module = (ProcessModule)ComboBox_ScanSpecificModule.SelectedItem;

                var baseAddress = module.BaseAddress;
                var endAddress = new IntPtr(module.BaseAddress.ToInt64() + module.ModuleMemorySize);

                Memory memory = new Memory(TargetProcess);
                var pointers = memory.SigScan(TextBox_Signature.Text.Replace('*', '?'), offset1, true);


                TextBox_LogScan.Text += "FileName= " + module.FileName + Environment.NewLine;
                TextBox_LogScan.Text += "MainModule= " + module.ModuleName + Environment.NewLine;
                TextBox_LogScan.Text += "BaseAddress= " + ((baseAddress.ToInt64().ToString("X").Length % 2 == 1) ? "0" + baseAddress.ToInt64().ToString("X") : baseAddress.ToInt64().ToString("X")) + Environment.NewLine;
                TextBox_LogScan.Text += "ModuleMemorySize= " + module.ModuleMemorySize.ToString() + Environment.NewLine;

                TextBox_LogScan.Text += "Signature= " + TextBox_Signature.Text + Environment.NewLine;
                TextBox_LogScan.Text += "Offset= " + TextBox_Signature.Text + Environment.NewLine;
                TextBox_LogScan.Text += "pointes.Count()= " + pointers.Count() + Environment.NewLine;
                TextBox_LogScan.Text += "Scan started. Please wait..." + Environment.NewLine;
                TextBox_LogScan.Text += "==================" + Environment.NewLine;

                string pString = "";
                var task = Task.Run(() =>
                {
                    foreach (var p in pointers)
                    {
                        var r0 = p[0].ToInt64() - baseAddress.ToInt64();
                        pString += "ptr: \"" + module.ModuleName + "\"+" +
                            ((r0.ToString("X").Length % 2 == 1) ? "0" + r0.ToString("X") : r0.ToString("X")) + " (" +
                            ((p[0].ToInt64().ToString("X").Length % 2 == 1) ? "0" + p[0].ToInt64().ToString("X") : p[0].ToInt64().ToString("X")) + ")";

                        if (p[1].ToInt64() >= baseAddress.ToInt64() && p[1].ToInt64() <= endAddress.ToInt64())
                        {
                            var r1 = p[1].ToInt64() - baseAddress.ToInt64();
                            pString += " -> \"" + module.ModuleName + "\"+" +
                                ((r1.ToString("X").Length % 2 == 1) ? "0" + r1.ToString("X") : r1.ToString("X")) + " (" +
                                ((p[1].ToInt64().ToString("X").Length % 2 == 1) ? "0" + p[1].ToInt64().ToString("X") : p[1].ToInt64().ToString("X")) + ")" + Environment.NewLine;
                        }
                        else
                        {
                            pString += " -> " + ((p[1].ToInt64().ToString("X").Length % 2 == 1) ? "0" + p[1].ToInt64().ToString("X") : p[1].ToInt64().ToString("X")) + Environment.NewLine;
                        }
                    }
                });
                await task;
                TextBox_LogScan.Text += pString;
                TextBox_LogScan.Text += "==================" + Environment.NewLine;
                TextBox_LogScan.Text += "Scan complete." + Environment.NewLine;
                SetUIEnabled(true);
            }
            else if (this.RadioButton_Group1_File.IsChecked == true)
            {
                if (string.IsNullOrWhiteSpace(BinFileName)) return;
                if (string.IsNullOrWhiteSpace(TextBox_Signature.Text)) return;

                SetUIEnabled(false);
                TextBox_LogScan.Clear();

                if (int.TryParse(TextBox_Offset1.Text, out int offset1) == false)
                {
                    offset1 = 0;
                }

                BinFile binFile = new BinFile(BinFileName);
                string fName = System.IO.Path.GetFileName(BinFileName);
                long fSize = new System.IO.FileInfo(BinFileName).Length;
                var pointers = binFile.SigScan(TextBox_Signature.Text.Replace('*', '?'), offset1, true);
                Console.WriteLine(pointers.Count());
                string pString = "";
                var task = Task.Run(() =>
                {
                    foreach (var p in pointers)
                    {
                        var r0 = p[0].ToInt64();
                        pString += "p: \"" + fName + "\"+" + ((r0.ToString("X").Length % 2 == 1) ? "0" + r0.ToString("X") : r0.ToString("X"));

                        if (p[1].ToInt64() >= 0 && p[1].ToInt64() <= fSize)
                        {
                            var r1 = p[1].ToInt64();
                            pString += " -> \"" + fName + "\"+" + ((r1.ToString("X").Length % 2 == 1) ? "0" + r1.ToString("X") : r1.ToString("X")) + Environment.NewLine;
                        }
                        else
                        {
                            pString += " -> " + ((p[1].ToInt64().ToString("X").Length % 2 == 1) ? "0" + p[1].ToInt64().ToString("X") : p[1].ToInt64().ToString("X")) + Environment.NewLine;
                        }
                    }
                });
                await task;
                TextBox_LogScan.Text += pString;
                SetUIEnabled(true);
            }
        }

        private void SetUIEnabled(bool isEnabled)
        {
            RadioButton_Group1_Process.IsEnabled = isEnabled;
            RadioButton_Group1_File.IsEnabled = isEnabled;

            if (isEnabled == true)
            {
                if (RadioButton_Group1_Process.IsChecked == true)
                {
                    Button_SelectProcess.IsEnabled = isEnabled;
                    Button_SaveDump.IsEnabled = isEnabled;
                }

                if (RadioButton_Group1_File.IsChecked == true)
                {
                    RadioButton_Group1_File.IsEnabled = isEnabled;
                }

            }
            else
            {
                Button_SelectProcess.IsEnabled = isEnabled;
                Button_SelectBinFile.IsEnabled = isEnabled;
                Button_SaveDump.IsEnabled = isEnabled;
            }

            Button_StartScan.IsEnabled = isEnabled;
            TextBox_FilterString.IsEnabled = isEnabled;
            CheckBox_AllModules.IsEnabled = isEnabled;
            ComboBox_SpecificModule.IsEnabled = isEnabled;
            ComboBox_ScanSpecificModule.IsEnabled = isEnabled;
            Button_ExportResults.IsEnabled = isEnabled;
            TextBox_Signature.IsEnabled = isEnabled;
            TextBox_Offset1.IsEnabled = isEnabled;
            TextBox_LogScan.IsEnabled = isEnabled;
            Button_SignatureScan.IsEnabled = isEnabled;

        }

        private void RadioButton_Group1_Checked(object sender, RoutedEventArgs e)
        {

            var radioButton = (RadioButton)sender;
            if (radioButton.Name == this.RadioButton_Group1_Process.Name)
            {
                this.Button_SelectProcess.IsEnabled = true;
                this.TextBox_ProcessName.Clear();
                this.TargetProcess = null;
                this.TargetProcessHandle = IntPtr.Zero;
                this.BinFileName = null;

                this.Button_SelectBinFile.IsEnabled = false;
                this.TextBox_BinFileName.Clear();
                this.TargetProcess = null;
                this.TargetProcessHandle = IntPtr.Zero;
                this.BinFileName = null;
            }
            else if (radioButton.Name == this.RadioButton_Group1_File.Name)
            {
                this.Button_SelectProcess.IsEnabled = false;
                this.TextBox_ProcessName.Clear();
                this.TargetProcess = null;
                this.TargetProcessHandle = IntPtr.Zero;
                this.BinFileName = null;

                this.Button_SelectBinFile.IsEnabled = true;
                this.TextBox_BinFileName.Clear();
                this.TargetProcess = null;
                this.TargetProcessHandle = IntPtr.Zero;
                this.BinFileName = null;
            }

        }

        private void TextBox_ProcessName_TextChanged()
        {
            if (string.IsNullOrWhiteSpace(TextBox_ProcessName.Text))
            {
                TabControl_Signatures.IsEnabled = false;
                Button_SaveDump.IsEnabled = false;
            }
            else
            {
                TabControl_Signatures.IsEnabled = true;
                Button_SaveDump.IsEnabled = true;
            }
        }

        private void TextBox_BinFileName_TextChanged()
        {
            if (string.IsNullOrWhiteSpace(TextBox_BinFileName.Text))
            {
                TabControl_Signatures.IsEnabled = false;
            }
            else
            {
                TabControl_Signatures.IsEnabled = true;
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }

    public class Module
    {
        public IntPtr startAddres { get; set; } = IntPtr.Zero;
        public IntPtr endAddres { get; set; } = IntPtr.Zero;
    }
    public class RIPEntry
    {
        public string Signature { get; set; }

        public IntPtr Address { get; set; } = IntPtr.Zero;
        public string AddressString => (Address.ToInt64().ToString("X").Length % 2 == 1) ? "0" + Address.ToInt64().ToString("X") : Address.ToInt64().ToString("X");
        public string AddressRelativeString { get; set; }
        public Int64 AddressValueInt64 { get; set; } = 0;

        public IntPtr TargetAddress { get; set; } = IntPtr.Zero;
        public string TargetAddressString => (TargetAddress.ToInt64().ToString("X").Length % 2 == 1) ? "0" + TargetAddress.ToInt64().ToString("X") : TargetAddress.ToInt64().ToString("X");
        public string TargetAddressRelativeString { get; set; }

    }
}
