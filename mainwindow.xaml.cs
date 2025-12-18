using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace MahfCPUControlPanel
{
    public partial class MainWindow : Window
    {
        // P/Invoke declarations
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DeviceIoControl(
            IntPtr hDevice,
            uint dwIoControlCode,
            IntPtr lpInBuffer,
            uint nInBufferSize,
            IntPtr lpOutBuffer,
            uint nOutBufferSize,
            out uint lpBytesReturned,
            IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        // Constants
        private const string DEVICE_NAME = "\\\\.\\MahfCPU";
        private const uint GENERIC_READ = 0x80000000;
        private const uint GENERIC_WRITE = 0x40000000;
        private const uint OPEN_EXISTING = 3;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x80;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;

        // IOCTL Codes
        private const uint IOCTL_MAHF_GET_CPU_INFO = 0x88802000;
        private const uint IOCTL_MAHF_GET_PERFORMANCE_DATA = 0x88802004;
        private const uint IOCTL_MAHF_SET_PERFORMANCE_STATE = 0x88802008;
        private const uint IOCTL_MAHF_RESET_DRIVER = 0x8880200C;

        // Performance states
        private enum PerformanceState
        {
            PowerSave = 0,
            Balanced = 1,
            Performance = 2,
            Extreme = 3
        }

        // Data structures
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct CPU_INFO
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 13)]
            public string Vendor;
            
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 49)]
            public string Brand;
            
            public uint Architecture;
            public uint CoreCount;
            public uint ThreadCount;
            public uint BaseFrequency;
            public uint MaxFrequency;
            public uint CurrentFrequency;
            
            [MarshalAs(UnmanagedType.Bool)]
            public bool HyperThreading;
            
            [MarshalAs(UnmanagedType.Bool)]
            public bool TurboBoost;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct PERFORMANCE_DATA
        {
            public uint State;
            public uint Usage;
            public uint Temperature;
            public uint PowerConsumption;
            public uint CurrentFrequency;
            public uint Voltage;
        }

        // Member variables
        private IntPtr driverHandle = IntPtr.Zero;
        private DispatcherTimer updateTimer;
        private CPU_INFO cpuInfo;
        private PERFORMANCE_DATA perfData;
        private bool isConnected = false;

        public MainWindow()
        {
            InitializeComponent();
            InitializeApplication();
        }

        private void InitializeApplication()
        {
            // Set window properties
            Title = "Mahf Firmware CPU Control Panel v3.0.0";
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            
            // Initialize driver connection
            ConnectToDriver();
            
            // Initialize update timer
            updateTimer = new DispatcherTimer();
            updateTimer.Interval = TimeSpan.FromSeconds(1);
            updateTimer.Tick += UpdateTimer_Tick;
            
            // Load initial data
            if (isConnected)
            {
                LoadCPUInfo();
                updateTimer.Start();
            }
            
            // Set initial state
            UpdateUI();
        }

        private void ConnectToDriver()
        {
            try
            {
                // Open device
                driverHandle = CreateFile(
                    DEVICE_NAME,
                    GENERIC_READ | GENERIC_WRITE,
                    FILE_SHARE_READ | FILE_SHARE_WRITE,
                    IntPtr.Zero,
                    OPEN_EXISTING,
                    FILE_ATTRIBUTE_NORMAL,
                    IntPtr.Zero);
                
                if (driverHandle.ToInt64() == -1)
                {
                    int error = Marshal.GetLastWin32Error();
                    UpdateStatus($"Driver not connected (Error: {error})", false);
                    isConnected = false;
                }
                else
                {
                    UpdateStatus("Driver connected", true);
                    isConnected = true;
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Connection error: {ex.Message}", false);
                isConnected = false;
            }
        }

        private void LoadCPUInfo()
        {
            if (!isConnected || driverHandle == IntPtr.Zero)
                return;
            
            try
            {
                int size = Marshal.SizeOf(typeof(CPU_INFO));
                IntPtr buffer = Marshal.AllocHGlobal(size);
                
                uint bytesReturned;
                bool success = DeviceIoControl(
                    driverHandle,
                    IOCTL_MAHF_GET_CPU_INFO,
                    IntPtr.Zero,
                    0,
                    buffer,
                    (uint)size,
                    out bytesReturned,
                    IntPtr.Zero);
                
                if (success && bytesReturned > 0)
                {
                    cpuInfo = Marshal.PtrToStructure<CPU_INFO>(buffer);
                    
                    // Update UI on main thread
                    Dispatcher.Invoke(() =>
                    {
                        CpuNameLabel.Content = cpuInfo.Brand;
                        CpuCoresLabel.Content = $"{cpuInfo.CoreCount} Cores / {cpuInfo.ThreadCount} Threads";
                        BaseFreqLabel.Content = $"Base: {cpuInfo.BaseFrequency} MHz";
                        MaxFreqLabel.Content = $"Max: {cpuInfo.MaxFrequency} MHz";
                        
                        // Set vendor image
                        if (cpuInfo.Vendor.Contains("Intel"))
                            VendorImage.Source = new System.Windows.Media.Imaging.BitmapImage(
                                new Uri("pack://application:,,,/Resources/intel.png"));
                        else if (cpuInfo.Vendor.Contains("AMD"))
                            VendorImage.Source = new System.Windows.Media.Imaging.BitmapImage(
                                new Uri("pack://application:,,,/Resources/amd.png"));
                    });
                }
                
                Marshal.FreeHGlobal(buffer);
            }
            catch (Exception ex)
            {
                UpdateStatus($"CPU info error: {ex.Message}", false);
            }
        }

        private void UpdatePerformanceData()
        {
            if (!isConnected || driverHandle == IntPtr.Zero)
                return;
            
            try
            {
                int size = Marshal.SizeOf(typeof(PERFORMANCE_DATA));
                IntPtr buffer = Marshal.AllocHGlobal(size);
                
                uint bytesReturned;
                bool success = DeviceIoControl(
                    driverHandle,
                    IOCTL_MAHF_GET_PERFORMANCE_DATA,
                    IntPtr.Zero,
                    0,
                    buffer,
                    (uint)size,
                    out bytesReturned,
                    IntPtr.Zero);
                
                if (success && bytesReturned > 0)
                {
                    perfData = Marshal.PtrToStructure<PERFORMANCE_DATA>(buffer);
                    
                    // Update UI on main thread
                    Dispatcher.Invoke(() =>
                    {
                        UpdatePerformanceUI();
                    });
                }
                
                Marshal.FreeHGlobal(buffer);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Performance data error: {ex.Message}", false);
            }
        }

        private void SetPerformanceState(PerformanceState state)
        {
            if (!isConnected || driverHandle == IntPtr.Zero)
                return;
            
            try
            {
                int size = Marshal.SizeOf(typeof(PerformanceState));
                IntPtr buffer = Marshal.AllocHGlobal(size);
                
                Marshal.StructureToPtr(state, buffer, false);
                
                uint bytesReturned;
                bool success = DeviceIoControl(
                    driverHandle,
                    IOCTL_MAHF_SET_PERFORMANCE_STATE,
                    buffer,
                    (uint)size,
                    IntPtr.Zero,
                    0,
                    out bytesReturned,
                    IntPtr.Zero);
                
                Marshal.FreeHGlobal(buffer);
                
                if (success)
                {
                    string stateName = state switch
                    {
                        PerformanceState.PowerSave => "Power Save",
                        PerformanceState.Balanced => "Balanced",
                        PerformanceState.Performance => "Performance",
                        PerformanceState.Extreme => "Extreme",
                        _ => "Unknown"
                    };
                    
                    UpdateStatus($"Performance state set to: {stateName}", true);
                }
                else
                {
                    int error = Marshal.GetLastWin32Error();
                    UpdateStatus($"Failed to set state (Error: {error})", false);
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"State change error: {ex.Message}", false);
            }
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            UpdatePerformanceData();
        }

        private void UpdatePerformanceUI()
        {
            // Update labels and progress bars
            CpuUsageLabel.Content = $"{perfData.Usage}%";
            CpuUsageBar.Value = perfData.Usage;
            
            TemperatureLabel.Content = $"{perfData.Temperature}°C";
            TemperatureBar.Value = perfData.Temperature;
            
            PowerLabel.Content = $"{perfData.PowerConsumption}W";
            PowerBar.Value = Math.Min(perfData.PowerConsumption, 150);
            
            FrequencyLabel.Content = $"{perfData.CurrentFrequency} MHz";
            VoltageLabel.Content = $"{perfData.Voltage / 1000.0:F2}V";
            
            // Update state indicator
            switch (perfData.State)
            {
                case 0:
                    CurrentModeLabel.Content = "Power Save";
                    ModeIndicator.Fill = System.Windows.Media.Brushes.Blue;
                    break;
                case 1:
                    CurrentModeLabel.Content = "Balanced";
                    ModeIndicator.Fill = System.Windows.Media.Brushes.Green;
                    break;
                case 2:
                    CurrentModeLabel.Content = "Performance";
                    ModeIndicator.Fill = System.Windows.Media.Brushes.Orange;
                    break;
                case 3:
                    CurrentModeLabel.Content = "Extreme";
                    ModeIndicator.Fill = System.Windows.Media.Brushes.Red;
                    break;
            }
            
            // Update window title with CPU usage
            Title = $"Mahf CPU Control Panel - {perfData.Usage}% CPU - {perfData.Temperature}°C";
        }

        private void UpdateUI()
        {
            if (!isConnected)
            {
                StatusLabel.Content = "Status: Not Connected";
                StatusLabel.Foreground = System.Windows.Media.Brushes.Red;
                
                // Disable controls
                PowerSaveButton.IsEnabled = false;
                BalancedButton.IsEnabled = false;
                PerformanceButton.IsEnabled = false;
                ExtremeButton.IsEnabled = false;
            }
            else
            {
                StatusLabel.Content = "Status: Connected";
                StatusLabel.Foreground = System.Windows.Media.Brushes.Green;
                
                // Enable controls
                PowerSaveButton.IsEnabled = true;
                BalancedButton.IsEnabled = true;
                PerformanceButton.IsEnabled = true;
                ExtremeButton.IsEnabled = true;
            }
        }

        private void UpdateStatus(string message, bool isSuccess)
        {
            Dispatcher.Invoke(() =>
            {
                StatusMessageLabel.Content = message;
                StatusMessageLabel.Foreground = isSuccess ? 
                    System.Windows.Media.Brushes.Green : 
                    System.Windows.Media.Brushes.Red;
                
                // Auto-hide success messages after 3 seconds
                if (isSuccess)
                {
                    var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
                    timer.Tick += (s, e) =>
                    {
                        StatusMessageLabel.Content = "";
                        timer.Stop();
                    };
                    timer.Start();
                }
            });
        }

        // Event handlers
        private void PowerSaveButton_Click(object sender, RoutedEventArgs e)
        {
            SetPerformanceState(PerformanceState.PowerSave);
        }

        private void BalancedButton_Click(object sender, RoutedEventArgs e)
        {
            SetPerformanceState(PerformanceState.Balanced);
        }

        private void PerformanceButton_Click(object sender, RoutedEventArgs e)
        {
            SetPerformanceState(PerformanceState.Performance);
        }

        private void ExtremeButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Extreme mode may increase temperatures and power consumption.\n\n" +
                "Continue with Extreme mode?",
                "Extreme Mode Warning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.Yes)
            {
                SetPerformanceState(PerformanceState.Extreme);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isConnected)
            {
                ConnectToDriver();
            }
            
            LoadCPUInfo();
            UpdatePerformanceData();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow();
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Mahf Firmware CPU Driver\n" +
                "Version 3.0.0\n\n" +
                "Copyright © 2024 Mahf Corporation\n" +
                "All Rights Reserved.\n\n" +
                "Universal CPU Performance and Power Management Driver\n" +
                "Supports: Intel, AMD, ARM architectures",
                "About Mahf CPU Driver",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            // Stop timer
            updateTimer?.Stop();
            
            // Close driver handle
            if (driverHandle != IntPtr.Zero && driverHandle.ToInt64() != -1)
            {
                CloseHandle(driverHandle);
            }
            
            base.OnClosing(e);
        }
    }
}