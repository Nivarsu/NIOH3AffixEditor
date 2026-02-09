using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;
using Nioh3AffixEditor.Engine;
using Nioh3AffixEditor.Infrastructure;
using Nioh3AffixEditor.Models;
using Nioh3AffixEditor.Services;

namespace Nioh3AffixEditor.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private const string AffixIdTableUrl = "https://docs.qq.com/sheet/DUXVrR29oZ0hjY2VS?tab=BB08J2";
    private const string AffixUsageTips =
        "1，爱用度满会重置词条一次！可以等爱用度满了再重新修改。\n"
        + "2，直接在这装备的界面是无法成功修改的，必须点进去到能显示同类型装备的界面。例如更换装备、持有物品、奉献武具。\n"
        + "3，护符词条是固定的，如果想调整数值必须要复制武具的词条才能调整。";

    private readonly UpdateCheckService _updateCheckService;
    private readonly IAffixEngine _engine;
    private readonly ProcessDiscoveryService _processDiscoveryService;
    private readonly AffixIdTable _affixIdTable;
    private readonly UnderworldSkillTable _underworldSkillTable;

    private readonly DispatcherTimer _pollTimer;
    private ulong? _lastEquipmentBase;
    private bool _pollInFlight;

    private string _startButtonText = "启动修改";
    private string _simpleStatusText = "未启动（请先进入游戏并打开装备界面）";
    private string _equipmentBaseText = "-";
    private string _updateHintText = "";
    private string _updateAnnouncementText = "";
    private bool _isUpdateAnnouncementVisible;
    private bool _isSkillBypassEnabled;
#if TEST_BUILD
    private string _testLogText = "[Test] 日志已启用。";
    private bool _isTestLogVisible = true;
#endif

    public MainViewModel(UpdateCheckService updateCheckService, IAffixEngine engine, ProcessDiscoveryService processDiscoveryService)
    {
        _updateCheckService = updateCheckService ?? throw new ArgumentNullException(nameof(updateCheckService));
        _engine = engine ?? throw new ArgumentNullException(nameof(engine));
        _processDiscoveryService = processDiscoveryService ?? throw new ArgumentNullException(nameof(processDiscoveryService));

        _affixIdTable = AffixIdTable.LoadDefault();
        _underworldSkillTable = UnderworldSkillTable.LoadDefault();

        Slots = new ObservableCollection<AffixSlotViewModel>(
            Enumerable.Range(1, 7).Select(i => new AffixSlotViewModel(_affixIdTable, i)));
        Equipment = new EquipmentViewModel(_underworldSkillTable);

        StartStopCommand = new RelayCommand(() => _ = StartStopAsync());
        OpenAffixIdTableCommand = new RelayCommand(OpenAffixIdTableInBrowser);
        RefreshAffixesCommand = new RelayCommand(() => _ = RefreshAffixesAsync(), () => _engine.IsAttached);
        ApplyAffixesCommand = new RelayCommand(() => _ = ApplyAffixesAsync(), () => _engine.IsAttached);
        RefreshEquipmentCommand = new RelayCommand(() => _ = RefreshEquipmentAsync(), () => _engine.IsAttached);
        ApplyEquipmentCommand = new RelayCommand(() => _ = ApplyEquipmentAsync(), () => _engine.IsAttached);

        _pollTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(500),
        };
        _pollTimer.Tick += (_, _) => PollEngineState();
    }

    public ObservableCollection<AffixSlotViewModel> Slots { get; }
    public EquipmentViewModel Equipment { get; }
    public IReadOnlyList<string> AffixIdOptions => _affixIdTable.Options;
    public IReadOnlyList<string> UnderworldSkillOptions => _underworldSkillTable.Options;

    public RelayCommand StartStopCommand { get; }
    public RelayCommand OpenAffixIdTableCommand { get; }
    public RelayCommand RefreshAffixesCommand { get; }
    public RelayCommand ApplyAffixesCommand { get; }
    public RelayCommand RefreshEquipmentCommand { get; }
    public RelayCommand ApplyEquipmentCommand { get; }

    public string StartButtonText
    {
        get => _startButtonText;
        private set => SetProperty(ref _startButtonText, value);
    }

    public string SimpleStatusText
    {
        get => _simpleStatusText;
        private set => SetProperty(ref _simpleStatusText, value);
    }

    public string EquipmentBaseText
    {
        get => _equipmentBaseText;
        private set => SetProperty(ref _equipmentBaseText, value);
    }

    public string UpdateHintText
    {
        get => _updateHintText;
        private set => SetProperty(ref _updateHintText, value);
    }

    public string UpdateAnnouncementText
    {
        get => _updateAnnouncementText;
        private set => SetProperty(ref _updateAnnouncementText, value);
    }

    public bool IsUpdateAnnouncementVisible
    {
        get => _isUpdateAnnouncementVisible;
        private set => SetProperty(ref _isUpdateAnnouncementVisible, value);
    }

    public string AffixUsageTipsText => AffixUsageTips;

#if TEST_BUILD
    public bool IsTestLogVisible
    {
        get => _isTestLogVisible;
        private set => SetProperty(ref _isTestLogVisible, value);
    }

    public string TestLogText
    {
        get => _testLogText;
        private set => SetProperty(ref _testLogText, value);
    }
#endif

    public bool IsSkillBypassEnabled
    {
        get => _isSkillBypassEnabled;
        set
        {
            if (SetProperty(ref _isSkillBypassEnabled, value))
            {
                OnSkillBypassChanged(value);
            }
        }
    }

    private void OnSkillBypassChanged(bool enabled)
    {
        if (_engine is not NativeAffixEngine nativeEngine)
        {
            return;
        }

        if (!_engine.IsAttached)
        {
            // 如果未连接，重置状态
            _isSkillBypassEnabled = false;
            OnPropertyChanged(nameof(IsSkillBypassEnabled));
            MessageBox.Show("请先点击启动修改。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        bool success;
        if (enabled)
        {
            success = nativeEngine.EnableSkillBypass();
            if (!success)
            {
                _isSkillBypassEnabled = false;
                OnPropertyChanged(nameof(IsSkillBypassEnabled));
                var error = NativeBridge.GetLastErrorString();
                MessageBox.Show($"启用技能学习条件绕过失败：{error}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        else
        {
            success = nativeEngine.DisableSkillBypass();
            if (!success)
            {
                _isSkillBypassEnabled = true;
                OnPropertyChanged(nameof(IsSkillBypassEnabled));
                var error = NativeBridge.GetLastErrorString();
                MessageBox.Show($"禁用技能学习条件绕过失败：{error}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    public Task InitializeAsync()
    {
        var localVersion = Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);
        var localText = $"{localVersion.Major}.{localVersion.Minor}.{localVersion.Build}";
        UpdateHintText = $"当前版本：{localText}（正在检查更新…）";
        UpdateAnnouncementText = string.Empty;
        IsUpdateAnnouncementVisible = false;
#if TEST_BUILD
        AppendTestLog($"初始化：本地版本 {localText}");
#endif

        _ = Task.Run(async () =>
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(4));
                var res = await _updateCheckService.CheckAsync(cts.Token);
#if TEST_BUILD
                AppendTestLog($"更新检查：远端版本={res.RemoteVersionRaw ?? "(null)"}, 错误={res.Error ?? "无"}");
                if (!string.IsNullOrWhiteSpace(res.RemoteAnnouncementsText))
                {
                    AppendTestLog($"更新公告匹配：{TrimForLog(res.RemoteAnnouncementsText, 120)}");
                }
                else
                {
                    AppendTestLog("更新公告匹配：无对应公告");
                }
#endif
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (!string.IsNullOrWhiteSpace(res.RemoteVersionRaw))
                    {
                        UpdateHintText = $"当前版本：{localText} / 远端：{res.RemoteVersionRaw}";
                    }
                    else
                    {
                        UpdateHintText = $"当前版本：{localText}（远端版本未知）";
                    }

                    if (res.RemoteVersion is not null && res.RemoteVersion > res.LocalVersion)
                    {
                        var versionText = string.IsNullOrWhiteSpace(res.RemoteVersionRaw)
                            ? "未知版本"
                            : res.RemoteVersionRaw;
                        var announcement = string.IsNullOrWhiteSpace(res.RemoteAnnouncementsText)
                            ? "该版本暂无公告。"
                            : res.RemoteAnnouncementsText;
                        UpdateAnnouncementText = $"更新公告（{versionText}）：{announcement}";
                        IsUpdateAnnouncementVisible = true;
                    }
                    else
                    {
                        UpdateAnnouncementText = string.Empty;
                        IsUpdateAnnouncementVisible = false;
                    }
                });

                if (res.RemoteVersion is not null && res.RemoteVersion > res.LocalVersion)
                {
#if TEST_BUILD
                    AppendTestLog("检测到新版本，准备弹窗提示。");
#endif
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var result = MessageBox.Show(
                            BuildUpdateFoundMessage(res),
                            "发现更新",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Information);

                        if (result == MessageBoxResult.Yes)
                        {
                            OpenPostInBrowser();
                        }
                    });
                }
            }
            catch
            {
                // Silent.
#if TEST_BUILD
                AppendTestLog("初始化更新检查：发生异常（已静默处理）。");
#endif
            }
        });

        return Task.CompletedTask;
    }

    private async Task StartStopAsync()
    {
        if (_engine.IsAttached)
        {
            await StopAsync();
            return;
        }

        await StartAsync();
    }

    private async Task StartAsync()
    {
        try
        {
            MessageBox.Show(AffixUsageTipsText, "词条修改提示", MessageBoxButton.OK, MessageBoxImage.Information);
#if TEST_BUILD
            AppendTestLog("点击启动修改。开始附加流程。\n");
#endif

            SimpleStatusText = "正在启动…";

            var candidates = _processDiscoveryService.FindByProcessName("Nioh3.exe");
            var proc = candidates.FirstOrDefault();
            if (proc is null)
            {
                SimpleStatusText = "未找到 Nioh3.exe（请先启动游戏）";
                MessageBox.Show("未找到 Nioh3.exe。\n\n请先启动游戏，然后再点启动修改。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
#if TEST_BUILD
                AppendTestLog("启动失败：未找到 Nioh3.exe。");
#endif
                return;
            }

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
            {
                await _engine.AttachAsync(proc, cts.Token);
            }
#if TEST_BUILD
            AppendTestLog("附加进程成功。\n");
#endif

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5)))
            {
                await _engine.EnableCaptureAsync(cts.Token);
            }
#if TEST_BUILD
            AppendTestLog("内存捕获已启用。\n");
#endif

            _lastEquipmentBase = null;
            _pollTimer.Start();

            StartButtonText = "停止修改";
            SimpleStatusText = "已启动（切换装备时会自动刷新）";

            RefreshAffixesCommand.NotifyCanExecuteChanged();
            ApplyAffixesCommand.NotifyCanExecuteChanged();
            RefreshEquipmentCommand.NotifyCanExecuteChanged();
            ApplyEquipmentCommand.NotifyCanExecuteChanged();

            await RefreshAllAsync();
#if TEST_BUILD
            AppendTestLog("启动流程完成。\n");
#endif
        }
        catch (NotImplementedException)
        {
            SimpleStatusText = "未接入引擎（需要你实现内存逻辑）";
            MessageBox.Show(
                "你还没有接入内存引擎。\n\n请实现 IAffixEngine（Attach/EnableCapture/Read/Write），然后在 MainWindow.xaml.cs 里替换 AffixEngineNotImplemented。",
                "未接入引擎",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            _pollTimer.Stop();
            SimpleStatusText = $"启动失败：{ex.Message}";
            MessageBox.Show($"启动失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
#if TEST_BUILD
            AppendTestLog($"启动异常：{TrimForLog(ex.Message, 180)}");
#endif
        }
    }

    private async Task StopAsync()
    {
        try
        {
            _pollTimer.Stop();
            SimpleStatusText = "正在停止…";

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                await _engine.DisableCaptureAsync(cts.Token);
            }
            catch
            {
                // Best-effort.
            }

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
                await _engine.DetachAsync(cts.Token);
            }
            catch
            {
                // Best-effort.
            }
        }
        finally
        {
            _lastEquipmentBase = null;
            EquipmentBaseText = "-";
            StartButtonText = "启动修改";
            SimpleStatusText = "未启动（请先进入游戏并打开装备界面）";
            _isSkillBypassEnabled = false;
            OnPropertyChanged(nameof(IsSkillBypassEnabled));
            RefreshAffixesCommand.NotifyCanExecuteChanged();
            ApplyAffixesCommand.NotifyCanExecuteChanged();
            RefreshEquipmentCommand.NotifyCanExecuteChanged();
            ApplyEquipmentCommand.NotifyCanExecuteChanged();
#if TEST_BUILD
            AppendTestLog("已停止修改。\n");
#endif
        }
    }

    private async void PollEngineState()
    {
        if (_pollInFlight)
        {
            return;
        }

        if (!_engine.IsAttached || !_engine.IsCaptureEnabled)
        {
            return;
        }

        var baseAddr = _engine.EquipmentBaseAddress;
        var baseText = baseAddr is null ? "-" : $"0x{baseAddr:X}";
        if (EquipmentBaseText != baseText)
        {
            EquipmentBaseText = baseText;
        }

        if (baseAddr is null || baseAddr == _lastEquipmentBase)
        {
            return;
        }

        _lastEquipmentBase = baseAddr;

        _pollInFlight = true;
        try
        {
            await RefreshAllAsync();
        }
        finally
        {
            _pollInFlight = false;
        }
    }

    private async Task RefreshAllAsync()
    {
        await RefreshAffixesAsync();
        await RefreshEquipmentAsync();
    }

    private async Task RefreshAffixesAsync()
    {
        if (!_engine.IsAttached)
        {
            MessageBox.Show("请先点击启动修改。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
#if TEST_BUILD
            AppendTestLog("刷新词条失败：未附加。\n");
#endif
            return;
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var slots = await _engine.ReadAffixesAsync(cts.Token);

            foreach (var vm in Slots)
            {
                var data = slots.FirstOrDefault(s => s.SlotIndex == vm.SlotIndex);
                if (data is not null)
                {
                    vm.SetFromData(data);
                }
            }
#if TEST_BUILD
            AppendTestLog("刷新词条成功。\n");
#endif
        }
        catch (NotImplementedException)
        {
            MessageBox.Show("你还没有实现 ReadAffixesAsync。", "未接入引擎", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("尚未捕获到装备基址"))
            {
                MessageBox.Show(ex.Message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"刷新失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
#if TEST_BUILD
            AppendTestLog($"刷新词条异常：{TrimForLog(ex.Message, 180)}");
#endif
        }
    }

    private async Task ApplyAffixesAsync()
    {
        if (!_engine.IsAttached)
        {
            MessageBox.Show("请先点击启动修改。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
#if TEST_BUILD
            AppendTestLog("应用词条失败：未附加。\n");
#endif
            return;
        }

        var list = new List<AffixSlotData>();
        foreach (var s in Slots)
        {
            if (!s.TryToData(out var data, out var err))
            {
                MessageBox.Show(err ?? "输入不合法。", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            list.Add(data);
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _engine.WriteAffixesAsync(list, cts.Token);

            await RefreshAffixesAsync();
#if TEST_BUILD
            AppendTestLog($"应用词条成功：共 {list.Count} 条。\n");
#endif
        }
        catch (NotImplementedException)
        {
            MessageBox.Show("你还没有实现 WriteAffixesAsync。", "未接入引擎", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("尚未捕获到装备基址"))
            {
                MessageBox.Show(ex.Message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"写入失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
#if TEST_BUILD
            AppendTestLog($"应用词条异常：{TrimForLog(ex.Message, 180)}");
#endif
        }
    }

#if TEST_BUILD
    private void AppendTestLog(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        var time = DateTime.Now.ToString("HH:mm:ss");
        var line = $"[{time}] {message.Trim()}";

        const int maxChars = 5000;
        var merged = string.IsNullOrWhiteSpace(TestLogText)
            ? line
            : $"{line}\n{TestLogText}";

        if (merged.Length > maxChars)
        {
            merged = merged.Substring(0, maxChars);
        }

        TestLogText = merged;
    }

    private static string TrimForLog(string? text, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var trimmed = text.Trim();
        if (trimmed.Length <= maxLen)
        {
            return trimmed;
        }

        return trimmed.Substring(0, maxLen) + "...";
    }
#endif

    private async Task RefreshEquipmentAsync()
    {
        if (!_engine.IsAttached)
        {
            return;
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            var data = await _engine.ReadEquipmentAsync(cts.Token);
            Equipment.SetFromData(data);
        }
        catch (NotImplementedException)
        {
            // Silent - equipment reading not implemented
        }
        catch (Exception ex)
        {
            if (!ex.Message.Contains("尚未捕获到装备基址"))
            {
                MessageBox.Show($"读取装备属性失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private async Task ApplyEquipmentAsync()
    {
        if (!_engine.IsAttached)
        {
            MessageBox.Show("请先点击启动修改。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!Equipment.TryToData(out var data, out var err))
        {
            MessageBox.Show(err ?? "输入不合法。", "输入错误", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _engine.WriteEquipmentAsync(data, cts.Token);

            await RefreshEquipmentAsync();
        }
        catch (NotImplementedException)
        {
            MessageBox.Show("你还没有实现 WriteEquipmentAsync。", "未接入引擎", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("尚未捕获到装备基址"))
            {
                MessageBox.Show(ex.Message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"写入装备属性失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void OpenPostInBrowser()
    {
    }

    private void OpenAffixIdTableInBrowser()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = AffixIdTableUrl,
                UseShellExecute = true,
            });
        }
        catch
        {
            try
            {
                Clipboard.SetText(AffixIdTableUrl);
            }
            catch
            {
                // Ignore.
            }

            MessageBox.Show(
                $"无法打开浏览器，链接已复制到剪贴板：\n{AffixIdTableUrl}",
                "打开失败",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private static string BuildUpdateFoundMessage(UpdateCheckResult result)
    {
        var announcement = result.RemoteAnnouncementsText;

        if (!string.IsNullOrWhiteSpace(announcement))
        {
            return $"发现新版本：{result.RemoteVersionRaw}\n\n更新公告：\n{announcement}\n\n是否打开发布帖下载？";
        }

        return $"发现新版本：{result.RemoteVersionRaw}\n\n是否打开发布帖下载？";
    }
}
