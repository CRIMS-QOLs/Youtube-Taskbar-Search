using System;
using System.Drawing;
using System.Windows;
using Microsoft.Win32;
using Forms = System.Windows.Forms;

namespace YoutubeTaskbarSearch
{
    public partial class App : Application
    {
        private Forms.NotifyIcon _notifyIcon;
        private const string AppName = "YoutubeTaskbarSearch";
        private const string RegistryKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run";
        private MainWindow _mainWindow;
        private System.Threading.Mutex _instanceMutex;
        private System.Threading.Timer _cleanupTimer;
        private static readonly string _executablePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? string.Empty;

        private async void Application_Startup(object sender, StartupEventArgs e)
        {
            // Ensure single instance
            _instanceMutex = new System.Threading.Mutex(true, AppName + "InstanceMutex", out bool createdNew);
            if (!createdNew)
            {
                // App is already running, exit cleanly
                Current.Shutdown();
                return;
            }

            // Ensure the working directory is the application's executable directory and not System32
            if (!string.IsNullOrEmpty(_executablePath))
            {
                string exeDir = System.IO.Path.GetDirectoryName(_executablePath);
                if (!string.IsNullOrEmpty(exeDir))
                    Environment.CurrentDirectory = exeDir;
            }

            // We want the app to stay alive even if the window is closed
            Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Initialize tray icon
            _notifyIcon = new Forms.NotifyIcon();
            _notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? SystemIcons.Information;
            _notifyIcon.Visible = true;
            _notifyIcon.Text = "YouTube Taskbar Search";

            // Create context menu for exiting and toggling startup
            var contextMenu = new Forms.ContextMenuStrip();

            // Setup Startup Toggle button in tray menu
            var startupMenuItem = new Forms.ToolStripMenuItem("Run on Startup", null, OnToggleStartupClicked);
            startupMenuItem.Checked = IsStartupEnabled();
            contextMenu.Items.Add(startupMenuItem);

            // Setup Exit button
            contextMenu.Items.Add(new Forms.ToolStripSeparator());
            contextMenu.Items.Add("Exit", null, OnExitClicked);

            _notifyIcon.ContextMenuStrip = contextMenu;
            _notifyIcon.MouseClick += NotifyIcon_MouseClick;

            // Start the main window
            _mainWindow = new MainWindow();
            
            // Setup periodic background memory trimming (e.g. every 10 minutes)
            _cleanupTimer = new System.Threading.Timer(_ => YoutubeTaskbarSearch.MainWindow.TrimMemory(), null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(10));
            
            // Handle display on startup
            bool isStartup = false;
            foreach (var arg in e.Args)
            {
                if (arg.Equals("--startup", StringComparison.OrdinalIgnoreCase))
                    isStartup = true;
            }

            if (isStartup)
            {
                // Delay showing to ensure Explorer and other apps have loaded, making it stay on top of the desktop.
                await System.Threading.Tasks.Task.Delay(2000);
                _mainWindow.Show();
                _mainWindow.Activate();
                _mainWindow.Focus();
            }
            else
            {
                // Show immediately for manual start
                _mainWindow.Show();
                _mainWindow.Activate();
                _mainWindow.Focus();
            }
        }

        private void NotifyIcon_MouseClick(object sender, Forms.MouseEventArgs e)
        {
            if (e.Button == Forms.MouseButtons.Left)
            {
                if (_mainWindow == null)
                    _mainWindow = new MainWindow();

                if (_mainWindow.IsVisible)
                {
                    _mainWindow.Hide();
                }
                else
                {
                    _mainWindow.Show();
                    _mainWindow.Activate();
                    _mainWindow.Focus();
                }
            }
        }

        private bool IsStartupEnabled()
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false))
            {
                if (key != null)
                {
                    string currentValue = key.GetValue(AppName) as string;
                    string expectedPath = $"\"{_executablePath}\" --startup";
                    return currentValue != null && currentValue.Equals(expectedPath, StringComparison.OrdinalIgnoreCase);
                }
                return false;
            }
        }

        private void OnToggleStartupClicked(object sender, EventArgs e)
        {
            if (sender is Forms.ToolStripMenuItem menuItem)
            {
                try
                {
                    using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true))
                    {
                        if (menuItem.Checked)
                        {
                            // Was checked, remove from startup
                            key?.DeleteValue(AppName, false);
                            menuItem.Checked = false;
                        }
                        else
                        {
                            // Was unchecked, add to startup
                            key?.SetValue(AppName, $"\"{_executablePath}\" --startup");
                            menuItem.Checked = true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Could not modify startup settings: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OnExitClicked(object sender, EventArgs e)
        {
             Current.Shutdown();
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            if (_instanceMutex != null)
            {
                _instanceMutex.ReleaseMutex();
                _instanceMutex.Dispose();
            }
            if (_cleanupTimer != null)
            {
                _cleanupTimer.Dispose();
            }
        }
    }
}
