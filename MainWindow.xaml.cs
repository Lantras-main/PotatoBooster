using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Win32;

namespace PotatoBooster
{
    public class ConfigData
    {
        public bool CleanTemp { get; set; } = true;
        public bool PowerPlan { get; set; } = true;
        public bool FlushRam { get; set; } = true;
        public bool Network { get; set; } = false;
        public bool CpuUnpark { get; set; } = false;
        public bool Telemetry { get; set; } = false;
        public bool Winsock { get; set; } = false;
        public bool EventLogs { get; set; } = false;
        public bool GpuCache { get; set; } = true;
        public bool AggressiveRam { get; set; } = false;
        public bool DisableVisualEffects { get; set; } = false;
        public string CustomGames { get; set; } = "mygame, customapp";
    }

    public partial class MainWindow : Window
    {
        // GitHub Update Checker Settings
        private const string CurrentVersion = "v1.0";
        private const string GitHubUser = "YOUR_GITHUB_USERNAME";
        private const string GitHubRepo = "YOUR_REPO_NAME";
        private const string DownloadUrl = "LINKTOGITHUBHAHAHA";

        [DllImport("psapi.dll")]
        private static extern int EmptyWorkingSet(IntPtr hwnd);

        [DllImport("user32.dll", EntryPoint = "SystemParametersInfo")]
        private static extern bool SystemParametersInfo(uint uiAction, uint uiParam, uint pvParam, uint fWinIni);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetProcessPriorityBoost(IntPtr hProcess, bool disablePriorityBoost);

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern int NtSetInformationProcess(IntPtr processHandle, int processInformationClass, ref int processInformation, int processInformationLength);

        private const string ConfigFileName = "config.json";

        private readonly HashSet<string> knownGameExecutables = new(StringComparer.OrdinalIgnoreCase)
        {
            "javaw", "minecraft", "cs2", "csgo", "valorant-win64-shipping", "fortniteclient-win64-shipping",
            "gta5", "rdr2", "cyberpunk2077", "overwatch", "leagueoflegends", "riotclientservices",
            "dota2", "apexpubg", "tslgame", "robloxplayerbeta", "genshinimpact", "starrail",
            "eldenring", "rocketleague", "rainbowsix", "destiny2", "fallguys_client_game",
            "deadbydaylight-win64-shipping", "fifa23", "fc24", "callofduty", "cod", "bf2042"
        };

        public MainWindow()
        {
            InitializeComponent();
            LoadConfig();
            Log("Potato Booster v1.0 ready.");
            LoadRunningGames();

            _ = CheckForUpdatesAsync();
        }

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("User-Agent", "PotatoBooster-UpdateChecker");

                string apiUrl = $"https://api.github.com/repos/{GitHubUser}/{GitHubRepo}/releases/latest";
                string response = await client.GetStringAsync(apiUrl);

                using JsonDocument doc = JsonDocument.Parse(response);
                if (doc.RootElement.TryGetProperty("tag_name", out JsonElement tagElement))
                {
                    string latestVersion = tagElement.GetString()?.Trim();

                    if (!string.IsNullOrEmpty(latestVersion) && !latestVersion.Equals(CurrentVersion, StringComparison.OrdinalIgnoreCase))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(
                                $"Your PotatoBooster is now outdated. Please install the latest version at {DownloadUrl}",
                                "Update Available",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information
                            );
                        });
                    }
                }
            }
            catch
            {
                // Silently swallow network/API errors to keep app startup smooth
            }
        }

        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                TextBlock logItem = new TextBlock
                {
                    Text = $"[{DateTime.Now:HH:mm:ss}] {message}",
                    Foreground = new SolidColorBrush(Color.FromRgb(0, 255, 102)),
                    FontFamily = new FontFamily("Consolas"),
                    FontSize = 11,
                    TextWrapping = TextWrapping.Wrap,
                    Opacity = 0
                };

                LogContainer.Children.Add(logItem);

                DoubleAnimation fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(200));
                logItem.BeginAnimation(OpacityProperty, fadeIn);

                LogScrollViewer.ScrollToEnd();
            });
        }

        private void LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigFileName))
                {
                    string json = File.ReadAllText(ConfigFileName);
                    var config = JsonSerializer.Deserialize<ConfigData>(json);

                    if (config != null)
                    {
                        ChkCleanTemp.IsChecked = config.CleanTemp;
                        ChkPowerPlan.IsChecked = config.PowerPlan;
                        ChkFlushRam.IsChecked = config.FlushRam;
                        ChkNetwork.IsChecked = config.Network;
                        ChkCpuUnpark.IsChecked = config.CpuUnpark;
                        ChkTelemetry.IsChecked = config.Telemetry;
                        ChkWinsock.IsChecked = config.Winsock;
                        ChkEventLogs.IsChecked = config.EventLogs;
                        ChkGpuCache.IsChecked = config.GpuCache;
                        ChkAggressiveRam.IsChecked = config.AggressiveRam;
                        ChkDisableVisualEffects.IsChecked = config.DisableVisualEffects;
                        TxtCustomGames.Text = config.CustomGames;
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"Config load error: {ex.Message}");
            }
        }

        private void SaveConfig()
        {
            try
            {
                var config = new ConfigData
                {
                    CleanTemp = ChkCleanTemp.IsChecked ?? true,
                    PowerPlan = ChkPowerPlan.IsChecked ?? true,
                    FlushRam = ChkFlushRam.IsChecked ?? true,
                    Network = ChkNetwork.IsChecked ?? false,
                    CpuUnpark = ChkCpuUnpark.IsChecked ?? false,
                    Telemetry = ChkTelemetry.IsChecked ?? false,
                    Winsock = ChkWinsock.IsChecked ?? false,
                    EventLogs = ChkEventLogs.IsChecked ?? false,
                    GpuCache = ChkGpuCache.IsChecked ?? true,
                    AggressiveRam = ChkAggressiveRam.IsChecked ?? false,
                    DisableVisualEffects = ChkDisableVisualEffects.IsChecked ?? false,
                    CustomGames = TxtCustomGames.Text
                };

                string json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigFileName, json);
                Log("Configuration saved.");
            }
            catch (Exception ex)
            {
                Log($"Config save error: {ex.Message}");
            }
        }

        private void BtnSaveConfig_Click(object sender, RoutedEventArgs e)
        {
            SaveConfig();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveConfig();
        }

        private void LoadRunningGames()
        {
            try
            {
                var customGames = TxtCustomGames.Text.Split(',')
                    .Select(s => s.Trim())
                    .Where(s => !string.IsNullOrEmpty(s));

                var filterList = new HashSet<string>(knownGameExecutables, StringComparer.OrdinalIgnoreCase);
                foreach (var game in customGames) filterList.Add(game);

                var runningGames = Process.GetProcesses()
                    .Select(p => p.ProcessName)
                    .Where(name => filterList.Contains(name))
                    .Distinct()
                    .OrderBy(name => name)
                    .ToList();

                CboProcesses.ItemsSource = runningGames;

                if (runningGames.Count > 0)
                {
                    CboProcesses.SelectedIndex = 0;
                    Log($"Found {runningGames.Count} active game process(es).");
                }
                else
                {
                    Log("No active games found.");
                }
            }
            catch (Exception ex)
            {
                Log($"Error: {ex.Message}");
            }
        }

        private void BtnRefreshProcesses_Click(object sender, RoutedEventArgs e)
        {
            LoadRunningGames();
        }

        private async void BtnBoostGame_Click(object sender, RoutedEventArgs e)
        {
            string selectedProcess = CboProcesses.SelectedItem as string;

            if (string.IsNullOrEmpty(selectedProcess))
            {
                Log("Select a game from the list.");
                return;
            }

            await Task.Run(() => PerformGameBoost(selectedProcess));
        }

        private async void BtnBoostAll_Click(object sender, RoutedEventArgs e)
        {
            Log("Starting optimizations...");

            bool cleanTemp = ChkCleanTemp.IsChecked ?? false;
            bool setPower = ChkPowerPlan.IsChecked ?? false;
            bool flushRam = ChkFlushRam.IsChecked ?? false;
            bool networkTweaks = ChkNetwork.IsChecked ?? false;
            bool unparkCpu = ChkCpuUnpark.IsChecked ?? false;
            bool disableTelemetry = ChkTelemetry.IsChecked ?? false;
            bool resetWinsock = ChkWinsock.IsChecked ?? false;
            bool clearLogs = ChkEventLogs.IsChecked ?? false;

            bool cleanGpu = ChkGpuCache.IsChecked ?? false;
            bool aggressiveRam = ChkAggressiveRam.IsChecked ?? false;
            bool disableFX = ChkDisableVisualEffects.IsChecked ?? false;

            await Task.Run(() =>
            {
                if (cleanTemp) PerformTempCleanup(cleanGpu);
                if (setPower) PerformPowerPlanOptimization();
                if (flushRam) PerformRamFlush(aggressiveRam);
                if (networkTweaks) PerformNetworkOptimization();
                if (unparkCpu) PerformCpuUnparking();
                if (disableTelemetry) PerformTelemetryDisable();
                if (resetWinsock) PerformWinsockReset();
                if (clearLogs) PerformEventLogCleanup();
                if (disableFX) PerformVisualFXOptimization();

                Dispatcher.Invoke(() =>
                {
                    string selectedProc = CboProcesses.SelectedItem as string;
                    if (!string.IsNullOrEmpty(selectedProc))
                    {
                        PerformGameBoost(selectedProc);
                    }
                });
            });

            Log("Optimization complete.");
        }

        private void PerformTempCleanup(bool includeGpu)
        {
            Log("Cleaning temp files...");

            List<string> tempFolders = new()
            {
                Path.GetTempPath(),
                @"C:\Windows\Temp",
                @"C:\Windows\Prefetch"
            };

            if (includeGpu)
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                tempFolders.Add(Path.Combine(localAppData, @"NVIDIA\DXCache"));
                tempFolders.Add(Path.Combine(localAppData, @"NVIDIA\GLCache"));
                tempFolders.Add(Path.Combine(localAppData, @"AMD\DxCache"));
                tempFolders.Add(Path.Combine(localAppData, @"D3DSCache"));
            }

            int filesDeleted = 0;

            foreach (string folder in tempFolders)
            {
                if (!Directory.Exists(folder)) continue;

                DirectoryInfo di = new DirectoryInfo(folder);

                foreach (FileInfo file in di.GetFiles())
                {
                    try { file.Delete(); filesDeleted++; }
                    catch { }
                }

                foreach (DirectoryInfo subDir in di.GetDirectories())
                {
                    try { subDir.Delete(true); }
                    catch { }
                }
            }

            Log($"Removed {filesDeleted} temp files.");
        }

        private void PerformPowerPlanOptimization()
        {
            Log("Enabling Ultimate High Performance power plan...");

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c powercfg -duplicatescheme e9a42b02-d5df-448d-aa00-03f14749eb61 && powercfg /s e9a42b02-d5df-448d-aa00-03f14749eb61",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };

                Process proc = Process.Start(psi);
                proc.WaitForExit();

                Log("Power plan set to High Performance.");
            }
            catch (Exception ex)
            {
                Log($"Power plan error: {ex.Message}");
            }
        }

        private void PerformRamFlush(bool aggressive)
        {
            Log("Flushing unused memory...");

            int processesOptimized = 0;
            Process[] processes = Process.GetProcesses();

            foreach (Process proc in processes)
            {
                try
                {
                    EmptyWorkingSet(proc.Handle);
                    processesOptimized++;
                }
                catch { }
            }

            if (aggressive)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }

            Log($"Flushed memory for {processesOptimized} background processes.");
        }

        private void PerformNetworkOptimization()
        {
            Log("Optimizing TCP throttling & flushing DNS...");

            try
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Multimedia\SystemProfile"))
                {
                    if (key != null)
                    {
                        key.SetValue("NetworkThrottlingIndex", 0xFFFFFFFF, RegistryValueKind.DWord);
                        key.SetValue("SystemResponsiveness", 0, RegistryValueKind.DWord);
                    }
                }

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "ipconfig",
                    Arguments = "/flushdns",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi)?.WaitForExit();

                Log("Network throttling disabled & DNS flushed.");
            }
            catch (Exception ex)
            {
                Log($"Network error: {ex.Message}");
            }
        }

        private void PerformCpuUnparking()
        {
            Log("Unparking CPU cores...");

            try
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Control\Power\PowerSettings\54533251-825e-4205-8411-57b80770073d\0cc5b647-c1df-4637-891a-dec35c3185b3"))
                {
                    if (key != null)
                    {
                        key.SetValue("Attributes", 0, RegistryValueKind.DWord);
                    }
                }

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "powercfg",
                    Arguments = "-setacvalueindex SCHEME_CURRENT SUB_PROCESSOR CPMINCORES 100",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi)?.WaitForExit();

                Log("CPU cores unparked.");
            }
            catch (Exception ex)
            {
                Log($"CPU error: {ex.Message}");
            }
        }

        private void PerformTelemetryDisable()
        {
            Log("Disabling tracking & telemetry services...");
            try
            {
                string[] services = { "DiagTrack", "dmwappushservice" };
                foreach (var svc in services)
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "sc",
                        Arguments = $"config {svc} start= disabled",
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    Process.Start(psi)?.WaitForExit();
                }
                Log("Telemetry services disabled.");
            }
            catch (Exception ex)
            {
                Log($"Telemetry error: {ex.Message}");
            }
        }

        private void PerformWinsockReset()
        {
            Log("Resetting Winsock and IP catalog...");
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = "winsock reset",
                    WindowStyle = ProcessWindowStyle.Hidden,
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi)?.WaitForExit();
                Log("Winsock catalog reset.");
            }
            catch (Exception ex)
            {
                Log($"Winsock error: {ex.Message}");
            }
        }

        private void PerformEventLogCleanup()
        {
            Log("Clearing Windows Event Logs...");
            try
            {
                string[] logs = { "Application", "System", "Security" };
                foreach (var logName in logs)
                {
                    ProcessStartInfo psi = new ProcessStartInfo
                    {
                        FileName = "wevtutil",
                        Arguments = $"cl {logName}",
                        WindowStyle = ProcessWindowStyle.Hidden,
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    Process.Start(psi)?.WaitForExit();
                }
                Log("Event logs cleared.");
            }
            catch (Exception ex)
            {
                Log($"Event log error: {ex.Message}");
            }
        }

        private void PerformVisualFXOptimization()
        {
            Log("Disabling Windows animations...");
            try
            {
                SystemParametersInfo(0x1003, 0, 0, 2);
                Log("Visual effects tuned.");
            }
            catch
            {
                Log("Could not adjust visual effects.");
            }
        }

        private void PerformGameBoost(string processName)
        {
            Log($"Targeting process '{processName}'...");

            Process[] processes = Process.GetProcessesByName(processName);

            if (processes.Length == 0)
            {
                Log($"Process '{processName}' not found.");
                return;
            }

            foreach (Process proc in processes)
            {
                try
                {
                    proc.PriorityClass = ProcessPriorityClass.High;
                    Log($"> Set Priority Class: HIGH (PID: {proc.Id})");

                    SetProcessPriorityBoost(proc.Handle, false);
                    Log("> Disabled Priority Decay");

                    int ioPriority = 3;
                    NtSetInformationProcess(proc.Handle, 33, ref ioPriority, sizeof(int));
                    Log("> Set Disk I/O Priority: HIGH");

                    try
                    {
                        using (RegistryKey key = Registry.LocalMachine.CreateSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces"))
                        {
                            if (key != null)
                            {
                                foreach (string subkeyName in key.GetSubKeyNames())
                                {
                                    using (RegistryKey subkey = key.OpenSubKey(subkeyName, true))
                                    {
                                        subkey?.SetValue("TcpAckFrequency", 1, RegistryValueKind.DWord);
                                        subkey?.SetValue("TCPNoDelay", 1, RegistryValueKind.DWord);
                                    }
                                }
                            }
                        }
                        Log("> Forced TCP Low Latency (Nagle Off)");
                    }
                    catch { }

                    int coreCount = Environment.ProcessorCount;
                    if (coreCount > 2)
                    {
                        long affinityMask = (1L << coreCount) - 1;
                        proc.ProcessorAffinity = (IntPtr)affinityMask;
                        Log($"> Assigned Affinity to all {coreCount} logical cores");
                    }

                    int trimmed = 0;
                    foreach (Process otherProc in Process.GetProcesses())
                    {
                        if (otherProc.Id != proc.Id && otherProc.ProcessName != "PotatoBooster")
                        {
                            try
                            {
                                EmptyWorkingSet(otherProc.Handle);
                                trimmed++;
                            }
                            catch { }
                        }
                    }
                    Log($"> Trimmed working set on {trimmed} other processes");
                    Log($"Boost applied to '{proc.ProcessName}'.");
                }
                catch (Exception ex)
                {
                    Log($"Boost error: {ex.Message}");
                }
            }
        }
    }
}