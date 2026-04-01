using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace YoutubeTaskbarSearch
{
    public enum SearchEngine
    {
        Youtube,
        ChatGPT
    }

    public partial class MainWindow : Window
    {
        private SearchEngine _currentEngine = SearchEngine.Youtube;
        private readonly System.Text.StringBuilder _sharedBuffer = new System.Text.StringBuilder(256);
        private string _targetWindowTitle;
        private IntPtr _foundWindow;
        private EnumWindowsProc _enumWindowsProcDelegate;

        public MainWindow()
        {
            InitializeComponent();
            this.IsVisibleChanged += MainWindow_IsVisibleChanged;
            this.Activated += MainWindow_Activated;
            this.Deactivated += MainWindow_Deactivated;
            _enumWindowsProcDelegate = new EnumWindowsProc(EnumWindowsCallback);
        }

        private void MainWindow_Activated(object sender, EventArgs e)
        {
            try { Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal; } catch { }
        }

        private void MainWindow_Deactivated(object sender, EventArgs e)
        {
            try { Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Idle; } catch { }
            TrimMemory();
        }

        private void MainWindow_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!(bool)e.NewValue)
            {
                TrimMemory();
            }
        }

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetProcessWorkingSetSize(IntPtr process, UIntPtr minimumWorkingSetSize, UIntPtr maximumWorkingSetSize);

        internal static void TrimMemory()
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, (UIntPtr)unchecked((ulong)-1), (UIntPtr)unchecked((ulong)-1));
                }
            }
            catch { }
        }

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);
        
        private readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        private const int WM_WINDOWPOSCHANGING = 0x0046;
        
        // SWP_NOACTIVATE = 0x0010, SWP_NOOWNERZORDER = 0x0200
        private const uint SWP_PINNING_FLAGS = 0x0010 | 0x0200;

        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPOS
        {
            public IntPtr hwnd;
            public IntPtr hwndInsertAfter;
            public int x;
            public int y;
            public int cx;
            public int cy;
            public uint flags;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            var helper = new WindowInteropHelper(this);
            HwndSource source = HwndSource.FromHwnd(helper.Handle);
            if (source != null)
            {
                source.AddHook(WndProc);
            }

            SetWindowPos(helper.Handle, HWND_BOTTOM, 0, 0, 0, 0, 0x0001 | 0x0002 | SWP_PINNING_FLAGS);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_WINDOWPOSCHANGING)
            {
                WINDOWPOS wp = (WINDOWPOS)Marshal.PtrToStructure(lParam, typeof(WINDOWPOS));
                wp.hwndInsertAfter = HWND_BOTTOM;
                wp.flags |= SWP_PINNING_FLAGS;
                Marshal.StructureToPtr(wp, lParam, false);
            }
            return IntPtr.Zero;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var workArea = SystemParameters.WorkArea;
            this.Left = workArea.Left + 15; 
            this.Top = workArea.Bottom - this.Height - 15;
            SearchBox.Focus();

            // Trim initial startup memory overhead after UI rendering
            await System.Threading.Tasks.Task.Delay(1500);
            TrimMemory();
        }

        private void MainBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void LeftIcon_Click(object sender, RoutedEventArgs e)
        {
            // The left icon is always the active one. Focus the search box.
            SearchBox.Focus();
        }

        private void RightIcon_Click(object sender, RoutedEventArgs e)
        {
            // The right icon is always the inactive one. Clicking it switches engines.
            SwitchEngine();
        }

        private void SwitchEngine()
        {
            var slideToLeft = new DoubleAnimation
            {
                To = 0,
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            var slideToRight = new DoubleAnimation
            {
                To = 314, // 370px (Grid) - 15px (left margin) - 15px (right margin) - 26px (icon width) = 314px
                Duration = TimeSpan.FromMilliseconds(400),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            var fadeOut = new DoubleAnimation
            {
                To = 0.4,
                Duration = TimeSpan.FromMilliseconds(400)
            };

            var fadeIn = new DoubleAnimation
            {
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(400)
            };

            if (_currentEngine == SearchEngine.Youtube)
            {
                // Switch to ChatGPT. ChatGPT becomes Active (Left), Youtube becomes Inactive (Right)
                IconYoutube.RenderTransform.BeginAnimation(TranslateTransform.XProperty, slideToRight);
                IconYoutube.BeginAnimation(UIElement.OpacityProperty, fadeOut);

                IconChatGPT.RenderTransform.BeginAnimation(TranslateTransform.XProperty, slideToLeft);
                IconChatGPT.BeginAnimation(UIElement.OpacityProperty, fadeIn);

                _currentEngine = SearchEngine.ChatGPT;
                SearchBox.Tag = "Type to search Chatgpt...";
            }
            else
            {
                // Switch to Youtube. Youtube becomes Active (Left), ChatGPT becomes Inactive (Right)
                IconChatGPT.RenderTransform.BeginAnimation(TranslateTransform.XProperty, slideToRight);
                IconChatGPT.BeginAnimation(UIElement.OpacityProperty, fadeOut);

                IconYoutube.RenderTransform.BeginAnimation(TranslateTransform.XProperty, slideToLeft);
                IconYoutube.BeginAnimation(UIElement.OpacityProperty, fadeIn);

                _currentEngine = SearchEngine.Youtube;
                SearchBox.Tag = "Type to search youtube...";
            }

            SearchBox.Focus();
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                string query = SearchBox.Text.Trim();
                if (!string.IsNullOrEmpty(query))
                {
                    PerformSearch(query);
                    SearchBox.Text = string.Empty; 
                }
            }
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder text, int count);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        static extern bool BringWindowToTop(IntPtr hWnd);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private IntPtr FindWindowByTitle(string titleSubstring)
        {
            _targetWindowTitle = titleSubstring;
            _foundWindow = IntPtr.Zero;
            EnumWindows(_enumWindowsProcDelegate, IntPtr.Zero);
            return _foundWindow;
        }

        private bool EnumWindowsCallback(IntPtr hWnd, IntPtr lParam)
        {
            if (GetWindowText(hWnd, _sharedBuffer, _sharedBuffer.Capacity) > 0)
            {
                string title = _sharedBuffer.ToString();
                if (title.IndexOf(_targetWindowTitle, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _foundWindow = hWnd;
                    return false; 
                }
            }
            return true;
        }

        private void ForceForegroundWindow(IntPtr hWnd)
        {
            uint foregroundThreadId = GetWindowThreadProcessId(GetForegroundWindow(), out _);
            uint appThreadId = GetCurrentThreadId();

            if (foregroundThreadId != appThreadId)
            {
                AttachThreadInput(foregroundThreadId, appThreadId, true);
                BringWindowToTop(hWnd);
                ShowWindow(hWnd, 9);
                SetForegroundWindow(hWnd);
                AttachThreadInput(foregroundThreadId, appThreadId, false);
            }
            else
            {
                BringWindowToTop(hWnd);
                ShowWindow(hWnd, 9);
                SetForegroundWindow(hWnd);
            }
        }

        private async void PerformSearch(string query)
        {
            try
            {
                string encodedQuery = WebUtility.UrlEncode(query);
                string url = _currentEngine == SearchEngine.Youtube 
                    ? $"https://www.youtube.com/results?search_query={encodedQuery}"
                    : $"https://chatgpt.com/?q={encodedQuery}";

                var startInfo = new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                };
                
                Process.Start(startInfo);

                if (_currentEngine == SearchEngine.ChatGPT)
                {
                    bool autoSubmitted = false;
                    for (int i = 0; i < 30; i++) // Try up to 15 seconds to detect the window anywhere in the OS
                    {
                        await System.Threading.Tasks.Task.Delay(500);
                        IntPtr chatGptWindow = FindWindowByTitle("ChatGPT");
                        
                        if (chatGptWindow != IntPtr.Zero)
                        {
                            // Wait roughly 2 seconds to let the site build its interface
                            await System.Threading.Tasks.Task.Delay(2000);
                            
                            // Send Enter up to 6 times over the next 9 seconds
                            for(int presses = 0; presses < 6; presses++)
                            {
                                IntPtr currentGptWindow = FindWindowByTitle("ChatGPT");
                                if (currentGptWindow != IntPtr.Zero)
                                {
                                    // Force window back to foreground using AttachThreadInput bypass
                                    ForceForegroundWindow(currentGptWindow);
                                    await System.Threading.Tasks.Task.Delay(300); // give OS time to focus
                                    
                                    // Verify that we successfully stole focus
                                    IntPtr newForeground = GetForegroundWindow();
                                    if (GetWindowText(newForeground, _sharedBuffer, _sharedBuffer.Capacity) > 0)
                                    {
                                        if (_sharedBuffer.ToString().IndexOf("ChatGPT", StringComparison.OrdinalIgnoreCase) >= 0)
                                        {
                                            System.Windows.Forms.SendKeys.SendWait("{ENTER}");
                                        }
                                    }
                                }
                                
                                await System.Threading.Tasks.Task.Delay(1500);
                            }
                            
                            autoSubmitted = true;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not open search. Error: {ex.Message}", "Search Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
