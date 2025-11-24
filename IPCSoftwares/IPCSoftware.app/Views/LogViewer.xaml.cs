using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace IPCSoftware.App.Views
{
    public partial class LogViewer : UserControl
    {
        private readonly string _logDir;
        private FileSystemWatcher _watcher;
        private string? _openedFile;

        public LogViewer()
        {
            InitializeComponent();

            _logDir = @"C:\IPCLogs";   // later dynamic from config page
            LoadLogFiles();
            SetupWatcher();
        }

        private void LoadLogFiles()
        {
            lstFiles.Items.Clear();

            if (!Directory.Exists(_logDir))
                return;
            System.Diagnostics.Debug.WriteLine("Reading logs");
            foreach (var f in Directory.GetFiles(_logDir, "*.txt"))
                lstFiles.Items.Add(System.IO.Path.GetFileName(f));
        }

        private void lstFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstFiles.SelectedItem == null)
                return;

            _openedFile = Path.Combine(_logDir, lstFiles.SelectedItem.ToString());

            try
            {
                using (var fs = new FileStream(_openedFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fs))
                {
                    txtLogContent.Text = reader.ReadToEnd();
                }

                txtLogContent.ScrollToEnd();
            }
            catch (Exception ex)
            {
                txtLogContent.Text = $"[Error reading log file]\n{ex.Message}";
            }
        }

        private void SetupWatcher()
        {
            _watcher = new FileSystemWatcher(_logDir, "*.txt");
            _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName;
            _watcher.EnableRaisingEvents = true;

            _watcher.Changed += (s, e) =>
            {
                Dispatcher.Invoke(() =>
                {
                    LoadLogFiles();

                    if (!string.IsNullOrEmpty(_openedFile) &&
                        e.FullPath.Equals(_openedFile, StringComparison.OrdinalIgnoreCase))
                    {
                        try
                        {
                            using (var fs = new FileStream(_openedFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            using (var reader = new StreamReader(fs))
                            {
                                txtLogContent.Text = reader.ReadToEnd();
                                txtLogContent.ScrollToEnd();
                            }
                        }
                        catch { }
                    }
                });
            };
        }


    }

}
